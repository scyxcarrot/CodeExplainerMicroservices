using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.Operations
{
    public class ImplantSupportImporter: BasePartsImporter
    {
        #region Regex Parameters
        // Note: If any format for the file name of implant support changed, might need to edit those parameters
        private const string ImplantSupportPattern = "^ImplantSupport_I([0-9]{1,2})$";
        private const int CaseIndexRegexGroupIndex = 1;
        #endregion

        public ImplantSupportImporter(CMFImplantDirector director) : base(director)
        {
        }

        public bool ContainsImplantSupportMesh(string[] filePaths)
        {
            return FilterImplantSupportNotMatchCaseIndex(GetImplantSupportsPartsWithCaseIndexes(filePaths)).Any();
        }

        private IDictionary<string, int> FilterImplantSupportNotMatchCaseIndex(
            IDictionary<string, int> implantSupportPartsInfo)
        {
            var existingCasesIndex =
                director.CasePrefManager.CasePreferences.Select(c => c.NCase).ToList();

            var newImplantSupportPartsInfo = new Dictionary<string, int>();
            foreach (var implantSupportPartInfo in implantSupportPartsInfo)  
            {
                if(existingCasesIndex.Contains(implantSupportPartInfo.Value))
                {
                    newImplantSupportPartsInfo.Add(implantSupportPartInfo.Key, implantSupportPartInfo.Value);
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Case {implantSupportPartInfo.Value} not exist," +
                                                                   $" the {implantSupportPartInfo.Key} will not be import");
                }
            }
            return newImplantSupportPartsInfo;
        }

        private static bool ExtractImplantSupportInfo(string implantSupportFileName, out int caseIndex)
        {
            caseIndex = -1;
            var match = Regex.Match(implantSupportFileName, ImplantSupportPattern);
            if (!match.Success)
            {
                return false;
            }

            caseIndex = Convert.ToInt32(match.Groups[CaseIndexRegexGroupIndex].Value);
            return true;
        }

        public bool ImportImplantSupportMesh(string[] filePaths, out List<Guid> importedSupportsGuid)
        {
            importedSupportsGuid = new List<Guid>();
            var partNamesWithFilePathsMap = GetPartNamesWithFilePaths(filePaths);
            var implantSupportsPartsWithCaseIndexesMap = GetImplantSupportsPartsWithCaseIndexes(filePaths);
            implantSupportsPartsWithCaseIndexesMap = FilterImplantSupportNotMatchCaseIndex(implantSupportsPartsWithCaseIndexesMap);
            var supportsMeshes = new Dictionary<int, Mesh>();

            foreach (var implantSupportsPartsWithCaseIndexesKeyPair in implantSupportsPartsWithCaseIndexesMap)
            {
                var partName = implantSupportsPartsWithCaseIndexesKeyPair.Key;
                var caseIndex = implantSupportsPartsWithCaseIndexesKeyPair.Value;
                var filePath = partNamesWithFilePathsMap[partName];
                if (!ImportMeshWithFilePath(filePath, out var mesh))
                {
                    return false;
                }
                supportsMeshes.Add(caseIndex, mesh);
            }

            var imported = true;
            foreach (var supportsMesh in supportsMeshes)
            {
                if (ReplaceImplantSupport(supportsMesh.Key, supportsMesh.Value, out var importedSupportGuid))
                {
                    importedSupportsGuid.Add(importedSupportGuid);
                    continue;
                }
                imported = false;
                IDSPluginHelper.WriteLine(LogCategory.Error, "Something went wrong while importing implant support mesh");
                break;
            }

            return imported;
        }

        public bool ReplaceImplantSupport(int caseIndex, Mesh mesh, out Guid supportGuid)
        {
            var implantSupportReplacement = new ImplantSupportReplacement(director);
            var casePref = director.CasePrefManager.GetCaseWithCaseIndex(caseIndex);
            return implantSupportReplacement.ReplaceImplantSupport(casePref, mesh, true, out supportGuid);
        }

        private IDictionary<string, string> GetPartNamesWithFilePaths(string[] filePaths)
        {
            return filePaths.ToDictionary(f => GetPartName(f), f=>f);
        }

        private IDictionary<string, int> GetImplantSupportsPartsWithCaseIndexes(string[] filePaths)
        {
            var partNames = GetPartNamesWithFilePaths(filePaths).Keys;
            return FilterImplantSupportsParts(partNames);
        }

        public string[] GetAllStlFilePaths(string folderPath)
        {
            var directory = new DirectoryInfo(folderPath);
            return directory.GetFiles("*.stl", SearchOption.TopDirectoryOnly).Select(
                file => file.FullName).ToArray();
        }

        public static IDictionary<string, int> FilterImplantSupportsParts(IEnumerable<string> partsName)
        {
            var implantSupportPartsInfo = new Dictionary<string, int>();
            foreach (var partName in partsName)
            {
                if(ExtractImplantSupportInfo(partName, out var caseIndex))
                {
                    implantSupportPartsInfo.Add(partName, caseIndex);
                }
            }
            return implantSupportPartsInfo;
        }

        public static IEnumerable<string> FilterImplantSupportsPartsName(IEnumerable<string> partsName)
        {
            return FilterImplantSupportsParts(partsName).Keys;
        }
    }
}
