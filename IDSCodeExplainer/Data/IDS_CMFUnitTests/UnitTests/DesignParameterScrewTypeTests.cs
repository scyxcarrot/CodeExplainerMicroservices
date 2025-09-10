﻿using System;
using IDS.CMF.Constants;
using IDS.CMF.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class DesignParameterScrewTypeTests
    {
        [Obsolete("Micro/Mini Slotted Screw Type will not be supported as of 4C0501")]
        [TestMethod]
        public void Micro_Slotted_Should_Not_Be_Reformated()
        {
            //arrange
            var screwType = "Micro Slotted";

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Micro Slotted", screwTypeForART);
        }

        [TestMethod]
        public void Micro_Crossed_Should_Be_Reformated()
        {
            //arrange
            var screwType = "Micro Crossed";

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Micro Cross-headed", screwTypeForART);
        }

        [Obsolete("Micro/Mini Slotted Screw Type will not be supported as of 4C0501")]
        [TestMethod]
        public void Mini_Slotted_Should_Not_Be_Reformatted()
        {
            //arrange
            var screwType = ObsoletedScrewStyle.MiniSlotted;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Slotted", screwTypeForART);
        }

        [Obsolete("Micro/Mini Slotted Screw Type will not be supported as of 4C0501")]
        [TestMethod]
        public void Mini_Slotted_Self_Drilling_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ReplacementForObsoletedScrewStyle.MiniSlottedSelfDrillingBarrel;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Slotted", screwTypeForART);
        }

        [Obsolete("Micro/Mini Slotted Screw Type will not be supported as of 4C0501")]
        [TestMethod]
        public void Mini_Slotted_Self_Tapping_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ReplacementForObsoletedScrewStyle.MiniSlottedSelfTappingBarrel;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Slotted", screwTypeForART);
        }

        [TestMethod]
        public void Mini_Crossed_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ObsoletedScrewStyle.MiniCrossed;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Cross-headed", screwTypeForART);
        }

        [TestMethod]
        public void Mini_Crossed_Self_Drilling_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ReplacementForObsoletedScrewStyle.MiniCrossedSelfDrillingBarrel;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Cross-headed", screwTypeForART);
        }

        [TestMethod]
        public void Mini_Crossed_Self_Tapping_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ReplacementForObsoletedScrewStyle.MiniCrossedSelfTappingBarrel;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Cross-headed", screwTypeForART);
        }

        [Obsolete("Micro/Mini Slotted Screw Type will not be supported as of 4C0501")]
        [TestMethod]
        public void Mini_Slotted_Hex_Barrel_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ObsoletedScrewStyle.MiniSlottedHexBarrel;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Slotted", screwTypeForART);
        }

        [Obsolete("Micro/Mini Slotted Screw Type will not be supported as of 4C0501")]
        [TestMethod]
        public void Mini_Slotted_Self_Drilling_Hex_Barrel_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ReplacementForObsoletedScrewStyle.MiniSlottedSelfDrillingHexBarrel;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Slotted", screwTypeForART);
        }

        [Obsolete("Micro/Mini Slotted Screw Type will not be supported as of 4C0501")]
        [TestMethod]
        public void Mini_Slotted_Self_Tapping_Hex_Barrel_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ReplacementForObsoletedScrewStyle.MiniSlottedSelfTappingHexBarrel;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Slotted", screwTypeForART);
        }

        [TestMethod]
        public void Mini_Crossed_Hex_Barrel_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ObsoletedScrewStyle.MiniCrossedHexBarrel;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Cross-headed", screwTypeForART);
        }

        [TestMethod]
        public void Mini_Crossed_Self_Drilling_Hex_Barrel_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ReplacementForObsoletedScrewStyle.MiniCrossedSelfDrillingHexBarrel;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Cross-headed", screwTypeForART);
        }

        [TestMethod]
        public void Mini_Crossed_Self_Tapping_Hex_Barrel_Should_Be_Reformatted()
        {
            //arrange
            var screwType = ReplacementForObsoletedScrewStyle.MiniCrossedSelfTappingHexBarrel;

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("Mini Cross-headed", screwTypeForART);
        }

        [TestMethod]
        public void Matrix_Screws_Should_Be_Reformated()
        {
            //arrange
            var screwType = "Matrix Mandible Ø2.0";

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("MatrixMANDIBLE", screwTypeForART);
        }

        [TestMethod]
        public void Matrix_Screws_With_Hex_Barrel_Should_Be_Reformated()
        {
            //arrange
            var screwType = "Matrix Mandible Ø2.4 Hex Barrel";

            //act
            var screwTypeForART = Queries.GetScrewTypeForDesignParameter(screwType);

            //assert
            Assert.AreEqual("MatrixMANDIBLE", screwTypeForART);
        }
    }
}
