using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    [TestClass]
    public class ScrewBubbleTests
    {
        [TestMethod]
        public void ScrewQcBubbleLabelTest()
        {
            var screws = GuideScrewTestUtilities.CreateGuideScrew(out _, out _, 2);
            var count = 1;
            screws.ForEach(s => s.Index = count++);
            var screwInfoRecords = screws.Select(s => new GuideScrewInfoRecord(s)).Cast<ScrewInfoRecord>().ToList();
            var bubble = new ScrewQcBubble(screwInfoRecords.ToImmutableList(), new List<string>().ToImmutableList());
            var sortedScrewInfoRecords = ScrewQcUtilitiesV2.SortScrewInfoRecords(screwInfoRecords);
            Assert.AreEqual(string.Join(",", sortedScrewInfoRecords.Select(r => r.GetScrewNumberForScrewQcBubble())), bubble.Label);
        }

        [TestMethod]
        public void ScrewQcBubblePassTest()
        {
            var screw = GuideScrewTestUtilities.CreateGuideScrew(out _, out _).First();
            screw.Index = 1;
            var screwInfoRecord = new GuideScrewInfoRecord(screw);
            var bubble = new ScrewQcBubble(screwInfoRecord, new List<string>().ToImmutableList());

            Assert.AreEqual(ScrewQcBubble.PassColor, bubble.BubbleColor);
        }

        [TestMethod]
        public void ScrewQcBubbleFailTest()
        {
            var screw = GuideScrewTestUtilities.CreateGuideScrew(out _, out _).First();
            screw.Index = 1;
            var screwInfoRecord = new GuideScrewInfoRecord(screw);
            var bubble = new ScrewQcBubble(screwInfoRecord, new List<string>(){"Dummy"}.ToImmutableList());

            Assert.AreEqual(ScrewQcBubble.FailColor, bubble.BubbleColor);
        }
    }
#endif
}
