using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Core.Tests
{
    public class AssetCatalogsTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Get_ReturnsTheCatalogOfThatKind()
        {
            var popups = Catalog("Popups", "Popup", ("Shop/Bought", Asset("A")));
            var sounds = Catalog("Sounds", "Sound");

            var catalogs = new AssetCatalogs(new[] { popups, sounds });

            Assert.That(catalogs.Get("Popup"), Is.SameAs(popups));
            Assert.That(catalogs.Get("Sound"), Is.SameAs(sounds));
        }

        [Test]
        public void Get_KindTheSceneDoesNotHave_FailsNamingTheKind()
        {
            var catalogs = new AssetCatalogs(new AssetCatalog[0]);

            var error = Assert.Throws<InvalidOperationException>(() => catalogs.Get("Popup"));

            Assert.That(error.Message, Does.Contain("'Popup'"));
        }

        [Test]
        public void TryGet_FindsTheAssetByIdOnly()
        {
            var a = Asset("A");
            var catalog = Catalog("Popups", "Popup", ("Shop/Bought", a));

            Assert.That(catalog.TryGet("Shop/Bought", out var found), Is.True);
            Assert.That(found, Is.SameAs(a));
            Assert.That(catalog.TryGet("Shop/Other", out _), Is.False);
            Assert.That(catalog.TryGet(null, out _), Is.False);
        }

        [Test]
        public void TwoCatalogsOfOneKind_FailNamingBoth()
        {
            var first = Catalog("GamePopups", "Popup");
            var second = Catalog("MorePopups", "Popup");

            var error = Assert.Throws<InvalidOperationException>(() => new AssetCatalogs(new[] { first, second }));

            Assert.That(error.Message, Does.Contain("GamePopups").And.Contain("MorePopups"));
        }

        [Test]
        public void EmptySlotOrMissingKind_Fails()
        {
            Assert.Throws<InvalidOperationException>(() => new AssetCatalogs(new AssetCatalog[] { null }));
            Assert.Throws<InvalidOperationException>(() => new AssetCatalogs(new[] { Catalog("NoKind", "") }));
        }

        [Test]
        public void IdListedTwice_FailsNamingCatalogAndId()
        {
            var catalog = Catalog("Popups", "Popup", ("Shop/Bought", Asset("A")), ("Shop/Bought", Asset("B")));

            var error = Assert.Throws<InvalidOperationException>(() => new AssetCatalogs(new[] { catalog }));

            Assert.That(error.Message, Does.Contain("Popups").And.Contain("Shop/Bought"));
        }

        [Test]
        public void EntryWithoutAssetOrId_Fails()
        {
            Assert.Throws<InvalidOperationException>(() => Catalog("Popups", "Popup", ("Shop/Bought", null)).Check());
            Assert.Throws<InvalidOperationException>(() => Catalog("Popups", "Popup", ("", Asset("A"))).Check());
        }

        private GameObject Asset(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        private AssetCatalog Catalog(string name, string kind, params (string id, Object asset)[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<AssetCatalog>();
            catalog.name = name;
            created.Add(catalog);
            var so = new SerializedObject(catalog);
            so.FindProperty("kind").stringValue = kind;
            var list = so.FindProperty("entries");
            list.arraySize = entries.Length;
            for (var i = 0; i < entries.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("id").stringValue = entries[i].id;
                item.FindPropertyRelative("asset").objectReferenceValue = entries[i].asset;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }
    }
}
