using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.ButtonTest
{
    /// <summary>Tạo view từ prefab của feature (có một component thực hiện <see cref="ITestButtonView"/>) và đặt chỗ theo config.</summary>
    internal sealed class PrefabTestButtonViewFactory : ITestButtonViewFactory
    {
        private readonly GameObject _prefab;
        private readonly ButtonTestConfig _config;

        internal PrefabTestButtonViewFactory(GameObject prefab, ButtonTestConfig config)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        public ITestButtonView Create()
        {
            var instance = Object.Instantiate(_prefab);
            instance.name = _prefab.name;
            var view = instance.GetComponent<ITestButtonView>();
            if (view == null)
            {
                Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"ButtonTest: prefab '{_prefab.name}' thiếu component thực hiện ITestButtonView.");
            }

            view.ApplyLayout(_config.Layout, CounterText.ButtonLabel);
            return view;
        }

        public void Destroy(ITestButtonView view)
        {
            if (view?.Root == null) return;
            var root = view.Root is Component c ? c.gameObject : view.Root;
            Object.Destroy(root);
        }
    }
}
