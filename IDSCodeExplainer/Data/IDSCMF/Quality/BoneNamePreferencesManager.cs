using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using Newtonsoft.Json;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class BoneNamePreferencesManager
    {
        public class OptionalBoneNameHandler
        {
            public string Name { get; set; }
            public List<string> MustHave { get; set; }
            public List<string> MustNotHave { get; set; }

            public bool TryMatchOptionalBoneName(IEnumerable<string> relatedBoneName, out string namePreference)
            {
                namePreference = null;

                if (relatedBoneName == null || !relatedBoneName.Any()|| (!MustHave.Any() && !MustNotHave.Any()))
                {
                    return false;
                }
                
                var match = true;
                foreach (var mustHave in MustHave)
                {
                    match &= relatedBoneName.Contains(mustHave);
                }

                foreach (var mustNotHave in MustNotHave)
                {
                    match &= !relatedBoneName.Contains(mustNotHave);
                }

                if (match)
                {
                    namePreference = Name;
                }

                return match;
            }
        }

        public class BoneNamePreferencesConfigBlock
        {
            public string SubLayer { get; set; }
            public bool OptionalSegmentation { get; set; }
            public string Name { get; set; }
            public List<OptionalBoneNameHandler> OptionalNames { get; set; }

            public string GetPreferenceBoneName(string boneName, IEnumerable<string> relatedBoneName)
            {
                if (!OptionalSegmentation)
                {
                    if (Name == null)
                    {
                        throw new Exception($"Name for bone \"{boneName}\" cannot be null when OptionalSegmentation is false, " + 
                                            "could be incorrect format of BoneNamePreferences.json");
                    }
                    return Name;
                }

                if (OptionalNames == null)
                {
                    throw new Exception($"OptionalNames for bone \"{boneName}\" cannot be null when OptionalSegmentation is true, " +
                                        "could be incorrect format of BoneNamePreferences.json");
                }

                string namePreference = null;

                foreach (var optionalName in OptionalNames)
                {
                    if (optionalName.TryMatchOptionalBoneName(relatedBoneName, out namePreference))
                    {
                        break;
                    }
                }

                return namePreference;
            }
        }

        private static BoneNamePreferencesManager _instance;

        public static BoneNamePreferencesManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BoneNamePreferencesManager();
                }

                return _instance;
            }
        }

        private readonly Dictionary<string, BoneNamePreferencesConfigBlock> _boneNamePref;
        
        private static ProPlanImportComponent _proPlanImportComponentInstance = new ProPlanImportComponent();

        private BoneNamePreferencesManager()
        {
            var resource = new CMFResources();
            _boneNamePref = ParseBoneNamePreferencesConfigBlocks(resource.BoneNamePreferencesJsonFile);
        }

        public BoneNamePreferencesManager(string pathName)
        {
            _boneNamePref = ParseBoneNamePreferencesConfigBlocks(pathName);
        }

        public static Dictionary<string, BoneNamePreferencesConfigBlock> ParseBoneNamePreferencesConfigBlocks(string pathName)
        {
            var jsonText = File.ReadAllText(pathName);
            return JsonConvert.DeserializeObject<Dictionary<string, BoneNamePreferencesConfigBlock>>(jsonText);
        }

        public string GetPreferenceBoneName(string boneName, string layerName, List<string> relatedBoneName)
        {
            if (!_boneNamePref.ContainsKey(boneName))
            {
                return layerName;
            }

            var boneNamePreferencesConfigBlock = _boneNamePref[boneName];
            var preferenceName = boneNamePreferencesConfigBlock.GetPreferenceBoneName(boneName, relatedBoneName);
            return preferenceName ?? layerName;
        }

        public string GetPreferenceBoneName(CMFImplantDirector director, RhinoObject boneRhObject)
        {
            var objectManager = new CMFObjectManager(director);
            var layerName = objectManager.FindLayerNameWithRhinoObject(boneRhObject);

            if (!_proPlanImportComponentInstance.GetPurePartNameFromBlockName(boneRhObject.Name, out var _, out var boneName))
            {
                return layerName;
            }

            var relatedBonesName = new List<string>();
            objectManager.GetAllObjectsByRhinoObject(boneRhObject).ForEach(b =>
            {
                if (_proPlanImportComponentInstance.GetPurePartNameFromBlockName(b.Name, out _, out var relatedBoneName))
                {
                    relatedBonesName.Add(relatedBoneName);
                }
            });

            return GetPreferenceBoneName(boneName, layerName, relatedBonesName);
        }

    }
}
