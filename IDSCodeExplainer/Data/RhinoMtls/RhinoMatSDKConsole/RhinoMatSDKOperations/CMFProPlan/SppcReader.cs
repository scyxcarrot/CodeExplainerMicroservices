using Materialise.SDK.MatSAX;
using Materialise.SDK.MDCK.Model.Objects;
using Materialise.SDK.MDCK.Operators;
using System;
using System.Collections.Generic;
using System.IO;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public class SppcReader
    {
        public static bool Read(string filePathToRead, string folderPathToWrite, string meshesName)
        {
            string xmlFilePath = folderPathToWrite + "\\header.xml";
            MSAXReaderWrapper.ExtractXMLHeader(filePathToRead, xmlFilePath);
            string xmlString = System.IO.File.ReadAllText(xmlFilePath);
            var proplanHeaderReader = new ProplanHeaderReader(xmlString);
            var proplanTransformation = proplanHeaderReader.transformationInfo;
            System.IO.File.Delete(xmlFilePath);

            var meshesNamesList = new List<string>(meshesName.Split(null));

            //var meshesName = new List<string> { "00SKU_composite", "00SKU", "00SKU_wrapped", "01SKU_remaining", "00SKU_cranium", "00SKU_cranium_wrapped", "00MAN_composite", "00MAN", "00MAN_wrapped", "00MAN_teeth" };
            var successful = true;
            using (var saxReader = new MSAXReaderWrapper(filePathToRead))
            {
                var dataReader = new ProPlanObjectReader();
                saxReader.Parse(dataReader);
                saxReader.InitHelpersAfterLoading();

                foreach (var model in dataReader.Models)
                {
                    if(meshesNamesList.Contains(model.Name))
                    {
                        var writePath = $@"{folderPathToWrite}\{model.Name}.stl";
                        if (!IO.MDCKConversion.ExportMDCK2StlFile(model, writePath))
                        {
                            successful = false;
                        }
                    }                  
                }

                foreach (var cuttingPath in dataReader.OsteotomyList)
                {
                    var writePath = $@"{folderPathToWrite}\{cuttingPath.Label}.stl";
                    if (!IO.MDCKConversion.ExportMDCK2StlFile(cuttingPath.ReConstruct(proplanTransformation), writePath))
                    {
                        successful = false;
                    }
                }
            }

            return successful;
        }
    }

}

