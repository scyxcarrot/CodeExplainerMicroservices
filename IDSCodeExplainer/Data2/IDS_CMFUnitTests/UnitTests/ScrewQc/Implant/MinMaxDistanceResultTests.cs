using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Utilities;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class MinMaxDistanceResultTests
    {
        [TestMethod]
        public void MinMaxDistanceResult_Returns_Empty_Message_When_No_Value_In_Content()
        {
            // arrange
            var content = new MinMaxDistanceContent();            

            //act
            var result = new MinMaxDistanceResult(ImplantScrewQcCheck.MinMaxDistance.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual(string.Empty, actualQcBubbleMessage, "QcBubbleMessage for No Value is incorrect!");
            Assert.AreEqual("<td>/</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for No Value is incorrect!");
        }

        [TestMethod]
        public void MinMaxDistanceResult_Returns_Correct_Messages()
        {
            // arrange
            MinMaxDistanceScrewQcTests.GetSampleCase(out var director, out var casePreferenceDataModel);

            var pastilleDiameter = casePreferenceDataModel.CasePrefData.PastilleDiameter;
            var plateThickness = casePreferenceDataModel.CasePrefData.PlateThicknessMm;
            var plateWidth = casePreferenceDataModel.CasePrefData.PlateWidthMm;

            var dotA = DataModelUtilities.CreateDotPastille(Point3d.Origin, Vector3d.ZAxis, plateThickness, pastilleDiameter);
            var dotB = DataModelUtilities.CreateDotPastille(Point3d.Origin + new Vector3d(10.0, 0, 0), Vector3d.ZAxis, plateThickness, pastilleDiameter);
            var dotC = DataModelUtilities.CreateDotPastille(Point3d.Origin + new Vector3d(0, 10.0, 0), Vector3d.ZAxis, plateThickness, pastilleDiameter);

            var conAB = ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidth, true);
            var conAC = ImplantCreationUtilities.CreateConnection(dotA, dotC, plateThickness, plateWidth, true);

            var connections = new List<IConnection>()
            {
                conAB,
                conAC
            };

            ((ImplantPreferenceModel)casePreferenceDataModel).ImplantDataModel.Update(connections);

            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, casePreferenceDataModel);

            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);

            screws[0].Index = 1;

            //arrange, act, assert
            MinMaxDistanceResult_Returns_Correct_CloseOnly_Messages(screws[0]);
            MinMaxDistanceResult_Returns_Correct_FarOnly_Messages(screws[0]);
            MinMaxDistanceResult_Returns_Correct_CloseFar_Messages(screws[0]);
            MinMaxDistanceResult_Returns_Correct_Multiple_CloseFar_Messages(screws);
        }

        private void MinMaxDistanceResult_Returns_Correct_CloseOnly_Messages(Screw screw1)
        {
            // arrange  
            var content = new MinMaxDistanceContent
            {
                TooCloseScrews = new List<ScrewInfoRecord>
                {
                    new ImplantScrewInfoRecord(screw1)
                }
            };

            //act
            var result = new MinMaxDistanceResult(ImplantScrewQcCheck.MinMaxDistance.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual("Close (1)", actualQcBubbleMessage, "QcBubbleMessage for Close only is incorrect!");
            Assert.AreEqual("<td>close (1)</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for Close only is incorrect!");
        }

        private void MinMaxDistanceResult_Returns_Correct_FarOnly_Messages(Screw screw1)
        {
            // arrange
            var content = new MinMaxDistanceContent
            {
                TooFarScrews = new List<ScrewInfoRecord>
                {
                    new ImplantScrewInfoRecord(screw1)
                }
            };

            //act
            var result = new MinMaxDistanceResult(ImplantScrewQcCheck.MinMaxDistance.ToString(), content);
            var actualMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual("Far (1)", actualMessage, "QcBubbleMessage for Far only is incorrect!");
            Assert.AreEqual("<td>far (1)</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for Far only is incorrect!");
        }

        private void MinMaxDistanceResult_Returns_Correct_CloseFar_Messages(Screw screw1)
        {
            // arrange            
            var content = new MinMaxDistanceContent
            {
                TooCloseScrews = new List<ScrewInfoRecord>
                {
                    new ImplantScrewInfoRecord(screw1)
                },
                TooFarScrews = new List<ScrewInfoRecord>
                {
                    new ImplantScrewInfoRecord(screw1)
                }
            };

            //act
            var result = new MinMaxDistanceResult(ImplantScrewQcCheck.MinMaxDistance.ToString(), content);
            var actualMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual("Close (1)\nFar (1)", actualMessage, "QcBubbleMessage for CloseFar is incorrect!");
            Assert.AreEqual("<td>close (1)<br>far (1)</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for CloseFar is incorrect!");
        }

        private void MinMaxDistanceResult_Returns_Correct_Multiple_CloseFar_Messages(List<Screw> screws)
        {
            // arrange
            screws[0].Index = 1;
            screws[1].Index = 7;
            screws[2].Index = 4;

            var content = new MinMaxDistanceContent
            {
                TooCloseScrews = new List<ScrewInfoRecord>
                {
                    new ImplantScrewInfoRecord(screws[0]),
                    new ImplantScrewInfoRecord(screws[1]),
                    new ImplantScrewInfoRecord(screws[2])
                },
                TooFarScrews = new List<ScrewInfoRecord>
                {
                    new ImplantScrewInfoRecord(screws[0]),
                    new ImplantScrewInfoRecord(screws[1]),
                    new ImplantScrewInfoRecord(screws[2])
                }
            };

            //act
            var result = new MinMaxDistanceResult(ImplantScrewQcCheck.MinMaxDistance.ToString(), content);
            var actualMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual("Close (1,4,7)\nFar (1,4,7)", actualMessage, "QcBubbleMessage for Multiple CloseFar is incorrect!");
            Assert.AreEqual("<td>close (1,4,7)<br>far (1,4,7)</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for Multiple CloseFar is incorrect!");
        }

        [TestMethod]
        public void MinMaxDistanceResult_Serialize_Deserialize_Repetitive_Test()
        {
            // Arrange
            var content = new MinMaxDistanceContent()
            {

                TooCloseScrews = new List<ScrewInfoRecord>()
                {
                    new ImplantScrewInfoRecord(new ImplantScrewSerializableDataModel()
                    {
                        NCase = 1,
                        Index = 1,
                    }),
                    new ImplantScrewInfoRecord(new ImplantScrewSerializableDataModel()
                    {
                        NCase = 2,
                        Index = 1,
                    }),
                    new ImplantScrewInfoRecord(new ImplantScrewSerializableDataModel()
                    {
                        NCase = 1,
                        Index = 2,
                    })
                },
                TooFarScrews = new List<ScrewInfoRecord>()
                {
                    new ImplantScrewInfoRecord(new ImplantScrewSerializableDataModel()
                    {
                        NCase = 3,
                        Index = 1,
                    }),
                    new ImplantScrewInfoRecord(new ImplantScrewSerializableDataModel()
                    {
                        NCase = 4,
                        Index = 1,
                    }),
                    new ImplantScrewInfoRecord(new ImplantScrewSerializableDataModel()
                    {
                        NCase = 3,
                        Index = 2,
                    })
                }
            };

            // Act
            var result = new MinMaxDistanceResult(ImplantScrewQcCheck.MinMaxDistance.ToString(), content);
            var serializableContent = result.GetSerializableScrewQcResult();
            var bson = BsonUtilities.Serialize(serializableContent);
            var deserializableContent = BsonUtilities.Deserialize<MinMaxDistanceSerializableContent>(bson);
            var deserializableResult = new MinMaxDistanceResult(ImplantScrewQcCheck.MinMaxDistance.ToString(), new MinMaxDistanceContent(deserializableContent));

            // Assert
            Assert.AreEqual(result.GetQcBubbleMessage(), deserializableResult.GetQcBubbleMessage(), "QcBubbleMessage aren't match after serialize & deserialize");
            Assert.AreEqual(result.GetQcDocTableCellMessage(), deserializableResult.GetQcDocTableCellMessage(), "QcDocTableCellMessage aren't match after serialize & deserialize");
        }
    }
}
