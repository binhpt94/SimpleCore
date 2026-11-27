using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.SceneFlow;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using SessionState = Game.Core.SessionState;

namespace Game.CheatPanel.Tests
{
    /// <summary>Nodes feature-cheat-panel/cheat-access/access-spec (button, panel), cheat-states/states-rules (rows, applying a row), cheat (panel-open).</summary>
    public sealed class CheatPanelTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
                if (o != null) Object.DestroyImmediate(o);
            _created.Clear();
        }

        private CheatPanelConfig Config()
        {
            var c = ScriptableObject.CreateInstance<CheatPanelConfig>();
            _created.Add(c);
            return c;
        }

        private static readonly CheatEntry[] SampleEntries =
        {
            new CheatEntry("Popup thua", "PopupLose/NewRecord", "Kỉ lục mới", "Game"),
            new CheatEntry("HUD", "Hud/Mid", "Giữa trận", "Game"),
            new CheatEntry("Chọn đội", "TeamSelect/Full", "Đủ đội", "Game"),
            new CheatEntry("HUD", "Hud/Low", "Máu thấp", "Game"),
            new CheatEntry("Loading", "SceneFlow/LoadError", "Lỗi tải", "Loading"),
        };

        // ---- rows (states-rules) ----

        [Test]
        public void NormalRowComesFirst_ThenStateRowsByFeatureTitleAToZ_KeepingEachFeaturesOrder()
        {
            var rows = CheatRows.Build("Chơi bình thường", SampleEntries);
            Assert.That(rows.Select(r => r.StateKey), Is.EqualTo(new[]
            {
                null, "TeamSelect/Full", "Hud/Mid", "Hud/Low", "SceneFlow/LoadError", "PopupLose/NewRecord",
            }));
            Assert.That(rows[0].IsNormal, Is.True);
            Assert.That(rows[0].Label, Is.EqualTo("Chơi bình thường"));
        }

        [Test]
        public void EachStateRowReadsFeatureTitleColonStateTitle_TakenFromTheSource()
        {
            var rows = CheatRows.Build("Chơi bình thường", SampleEntries);
            Assert.That(rows.Single(r => r.StateKey == "PopupLose/NewRecord").Label, Is.EqualTo("Popup thua: Kỉ lục mới"));
            Assert.That(rows.Single(r => r.StateKey == "Hud/Mid").Label, Is.EqualTo("HUD: Giữa trận"));
        }

        [Test]
        public void OnlyStatesTheBuildCanBuildHaveRows_AStateMissingFromTheCatalogHasNoRow()
        {
            // The catalog of a build lists only the sources it boots: a state whose code is not built is absent.
            var catalog = new FakeCatalog(new CheatStateSceneSource("Game", new FakeSource("Popup thua", new CheatState("PopupLose/NewRecord", "Kỉ lục mới"))));
            var states = new CatalogCheatStates(catalog, new CheatSelection());
            var rows = CheatRows.Build("Chơi bình thường", states.Entries);
            Assert.That(rows.Select(r => r.StateKey), Is.EqualTo(new[] { null, "PopupLose/NewRecord" }));
            Assert.That(rows[1].Scene, Is.EqualTo("Game"), "the row reloads the scene whose boot builds the state");
        }

        [Test]
        public void NoStateInTheBuild_OnlyTheNormalRowAndTheEmptyLabel()
        {
            var p = new CheatPanelPresenter(Config(), new NoCheatStates(), new FakeSwitch());
            Assert.That(p.Rows, Has.Count.EqualTo(1));
            Assert.That(p.Rows[0].IsNormal, Is.True);
            Assert.That(p.Screen.EmptyLabel, Is.EqualTo("Bản này không có state cheat"));

            var withStates = new CheatPanelPresenter(Config(), new FakeStates(SampleEntries), new FakeSwitch());
            Assert.That(withStates.Screen.EmptyLabel, Is.Null, "the empty label shows only without state rows");
        }

        [Test]
        public void RunningStatesRowIsMarked_NoStateMarksTheNormalRow()
        {
            var states = new FakeStates(SampleEntries);
            var p = new CheatPanelPresenter(Config(), states, new FakeSwitch());
            Assert.That(p.ActiveRow, Is.Zero, "no state running: normal row");
            states.Running = "Hud/Low";
            Assert.That(p.Rows[p.ActiveRow].StateKey, Is.EqualTo("Hud/Low"));
            Assert.That(p.Screen.ActiveRow, Is.EqualTo(p.ActiveRow));
        }

        // ---- applying a row (states-rules) ----

        [Test]
        public void TappingAStateRow_PicksItAndReloadsItsScene_PanelClosesAndForcesNothingElse()
        {
            var states = new FakeStates(SampleEntries);
            var sw = new FakeSwitch();
            var p = OpenPanel(states, sw);
            var index = IndexOf(p, "TeamSelect/Full");

            p.PressRow(index);

            Assert.That(states.Picks, Is.EqualTo(new[] { "TeamSelect/Full" }), "the owning feature builds it when the scene boots");
            Assert.That(sw.Reloads, Is.EqualTo(new[] { "Game" }));
            Assert.That(sw.Restarts, Is.Zero);
            Assert.That(p.IsOpen, Is.False, "choosing a row closes the panel");
        }

        [Test]
        public void TappingTheRunningStatesRowAgain_ReloadsItsSceneSoTheStateIsBuiltFromTheStart()
        {
            var states = new FakeStates(SampleEntries) { Running = "Hud/Mid" };
            var sw = new FakeSwitch();
            var p = OpenPanel(states, sw);

            p.PressRow(IndexOf(p, "Hud/Mid"));

            Assert.That(states.Picks, Is.EqualTo(new[] { "Hud/Mid" }));
            Assert.That(sw.Reloads, Is.EqualTo(new[] { "Game" }), "reloaded even though it is the running scene and state");
        }

        [Test]
        public void TappingNormalPlay_DropsTheCheatStateAndRestartsFromTheFirstScene()
        {
            var states = new FakeStates(SampleEntries) { Running = "Hud/Mid" };
            var sw = new FakeSwitch();
            var p = OpenPanel(states, sw);

            p.PressRow(0);

            Assert.That(states.Picks, Is.EqualTo(new string[] { null }), "normal play is picked: no installer builds a state");
            Assert.That(sw.Restarts, Is.EqualTo(1));
            Assert.That(sw.Reloads, Is.Empty);
            Assert.That(p.IsOpen, Is.False);
        }

        [Test]
        public void CheatStateWritesNothingToTheDevice_PickStaysInMemoryAndPanelOpenReportsRunning()
        {
            var selection = new CheatSelection();
            var states = new CatalogCheatStates(new FakeCatalog(new CheatStateSceneSource("Game", new FakeSource("HUD", new CheatState("Hud/Mid", "Giữa trận")))), selection);
            states.Pick("Hud/Mid");
            Assert.That(selection.PickedKey, Is.EqualTo("Hud/Mid"), "the pick lives in Core's in-memory selection");

            // panel-open is a cheat state too: it reports running, which turns saving off (Save checks RunningKey).
            var registry = Registry(new FakeSession(), selection: new CheatSelection(), catalog: new FakeCatalog());
            var sel = registry.Resolve<ICheatSelection>();
            var installer = Installer(CheatPanelCheatState.PanelOpen);
            installer.Install(registry);
            Assert.That(sel.RunningKey, Is.EqualTo(CheatPanelInstaller.PanelOpenKey));

            foreach (var file in Directory.GetFiles("Assets/Features/CheatPanel/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                Assert.That(text, Does.Not.Contain("PlayerPrefs").And.Not.Contain("File.Write").And.Not.Contain("ISavedRecord"), file);
            }
        }

        // ---- transition lock (access-spec) ----

        [Test]
        public void WhileTheSceneSwitches_EveryRowIsLockedAndATapDoesNothing_TheButtonStillOpensAndCloses()
        {
            var states = new FakeStates(SampleEntries);
            var sw = new FakeSwitch();
            var p = OpenPanel(states, sw);
            sw.SetSwitching(true);

            Assert.That(p.RowsLocked, Is.True);
            Assert.That(p.Screen.RowsLocked, Is.True);
            p.PressRow(0);
            p.PressRow(1);
            Assert.That(states.Picks, Is.Empty);
            Assert.That(sw.Reloads, Is.Empty);
            Assert.That(sw.Restarts, Is.Zero);
            Assert.That(p.IsOpen, Is.True, "a locked row does not close the panel");

            p.PressButton();
            Assert.That(p.IsOpen, Is.False);
            p.PressButton();
            Assert.That(p.IsOpen, Is.True);

            sw.SetSwitching(false);
            Assert.That(p.RowsLocked, Is.False, "rows unlock once the switch ends");
        }

        [Test]
        public void TheViewIsRedrawnWhenASceneSwitchStartsAndEnds()
        {
            var sw = new FakeSwitch();
            var p = new CheatPanelPresenter(Config(), new FakeStates(SampleEntries), sw);
            var display = new FakeDisplay();
            p.Attach(display);
            sw.SetSwitching(true);
            Assert.That(display.Last.RowsLocked, Is.True);
            sw.SetSwitching(false);
            Assert.That(display.Last.RowsLocked, Is.False);
        }

        // ---- closing (access-spec) ----

        [Test]
        public void PanelOpensAndClosesWithTheButton_AndClosesWhenARowIsChosen()
        {
            var p = new CheatPanelPresenter(Config(), new FakeStates(SampleEntries), new FakeSwitch());
            Assert.That(p.IsOpen, Is.False, "starts closed");
            p.PressButton();
            Assert.That(p.IsOpen, Is.True);
            p.PressButton();
            Assert.That(p.IsOpen, Is.False);
            p.PressButton();
            p.PressRow(1);
            Assert.That(p.IsOpen, Is.False);
        }

        [Test]
        public void SceneChangeWithThePanelOpen_ClosesIt_TheNextSceneHasTheButtonWithThePanelClosed()
        {
            var installer = Installer(CheatPanelCheatState.None);
            var spawner = new ViewSpawner(_created);
            installer.SpawnerOverride = spawner;
            var first = new FakeSession();
            var sw = new FakeSwitch();
            installer.Install(Registry(first, sw));
            var participant = (CheatOverlayParticipant)first.Joined[0];
            participant.Run();
            participant.Presenter.PressButton();
            Assert.That(participant.Presenter.IsOpen, Is.True);

            participant.DisposeAsync().GetAwaiter().GetResult();
            Assert.That(spawner.Despawned, Is.EqualTo(1), "the old scene's overlay, panel included, is gone");

            var next = new FakeSession();
            installer.Install(Registry(next, new FakeSwitch()));
            var again = (CheatOverlayParticipant)next.Joined[0];
            again.Run();
            Assert.That(spawner.Spawned, Is.EqualTo(2), "the button is back in the new scene");
            Assert.That(again.Presenter.IsOpen, Is.False);
        }

        [Test]
        public void OverlayWithAViewButNoSceneSwitch_FailsNamingISceneSwitch()
        {
            var installer = Installer(CheatPanelCheatState.None);
            installer.SpawnerOverride = new ViewSpawner(_created);
            var session = new FakeSession();
            installer.Install(Registry(session, null));
            var error = Assert.Throws<InvalidOperationException>(() => session.Joined[0].Run());
            Assert.That(error.Message, Does.Contain(nameof(ISceneSwitch)));
        }

        // ---- view (access-spec) ----

        [Test]
        public void CheatButtonReadsCheat_AndLooksDifferentWhenThePanelIsOpen()
        {
            var view = View();
            var p = new CheatPanelPresenter(Config(), new FakeStates(SampleEntries), new FakeSwitch());
            p.Attach(view);
            var button = Find(view, "cheat-button");
            Assert.That(Text(button), Is.EqualTo("Cheat"));
            var closed = ColorOf(button);
            p.PressButton();
            Assert.That(ColorOf(button), Is.Not.EqualTo(closed));
        }

        [Test]
        public void PanelHasTitleCheat_AScrollableRowList_AndAShadeThatBlocksTapsBelow()
        {
            var view = View();
            var p = new CheatPanelPresenter(Config(), new FakeStates(SampleEntries), new FakeSwitch());
            p.Attach(view);
            var shade = Find(view, "cheat-shade");
            Assert.That(shade.activeSelf, Is.False, "closed: no shade, no tap blocked");
            p.PressButton();
            Assert.That(shade.activeSelf, Is.True);
            Assert.That(Get(Comp(shade, "Image"), "raycastTarget"), Is.True, "the shade catches taps");
            Assert.That(Comp(shade, "Selectable"), Is.Null, "tapping the shade does nothing");
            Assert.That(((RectTransform)shade.transform).anchorMax, Is.EqualTo(Vector2.one), "covers the whole screen");
            Assert.That(Text(Find(view, "panel-title")), Is.EqualTo("Cheat"));
            var scroll = Comp(Find(view, "state-list"), "ScrollRect");
            Assert.That(Get(scroll, "vertical"), Is.True);
            Assert.That(Get(scroll, "horizontal"), Is.False);
            Assert.That(CompsInChildren(Content(view), "Selectable"), Has.Length.EqualTo(SampleEntries.Length + 1));
        }

        [Test]
        public void PopupOpen_ButtonAndPanelSitAboveEveryPopup()
        {
            var view = View();
            new CheatPanelPresenter(Config(), new FakeStates(SampleEntries), new FakeSwitch()).Attach(view);
            var canvas = view.GetComponentInChildren<Canvas>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.sortingOrder, Is.GreaterThan(1000), "popup layer uses 1000");
            var button = Find(view, "cheat-button");
            Assert.That(button.transform.GetSiblingIndex(), Is.GreaterThan(Find(view, "cheat-shade").transform.GetSiblingIndex()),
                "the button stays tappable over the shade");
        }

        [Test]
        public void ListLongerThanThePanel_ScrollsVerticallyWithoutShrinkingText()
        {
            var many = Enumerable.Range(0, 30).Select(i => new CheatEntry("F" + i.ToString("00"), "F/" + i, "State " + i, "Game")).ToArray();
            var view = View();
            new CheatPanelPresenter(Config(), new FakeStates(many), new FakeSwitch()).Attach(view);
            var viewport = (RectTransform)Find(view, "state-list").transform;
            var content = (RectTransform)Content(view).transform;
            Assert.That(content.sizeDelta.y, Is.GreaterThan(viewport.sizeDelta.y), "content is taller than the list: it scrolls");
            foreach (var label in CompsInChildren(content.gameObject, "TMP_Text"))
                Assert.That(Get(label, "enableAutoSizing"), Is.False, (string)Get(label, "text"));
        }

        [Test]
        public void NoStateInTheBuild_ViewShowsTheNormalRowAndTheEmptyLabel()
        {
            var view = View();
            new CheatPanelPresenter(Config(), new NoCheatStates(), new FakeSwitch()).Attach(view);
            var content = Content(view);
            Assert.That(CompsInChildren(content, "Selectable"), Has.Length.EqualTo(1));
            Assert.That(CompsInChildren(content, "TMP_Text").Select(t => (string)Get(t, "text")),
                Is.EquivalentTo(new[] { "Chơi bình thường", "Bản này không có state cheat" }));
        }

        [Test]
        public void LockedRowsAreNotInteractable_InTheView()
        {
            var view = View();
            var sw = new FakeSwitch();
            new CheatPanelPresenter(Config(), new FakeStates(SampleEntries), sw).Attach(view);
            sw.SetSwitching(true);
            var buttons = CompsInChildren(Content(view), "Selectable");
            Assert.That(buttons, Is.Not.Empty);
            Assert.That(buttons.All(b => !(bool)Get(b, "interactable")), Is.True);
            Assert.That(Get(Comp(Find(view, "cheat-button"), "Selectable"), "interactable"), Is.True);
        }

        [Test]
        public void ShippedOverlayPrefabCarriesTheCheatPanelView()
        {
            var installer = UnityEditor.AssetDatabase.LoadAssetAtPath<CheatPanelInstaller>("Assets/Features/CheatPanel/FeatureAssets/Public/CheatPanelInstaller.asset");
            Assert.That(installer.OverlayPrefab, Is.Not.Null);
            Assert.That(installer.OverlayPrefab.GetComponent<CheatOverlayView>(), Is.Not.Null);
            Assert.That(installer.Cheat, Is.EqualTo(CheatPanelCheatState.None), "shipped asset builds no cheat state");
        }

        [Test]
        public void PrefabSpawner_DrawsTheConfigTexts_ButtonLabelPanelTitleNormalRowLabelEmptyLabel()
        {
            var installer = UnityEditor.AssetDatabase.LoadAssetAtPath<CheatPanelInstaller>("Assets/Features/CheatPanel/FeatureAssets/Public/CheatPanelInstaller.asset");
            var config = Config();
            config.ButtonLabel = "B-lbl";
            config.PanelTitle = "P-title";
            config.NormalRowLabel = "N-row";
            config.EmptyLabel = "E-lbl";
            var overlay = new PrefabCheatOverlaySpawner(installer.OverlayPrefab).Spawn(config);
            _created.Add(overlay);
            var view = overlay.GetComponent<CheatOverlayView>();
            Assert.That(Text(Find(view, "cheat-button")), Is.EqualTo("B-lbl"));
            Assert.That(Text(Find(view, "panel-title")), Is.EqualTo("P-title"));
            Assert.That(Text(Find(view, "empty-label")), Is.EqualTo("E-lbl"));
            Assert.That(CompsInChildren(Content(view), "TMP_Text").Select(t => (string)Get(t, "text")), Has.Member("N-row"));
        }

        // ---- cheat#panel-open ----

        [Test]
        public void CheatPanelOpen_PanelIsOpenAsSoonAsTheSceneRuns_WithNormalRowAndEveryStateRow_ButtonShowsOpen()
        {
            var installer = Installer(CheatPanelCheatState.PanelOpen);
            installer.SpawnerOverride = new ViewSpawner(_created);
            var session = new FakeSession();
            var catalog = new FakeCatalog(
                new CheatStateSceneSource("Game", new FakeSource("HUD", new CheatState("Hud/Mid", "Giữa trận"))),
                new CheatStateSceneSource("Game", new FakeSource("Popup thua", new CheatState("PopupLose/NewRecord", "Kỉ lục mới"))));
            installer.Install(Registry(session, new FakeSwitch(), new CheatSelection(), catalog));
            var participant = (CheatOverlayParticipant)session.Joined[0];
            participant.Run();

            Assert.That(participant.Presenter.IsOpen, Is.True);
            Assert.That(participant.Presenter.Rows.Select(r => r.Label),
                Is.EqualTo(new[] { "Chơi bình thường", "HUD: Giữa trận", "Popup thua: Kỉ lục mới" }));
            Assert.That(participant.Presenter.Screen.IsOpen, Is.True, "the button shows its open look");
        }

        [Test]
        public void CheatPanelOpen_IsIgnoredOnceSomethingWasPickedInThisRun()
        {
            var installer = Installer(CheatPanelCheatState.PanelOpen);
            installer.SpawnerOverride = new ViewSpawner(_created);
            var selection = new CheatSelection();
            selection.Pick(null);
            var session = new FakeSession();
            installer.Install(Registry(session, new FakeSwitch(), selection, new FakeCatalog()));
            session.Joined[0].Run();
            Assert.That(((CheatOverlayParticipant)session.Joined[0]).Presenter.IsOpen, Is.False);
            Assert.That(selection.RunningKey, Is.Null);
        }

        // ---- helpers ----

        private CheatPanelPresenter OpenPanel(ICheatStates states, FakeSwitch sw)
        {
            var p = new CheatPanelPresenter(Config(), states, sw);
            p.PressButton();
            return p;
        }

        private static int IndexOf(CheatPanelPresenter p, string key)
        {
            for (var i = 0; i < p.Rows.Count; i++)
                if (p.Rows[i].StateKey == key) return i;
            throw new AssertionException(key + " has no row");
        }

        private CheatPanelInstaller Installer(CheatPanelCheatState cheat)
        {
            var installer = ScriptableObject.CreateInstance<CheatPanelInstaller>();
            _created.Add(installer);
            installer.Config = Config();
            installer.Cheat = cheat;
            installer.SpawnerOverride = new ViewSpawner(_created);
            return installer;
        }

        private static ServiceRegistry Registry(FakeSession session, ISceneSwitch sw = null, ICheatSelection selection = null, ICheatStateCatalog catalog = null)
        {
            var r = new ServiceRegistry();
            r.Register<ISessionScene>(session);
            if (sw != null) r.Register(sw);
            if (selection != null) r.Register(selection);
            if (catalog != null) r.Register(catalog);
            return r;
        }

        private CheatOverlayView View()
        {
            var go = new GameObject("CheatOverlay");
            _created.Add(go);
            return go.AddComponent<CheatOverlayView>();
        }

        private static bool IsKindOf(Component c, string type)
        {
            for (var t = c.GetType(); t != null; t = t.BaseType)
                if (t.Name == type) return true;
            return false;
        }

        private static Component Comp(GameObject go, string type) => go.GetComponents<Component>().FirstOrDefault(c => IsKindOf(c, type));

        private static Component[] CompsInChildren(GameObject go, string type) =>
            go.GetComponentsInChildren<Component>(true).Where(c => IsKindOf(c, type)).ToArray();

        private static object Get(Component c, string prop) => c.GetType().GetProperty(prop).GetValue(c);

        private static GameObject Content(Component view) => ((RectTransform)Get(Comp(Find(view, "state-list"), "ScrollRect"), "content")).gameObject;

        private static string Text(GameObject go) => (string)Get(CompsInChildren(go, "TMP_Text")[0], "text");

        private static Color ColorOf(GameObject go) => (Color)Get(Comp(go, "Image"), "color");

        private static GameObject Find(Component root, string name) =>
            root.GetComponentsInChildren<Transform>(true).First(t => t.name == name).gameObject;

        private sealed class ViewSpawner : ICheatOverlaySpawner
        {
            private readonly List<Object> _created;
            public int Spawned, Despawned;
            public ViewSpawner(List<Object> created) => _created = created;

            public GameObject Spawn(CheatPanelConfig config)
            {
                Spawned++;
                var go = new GameObject("CheatOverlay");
                go.AddComponent<CheatOverlayView>();
                _created.Add(go);
                return go;
            }

            public void Despawn(GameObject overlay)
            {
                Despawned++;
                if (overlay != null) Object.DestroyImmediate(overlay);
            }
        }

        private sealed class FakeDisplay : ICheatPanelDisplay
        {
            public CheatPanelScreen Last;
            public void Bind(ICheatPanelInput input) { }
            public void Show(CheatPanelScreen screen) => Last = screen;
        }

        private sealed class FakeStates : ICheatStates
        {
            public readonly List<string> Picks = new List<string>();
            public FakeStates(IReadOnlyList<CheatEntry> entries) => Entries = entries;
            public IReadOnlyList<CheatEntry> Entries { get; }
            public string Running;
            public string RunningKey => Running;
            public void Pick(string key) => Picks.Add(key);
        }

        private sealed class FakeSwitch : ISceneSwitch
        {
            public readonly List<string> Reloads = new List<string>();
            public int Restarts;
            public bool IsSwitching { get; private set; }
            public event Action SwitchingChanged;
            public void SetSwitching(bool on) { IsSwitching = on; SwitchingChanged?.Invoke(); }
            public void Reload(string scene) { if (!IsSwitching) Reloads.Add(scene); }
            public void Restart() { if (!IsSwitching) Restarts++; }
        }

        private sealed class FakeSource : ICheatStateSource
        {
            public FakeSource(string title, params CheatState[] states) { CheatFeatureTitle = title; CheatStates = states; }
            public string CheatFeatureTitle { get; }
            public IReadOnlyList<CheatState> CheatStates { get; }
        }

        private sealed class FakeCatalog : ICheatStateCatalog
        {
            public FakeCatalog(params CheatStateSceneSource[] sources) => Sources = sources;
            public IReadOnlyList<CheatStateSceneSource> Sources { get; }
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
            public SessionState State => SessionState.Init;
            public event Action<SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }
    }
}
