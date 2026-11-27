using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.Composition
{
    /// <summary>The project stage the build is at; features that start at a later stage stay out of the boot.</summary>
    internal enum GameStage
    {
        Idea = 0,
        Prototype = 1,
        Full = 2,
    }

    /// <summary>
    /// Starts a scene: registers the scene's asset catalogs, then runs each feature's installer in list order.
    /// Each scene uses its own variant of Boot.prefab (Boot.Loading, Boot.Game); the variant's lists are
    /// that scene's boot order and shared assets. No game logic here.
    /// <para>
    /// <see cref="Installers"/> keeps every installer of the scene in its place. Installers also listed in
    /// <c>fromPrototype</c> are skipped while <c>stage</c> is <see cref="GameStage.Idea"/>, following each
    /// feature's stage table. Likewise <c>catalogs</c> are the IDEA catalogs and each of <c>prototypeCatalogs</c>
    /// replaces the catalog of its kind from PROTOTYPE on. The stage is set once on Boot.prefab and shared by
    /// every scene.
    /// </para>
    /// </summary>
    public sealed class Boot : MonoBehaviour
    {
        [SerializeField] private GameStage stage = GameStage.Idea;
        [SerializeField] private InstallerAsset[] installers = new InstallerAsset[0];
        [Tooltip("Installers of features that start at PROTOTYPE: kept in the list, skipped at IDEA.")]
        [SerializeField] private InstallerAsset[] fromPrototype = new InstallerAsset[0];
        [SerializeField] private AssetCatalog[] catalogs = new AssetCatalog[0];
        [Tooltip("From PROTOTYPE on, each of these replaces the catalog of the same kind (or adds a new kind).")]
        [SerializeField] private AssetCatalog[] prototypeCatalogs = new AssetCatalog[0];

        [Header("Cheat (dev builds only; set once on Boot.prefab)")]
        [Tooltip("The CheatSelectionAsset shared by every scene. Typed as ScriptableObject so release builds keep the same layout.")]
        [SerializeField] private ScriptableObject cheatSelection;
        [Tooltip("Every scene with its Boot variant, so the cheat list holds the states of every scene, not only this one.")]
        [SerializeField] private CheatScene[] cheatScenes = new CheatScene[0];

        /// <summary>A scene name (as SceneFlow loads it) and the Boot variant that boots it.</summary>
        [Serializable]
        internal struct CheatScene
        {
            public string scene;
            public Boot boot;
        }

        internal GameStage Stage => stage;

        internal IReadOnlyList<CheatScene> CheatScenes => cheatScenes;

        internal IReadOnlyList<InstallerAsset> Installers => installers;

        internal IReadOnlyList<InstallerAsset> FromPrototype => fromPrototype;

        internal IReadOnlyList<AssetCatalog> Catalogs => catalogs;

        internal IReadOnlyList<AssetCatalog> PrototypeCatalogs => prototypeCatalogs;

        /// <summary>The catalogs registered at <paramref name="atStage"/>: one per kind.</summary>
        internal IReadOnlyList<AssetCatalog> CatalogsAt(GameStage atStage)
        {
            if (atStage < GameStage.Prototype) return catalogs;
            var later = prototypeCatalogs.Where(c => c != null).Select(c => c.Kind).ToList();
            return catalogs.Where(c => c == null || !later.Contains(c.Kind)).Concat(prototypeCatalogs).ToArray();
        }

        private void Awake()
        {
            Run();
        }

        /// <summary>The installers that run at <paramref name="atStage"/>, in list order.</summary>
        internal IReadOnlyList<InstallerAsset> InstallersAt(GameStage atStage)
        {
            if (atStage >= GameStage.Prototype) return installers;
            return installers.Where(i => Array.IndexOf(fromPrototype, i) < 0).ToArray();
        }

        internal IServiceRegistry Run() => Run(stage);

        internal IServiceRegistry Run(GameStage atStage)
        {
            var registry = new ServiceRegistry();
            registry.Register<IAssetCatalogs>(new AssetCatalogs(CatalogsAt(atStage)));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterCheats(registry, atStage);
#endif
            foreach (var installer in InstallersAt(atStage))
            {
                installer.Install(registry);
            }
            return registry;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Before any installer: starts the scene on the shared cheat selection (keeps the pick, clears the running
        /// state) and registers it with the catalog of every cheat state source the build boots, in every scene.
        /// </summary>
        private void RegisterCheats(IServiceRegistry registry, GameStage atStage)
        {
            if (!(cheatSelection is CheatSelectionAsset asset))
                throw new InvalidOperationException(
                    $"Boot ({name}): cheatSelection is not set to a CheatSelectionAsset (set it on Boot.prefab).");
            var selection = asset.Selection;
            selection.BeginScene();
            registry.Register<ICheatSelection>(selection);
            registry.Register<ICheatStateCatalog>(new CheatStateCatalog(CheatSourcesAt(atStage)));
        }

        /// <summary>Each cheat state source booted at <paramref name="atStage"/> with its scene; a source listed in two scenes counts once, at the first.</summary>
        internal IReadOnlyList<CheatStateSceneSource> CheatSourcesAt(GameStage atStage)
        {
            var sources = new List<CheatStateSceneSource>();
            foreach (var entry in cheatScenes)
            {
                if (entry.boot == null || string.IsNullOrEmpty(entry.scene))
                    throw new InvalidOperationException($"Boot ({name}): a cheatScenes entry has no scene name or no Boot.");
                foreach (var installer in entry.boot.InstallersAt(atStage))
                {
                    if (installer is ICheatStateSource source && sources.All(s => s.Source != source))
                        sources.Add(new CheatStateSceneSource(entry.scene, source));
                }
            }
            return sources;
        }

        private sealed class CheatStateCatalog : ICheatStateCatalog
        {
            internal CheatStateCatalog(IReadOnlyList<CheatStateSceneSource> sources) => Sources = sources;

            public IReadOnlyList<CheatStateSceneSource> Sources { get; }
        }
#endif
    }
}
