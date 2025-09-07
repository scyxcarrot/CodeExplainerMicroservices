using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace IDSEnlightCMFIntegration.Testing.UnitTests
{
    [TestClass]
    public class InternalNameTests
    {
        [TestMethod]
        public void EnlightCMFFile_Contains_All_The_Internal_Names_1()
        {
            var resource = new TestResources();
            EnlightCMFFile_Contains_All_The_Internal_Names(resource.EnlightCmfFullWorkflowFilePath);
        }

        [TestMethod]
        public void EnlightCMFFile_Contains_All_The_Internal_Names_2()
        {
            var resource = new TestResources();
            EnlightCMFFile_Contains_All_The_Internal_Names(resource.EnlightCmfFullWorkflowWithSingleSplitFilePath);
        }

        private void EnlightCMFFile_Contains_All_The_Internal_Names(string enlightCmfFilePath)
        {
            //Arrange
            var resource = new TestResources();
            var jsonText = File.ReadAllText(resource.EnlightCmfInternalNameMappingFilePath);
            var nameMapping = new Dictionary<string, string>(JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText), StringComparer.OrdinalIgnoreCase);

            //Act
            var reader = new EnlightCMFReader(enlightCmfFilePath);

            var allParts = GetAllParts(reader);          

            //Assert
            foreach (var item in allParts)
            {
                if (nameMapping.ContainsKey(item.Name))
                {
                    var expectedInternalName = nameMapping[item.Name];
                    Assert.IsTrue(!string.IsNullOrEmpty(item.InternalName), $"{item.Name} does not have internal name!");

                    Assert.AreEqual(expectedInternalName, item.InternalName, $"Internal name for {expectedInternalName} is not {item.InternalName}!");
                }
            }
        }

        private List<IObjectProperties> GetAllParts(EnlightCMFReader reader)
        {
            var allParts = new List<IObjectProperties>();

            List<StlProperties> stlsWithLabelName;
            reader.GetAllStlProperties(out stlsWithLabelName);

            foreach (var stl in stlsWithLabelName)
            {
                allParts.Add(stl);
            }

            List<OsteotomyProperties> osteotomiesWithLabelName;
            reader.GetAllOsteotomyProperties(out osteotomiesWithLabelName);

            foreach (var osteotomy in osteotomiesWithLabelName)
            {
                allParts.Add(osteotomy);
            }

            List<SplineProperties> splinesWithLabelName;
            reader.GetAllSplineProperties(out splinesWithLabelName);

            foreach (var spline in splinesWithLabelName)
            {
                allParts.Add(spline);
            }

            return allParts;
        }
    }
}
