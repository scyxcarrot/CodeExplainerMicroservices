using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Visualization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    [TestClass]
    public class ScrewQcBubbleManagerTest
    {
        [TestMethod]
        public void ScrewQcBubbleVisualTest()
        {
            var screws = GuideScrewTestUtilities.CreateGuideScrew(out _, out _, 2);
            var count = 1;
            screws.ForEach(s => s.Index = count++);
            var screwInfoRecords = screws.Select(s => new GuideScrewInfoRecord(s)).Cast<ScrewInfoRecord>().ToList();
            var bubbles = screwInfoRecords.Select(r =>
                new ScrewQcBubble(r, new List<string>().ToImmutableList())).ToImmutableList();

            var mockExtraDisplay = new Mock<IDisplay>();
            mockExtraDisplay.SetupAllProperties();
            var allMockExtraDisplays = new List<IDisplay>() { mockExtraDisplay.Object }.ToImmutableList();

            var screwQcBubbleManager = new ScrewQcBubbleManager(allMockExtraDisplays);
            screwQcBubbleManager.UpdateScrewBubbles(bubbles);

            // Initially all bubble and display is hide
            Assert.IsFalse(bubbles.Any(b => b.Enabled));
            Assert.IsFalse(allMockExtraDisplays.Any(b => b.Enabled));
            Assert.IsFalse(screwQcBubbleManager.IsShow());

            // All bubble and display was shown
            screwQcBubbleManager.Show();
            Assert.IsTrue(bubbles.All(b => b.Enabled));
            Assert.IsTrue(allMockExtraDisplays.All(b => b.Enabled));
            Assert.IsTrue(screwQcBubbleManager.IsShow());

            // All bubble and display was hidden
            screwQcBubbleManager.Hide();
            Assert.IsFalse(bubbles.Any(b => b.Enabled));
            Assert.IsFalse(allMockExtraDisplays.Any(b => b.Enabled));
            Assert.IsFalse(screwQcBubbleManager.IsShow());

            // All bubble and display was shown
            screwQcBubbleManager.Show();
            Assert.IsTrue(bubbles.All(b => b.Enabled));
            Assert.IsTrue(allMockExtraDisplays.All(b => b.Enabled));
            Assert.IsTrue(screwQcBubbleManager.IsShow());

            // All bubble and display was hidden and remove from the list 
            screwQcBubbleManager.Clear();
            Assert.IsFalse(bubbles.Any(b => b.Enabled));
            Assert.IsFalse(allMockExtraDisplays.Any(b => b.Enabled));
            Assert.IsFalse(screwQcBubbleManager.IsShow());

            // All bubble and display won't manage by bubble manager
            screwQcBubbleManager.Show();
            Assert.IsFalse(bubbles.Any(b => b.Enabled));
            Assert.IsFalse(allMockExtraDisplays.Any(b => b.Enabled));
            Assert.IsFalse(screwQcBubbleManager.IsShow());
        }
    }
#endif
}
