using IDS.Glenius.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace IDS.Glenius.Operations
{
    public class GeometricalComponentsChecker
    {
        private readonly XmlDocument partNameColorXmlDocument;
        private const string gleniusCaseType = "GR";

        public GeometricalComponentsChecker(string colorsXmlFilePath)
        {
            partNameColorXmlDocument = new XmlDocument();
            partNameColorXmlDocument.Load(colorsXmlFilePath);
        }

        public bool QueryGeometricalComponentsInFiles(IEnumerable<string> filePaths, out List<string> warnings)
        {
            warnings = new List<string>();
            var fileInfos = new List<GleniusImportFileName>();

            if (!filePaths.Any())
            {
                warnings.Add("STL file is not found.");
                return false;
            }

            var dataProvider = new PreopDataProvider();
            fileInfos = dataProvider.GetSTLFileInfos(filePaths);
            if (!AreAllFileNamesValid(fileInfos))
            {
                warnings.Add("Invalid file name.");
                return false;
            }

            var checkComplete = true;
            if (!AreAllComponentsHavingSameCaseID(fileInfos))
            {
                warnings.Add("CaseID Check: Different CaseID found.");
                checkComplete = false;
            }

            if (!AreAllComponentsHavingSameCaseType(fileInfos))
            {
                warnings.Add("Case Type Check: Non-Glenius item found.");
                checkComplete = false;
            }

            if (!IsScapulaBoneAvailable(fileInfos))
            {
                warnings.Add("Scapula Check: Scapula not found.");
                checkComplete = false;
            }
            else if (!AreAllScapulaComponentsHavingSameSide(fileInfos))
            {
                warnings.Add("Component Side Check: Different Scapula Component side found.");
                checkComplete = false;
            }
            else
            {
                var scapulaSide = fileInfos.Where(file => file.ScapulaOrHumerus.Equals(PreopDataProvider.ScapulaAbbr)).Select(file => file.Side).First();
                if (!AreAllHumerusComponentsHavingSameSideAsScapula(fileInfos, scapulaSide))
                {
                    warnings.Add("Component Side Check: Different Humerus Component sides found.");
                    checkComplete = false;
                }
            }

            List<string> notSupportedParts;
            if (!AreAllComponentNamesHasKeyword(fileInfos, out notSupportedParts))
            {
                warnings.Add(string.Format("The following parts are not supported by the software:\n{0}", string.Join(",\n", notSupportedParts)));
                checkComplete = false;
            }

            checkComplete = checkComplete && fileInfos.All(file => file.BuildingBlock.HasValue);

            return checkComplete;
        }

        public bool CheckGeometricalComponentsInFiles(IEnumerable<string> filePaths, out List<string> warnings, string caseId, string defectSide)
        {
            var checkComplete = QueryGeometricalComponentsInFiles(filePaths, out warnings);

            if (checkComplete)
            {
                var dataProvider = new PreopDataProvider();
                var fileInfos = dataProvider.GetSTLFileInfos(filePaths);

                //additional checks
                if (!AreAllComponentsHavingSameProjectCaseIDAsValueGiven(fileInfos, caseId))
                {
                    warnings.Add("Parts with a Case ID that does not match the current project found in the inputs. Correct your inputs and try again.");
                    checkComplete = false;
                }

                if (!AreComponentsHavingSameProjectSideAsValueGiven(fileInfos, defectSide))
                {
                    warnings.Add("The defect scapula in the inputs does not match the defect side of the project. Correct your inputs and try again.");
                    checkComplete = false;
                }
            }

            return checkComplete;
        }

        private bool AreAllFileNamesValid(List<GleniusImportFileName> files)
        {
            return files.All(file => file.IsValid);
        }

        private bool AreAllComponentsHavingSameCaseID(List<GleniusImportFileName> files)
        {
            return files.All(file =>
            {
                return file.CaseID.Equals(files.First().CaseID);
            });
        }

        private bool AreAllComponentsHavingSameCaseType(List<GleniusImportFileName> files)
        {
            return files.All(file =>
            {
                return file.CaseType.Equals(gleniusCaseType);
            });
        }

        private bool AreAllScapulaComponentsHavingSameSide(List<GleniusImportFileName> files)
        {
            var scapulaComponents = files.Where(file => file.ScapulaOrHumerus.Equals(PreopDataProvider.ScapulaAbbr));
            return scapulaComponents.All(file =>
            {
                return file.Side.Equals(scapulaComponents.First().Side);
            });
        }

        private bool AreAllHumerusComponentsHavingSameSideAsScapula(List<GleniusImportFileName> files, string scapulaSide)
        {
            var humerusComponents = files.Where(file => file.ScapulaOrHumerus.Equals(PreopDataProvider.HumerusAbbr));
            return humerusComponents.All(file =>
            {
                return file.Side.Equals(scapulaSide);
            });
        }

        private bool IsScapulaBoneAvailable(List<GleniusImportFileName> files)
        {
            //only scapula is required
            var nodes = partNameColorXmlDocument.SelectNodes($"{PreopDataProvider.GetPartPath()}[{PreopDataProvider.GetTranslateElementValueToUpperCaseXPathFunction("@name")}='{PreopDataProvider.ScapulaPartName}']");
            if (nodes.Count > 0)
            {
                var keywords = nodes.Cast<XmlNode>().Select(node => node.InnerText.ToUpperInvariant());
                return files.Any(file =>
                {
                    var keyword = PreopDataProvider.GetKeyword(file);
                    return keywords.Contains(keyword);
                });
            }
            return false;
        }

        private bool AreAllComponentNamesHasKeyword(List<GleniusImportFileName> files, out List<string> notSupportedParts)
        {
            notSupportedParts = new List<string>();

            foreach (var file in files)
            {
                if (!file.BuildingBlock.HasValue)
                {
                    var keyword = PreopDataProvider.GetKeyword(file);
                    notSupportedParts.Add(keyword);
                }
            }

            return notSupportedParts.Count == 0;
        }

        private bool AreAllComponentsHavingSameProjectCaseIDAsValueGiven(List<GleniusImportFileName> files, string caseId)
        {
            return files.All(file =>
            {
                return caseId.Equals($"{file.CaseID}_{file.CaseType}");
            });
        }

        private bool AreComponentsHavingSameProjectSideAsValueGiven(List<GleniusImportFileName> files, string defectSide)
        {
            return files.All(file =>
            {
                return defectSide.Equals(file.Side.ToUpperInvariant() == AnatomicalSide.LeftAbbr ? AnatomicalSide.Left : AnatomicalSide.Right);
            });
        }
    }
}