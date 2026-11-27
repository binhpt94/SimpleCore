using System.Linq;
using Game.Core;
using Game.Hero;
using Game.Run;
using Game.Upgrade;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;

namespace Game.HudUpgrade.Tests
{
    public sealed class HudUpgradeSessionTests
    {
        private const string InstallerPath = "Assets/Features/HudUpgrade/FeatureAssets/Public/HudUpgradeInstaller.asset";
        private const string ConfigPath = "Assets/Features/HudUpgrade/FeatureAssets/Private/HudUpgradeConfig.asset";
        private const string BarPrefabPath = "Assets/Features/HudUpgrade/FeatureAssets/Private/UpgradeBar.prefab";

        [Test]
        public void Bar_opens_when_session_runs_and_is_removed_when_session_ends()
        {
            var wallet = new FakeWallet();
            var shop = new FakeShop(wallet);
            shop.SetTeam("knight", "archer", "mage");
            var views = new FakeViewFactory();
            var participant = new HudUpgradeParticipant(shop, new FakeOrders(shop), new FakeRoster(), new FakeRun(), new FakeDownState(), views);

            participant.Run();
            Assert.AreEqual(1, views.Created);
            Assert.AreEqual(6, views.View.Pictures.Count);

            participant.DisposeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(1, views.Destroyed);
            wallet.Coins = 1000;
            shop.Changed();
            Assert.IsFalse(views.View.Looks[5].Lit, "bar must stop listening once the session ended");
        }

        [Test]
        public void Session_counts_the_bar_as_held_while_it_runs_and_nothing_once_it_ended()
        {
            var shop = new FakeShop(new FakeWallet());
            shop.SetTeam("knight", "axeman", "archer");
            var views = new GameObjectViewFactory();
            var participant = new HudUpgradeParticipant(shop, new FakeOrders(shop), new FakeRoster(), new FakeRun(), new FakeDownState(), views);
            var usage = (ISessionParticipantUsage)participant;
            Assert.IsFalse(usage.OpensViewsOnRun,
                "the game opens on the start screen; the bar only shows once a battle is built, so the session must not require a view right after Run");
            Assert.AreEqual(0, usage.HeldViews);
            Assert.AreEqual(0, usage.RunningJobs);

            participant.Run();
            Assert.AreEqual(1, usage.HeldViews, "the bar is the one view");
            Assert.AreEqual(0, usage.RunningJobs, "the bar runs no timer or loop of its own");

            participant.DisposeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(0, usage.HeldViews);
            Assert.AreEqual(0, usage.RunningJobs);
            Assert.IsTrue(views.Last == null, "the bar's GameObject is gone");
        }

        [Test]
        public void Bar_shows_when_run_starts_a_frame_after_the_session_runs()
        {
            // Session reaches Running with no run yet: the shop is empty, the step is Home.
            var wallet = new FakeWallet { Coins = 50 };
            var shop = new FakeShop(wallet);
            var run = new FakeRun();
            var views = new FakeViewFactory();
            var participant = new HudUpgradeParticipant(shop, new FakeOrders(shop), new FakeRoster(), run, new FakeDownState(), views);
            participant.Run();
            Assert.IsFalse(views.View.Visible);

            // Next frame: RunStarted (shop builds its list), BattleStarted, then Step = Playing.
            shop.SetTeam("knight", "archer", "mage");
            run.StartBattle();
            run.GoTo(RunStep.Playing);

            Assert.AreEqual(6, views.View.Pictures.Count);
            Assert.IsTrue(views.View.Visible);
            participant.DisposeAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void Bar_shows_when_the_step_turns_playing_before_the_items_arrive()
        {
            var wallet = new FakeWallet { Coins = 50 };
            var shop = new FakeShop(wallet);
            var run = new FakeRun();
            var views = new FakeViewFactory();
            var participant = new HudUpgradeParticipant(shop, new FakeOrders(shop), new FakeRoster(), run, new FakeDownState(), views);
            participant.Run();

            run.GoTo(RunStep.Playing);
            Assert.IsFalse(views.View.Visible, "no buttons yet");
            shop.SetTeam("knight", "archer", "mage");

            Assert.IsTrue(views.View.Visible);
            participant.DisposeAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void Bar_shows_when_items_arrive_after_step_changed_but_before_its_event()
        {
            var wallet = new FakeWallet { Coins = 50 };
            var shop = new FakeShop(wallet);
            var run = new FakeRun();
            var views = new FakeViewFactory();
            var participant = new HudUpgradeParticipant(shop, new FakeOrders(shop), new FakeRoster(), run, new FakeDownState(), views);
            participant.Run();

            run.SetStepQuietly(RunStep.Playing);
            shop.SetTeam("knight", "archer", "mage");

            Assert.IsTrue(views.View.Visible);
            participant.DisposeAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void Mixed_buttons_cheat_lights_five_buttons_and_dims_the_skill_button()
        {
            var wallet = new FakeWallet { Coins = 50 };
            var shop = new FakeShop(wallet);
            shop.SetTeam("knight", "axeman", "archer");
            // Prices of the state's levels from the design's price table; lower levels cost little, the cheat tops up.
            var levelsOfState = new[] { 3, 5, 1, 2, 4, 3 };
            var pricesOfState = new[] { 55, 100, 30, 56, 110, 135 };
            shop.PriceOf = (index, level) => level == levelsOfState[index] ? pricesOfState[index] : 10;
            var run = new FakeRun();
            var views = new FakeViewFactory();
            var cheat = new HudUpgradeParticipant.CheatSetup(HudUpgradeCheatState.MixedButtons, 120,
                levelsOfState, wallet, wallet, wallet, new[] { "knight", "axeman", "archer" });
            var participant = new HudUpgradeParticipant(shop, new FakeOrders(shop), new FakeRoster(), run, new FakeDownState(), views, cheat);

            participant.Run();
            run.StartBattle();

            Assert.AreEqual(120, wallet.Coins);
            var levels = new int[6];
            for (var i = 0; i < 6; i++) levels[i] = shop.Get(i).Level;
            CollectionAssert.AreEqual(new[] { 3, 5, 1, 2, 4, 3 }, levels);
            CollectionAssert.AreEqual(new[] { "55", "100", "30", "56", "110", "135" },
                new[] { 0, 1, 2, 3, 4, 5 }.Select(i => views.View.Looks[i].PriceText).ToArray());
            CollectionAssert.AreEqual(new[] { true, true, true, true, true, false },
                new[] { 0, 1, 2, 3, 4, 5 }.Select(i => views.View.Looks[i].Lit).ToArray(),
                "five buttons lit, the castle skill button (135 > 120) dim");

            participant.DisposeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(0, run.BattleStartedListeners);
        }

        [Test]
        public void Mixed_buttons_cheat_accepts_the_knight_axeman_archer_team()
        {
            var wallet = new FakeWallet();
            var shop = new FakeShop(wallet);
            shop.SetTeam("knight", "axeman", "archer");
            var cheat = new UpgradeBarCheat(shop, wallet, wallet, wallet);

            Assert.IsNull(cheat.FindTeamMismatch(new[] { "knight", "axeman", "archer" }));
        }

        [Test]
        public void Config_asset_holds_the_mixed_buttons_state_of_the_design()
        {
            var config = AssetDatabase.LoadAssetAtPath<HudUpgradeConfig>(ConfigPath);
            Assert.IsNotNull(config, "missing " + ConfigPath);

            Assert.AreEqual(120, config.MixedButtonsCoins);
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, config.MixedButtonsTeam);
            CollectionAssert.AreEqual(new[] { 3, 5, 1, 2, 4, 3 }, config.MixedButtonsLevels);
        }

        [Test]
        public void Mixed_buttons_cheat_names_both_teams_when_another_team_was_picked()
        {
            var wallet = new FakeWallet();
            var shop = new FakeShop(wallet);
            shop.SetTeam("knight", "archer", "mage");
            var cheat = new UpgradeBarCheat(shop, wallet, wallet, wallet);

            var message = cheat.FindTeamMismatch(new[] { "knight", "axeman", "archer" });

            StringAssert.Contains("knight, archer, mage", message);
            StringAssert.Contains("knight, axeman, archer", message);
        }

        [Test]
        public void Cheat_is_off_by_default_and_changes_nothing()
        {
            var wallet = new FakeWallet { Coins = 50 };
            var shop = new FakeShop(wallet);
            shop.SetTeam("knight", "archer", "mage");
            var run = new FakeRun();
            var participant = new HudUpgradeParticipant(shop, new FakeOrders(shop), new FakeRoster(), run, new FakeDownState(), new FakeViewFactory());

            participant.Run();
            run.StartBattle();

            Assert.AreEqual(50, wallet.Coins);
            Assert.AreEqual(0, shop.BuyCalls);
        }

        [Test]
        public void Bar_prefab_has_hero_portraits_castle_icons_coin_wider_group_gap_and_short_bounce()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(BarPrefabPath);
            Assert.IsNotNull(prefab, "missing " + BarPrefabPath);
            UnityEngine.Component view = null;
            foreach (var c in prefab.GetComponentsInChildren<UnityEngine.Component>(true))
                if (c != null && c.GetType().Name == "UpgradeBarView") view = c;
            Assert.IsNotNull(view, "UpgradeBar prefab has no UpgradeBarView");
            var so = new SerializedObject(view);

            var portraits = so.FindProperty("heroPortraits");
            foreach (var code in new[] { "knight", "axeman", "archer" })
            {
                var found = false;
                for (var i = 0; i < portraits.arraySize; i++)
                {
                    var p = portraits.GetArrayElementAtIndex(i);
                    if (p.FindPropertyRelative("heroCode").stringValue != code) continue;
                    found = p.FindPropertyRelative("sprite").objectReferenceValue != null;
                }

                Assert.IsTrue(found, "no portrait for " + code);
            }

            foreach (var icon in new[] { "castleHealthIcon", "castleAttackIcon", "castleSkillIcon", "coinIcon" })
                Assert.IsNotNull(so.FindProperty(icon).objectReferenceValue, "missing " + icon);

            Assert.Greater(so.FindProperty("groupGap").floatValue, so.FindProperty("buttonGap").floatValue);

            // On the 1920x1080 reference screen the last button must end left of the skill cluster (x 1420).
            var settings = new BarLayout.Settings(so.FindProperty("barLeft").floatValue,
                so.FindProperty("buttonSize").vector2Value.x, so.FindProperty("buttonGap").floatValue,
                so.FindProperty("groupGap").floatValue,
                1920 - so.FindProperty("skillsLeft").floatValue + so.FindProperty("skillsGap").floatValue);
            Assert.AreEqual(1420f, so.FindProperty("skillsLeft").floatValue);
            Assert.AreEqual(40f, so.FindProperty("skillsGap").floatValue);
            var layout = BarLayout.Compute(new[] { 0, 0, 0, 1, 1, 1 }, 1920, settings);
            Assert.LessOrEqual(layout.RightEdge(settings), 1420f);
            Assert.AreEqual(0.2f, so.FindProperty("bounceSeconds").floatValue, 0.0001f);
        }

        [Test]
        public void Installer_asset_joins_the_session_with_the_bar()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            Assert.IsNotNull(installer, "missing " + InstallerPath);

            var session = new FakeSession();
            var cheats = new FakeCheatSelection();

            installer.Install(FullRegistry(session, cheats));

            Assert.AreEqual(1, session.Joined.Count);
            Assert.AreEqual(HudUpgradeCheatState.None, ((HudUpgradeParticipant)session.Joined[0]).CheatState,
                "the shipped config keeps the cheat off");
            Assert.IsEmpty(cheats.Reported);
        }

        [Test]
        public void Installer_fails_at_once_naming_the_missing_shop()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var registry = new FakeRegistry();
            registry.Register<ICheatSelection>(new FakeCheatSelection());
            registry.Register<ISessionScene>(new FakeSession());
            var error = Assert.Throws<System.InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains("IUpgradeShop", error.Message);
        }

        [Test]
        public void Installer_fails_at_once_naming_the_missing_buy_orders()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var registry = new FakeRegistry();
            registry.Register<ICheatSelection>(new FakeCheatSelection());
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IUpgradeShop>(new FakeShop(new FakeWallet()));
            var error = Assert.Throws<System.InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains("IUpgradeOrders", error.Message);
        }
    
        // --- Cheat list (ICheatStateSource) and the pick (ICheatSelection) ---

        private static FakeRegistry FullRegistry(FakeSession session, FakeCheatSelection cheats, FakeCheatStart start = null)
        {
            var registry = new FakeRegistry();
            registry.Register<IRunCheatStart>(start ?? new FakeCheatStart());
            if (cheats != null) registry.Register<ICheatSelection>(cheats);
            var wallet = new FakeWallet();
            registry.Register<ISessionScene>(session);
            var shop = new FakeShop(wallet);
            registry.Register<IUpgradeShop>(shop);
            registry.Register<IUpgradeOrders>(new FakeOrders(shop));
            registry.Register<IHeroRoster>(new FakeRoster());
            registry.Register<IRunFlow>(new FakeRun());
            registry.Register<IHeroDownState>(new FakeDownState());
            registry.Register<IWalletBalance>(wallet);
            registry.Register<IWalletIncome>(wallet);
            registry.Register<IWalletService>(wallet);
            return registry;
        }

        /// <summary>An installer like the shipped one, but its config has <paramref name="assetState"/> chosen. Caller destroys both.</summary>
        private static HudUpgradeInstaller InstallerWithCheatOnAsset(HudUpgradeCheatState assetState, out HudUpgradeConfig config)
        {
            var shipped = AssetDatabase.LoadAssetAtPath<HudUpgradeInstaller>(InstallerPath);
            Assert.IsNotNull(shipped, "missing " + InstallerPath);
            config = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<HudUpgradeConfig>(ConfigPath));
            var configSo = new SerializedObject(config);
            configSo.FindProperty("cheatState").enumValueIndex = (int)assetState;
            configSo.ApplyModifiedPropertiesWithoutUndo();

            var installer = UnityEngine.Object.Instantiate(shipped);
            var installerSo = new SerializedObject(installer);
            installerSo.FindProperty("config").objectReferenceValue = config;
            installerSo.ApplyModifiedPropertiesWithoutUndo();
            return installer;
        }

        private static HudUpgradeCheatState BuiltState(FakeSession session) =>
            ((HudUpgradeParticipant)session.Joined.Single()).CheatState;

        [Test]
        public void Installer_fails_at_the_start_naming_the_missing_cheat_selection()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var error = Assert.Throws<System.InvalidOperationException>(
                () => installer.Install(FullRegistry(new FakeSession(), null)));
            StringAssert.Contains(nameof(ICheatSelection), error.Message);
        }

        [Test]
        public void Shipped_installer_lists_the_bar_and_its_states_in_the_order_of_the_cheat_node()
        {
            var source = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath) as ICheatStateSource;
            Assert.IsNotNull(source, "the installer asset must be a cheat state source");

            Assert.AreEqual("Thanh nâng cấp", source.CheatFeatureTitle);
            CollectionAssert.AreEqual(new[] { "HudUpgrade/MixedButtons" }, source.CheatStates.Select(s => s.Key).ToArray());
            CollectionAssert.AreEqual(new[] { "Giữa trận, đủ tiền mua năm mục, thiếu tiền cho một mục" },
                source.CheatStates.Select(s => s.Title).ToArray());
        }

        [Test]
        public void Pick_of_own_state_builds_it_and_reports_it_running()
        {
            var installer = InstallerWithCheatOnAsset(HudUpgradeCheatState.None, out var config);
            try
            {
                var cheats = new FakeCheatSelection();
                cheats.Pick("HudUpgrade/MixedButtons");
                var session = new FakeSession();

                installer.Install(FullRegistry(session, cheats));

                Assert.AreEqual(HudUpgradeCheatState.MixedButtons, BuiltState(session));
                CollectionAssert.AreEqual(new[] { "HudUpgrade/MixedButtons" }, cheats.Reported);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [TestCase("PopupLose/NewRecord")]
        [TestCase(null)]
        public void Pick_of_another_feature_or_normal_play_builds_nothing_even_with_a_state_on_the_asset(string pickedKey)
        {
            var installer = InstallerWithCheatOnAsset(HudUpgradeCheatState.MixedButtons, out var config);
            try
            {
                var cheats = new FakeCheatSelection();
                cheats.Pick(pickedKey);
                var session = new FakeSession();

                installer.Install(FullRegistry(session, cheats));

                Assert.AreEqual(HudUpgradeCheatState.None, BuiltState(session));
                Assert.IsEmpty(cheats.Reported, "no state was built");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        // Bug b20261127-ab7a: picking "Giữa trận" opened the start screen; the bar's state only showed after Play then
        // Enter battle. Building the state now asks the run to open the scene straight in battle, with the state's team.
        [Test]
        public void Picked_mixed_buttons_asks_to_open_straight_in_battle_with_the_states_team()
        {
            var installer = InstallerWithCheatOnAsset(HudUpgradeCheatState.None, out var config);
            try
            {
                var cheats = new FakeCheatSelection();
                cheats.Pick("HudUpgrade/MixedButtons");
                var start = new FakeCheatStart();

                installer.Install(FullRegistry(new FakeSession(), cheats, start));

                Assert.AreEqual(1, start.Requests.Count, "asked for a battle once");
                CollectionAssert.AreEqual(config.MixedButtonsTeam, start.Requests[0].HeroCodes);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Pick_of_another_feature_asks_for_no_battle()
        {
            var installer = InstallerWithCheatOnAsset(HudUpgradeCheatState.MixedButtons, out var config);
            try
            {
                var cheats = new FakeCheatSelection();
                cheats.Pick("PopupLose/NewRecord");
                var start = new FakeCheatStart();

                installer.Install(FullRegistry(new FakeSession(), cheats, start));

                Assert.IsEmpty(start.Requests);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Without_a_pick_the_state_on_the_asset_is_built_and_reported_running()
        {
            var installer = InstallerWithCheatOnAsset(HudUpgradeCheatState.MixedButtons, out var config);
            try
            {
                var cheats = new FakeCheatSelection();
                var session = new FakeSession();

                installer.Install(FullRegistry(session, cheats));

                Assert.AreEqual(HudUpgradeCheatState.MixedButtons, BuiltState(session));
                CollectionAssert.AreEqual(new[] { "HudUpgrade/MixedButtons" }, cheats.Reported);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
