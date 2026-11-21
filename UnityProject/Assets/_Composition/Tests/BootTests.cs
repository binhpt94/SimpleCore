using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Composition.Tests
{
    public class BootTests
    {
        private const string PrefabPath = "Assets/_Composition/Boot.prefab";

        [Test]
        public void BootPrefab_ListsEveryInstaller_NoneMissing()
        {
            var boot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<Boot>();

            Assert.That(boot, Is.Not.Null, "Boot component missing on Boot.prefab");
            Assert.That(boot.Installers.Count, Is.EqualTo(4));
            Assert.That(boot.Installers, Has.None.Null);
        }

        [Test]
        public void Run_InstallsAllFeaturesInListOrder_WithoutMissingDependencies()
        {
            var boot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<Boot>();

            // Wallet resolves the session, Shop resolves Wallet and Popup during Install,
            // so a wrong order throws here.
            Assert.DoesNotThrow(() => boot.Run());
        }
    }
}
