using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Newtonsoft.Json;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.CMF.Compatibility
{
    public class ProPlanNameCompatibleHelper
    {
        private readonly Dictionary<string, string> _proPlanImportNameCompatibleDictionary;

        private readonly ProPlanImportComponent _proPlanImportComponentInstance;

        public ProPlanNameCompatibleHelper()
        {
            var resource = new CMFResources();
            var jsonText = File.ReadAllText(resource.ProPlanImportNameCompatibleJsonFile);

            _proPlanImportNameCompatibleDictionary = new Dictionary<string, string>
                (JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText), StringComparer.OrdinalIgnoreCase);
            _proPlanImportComponentInstance = new ProPlanImportComponent();
        }

        public void RenameNewProPlanRhinoObject(RhinoObject proPlanPartRhinoObject, RhinoDoc document, ref string orgProPlanBlockName)
        {
            var newProPlanBlockName = RenameNewProPlanBlockName(orgProPlanBlockName);
            if (orgProPlanBlockName == newProPlanBlockName)
            {
                return;
            }
            proPlanPartRhinoObject.Attributes.Name = newProPlanBlockName;
            proPlanPartRhinoObject.Attributes.UserDictionary.Set(ImplantBuildingBlockProperties.KeyBlockType, newProPlanBlockName);
            proPlanPartRhinoObject.CommitChanges();
            var matIdx = document.Materials.Find(orgProPlanBlockName, true);
            if (matIdx >= 0)
            {
                var mat = document.Materials[matIdx];
                mat.Name = newProPlanBlockName;
                mat.RenderMaterial.Name = newProPlanBlockName;
                mat.CommitChanges();
            }
            orgProPlanBlockName = newProPlanBlockName;
            IDSPluginHelper.WriteLine(LogCategory.Default, $"The ProPlan Part, \"{orgProPlanBlockName}\" have changed to \"{newProPlanBlockName}\"");
        }

        public string RenameNewProPlanBlockName(string oldProPlanBlockName)
        {
            if (!_proPlanImportComponentInstance.GetPurePartNameFromBlockName(oldProPlanBlockName, out var surgeryState,
                out var purePartName))
            {
                return oldProPlanBlockName;
            }
            var correctPartName = FindNewProPlanName(purePartName);
            return _proPlanImportComponentInstance.ConstructProPlanName(surgeryState, correctPartName);
        }
        
        public string RenameNewProPlanPartName(string oldProPlanPartName)
        {
            if (!_proPlanImportComponentInstance.GetPurePartName(oldProPlanPartName, out var surgeryState,
                out var purePartName))
            {
                return oldProPlanPartName;
            }
            var correctPartName = FindNewProPlanName(purePartName);
            return $"{surgeryState}{correctPartName}";
        }

        public IEnumerable<string> RenameMultiNewProPlanPartName(IEnumerable<string> oldProPlanPartNames)
        {
            return oldProPlanPartNames.Select(RenameNewProPlanPartName);
        }

        private string FindNewProPlanName(string oldProPlanName)
        {
            return _proPlanImportNameCompatibleDictionary.ContainsKey(oldProPlanName) ? _proPlanImportNameCompatibleDictionary[oldProPlanName] : oldProPlanName;
        }
    }
}
