using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class TeamRulesTests
    {
        [Test]
        public void Name_IsSanitizedAndClamped()
        {
            Assert.AreEqual("The Night Owls", TeamRules.SanitizeName("  The   Night\tOwls  "));
            Assert.AreEqual(TeamRules.DefaultName, TeamRules.SanitizeName("   "));
            Assert.LessOrEqual(TeamRules.SanitizeName("A really really long team name").Length, TeamRules.MaxNameLength);
        }

        [Test]
        public void Slug_IsUrlSafe()
        {
            Assert.AreEqual("the-night-owls", TeamRules.Slug("The Night Owls!"));
            Assert.AreEqual("team-42", TeamRules.Slug("  Team #42  "));
            Assert.AreEqual("team", TeamRules.Slug("!!!"));
        }

        [Test]
        public void Slug_HasNoLeadingOrTrailingDashes()
        {
            var s = TeamRules.Slug("--Hello--");
            Assert.IsFalse(s.StartsWith("-"));
            Assert.IsFalse(s.EndsWith("-"));
            Assert.AreEqual("hello", s);
        }
    }
}
