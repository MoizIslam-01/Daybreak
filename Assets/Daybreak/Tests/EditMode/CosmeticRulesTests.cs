using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class CosmeticRulesTests
    {
        private static WalletDto Wallet(int sparks, params string[] owned) =>
            new WalletDto { sparks = sparks, owned = owned };

        [Test]
        public void Catalog_HasItems_AndLookupWorks()
        {
            Assert.Greater(CosmeticCatalog.All().Count, 0);
            Assert.AreEqual("Tactician", CosmeticCatalog.Get("tactician").name);
            Assert.IsNull(CosmeticCatalog.Get("nope"));
        }

        [Test]
        public void CanBuy_WhenAffordableAndNotOwned()
        {
            var c = CosmeticCatalog.Get("tactician"); // 50
            Assert.IsTrue(CosmeticRules.CanBuy(Wallet(50), c, out _));
            Assert.IsTrue(CosmeticRules.CanBuy(Wallet(100), c, out _));
        }

        [Test]
        public void CannotBuy_WhenTooPoor()
        {
            var c = CosmeticCatalog.Get("warlord"); // 100
            Assert.IsFalse(CosmeticRules.CanBuy(Wallet(99), c, out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void CannotBuy_WhenAlreadyOwned()
        {
            var c = CosmeticCatalog.Get("veteran");
            Assert.IsFalse(CosmeticRules.CanBuy(Wallet(500, "veteran"), c, out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void Owns_DetectsOwnership()
        {
            Assert.IsTrue(CosmeticRules.Owns(Wallet(0, "a", "b"), "b"));
            Assert.IsFalse(CosmeticRules.Owns(Wallet(0, "a"), "z"));
            Assert.IsFalse(CosmeticRules.Owns(Wallet(0), "a"));
        }
    }
}
