using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class SquadDraftTests
    {
        [Test]
        public void PlacingFiveUnits_CompletesAndBuilds()
        {
            var d = new SquadDraft { Tactic = Tactic.Berserk };
            Assert.IsTrue(d.Place(0, "aegis", out _));
            Assert.IsTrue(d.Place(1, "warhound", out _));
            Assert.IsTrue(d.Place(2, "ripper", out _));
            Assert.IsTrue(d.Place(3, "arcanist", out _));
            Assert.IsTrue(d.Place(4, "flicker", out _));

            Assert.IsTrue(d.IsComplete);
            Assert.IsTrue(d.TryBuild("me", out var squad, out var err), err);
            Assert.AreEqual(5, squad.Units.Length);
            Assert.AreEqual(Tactic.Berserk, squad.Tactic);
        }

        [Test]
        public void RowAndCol_MapFromSlotIndex()
        {
            var d = new SquadDraft();
            d.Place(SquadDraft.SlotIndex(1, 2), "flicker", out _); // back row, col 2
            d.Place(0, "aegis", out _);
            d.Place(1, "warhound", out _);
            d.Place(2, "ripper", out _);
            d.Place(3, "arcanist", out _);

            Assert.IsTrue(d.TryBuild("me", out var squad, out _));
            var flicker = System.Array.Find(squad.Units, p => p.UnitId == "flicker");
            Assert.AreEqual(1, flicker.Row);
            Assert.AreEqual(2, flicker.Col);
        }

        [Test]
        public void CannotPlaceSameUnitTwice()
        {
            var d = new SquadDraft();
            Assert.IsTrue(d.Place(0, "aegis", out _));
            Assert.IsFalse(d.Place(1, "aegis", out var err));
            Assert.IsNotEmpty(err);
        }

        [Test]
        public void CannotExceedFiveUnits()
        {
            var d = new SquadDraft();
            d.Place(0, "aegis", out _);
            d.Place(1, "warhound", out _);
            d.Place(2, "ripper", out _);
            d.Place(3, "arcanist", out _);
            d.Place(4, "flicker", out _);
            // Slot 5 is the last empty one; filling it would make six.
            Assert.IsFalse(d.Place(5, "charger", out var err));
            Assert.IsNotEmpty(err);
        }

        [Test]
        public void OverwritingAFilledSlot_KeepsCountAtFive()
        {
            var d = new SquadDraft();
            d.Place(0, "aegis", out _);
            d.Place(1, "warhound", out _);
            d.Place(2, "ripper", out _);
            d.Place(3, "arcanist", out _);
            d.Place(4, "flicker", out _);
            Assert.IsTrue(d.Place(0, "charger", out _)); // replace aegis in slot 0
            Assert.AreEqual(5, d.FilledCount);
            Assert.IsFalse(d.Contains("aegis"));
            Assert.IsTrue(d.Contains("charger"));
        }

        [Test]
        public void IncompleteDraft_DoesNotBuild()
        {
            var d = new SquadDraft();
            d.Place(0, "aegis", out _);
            Assert.IsFalse(d.TryBuild("me", out _, out var err));
            Assert.IsNotEmpty(err);
        }

        [Test]
        public void ClearUnit_RemovesItFromItsSlot()
        {
            var d = new SquadDraft();
            d.Place(2, "ripper", out _);
            Assert.IsTrue(d.Contains("ripper"));
            d.ClearUnit("ripper");
            Assert.IsFalse(d.Contains("ripper"));
            Assert.AreEqual(0, d.FilledCount);
        }
    }
}
