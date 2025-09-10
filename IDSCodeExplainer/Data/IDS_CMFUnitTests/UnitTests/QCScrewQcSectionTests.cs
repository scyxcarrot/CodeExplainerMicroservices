using IDS.CMF.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class QCScrewQcSectionTests
    {
        [TestMethod]
        public void Cell_For_Implant_Screw_Angle_Less_Than_15_Is_Green()
        {
            //arrange
            var infoData = new QcDocBaseScrewInfoData
            {
                Angle = 14.9
            };
            var resultInfo = new QcDocScrewAndResultsInfoModel(infoData, new List<string>());
            var expectedCellColor = "col_green";

            //act
            var generatedHtmlString = QCScrewQcSection.GenerateImplantScrewInfoTableRow(resultInfo);

            //assert
            AssertForAngleCell(generatedHtmlString, resultInfo.Angle, expectedCellColor);
        }

        [TestMethod]
        public void Cell_For_Implant_Screw_Angle_More_Than_15_And_Less_Than_20_Is_Orange()
        {
            //arrange
            var infoData = new QcDocBaseScrewInfoData
            {
                Angle = 18.9
            };
            var resultInfo = new QcDocScrewAndResultsInfoModel(infoData, new List<string>());
            var expectedCellColor = "col_orange";

            //act
            var generatedHtmlString = QCScrewQcSection.GenerateImplantScrewInfoTableRow(resultInfo);

            //assert
            AssertForAngleCell(generatedHtmlString, resultInfo.Angle, expectedCellColor);
        }

        [TestMethod]
        public void Cell_For_Implant_Screw_Angle_More_Than_20_Is_Red()
        {
            //arrange
            var infoData = new QcDocBaseScrewInfoData
            {
                Angle = 20.1
            };
            var resultInfo = new QcDocScrewAndResultsInfoModel(infoData, new List<string>());
            var expectedCellColor = "col_red";

            //act
            var generatedHtmlString = QCScrewQcSection.GenerateImplantScrewInfoTableRow(resultInfo);

            //assert
            AssertForAngleCell(generatedHtmlString, resultInfo.Angle, expectedCellColor);
        }

        private void AssertForAngleCell(string htmlString, string expectedAngleValue, string expectedClassValue)
        {
            var cells = GetCellsFromHtmlString(htmlString);

            var cellIndexForAngle = 2;
            var angleCell = cells[cellIndexForAngle];
            var angleValueString = angleCell.Substring(angleCell.IndexOf(">") + 1);
            //this is just to check that we are indeed testing on the correct cell (cells of index: cellIndexForAngle)
            Assert.AreEqual(expectedAngleValue, angleValueString);

            Assert.IsTrue(angleCell.Contains($"class=\"{expectedClassValue}\""));
        }

        private List<string> GetCellsFromHtmlString(string htmlString)
        {
            //returns cells string without closing tag (</td>)

            var rowTagRemovedHtmlString = htmlString.Replace("<tr>", "").Replace("</tr>", "");

            var regex = new Regex("</td>");
            var cellArray = regex.Split(rowTagRemovedHtmlString);

            return cellArray.ToList();
        }
    }

#endif
}