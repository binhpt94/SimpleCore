using System;
using UnityEngine;

namespace Game.TeamSelect
{
    /// <summary>Instantiates the team select view prefab; its root must carry a component implementing <see cref="ITeamSelectView"/>.</summary>
    internal sealed class PrefabViewFactory : ITeamSelectViewFactory
    {
        private readonly GameObject _prefab;

        public PrefabViewFactory(GameObject prefab)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
        }

        public ITeamSelectView Create()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab);
            instance.name = _prefab.name;
            var view = instance.GetComponent<ITeamSelectView>();
            if (view == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException($"TeamSelect: prefab '{_prefab.name}' has no ITeamSelectView component.");
            }
            return view;
        }

        public void Destroy(ITeamSelectView view)
        {
            if (view is Component component && component != null) UnityEngine.Object.Destroy(component.gameObject);
        }
    }
}
