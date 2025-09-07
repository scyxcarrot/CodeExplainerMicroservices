using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    [TestClass]
    public class ScrewQcCheckerTests
    {
        private delegate IImmutableDictionary<Guid, IScrewQcResult> CheckAll(IEnumerable<Screw> screws, out Dictionary<Guid, long> timeTracker);

        private IScrewQcResult MockResult(Screw screw, string checkerName, bool pass = true)
        {
            var mockResult = new Mock<IScrewQcResult>();
            mockResult.Setup(r => r.GetQcBubbleMessage()).Returns(pass ? "" : "Error");
            mockResult.Setup(r => r.GetQcDocTableCellMessage()).Returns(pass ? "" : "Error");
            mockResult.Setup(r => r.GetQcBubbleMessage()).Returns(checkerName);
            return mockResult.Object;
        }

        private IImmutableDictionary<Guid, IScrewQcResult> MockResults(IEnumerable<Screw> screws, string checkerName, bool pass = true)
        {
            var result = screws.ToDictionary(s => s.Id, s => MockResult(s, checkerName, pass));
            return result.ToImmutableDictionary();
        }

        private IScrewQcChecker MockChecker(string checkerName, bool pass = true)
        {
            var mockChecker = new Mock<IScrewQcChecker>();

            mockChecker.SetupGet(c => c.ScrewQcCheckName).Returns(checkerName);
            mockChecker.SetupGet(c => c.ScrewQcCheckTrackerName).Returns(checkerName);
            Dictionary<Guid, long> dummyTimeTracker;
            mockChecker.Setup(c => c.CheckAll(It.IsAny<IEnumerable<Screw>>(), out dummyTimeTracker))
                .Returns(new CheckAll((IEnumerable<Screw> screws, out Dictionary<Guid, long> timeTracker) =>
                {
                    var result = MockResults(screws, checkerName, pass);
                    timeTracker = result.ToDictionary(r => r.Key, _ => (long)0);
                    return result;
                }));
            mockChecker.Setup(c => c.Check(It.IsAny<Screw>())).Returns<Screw>(s => MockResult(s, checkerName, pass));

            return mockChecker.Object;
        }

        [TestMethod]
        public void ScrewQcCheckerManagerCheckAllTest()
        {
            var checker1 = MockChecker("Check1");
            var checker2 = MockChecker("Check2", false);
            var checker3 = MockChecker("Check3", false);

            var checkers = new List<IScrewQcChecker>()
            {
                checker1,
                checker2,
                checker3
            };

            var screws = GuideScrewTestUtilities.CreateGuideScrew(out var director, out _, 2);
            var checkerManager = new ScrewQcCheckerManager(director, checkers);
            var results = checkerManager.CheckAll(screws, out _);

            Assert.AreEqual(2, results.Count);
            foreach (var result in results)
            {
                Assert.IsTrue(screws.Any(s => s.Id == result.Key));
                Assert.AreEqual(3, result.Value.Count());
            }
        }
    }
#endif
}
