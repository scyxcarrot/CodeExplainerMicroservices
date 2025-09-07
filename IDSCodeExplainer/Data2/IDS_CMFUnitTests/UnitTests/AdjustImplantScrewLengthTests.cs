using IDS.CMF.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class AdjustImplantScrewLengthTests
    {
        [TestMethod]
        public void Nearest_Available_Screw_Length_Should_Be_The_Minimum_When_CurrentLength_Is_Lesser_Than_Minumum()
        {
            //arrange
            var availableScrewLengths = new List<double>
            {
                4,
                5.5,
                7,
                9,
                11,
                13,
                15,
                18
            };
            const double currentLength = 3.0;

            //act
            var nearestAvailableScrewLength = Queries.GetNearestAvailableScrewLength(availableScrewLengths, currentLength);

            //assert
            Assert.AreEqual(nearestAvailableScrewLength, 4);
        }

        [TestMethod]
        public void Nearest_Available_Screw_Length_Should_Be_The_Maximum_When_CurrentLength_Is_More_Than_Maximum()
        {
            //arrange
            var availableScrewLengths = new List<double>
            {
                4,
                5.5,
                7,
                9,
                11,
                13,
                15,
                18
            };
            const double currentLength = 30.0;

            //act
            var nearestAvailableScrewLength = Queries.GetNearestAvailableScrewLength(availableScrewLengths, currentLength);

            //assert
            Assert.AreEqual(nearestAvailableScrewLength, 18);
        }

        [TestMethod]
        public void Nearest_Available_Screw_Length_Should_Be_Rounded_To_The_Upper_Limit_When_CurrentLength_Is_Not_Available()
        {
            //arrange
            var availableScrewLengths = new List<double>
            {
                4,
                5.5,
                7,
                9,
                11,
                13,
                15,
                18
            };
            const double currentLength = 5.8;

            //act
            var nearestAvailableScrewLength = Queries.GetNearestAvailableScrewLength(availableScrewLengths, currentLength);

            //assert
            Assert.AreEqual(nearestAvailableScrewLength, 7);
        }

        [TestMethod]
        public void Nearest_Available_Screw_Length_Should_Be_The_Same_As_CurrentLength_When_CurrentLength_Has_Small_Deviation()
        {
            //arrange
            var availableScrewLengths = new List<double>
            {
                4,
                5.5,
                7,
                9,
                11,
                13,
                15,
                18
            };
            const double currentLength = 5.500000000000506;

            //act
            var nearestAvailableScrewLength = Queries.GetNearestAvailableScrewLength(availableScrewLengths, currentLength);

            //assert
            Assert.AreEqual(nearestAvailableScrewLength, 5.5);
        }
    }
}
