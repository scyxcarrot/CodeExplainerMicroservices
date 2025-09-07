using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.Creators;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class ScrewStampImprintCreatorTests
    {
        private const string MatrixMandible_2_0Key = "Matrix Mandible Ø2.0";
        private const string MatrixMandible_2_4Key = "Matrix Mandible Ø2.4";
        private const string MatrixMidface_1_55Key = "Matrix Midface Ø1.55";
        private const string MatrixOrthognathic_1_85Key = "Matrix Orthognathic Ø1.85";
        private const string MiniSlottedKey = "Mini Slotted";
        private const string MiniSlottedSelfTappingKey = "Mini Slotted Self-Tapping";
        private const string MiniSlottedSelfDrillingKey = "Mini Slotted Self-Drilling";
        private const string MicroSlottedKey = "Micro Slotted";
        private const string MiniCrossedKey = "Mini Crossed";
        private const string MiniCrossedSelfTappingKey = "Mini Crossed Self-Tapping";
        private const string MiniCrossedSelfDrillingKey = "Mini Crossed Self-Drilling";
        private const string MicroCrossedKey = "Micro Crossed";

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void Creator_Throws_Exception_When_Incorrect_ComponentInfo_Given()
        {
            //Arrange
            var console = new TestConsole();
            var mockComponentInfo = new Mock<IComponentInfo>();

            //Act
            var creator = new ScrewStampImprintCreator(console, mockComponentInfo.Object, new Configuration());
            creator.CreateComponentAsync();

            //Assert
            //Exception thrown
        }

        [TestMethod]
        public void Creator_Do_Not_Throw_Exception_When_Correct_ComponentInfo_Given()
        {
            //Arrange
            var console = new TestConsole();
            var componentInfo = new ScrewStampImprintComponentInfo();

            //Act
            var creator = new ScrewStampImprintCreator(console, componentInfo, new Configuration());
            var result = creator.CreateComponentAsync();

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void ScrewStampImprintComponent_Should_Return_Null_If_Over_Thickness()
        {
            // Arrange
            var console = new TestConsole();
            var idsScrewHeadPoint = new IDSPoint3D(1, 1, 0);
            var idsScrewDirection = new IDSVector3D(0, 0, 1);
            var screwType = MatrixMandible_2_0Key;
            var thickness = 2.0;

            // Act
            Dictionary<string, object> intermediateObjects;
            List<string> errorMessages;
            var newScrewStampImprint = CreateScrewStampImprintMesh(console,
                idsScrewHeadPoint, idsScrewDirection, screwType, thickness,
                out intermediateObjects, out errorMessages);

            // Assert
            var errorMessageExpected =
                $"Pastille thickness {thickness} is more than the max allowable thickness of " +
                $"{intermediateObjects["ShapeCreationMaxPastilleThickness"]}, hence it is skipped";
            Assert.IsNull(newScrewStampImprint);
            Assert.AreEqual(1, errorMessages.Count);
            Assert.AreEqual(errorMessageExpected, errorMessages[0]);
        }

        [TestMethod]
        public void Check_ScrewStampImprintComponent_Returns_Correct_IntermediateObjects()
        {
            // Arrange
            var console = new TestConsole();
            var idsScrewHeadPoint = new IDSPoint3D(1, 1, 0);
            var idsScrewDirection = new IDSVector3D(0, 0, 1);

            var screwTypeAndExpectedShapeOffsetDict = new Dictionary<string, double>
            {
                {MatrixMandible_2_0Key, 0.9 },
                {MatrixMandible_2_4Key, 0.9 },
                {MatrixMidface_1_55Key, 0.65 },
                {MatrixOrthognathic_1_85Key, 0.55 },
                {MiniSlottedKey, 1.2 },
                {MiniSlottedSelfTappingKey, 1.2 },
                {MiniSlottedSelfDrillingKey, 1.2 },
                {MicroSlottedKey, 0.48 },
                {MiniCrossedKey, 1.2 },
                {MiniCrossedSelfTappingKey, 1.2 },
                {MiniCrossedSelfDrillingKey, 1.2 },
                {MicroCrossedKey, 0.48 }
            };

            var screwTypeAndExpectedShapeWidthDict = new Dictionary<string, double>
            {
                {MatrixMandible_2_0Key, 5.5 },
                {MatrixMandible_2_4Key, 5.5 },
                {MatrixMidface_1_55Key, 3.0 },
                {MatrixOrthognathic_1_85Key, 3.5 },
                {MiniSlottedKey, 3.0 },
                {MiniSlottedSelfTappingKey, 3.0 },
                {MiniSlottedSelfDrillingKey, 3.0 },
                {MicroSlottedKey, 2.0 },
                {MiniCrossedKey, 3.0 },
                {MiniCrossedSelfTappingKey, 3.0 },
                {MiniCrossedSelfDrillingKey, 3.0 },
                {MicroCrossedKey, 2.0 }
            };

            var screwTypeAndExpectedShapeHeightDict = new Dictionary<string, double>
            {
                {MatrixMandible_2_0Key, 4.0 },
                {MatrixMandible_2_4Key, 4.0 },
                {MatrixMidface_1_55Key, 1.0 },
                {MatrixOrthognathic_1_85Key, 2.5 },
                {MiniSlottedKey, 1.0 },
                {MiniSlottedSelfTappingKey, 1.0 },
                {MiniSlottedSelfDrillingKey, 1.0 },
                {MicroSlottedKey, 1.0 },
                {MiniCrossedKey, 1.0 },
                {MiniCrossedSelfTappingKey, 1.0 },
                {MiniCrossedSelfDrillingKey, 1.0 },
                {MicroCrossedKey, 1.0 }
            };

            var screwTypeAndExpectedSectionHeightRatioDict = new Dictionary<string, double>
            {
                {MatrixMandible_2_0Key, 0.2 },
                {MatrixMandible_2_4Key, 0.2 },
                {MatrixMidface_1_55Key, 0.2 },
                {MatrixOrthognathic_1_85Key, 0.2 },
                {MiniSlottedKey, 0.2 },
                {MiniSlottedSelfTappingKey, 0.2 },
                {MiniSlottedSelfDrillingKey, 0.2 },
                {MicroSlottedKey, 0.2 },
                {MiniCrossedKey, 0.2 },
                {MiniCrossedSelfTappingKey, 0.2 },
                {MiniCrossedSelfDrillingKey, 0.2 },
                {MicroCrossedKey, 0.2 }
            };

            var screwTypeAndExpectedMaxPastilleThicknessDict = new Dictionary<string, double>
            {
                {MatrixMandible_2_0Key, 1.5 },
                {MatrixMandible_2_4Key, 1.5 },
                {MatrixMidface_1_55Key, 1.5 },
                {MatrixOrthognathic_1_85Key, 1.5 },
                {MiniSlottedKey, 1.5 },
                {MiniSlottedSelfTappingKey, 1.5 },
                {MiniSlottedSelfDrillingKey, 1.5 },
                {MicroSlottedKey, 1.5 },
                {MiniCrossedKey, 1.5 },
                {MiniCrossedSelfTappingKey, 1.5 },
                {MiniCrossedSelfDrillingKey, 1.5 },
                {MicroCrossedKey, 1.5 }
            };

            foreach (var screwType in screwTypeAndExpectedMaxPastilleThicknessDict.Keys)
            {
                // Act
                Dictionary<string, object> intermediateObjects;
                List<string> errorMessages;
                var newScrewStampImprint =
                    CreateScrewStampImprintMesh(console,
                        idsScrewHeadPoint, idsScrewDirection, screwType, 0.5,
                        out intermediateObjects, out errorMessages);

                // Assert
                Assert.AreEqual(screwTypeAndExpectedShapeOffsetDict[screwType], intermediateObjects["ShapeOffset"]);
                Assert.AreEqual(screwTypeAndExpectedShapeWidthDict[screwType], intermediateObjects["ShapeWidth"]);
                Assert.AreEqual(screwTypeAndExpectedShapeHeightDict[screwType], intermediateObjects["ShapeHeight"]);
                Assert.AreEqual(screwTypeAndExpectedSectionHeightRatioDict[screwType], intermediateObjects["ShapeSectionHeightRatio"]);
                Assert.AreEqual(screwTypeAndExpectedMaxPastilleThicknessDict[screwType], intermediateObjects["ShapeCreationMaxPastilleThickness"]);
                Assert.AreEqual(0, errorMessages.Count);
            }
        }

        private ScrewStampImprintComponentInfo GetScrewStampImprintComponentInfo(
            IPoint3D screwHeadPoint, IVector3D screwDirection, string screwType, double thickness)
        {
            var component = new ScrewStampImprintComponentInfo
            {
                DisplayName = $"ForUnitTest",
                IsActual = true,
                ScrewType = screwType,
                ScrewHeadPoint = screwHeadPoint,
                ScrewDirection = screwDirection,
                Thickness = thickness
            };

            return component;
        }

        private IMesh CreateScrewStampImprintMesh(IConsole console, 
            IPoint3D screwHeadPoint, IVector3D screwDirection, string screwType, double thickness, 
            out Dictionary<string, object> intermediateObjects, out List<string> errorMessages)
        {
            var componentInfo = GetScrewStampImprintComponentInfo(screwHeadPoint, screwDirection, screwType, thickness);
            var factory = new ImplantFactory(console);
            var taskResult = factory.CreateImplantAsync(componentInfo).Result;
            var screwStampImprint = taskResult.IntermediateMeshes["StampImprint"];
            intermediateObjects = taskResult.IntermediateObjects;
            errorMessages = taskResult.ErrorMessages;

            return screwStampImprint;
        }
    }
}
