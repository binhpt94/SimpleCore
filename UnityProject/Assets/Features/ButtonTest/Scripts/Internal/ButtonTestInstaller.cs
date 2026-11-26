using System;
using Game.Core;
using Game.Run;
using UnityEngine;

namespace Game.ButtonTest
{
    /// <summary>
    /// Nối ButtonTest vào phiên của scene Game. Asset ButtonTestInstaller ở FeatureAssets/Public là thứ duy nhất
    /// _Composition thấy. Cài sau SessionScene và Run. Không đăng ký contract nào: không feature nào gọi tới nút thử.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/ButtonTest/Installer", fileName = "ButtonTestInstaller")]
    public sealed class ButtonTestInstaller : InstallerAsset
    {
        [SerializeField] private ButtonTestConfig config;
        [SerializeField] private GameObject viewPrefab;

        [Tooltip("State cheat của feature-button-test/cheat; chỉ có tác dụng ở bản dev.")]
        [SerializeField] private ButtonTestCheat cheat = ButtonTestCheat.None;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null) throw new InvalidOperationException($"{name}: thiếu config (ButtonTestConfig).");
            if (viewPrefab == null) throw new InvalidOperationException($"{name}: thiếu viewPrefab (prefab nút thử).");
            config.Validate();

            var session = registry.Resolve<ISessionScene>();
            var run = registry.Resolve<IRunFlow>();

            var factory = new PrefabTestButtonViewFactory(viewPrefab, config);
            session.Join(new ButtonTestParticipant(run, factory, cheat));
        }
    }
}
