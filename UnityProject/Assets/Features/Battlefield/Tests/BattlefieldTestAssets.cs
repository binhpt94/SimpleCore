using NUnit.Framework;
using UnityEditor;

namespace Game.Battlefield.Tests
{
    /// <summary>The real Battlefield assets, so the tests check the numbers the game uses.</summary>
    internal static class BattlefieldTestAssets
    {
        public const string ConfigPath = "Assets/Features/Battlefield/FeatureAssets/Private/BattlefieldConfig.asset";
        public const string InstallerPath = "Assets/Features/Battlefield/FeatureAssets/Public/BattlefieldInstaller.asset";

        public static BattlefieldConfig Config()
        {
            var config = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, ConfigPath + " missing");
            return config;
        }

        public static BattlefieldInstaller Installer()
        {
            var installer = AssetDatabase.LoadAssetAtPath<BattlefieldInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, InstallerPath + " missing");
            return installer;
        }
    }
}
