using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class SquadValidationTests
    {
        private static Squad Valid()
        {
            return new Squad
            {
                OwnerId = "moiz",
                Tactic = Tactic.Balanced,
                Units = new[]
                {
                    new Placement("aegis", 0, 0),
                    new Placement("charger", 0, 1),
                    new Placement("ripper", 0, 2),
                    new Placement("arcanist", 1, 0),
                    new Placement("windrunner", 1, 1)
                }
            };
        }

        [Test]
        public void WellFormedSquad_Passes()
        {
            Assert.IsTrue(Valid().IsValid(out var error), error);
        }

        [Test]
        public void WrongUnitCount_Fails()
        {
            var squad = Valid();
            squad.Units = new[] { new Placement("aegis", 0, 0) };
            Assert.IsFalse(squad.IsValid(out _));
        }

        [Test]
        public void OverlappingSlots_Fail()
        {
            var squad = Valid();
            squad.Units[1] = new Placement("charger", 0, 0);
            Assert.IsFalse(squad.IsValid(out _));
        }

        [Test]
        public void OffGridPlacement_Fails()
        {
            var squad = Valid();
            squad.Units[0] = new Placement("aegis", 2, 0);
            Assert.IsFalse(squad.IsValid(out _));
        }

        [Test]
        public void DuplicateUnit_Fails()
        {
            var squad = Valid();
            squad.Units[4] = new Placement("aegis", 1, 1);
            Assert.IsFalse(squad.IsValid(out _));
        }
    }
}
