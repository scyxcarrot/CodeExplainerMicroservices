using IDS.CMF.ScrewQc;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ImplantScrewVicinityResultTests
    {
        // this class is to check the content and result class to see output correct
        [TestMethod]
        public void ImplantScrewVicinityResult_Returns_Slash_And_Empty_If_No_Intersect()
        {
            // arrange
            var content = new ImplantScrewVicinityContent();

            //act
            var result = new ImplantScrewVicinityResult(ImplantScrewQcCheck.ImplantScrewVicinity.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual(string.Empty, actualQcBubbleMessage, "QcBubbleMessage when there are no screws in vicinity is incorrect");
            Assert.AreEqual("<td>/</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage when there are no screws in vicinity is incorrect!");
        }

        [TestMethod]
        public void ImplantScrewVicinityResult_Returns_Implant_Screw_Number_And_Intersect_If_Intersect()
        {
            // arrange
            var content = new ImplantScrewVicinityContent();

            var sampleScrew = ImplantScrewTestUtilities.CreateScrew(testPoint: new IDSPoint3D(1, 1, 2), Transform.Identity, true);
            sampleScrew.Index = 1;
            content.ScrewsInVicinity.Add(new ImplantScrewInfoRecord(sampleScrew));

            //act
            var result = new ImplantScrewVicinityResult(ImplantScrewQcCheck.ImplantScrewVicinity.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual("Intersect (1.I1)", actualQcBubbleMessage, "QcBubbleMessage when there are 1 screw in vicinity is incorrect");
            Assert.AreEqual("<td>1.I1</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage when there are 1 screw in vicinity is incorrect!");

        }

        [TestMethod]
        public void ImplantScrewVicinityResult_Returns_Implant_Screw_Number_And_Intersect_If_Double_Intersect()
        {
            // arrange
            var content = new ImplantScrewVicinityContent();
            var screwXCoordinates = new List<double> {1, 2};

            var screwHeadPts = new List<IDSPoint3D>();
            foreach (var xCoordinate in screwXCoordinates)
            {
                var screwHeadPt = new IDSPoint3D(xCoordinate, 1, 2);
                screwHeadPts.Add(screwHeadPt);
            }

            var sampleScrews = ImplantScrewTestUtilities.CreateMultipleScrews(testPoints: screwHeadPts, Transform.Identity, true);
            var index = 1;
            foreach (var screw in sampleScrews)
            {
                screw.Index = index;
                index++;
                content.ScrewsInVicinity.Add(new ImplantScrewInfoRecord(screw));
            }

            //act
            var result = new ImplantScrewVicinityResult(ImplantScrewQcCheck.ImplantScrewVicinity.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual("Intersect (1.I1,2.I1)", actualQcBubbleMessage, "QcBubbleMessage when there are 2 screws in vicinity is incorrect");
            Assert.AreEqual("<td>1.I1,2.I1</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage when there are 2 screws in vicinity incorrect!");
        }

        [TestMethod]
        public void ImplantScrewVicinityResult_Returns_Implant_Screw_Number_And_Intersect_If_Double_Intersect_From_Diff_Case()
        {
            // arrange
            var content = new ImplantScrewVicinityContent();
            var screwXCoordinates = new List<double> { 1, 2 };

            var screwHeadPts = new List<IDSPoint3D>();
            foreach (var xCoordinate in screwXCoordinates)
            {
                var screwHeadPt = new IDSPoint3D(xCoordinate, 1, 2);
                screwHeadPts.Add(screwHeadPt);
            }

            var sampleScrews = ImplantScrewTestUtilities.CreateMultipleScrewsAndImplants(
                testPoints: screwHeadPts, Transform.Identity, 2, true);

            var index = 1;
            foreach (var screw in sampleScrews)
            {
                screw.Index = index;
                index++;

                content.ScrewsInVicinity.Add(new ImplantScrewInfoRecord(screw));

            }

            //act
            var result = new ImplantScrewVicinityResult(ImplantScrewQcCheck.ImplantScrewVicinity.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual("Intersect (1.I1,2.I2)", actualQcBubbleMessage, "QcBubbleMessage when there are multiple implant case screws in vicinity is incorrect");
            Assert.AreEqual("<td>1.I1,2.I2</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage when there are multiple implant case screws in vicinity incorrect!");

        }

        [TestMethod]
        public void ImplantScrewVicinityResult_Serialize_Deserialize_Repetitive_Test()
        {
            // Arrange
            var content = new ImplantScrewVicinityContent()
            {
                ScrewsInVicinity = new List<ImplantScrewInfoRecordV2>()
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
                }
            };

            // Act
            var result = new ImplantScrewVicinityResult(ImplantScrewQcCheck.ImplantScrewVicinity.ToString(), content);
            var serializableContent = result.GetSerializableScrewQcResult();
            var bson = BsonUtilities.Serialize(serializableContent);
            var deserializableContent = BsonUtilities.Deserialize<ImplantScrewVicinitySerializableContent>(bson);
            var deserializableResult = new ImplantScrewVicinityResult(ImplantScrewQcCheck.ImplantScrewVicinity.ToString(), new ImplantScrewVicinityContent(deserializableContent));

            // Assert
            Assert.AreEqual(result.GetQcBubbleMessage(), deserializableResult.GetQcBubbleMessage(), "QcBubbleMessage aren't match after serialize & deserialize");
            Assert.AreEqual(result.GetQcDocTableCellMessage(), deserializableResult.GetQcDocTableCellMessage(), "QcDocTableCellMessage aren't match after serialize & deserialize");
        }
    }
}
