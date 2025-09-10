using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Utilities;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Globalization;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class OsteotomyDistanceResultTests
    {
        [TestMethod]
        public void OsteotomyDistanceResult_Bubble_Message_Should_Return_Empty_When_Check_IsOk()
        {
            var content = new OsteotomyDistanceContent()
            {
                IsOk = true
            };

            var result = new OsteotomyDistanceResult(ImplantScrewQcCheck.OsteotomyDistance.ToString(), content);
            Assert.AreEqual(string.Empty, result.GetQcBubbleMessage(), "QcBubbleMessage for IsOk=True is incorrect!");
        }

        [TestMethod]
        public void OsteotomyDistanceResult_Bubble_Message_Distance_Value_Should_Be_Two_Decimal_Placed()
        {
            var distanceValue = 5.888888;
            var expectedValue = 5.89;

            var content = new OsteotomyDistanceContent()
            {
                IsOk = false,
                Distance = distanceValue
            };

            var result = new OsteotomyDistanceResult(ImplantScrewQcCheck.OsteotomyDistance.ToString(), content);
            Assert.IsTrue(result.GetQcBubbleMessage().Contains(expectedValue.ToString(CultureInfo.InvariantCulture)), 
                "QcBubbleMessage distance value should be 2 decimal placed!");
        }

        [TestMethod]
        public void OsteotomyDistanceResult_QcDoc_Should_Return_Red_Column_When_Check_Is_Not_Ok()
        {
            var content = new OsteotomyDistanceContent()
            {
                IsOk = false,
                // Random Value to avoid errors
                Distance = 3.000
            };

            var result = new OsteotomyDistanceResult(ImplantScrewQcCheck.OsteotomyDistance.ToString(), content);
            Assert.IsTrue(result.GetQcDocTableCellMessage().Contains("col_red"), "QcDocTableCell column color for IsOk=False is incorrect!");
        }

        [TestMethod]
        public void OsteotomyDistanceResult_QcDoc_Should_Return_Green_Column_When_Check_IsOk()
        {
            var content = new OsteotomyDistanceContent()
            {
                IsOk = true,
                // Random Value to avoid errors
                Distance = 5.00
            };

            var result = new OsteotomyDistanceResult(ImplantScrewQcCheck.OsteotomyDistance.ToString(), content);
            Assert.IsTrue(result.GetQcDocTableCellMessage().Contains("col_green"), "QcDocTableCell column color for IsOk=True is incorrect!");
        }

        [TestMethod]
        public void OsteotomyDistanceResult_QcDoc_Distance_Value_Should_Be_Two_Decimal_Placed()
        {
            var distanceValue = 5.888888;
            var expectedValue = 5.89;

            var content = new OsteotomyDistanceContent()
            {
                IsOk = false,
                Distance = distanceValue
            };

            var result = new OsteotomyDistanceResult(ImplantScrewQcCheck.OsteotomyDistance.ToString(), content);
            Assert.IsTrue(result.GetQcDocTableCellMessage().Contains(expectedValue.ToString(CultureInfo.InvariantCulture)),
                "QcDocTableCell distance value should be 2 decimal placed!");
        }

        [TestMethod]
        public void OsteotomyDistanceResult_QcDoc_Distance_Value_Should_Be_NaN_When_No_Osteotomies()
        {
            var content = new OsteotomyDistanceContent();

            var result = new OsteotomyDistanceResult(ImplantScrewQcCheck.OsteotomyDistance.ToString(), content);
            Assert.IsTrue(result.GetQcDocTableCellMessage().Contains("NaN"), 
                "QcDocTableCell distance value should be NaN when there are no osteotomies!");
        }

        [TestMethod]
        public void OsteotomyDistanceResult_QcDoc_Distance_Value_Should_Be_No_QC_Check_When_Screws_Floating()
        {
            var content = new OsteotomyDistanceContent()
            {
                IsFloatingScrew = true
            };

            var result = new OsteotomyDistanceResult(ImplantScrewQcCheck.OsteotomyDistance.ToString(), content);
            Assert.IsTrue(result.GetQcDocTableCellMessage().Contains("No QC Check"),
                "QcDocTableCell distance value should be No QC Check when screws are floating!");
        }

        [TestMethod]
        public void OsteotomyDistanceContent_Assert_Default_Value()
        {
            var content = new OsteotomyDistanceContent();
            Assert.AreEqual(true, content.IsOk, "Default value for IsOk is incorrect!");
            Assert.AreEqual(Double.NaN, content.Distance, "Default value for Distance is incorrect!");
            Assert.AreEqual(false, content.IsFloatingScrew, "Default value for IsFloatingScrew is incorrect!");
        }

        [TestMethod]
        public void OsteotomyDistanceResult_Serialize_Deserialize_Repetitive_Test()
        {
            // Arrange
            var content = new OsteotomyDistanceContent()
            {
                IsOk = false,
                Distance = 321474.23,
                IsFloatingScrew = false,
                PtFrom = new Point3d(12, 43, 54),
                PtTo = new Point3d(423, 65, 765),
            };

            // Act
            var result = new OsteotomyDistanceResult(ImplantScrewQcCheck.OsteotomyDistance.ToString(), content);
            var serializableContent = result.GetSerializableScrewQcResult();
            var bson = BsonUtilities.Serialize(serializableContent);
            var deserializableContent = BsonUtilities.Deserialize<OsteotomyDistanceSerializableContent>(bson);
            var deserializableResult = new OsteotomyDistanceResult(ImplantScrewQcCheck.OsteotomyDistance.ToString(), new OsteotomyDistanceContent(deserializableContent));

            // Assert
            Assert.AreEqual(result.GetQcBubbleMessage(), deserializableResult.GetQcBubbleMessage(), "QcBubbleMessage aren't match after serialize & deserialize");
            Assert.AreEqual(result.GetQcDocTableCellMessage(), deserializableResult.GetQcDocTableCellMessage(), "QcDocTableCellMessage aren't match after serialize & deserialize");

            PositionTestUtilities.AssertIPoint3DAreEqual(RhinoPoint3dConverter.ToIDSPoint3D(content.PtFrom), deserializableContent.PtFrom, "OsteotomyDistanceContent.PtFrom");
            PositionTestUtilities.AssertIPoint3DAreEqual(RhinoPoint3dConverter.ToIDSPoint3D(content.PtTo), deserializableContent.PtTo, "OsteotomyDistanceContent.PtTo");
        }
    }
}
