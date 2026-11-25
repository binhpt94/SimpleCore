using NUnit.Framework;
using UnityEditor;

namespace Game.Combat.Tests
{
    public class CombatConfigTests
    {
        private const string ConfigPath = "Assets/Features/Combat/FeatureAssets/Private/CombatConfig.asset";

        [Test]
        public void BattleStepsAreTwoHundredthsOfASecondAndAtMostFivePerFrame()
        {
            var config = AssetDatabase.LoadAssetAtPath<CombatConfig>(ConfigPath);
            Assert.IsNotNull(config, $"missing {ConfigPath}");

            var settings = config.ToSettings();

            Assert.AreEqual(0.02f, settings.StepSeconds, 1e-6f);
            Assert.AreEqual(5, settings.MaxStepsPerFrame);
        }
    }
}
