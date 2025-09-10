using Microsoft.VisualStudio.TestTools.UnitTesting;
using IDS.Core.Utilities;
using IDS.CMF.Utilities;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class ScrewGaugeUtilitiesTests
    {
        [TestMethod]
        public void Screw_Gauge_Color_Test()
        {
            var screwTypeList = GetScrewTypes();
            var correctScrewGaugeColor = new List<int[]>()
            {
                new int[] {35, 74, 113},
                new int[] {117, 157, 157},
                new int[] {155, 160, 116},
                new int[] {186, 149, 97},
                new int[] {121, 95, 135},
                new int[] {91, 47, 48},
                new int[] {128, 128, 0},
                new int[] {135, 140, 203}
            };

            screwTypeList.ForEach(screwType => ValidateScrewGaugeColor(screwType, correctScrewGaugeColor));
        }

        private void ValidateScrewGaugeColor(string screwType, List<int[]> screwGaugeColors)
        {
            var emptyScrew = new Screw();
            var screwGaugeList = ScrewGaugeUtilities.CreateScrewGauges(emptyScrew, screwType);

            for (int index = 0; index < screwGaugeList.Count; index++)
            {
                CollectionAssert.AreEqual(screwGaugeColors[index], screwGaugeList[index].Color,
                    $"The {screwType} screw with gauge index of {screwGaugeList[index].GaugeIndex} has the wrong color!");
            }
        }

        private List<string> GetScrewTypes()
        {
            var resources = new CMFResources();
            return XmlDocumentUtilities.ExtractValueFromXml(resources.ScrewPartSpecificationFilePath, "Name");
        }
    }

#endif
}