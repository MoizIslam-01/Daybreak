using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class ProfileRulesTests
    {
        [Test]
        public void EmptyName_FallsBackToDefault()
        {
            Assert.AreEqual(ProfileRules.DefaultName, ProfileRules.SanitizeName(null));
            Assert.AreEqual(ProfileRules.DefaultName, ProfileRules.SanitizeName("   "));
        }

        [Test]
        public void Name_IsTrimmedAndClamped()
        {
            Assert.AreEqual("Moiz", ProfileRules.SanitizeName("  Moiz  "));
            var longName = ProfileRules.SanitizeName("ThisNameIsWayTooLongToKeep");
            Assert.LessOrEqual(longName.Length, ProfileRules.MaxNameLength);
        }

        [Test]
        public void Name_CollapsesInnerWhitespace_AndStripsControlChars()
        {
            Assert.AreEqual("Sam I Am", ProfileRules.SanitizeName("Sam   I\tAm"));
            Assert.AreEqual("AB", ProfileRules.SanitizeName("AB"));
        }

        [Test]
        public void Color_ValidatesHex()
        {
            Assert.AreEqual("#4A90D9", ProfileRules.SanitizeColor("#4a90d9"));
            Assert.AreEqual(ProfileRules.DefaultColorHex, ProfileRules.SanitizeColor("blue"));
            Assert.AreEqual(ProfileRules.DefaultColorHex, ProfileRules.SanitizeColor("#12345"));
            Assert.AreEqual(ProfileRules.DefaultColorHex, ProfileRules.SanitizeColor("#GGGGGG"));
        }

        [Test]
        public void Sanitize_FillsDefaults_ForNullProfile()
        {
            var p = ProfileRules.Sanitize(null);
            Assert.AreEqual(ProfileRules.DefaultName, p.displayName);
            Assert.AreEqual(ProfileRules.DefaultColorHex, p.colorHex);
            Assert.AreEqual("", p.teamId);
        }
    }
}
