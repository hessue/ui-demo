
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockAndDagger.Core;

namespace Tests
{
    public class AddressablesManagerTests
    {
        private IAddressablesManager _addressablesManager;

        [SetUp]
        public void SetUp()
        {
            _addressablesManager = new AddressablesManagerConcreteForTests();
        }

        [TearDown]
        public async Task TearDown()
        {
            await _addressablesManager.ReleasePreloadedGroupAsync(null);
            _addressablesManager = null;
        }

        [Test]
        public async Task StartPreload_WithNull_KeepsEmptyCache()
        {
            var res = await _addressablesManager.StartPreloadGroupAssets(null);
            var keys = new HashSet<string>(res.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);
            Assert.IsNotNull(keys);
            Assert.AreEqual(0, keys.Count);
        }

        [Test]
        public async Task StartPreload_WithABC_PopulatesCache()
        {
            var res = await _addressablesManager.StartPreloadGroupAssets(new[] { "a", "b", "c" });
            var keys = new HashSet<string>(res.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(3, keys.Count);
            Assert.IsTrue(keys.Contains("a"));
            Assert.IsTrue(keys.Contains("b"));
            Assert.IsTrue(keys.Contains("c"));
        }

        [Test]
        public async Task PreloadABC_ThenReleaseToBC_ThenPreloadB_NoopKeepsBC()
        {
            var res1 = await _addressablesManager.StartPreloadGroupAssets(new[] { "a", "b", "c" });
            var keys = new HashSet<string>(res1.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(3, keys.Count);

            var res2 = await _addressablesManager.ReleasePreloadedGroupAsync(new List<string> { "b", "c" });
            var afterRelease = new HashSet<string>(res2.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(2, afterRelease.Count);
            Assert.IsTrue(afterRelease.Contains("b"));
            Assert.IsTrue(afterRelease.Contains("c"));

            var res3 = await _addressablesManager.StartPreloadGroupAssets(new[] { "b" });
            var afterPreload = new HashSet<string>(res3.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(2, afterPreload.Count);
            Assert.IsTrue(afterPreload.Contains("b"));
            Assert.IsTrue(afterPreload.Contains("c"));
        }

        [Test]
        public async Task ReleasePreloadedGroupAsync_WithSameValues_IsNoop()
        {
            var res1 = await _addressablesManager.StartPreloadGroupAssets(new[] { "b", "c" });
            var before = new HashSet<string>(res1.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(2, before.Count);

            var res2 = await _addressablesManager.ReleasePreloadedGroupAsync(new List<string> { "b", "c" });
            var after = new HashSet<string>(res2.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(2, after.Count);
            Assert.IsTrue(after.Contains("b"));
            Assert.IsTrue(after.Contains("c"));
        }

        [Test]
        public async Task PreloadReleasePreloadSequence()
        {
            var res1 = await _addressablesManager.StartPreloadGroupAssets(new[] { "a", "b", "c" });
            var initial = new HashSet<string>(res1.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(3, initial.Count);
            Assert.IsTrue(initial.Contains("a"));
            Assert.IsTrue(initial.Contains("b"));
            Assert.IsTrue(initial.Contains("c"));

            var res2 = await _addressablesManager.ReleasePreloadedGroupAsync(new List<string> { "b", "c" });
            var afterRelease = new HashSet<string>(res2.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(2, afterRelease.Count);
            Assert.IsTrue(afterRelease.Contains("b"));
            Assert.IsTrue(afterRelease.Contains("c"));

            var res3 = await _addressablesManager.StartPreloadGroupAssets(new[] { "b" });
            var afterPreload = new HashSet<string>(res3.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(2, afterPreload.Count);
            Assert.IsTrue(afterPreload.Contains("b"));
            Assert.IsTrue(afterPreload.Contains("c"));
        }

        [Test]
        public async Task StartPreload_DoesNotReload_WhenKeysAlreadyCached()
        {
            var res1 = await _addressablesManager.StartPreloadGroupAssets(new[] { "a", "b", "c" });
            var initial = new HashSet<string>(res1.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);

            var res2 = await _addressablesManager.StartPreloadGroupAssets(new[] { "b" });
            var after = new HashSet<string>(res2.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);

            Assert.AreEqual(initial.Count, after.Count);
            Assert.IsTrue(initial.SetEquals(after));
        }

        [Test]
        public async Task StartPreload_Again_ClearsCache()
        {
            var res1 = await _addressablesManager.StartPreloadGroupAssets(new[] { "a", "b", "c" });
            var initial = new HashSet<string>(res1.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);

            var res2 = await _addressablesManager.StartPreloadGroupAssets(new[] { "d" });
            var after = new HashSet<string>(res2.PreloadedGroupKeys, System.StringComparer.OrdinalIgnoreCase);

            Assert.IsTrue(initial.Contains("a"));
            Assert.AreEqual(1, after.Count);
            Assert.IsTrue(after.Contains("d"));
            Assert.IsFalse(after.Contains("a"));
        }
    }
}

