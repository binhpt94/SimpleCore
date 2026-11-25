using System;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.PopupRevive.Tests
{
    public class PopupReviveInstallerTests
    {
        [Test]
        public void InstallWithoutRunReviveNamesTheMissingContract()
        {
            var installer = ScriptableObject.CreateInstance<PopupReviveInstaller>();
            try
            {
                var error = Assert.Throws<InvalidOperationException>(() => installer.Install(new EmptyRegistry()));
                StringAssert.Contains("is not installed", error.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
            }
        }

        private sealed class EmptyRegistry : IServiceRegistry
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

            public T Resolve<T>() where T : class => (T)_services[typeof(T)];

            public bool TryResolve<T>(out T service) where T : class
            {
                service = _services.TryGetValue(typeof(T), out var found) ? (T)found : null;
                return service != null;
            }
        }
    }
}
