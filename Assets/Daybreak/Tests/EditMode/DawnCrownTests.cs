using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class DawnCrownTests
    {
        [Test]
        public void DawnCrownId_IsPerWeek_AndRecognized()
        {
            var id = CosmeticCatalog.DawnCrownId(87);
            Assert.AreEqual("dawn_crown_w87", id);
            Assert.IsTrue(CosmeticCatalog.IsDawnCrown(id));
            Assert.IsFalse(CosmeticCatalog.IsDawnCrown("tactician"));
            Assert.IsFalse(CosmeticCatalog.IsDawnCrown(null));
        }

        [Test]
        public void DisplayName_HandlesCrownsAndCatalogItems()
        {
            Assert.AreEqual("Dawn Crown (Wk 87)", CosmeticCatalog.DisplayName("dawn_crown_w87"));
            Assert.AreEqual("Tactician", CosmeticCatalog.DisplayName("tactician"));
            Assert.AreEqual("", CosmeticCatalog.DisplayName("unknown_id"));
            Assert.AreEqual("", CosmeticCatalog.DisplayName(null));
        }

        [Test]
        public void DawnCrown_IsNotInThePurchasableCatalog()
        {
            // The crown can't be bought — it only exists as a granted, dated id.
            Assert.IsNull(CosmeticCatalog.Get(CosmeticCatalog.DawnCrownId(1)));
        }
    }
}
