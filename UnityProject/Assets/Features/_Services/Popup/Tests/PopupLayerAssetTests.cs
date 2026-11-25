using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Services.Popup.Tests
{
    /// <summary>
    /// feature-popup/popup-layer/stack-rules: the layer, backdrop and config assets the service really ships with,
    /// and how the Unity presenter stacks "backdrop, popup" pairs.
    /// </summary>
    public class PopupLayerAssetTests
    {
        private const string Private = "Assets/Features/_Services/Popup/FeatureAssets/Private/";

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Config_AllowsAtMostThreePopupsAtOnce()
        {
            var config = Load<PopupConfig>("PopupConfig.asset");

            Assert.That(config.MaxOpen, Is.EqualTo(3));
            Assert.DoesNotThrow(config.Check);
        }

        [Test]
        public void Layer_IsAnOverlayCanvasDrawnAboveEveryOtherViewOfTheScene()
        {
            var canvas = Load<GameObject>("PopupLayer.prefab").GetComponent<Canvas>();

            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.sortingOrder, Is.EqualTo(1000));
        }

        [Test]
        public void Backdrop_DarkensTheWholeScreenBy60Percent()
        {
            var backdrop = Load<GameObject>("PopupBackdrop.prefab");
            var rect = backdrop.GetComponent<RectTransform>();
            var color = Graphic(backdrop).FindProperty("m_Color").colorValue;

            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(color.r + color.g + color.b, Is.EqualTo(0f));
            Assert.That(color.a, Is.EqualTo(0.6f).Within(0.001f));
        }

        [Test]
        public void Backdrop_TakesEveryTouch_AndATapOnItDoesNothing()
        {
            var backdrop = Load<GameObject>("PopupBackdrop.prefab");

            Assert.That(Graphic(backdrop).FindProperty("m_RaycastTarget").boolValue, Is.True, "blocks touches below");
            foreach (var component in backdrop.GetComponents<Component>())
                Assert.That(new SerializedObject(component).FindProperty("m_OnClick"), Is.Null,
                    $"{component.GetType().Name} reacts to a tap");
        }

        [Test]
        public void Presenter_MakesNoLayerUntilTheFirstPopup()
        {
            var presenter = NewPresenter();

            Assert.That(LayersInScene(), Is.EqualTo(0), "nothing darkens or blocks while no popup is open");

            var popup = presenter.Present(Popup("A"));
            created.Add(popup.transform.parent.gameObject);

            Assert.That(LayersInScene(), Is.EqualTo(1));
        }

        [Test]
        public void Presenter_PutsEachPopupRightAfterItsOwnBackdrop_NewestPairDrawnOnTop()
        {
            var presenter = NewPresenter();

            var first = presenter.Present(Popup("A"));
            var second = presenter.Present(Popup("B"));
            var layer = first.transform.parent;
            created.Add(layer.gameObject);

            Assert.That(second.transform.parent, Is.SameAs(layer));
            Assert.That(layer.childCount, Is.EqualTo(4));
            Assert.That(layer.GetChild(0).name, Does.StartWith("PopupBackdrop"));
            Assert.That(layer.GetChild(1), Is.SameAs(first.transform));
            Assert.That(layer.GetChild(2).name, Does.StartWith("PopupBackdrop"), "the newer backdrop darkens the older popup too");
            Assert.That(layer.GetChild(3), Is.SameAs(second.transform));
        }

        // ---- helpers

        private static T Load<T>(string file) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Private + file);
            Assert.That(asset, Is.Not.Null, Private + file);
            return asset;
        }

        private static UnityPopupPresenter NewPresenter() =>
            new UnityPopupPresenter(Load<GameObject>("PopupLayer.prefab"), Load<GameObject>("PopupBackdrop.prefab"));

        private static int LayersInScene()
        {
            var count = 0;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == "PopupLayer") count++;
            return count;
        }

        /// <summary>The backdrop's UI image, read through serialization (the test assembly does not reference uGUI).</summary>
        private static SerializedObject Graphic(GameObject go)
        {
            foreach (var component in go.GetComponents<Component>())
            {
                var so = new SerializedObject(component);
                if (so.FindProperty("m_Color") != null) return so;
            }
            Assert.Fail($"{go.name} has no UI graphic");
            return null;
        }

        private GameObject Popup(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            created.Add(go);
            return go;
        }
    }
}
