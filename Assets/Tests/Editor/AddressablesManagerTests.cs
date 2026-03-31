using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using BlockAndDagger.Core;

namespace Tests
{
    public class AddressablesManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            AddressablesManager.SetTestModeForEditor(true);
        }

        [TearDown]
        public async Task TearDown()
        {
            // Ensure cache cleared using production release method in test mode
            await AddressablesManager.ReleasePreloadedGroupAsync(null);
            AddressablesManager.SetTestModeForEditor(false);
        }

        [Test]
        public async Task StartPreload_WithNull_KeepsEmptyCache()
        {
            await AddressablesManager.StartPreloadGroupAssets(null);
            var keys = AddressablesManager.GetPreloadedGroupKeysForTests();
            Assert.IsNotNull(keys);
            Assert.AreEqual(0, keys.Count);
        }

        [Test]
        public async Task StartPreload_WithABC_PopulatesCache()
        {
            await AddressablesManager.StartPreloadGroupAssets(new[] { "a", "b", "c" });
            var keys = AddressablesManager.GetPreloadedGroupKeysForTests();
            Assert.AreEqual(3, keys.Count);
            Assert.IsTrue(keys.Contains("a"));
            Assert.IsTrue(keys.Contains("b"));
            Assert.IsTrue(keys.Contains("c"));
        }

        [Test]
        public async Task PreloadABC_ThenReleaseToBC_ThenPreloadB_NoopKeepsBC()
        {
            await AddressablesManager.StartPreloadGroupAssets(new[] { "a", "b", "c" });
            var keys = AddressablesManager.GetPreloadedGroupKeysForTests();
            Assert.AreEqual(3, keys.Count);

            await AddressablesManager.ReleasePreloadedGroupAsync(new List<string> { "b", "c" });
            var afterRelease = AddressablesManager.GetPreloadedGroupKeysForTests();
            Assert.AreEqual(2, afterRelease.Count);
            Assert.IsTrue(afterRelease.Contains("b"));
            Assert.IsTrue(afterRelease.Contains("c"));

            await AddressablesManager.StartPreloadGroupAssets(new[] { "b" });
            var afterPreload = AddressablesManager.GetPreloadedGroupKeysForTests();
            Assert.AreEqual(2, afterPreload.Count);
            Assert.IsTrue(afterPreload.Contains("b"));
            Assert.IsTrue(afterPreload.Contains("c"));
        }

        [Test]
        public async Task ReleasePreloadedGroupAsync_WithSameValues_IsNoop()
        {
            await AddressablesManager.StartPreloadGroupAssets(new[] { "b", "c" });
            var before = AddressablesManager.GetPreloadedGroupKeysForTests();
            Assert.AreEqual(2, before.Count);

            await AddressablesManager.ReleasePreloadedGroupAsync(new List<string> { "b", "c" });
            var after = AddressablesManager.GetPreloadedGroupKeysForTests();
            Assert.AreEqual(2, after.Count);
            Assert.IsTrue(after.Contains("b"));
            Assert.IsTrue(after.Contains("c"));
        }
        
        [Test]
        public async Task PreloadReleasePreloadSequence()
        {
            await AddressablesManager.StartPreloadGroupAssets(new[] { "a", "b", "c" });
            var initial = AddressablesManager.GetPreloadedGroupKeysForTests();
            Assert.AreEqual(3, initial.Count);
            Assert.IsTrue(initial.Contains("a"));
            Assert.IsTrue(initial.Contains("b"));
            Assert.IsTrue(initial.Contains("c"));

            await AddressablesManager.ReleasePreloadedGroupAsync(new List<string> { "b", "c" });
            var afterRelease = AddressablesManager.GetPreloadedGroupKeysForTests();
            Assert.AreEqual(2, afterRelease.Count);
            Assert.IsTrue(afterRelease.Contains("b"));
            Assert.IsTrue(afterRelease.Contains("c"));

            await AddressablesManager.StartPreloadGroupAssets(new[] { "b" });
            var afterPreload = AddressablesManager.GetPreloadedGroupKeysForTests();
            // Should be unchanged (no-op) because cached keys already contain requested key
            Assert.AreEqual(2, afterPreload.Count);
            Assert.IsTrue(afterPreload.Contains("b"));
            Assert.IsTrue(afterPreload.Contains("c"));
        }

        [Test]
        public async Task StartPreload_DoesNotReload_WhenKeysAlreadyCached()
        {
            await AddressablesManager.StartPreloadGroupAssets(new[] { "a", "b", "c" });
            var initial = AddressablesManager.GetPreloadedGroup();
            var initialCount = initial.Count;
            var initialIds = initial.Select(g => g.GetInstanceID()).ToList();

            await AddressablesManager.StartPreloadGroupAssets(new[] { "b" });
            var after = AddressablesManager.GetPreloadedGroup();

            Assert.AreEqual(initialCount, after.Count);
            CollectionAssert.AreEqual(initialIds, after.Select(g => g.GetInstanceID()).ToList());
        }

        [Test]
        public async Task StartPreload_Again_ClearsCache()
        {
            await AddressablesManager.StartPreloadGroupAssets(new[] { "a", "b", "c" });
            var initial = AddressablesManager.GetPreloadedGroup();
            var initialIds = initial.Select(g => g.GetInstanceID()).ToList();

            await AddressablesManager.StartPreloadGroupAssets(new[] { "d" });
            var after = AddressablesManager.GetPreloadedGroup();

            Assert.AreEqual(1, after.Count);
            //initialIds does not contain after ids
            CollectionAssert.DoesNotContain(initialIds, after.Select(g => g.GetInstanceID()).ToList());
        }
    }
}

