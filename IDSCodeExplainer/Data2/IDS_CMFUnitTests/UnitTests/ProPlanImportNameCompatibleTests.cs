using IDS.CMF.Compatibility;
using IDS.CMF.DataModel;
using IDS.CMF.FileSystem;
using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using IDS.CMF.V2.FileSystem;
using IDS.CMF.V2.Logics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.Testing.UnitTests
{
    /// <summary>
    /// Summary description for ProPlanImportNameCompatibleTests
    /// </summary>
    [TestClass]
    public class ProPlanImportNameCompatibleTests
    {
        private readonly TestResources testResource = new TestResources();
        private readonly CMFResources cmfResource = new CMFResources();
        private readonly CMFResourcesV2 cmfV2Resource = new CMFResourcesV2();

        [TestMethod]
        // Compare the any changed on ProPlan Import Json
        public void Pro_Plan_Import_JSON_Part_Name_No_Changed_Test()
        {
            var jsonText = File.ReadAllText(testResource.ProPlanImportReferencesPath);
            var proPlanImportReferences = JsonConvert.DeserializeObject<List<ProPlanImportBlockJsonParser.ProPlanImportJsonBlock>>(jsonText);

            jsonText = File.ReadAllText(cmfV2Resource.ProPlanImportJsonFile);
            var proPlanImportCurrent = JsonConvert.DeserializeObject<List<ProPlanImportBlockJsonParser.ProPlanImportJsonBlock>>(jsonText);

            Assert.AreEqual(proPlanImportReferences.Count, proPlanImportCurrent.Count,
                $"The size of the list in \"{cmfV2Resource.ProPlanImportJsonFile}\" might been changed");

            var maxCount = proPlanImportCurrent.Count;
            for (var i = 0; i < maxCount; i++)
            {
                Assert.AreEqual(
                    proPlanImportReferences[i].Part, 
                    proPlanImportCurrent[i].Part,
                    $"\"{proPlanImportCurrent[i].Part}\" is not same as \"{proPlanImportReferences[i].Part}\"");

                CollectionAssert.AreEqual(
                    proPlanImportReferences[i].Color,
                    proPlanImportCurrent[i].Color,
                    $"Color attribute for {proPlanImportCurrent[i].Part} is not equal");
                Assert.AreEqual(
                    proPlanImportReferences[i].PartType,
                    proPlanImportCurrent[i].PartType,
                    $"PartType attribute for {proPlanImportCurrent[i].Part} is not equal");
                Assert.AreEqual(
                    proPlanImportReferences[i].SubLayer,
                    proPlanImportCurrent[i].SubLayer,
                    $"SubLayer attribute for {proPlanImportCurrent[i].Part} is not equal");
                Assert.AreEqual(
                    proPlanImportReferences[i].IsImplantPlacable,
                    proPlanImportCurrent[i].IsImplantPlacable,
                    $"IsImplantPlacable attribute for {proPlanImportCurrent[i].Part} is not equal");
                Assert.AreEqual(
                    proPlanImportReferences[i].IsDefaultAnatomicalObstacle,
                    proPlanImportCurrent[i].IsDefaultAnatomicalObstacle,
                    $"IsDefaultAnatomicalObstacle attribute for {proPlanImportCurrent[i].Part} is not equal");
                Assert.AreEqual(
                    proPlanImportReferences[i].ImportInIDS,
                    proPlanImportCurrent[i].ImportInIDS,
                    $"ImportInIDS attribute for {proPlanImportCurrent[i].Part} is not equal");
            }
        }

        [TestMethod]
        public void Pro_Plan_Import_Name_Compatible_Valid_Test()
        {
            var proPlanNameCompatibleHelper = new ProPlanNameCompatibleHelper();

            var jsonText = File.ReadAllText(testResource.ProPlanNameCompatibleValidCasePath);
            var proPlanNameCompatibleValidCases = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText);

            foreach (var proPlanNameCompatibleValidCase in proPlanNameCompatibleValidCases)
            {
                var oldProPlanPartName = proPlanNameCompatibleValidCase.Key;
                var ExpectedNewProPlanPartName = proPlanNameCompatibleValidCase.Value;
                var newProPlanPartName = proPlanNameCompatibleHelper.RenameNewProPlanPartName(oldProPlanPartName);
                ////assert
                Assert.AreEqual(ExpectedNewProPlanPartName, newProPlanPartName,
                    $"The converted name, \"{newProPlanPartName}\" is not same as expected name \"{ExpectedNewProPlanPartName}\"");
            }
        }

        [TestMethod]
        // Check whether forget to run one of the python script to generate ProPlanImport.json or ToggleTransparency.json
        public void Pro_Plan_Import_Name_Same_As_Toggle_Transparency_Test()
        {
            var partOnlyInTransparencyJson = new List<string>()
            {
                "Implant Support",
                "Nerves_Wrapped",
                "Guide Support",
                "Guide Surface Wrap"
            };
            var jsonText = File.ReadAllText(cmfV2Resource.ProPlanImportJsonFile);
            var proPlanImport = JsonConvert.DeserializeObject<List<ProPlanImportBlockJsonParser.ProPlanImportJsonBlock>>(jsonText);
            var proPlanPartsName = proPlanImport.Select(p => p.Part).ToList();

            jsonText = File.ReadAllText(cmfResource.ToggleTransparencyJsonFile);
            var toggleTransparencyItems = JsonConvert.DeserializeObject<List<ToggleTransparencyComponentJsonParser.JsonLayerTransparency>>(jsonText);

            foreach (var toggleTransparencyItem in toggleTransparencyItems)
            {
                if (partOnlyInTransparencyJson.Contains(toggleTransparencyItem.Part))
                {
                    partOnlyInTransparencyJson.Remove(toggleTransparencyItem.Part);
                    continue;
                }
                Assert.IsTrue(proPlanPartsName.Contains(toggleTransparencyItem.Part), $"\"{toggleTransparencyItem.Part}\" is not able found in current \"ProPlaImport.json\"");
            }
            Assert.IsFalse(partOnlyInTransparencyJson.Any(), $"The \"ToggleTransparency.json\" have missing the part \"{string.Join(",", partOnlyInTransparencyJson)}\"");
        }

        [TestMethod]
        // Check whether forget to update the name in BoneNamePreferences.json
        public void Pro_Plan_Import_Name_Same_As_Bone_Name_Preferences_Test()
        {
            var jsonText = File.ReadAllText(cmfV2Resource.ProPlanImportJsonFile);
            var proPlanImport =
                JsonConvert.DeserializeObject<List<ProPlanImportBlockJsonParser.ProPlanImportJsonBlock>>(jsonText);
            var proPlanPartsName = proPlanImport.Select(p => GetProPlanImportPartName(p.Part)).ToList();
            proPlanPartsName = proPlanPartsName.Where(p => p != null).ToList();

            var boneNamePreferencesConfigBlocks =
                BoneNamePreferencesManager.ParseBoneNamePreferencesConfigBlocks(cmfResource.BoneNamePreferencesJsonFile);
            var proPlanPartNamesInConfigBlocks = new List<string>();

            foreach (var boneNamePreferencesConfigBlock in boneNamePreferencesConfigBlocks)
            {
                proPlanPartNamesInConfigBlocks.Add(boneNamePreferencesConfigBlock.Key);
                if (boneNamePreferencesConfigBlock.Value.OptionalSegmentation)
                {
                    foreach (var optionalName in boneNamePreferencesConfigBlock.Value.OptionalNames)
                    {
                        foreach (var mustHave in optionalName.MustHave)
                        {
                            if (!proPlanPartNamesInConfigBlocks.Contains(mustHave))
                            {
                                proPlanPartNamesInConfigBlocks.Add(mustHave);
                            }
                        }

                        foreach (var mustNotHave in optionalName.MustNotHave)
                        {
                            if (!proPlanPartNamesInConfigBlocks.Contains(mustNotHave))
                            {
                                proPlanPartNamesInConfigBlocks.Add(mustNotHave);
                            }
                        }
                    }
                }
            }

            foreach (var proPlanPartNamesInConfigBlock in proPlanPartNamesInConfigBlocks)
            {
                Assert.IsTrue(proPlanPartsName.Contains(proPlanPartNamesInConfigBlock),
                    $"\"{proPlanPartNamesInConfigBlock}\" is not able found in current \"ProPlaImport.json\"");
            }
        }

        [TestMethod]
        // Checks if color is correct for Metal, Nerve, Osteotomy and Teeth part type
        public void Pro_Plan_Import_Part_Type_Color_Test()
        {
            var partTypeToValidate = new List<PartTypeColor>()
            {
                new PartTypeColor(ProPlanImportPartType.Metal, new List<int>{ 192, 192, 192 }),
                new PartTypeColor(ProPlanImportPartType.Nerve, new List<int>{ 255, 128, 64 }),
                new PartTypeColor(ProPlanImportPartType.OsteotomyPlane, new List<int>{ 139, 205, 50 }),
                new PartTypeColor(ProPlanImportPartType.Teeth, new List<int>{ 244, 244, 244 }),
            };

            var jsonText = File.ReadAllText(cmfV2Resource.ProPlanImportJsonFile);
            var proPlanImport = JsonConvert.DeserializeObject<List<ProPlanImportBlockJsonParser.ProPlanImportJsonBlock>>(jsonText);
            var wrongPartColor = proPlanImport.Where(part => partTypeToValidate.Exists
                (validate => validate.PartType == part.PartType && !validate.PartColor.SequenceEqual(part.Color))).ToList();

            wrongPartColor.ForEach(part => Assert.IsNull(part, $"The part {part.Part} has the wrong color configured in ProPlanImport.json"));
        }


        private string GetProPlanImportPartName(string partName)
        {
            var rx = new Regex(@"^(\d{2}|\d\[\d-\d\])?([a-z][a-z0-9_]{1,})$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var matches = rx.Matches(partName);
            if (matches.Count != 1)
            {
                return null;
            }

            var groups = matches[0].Groups;

            if (groups.Count != 3)
            {
                return null;
            }

            return groups[2].Value;
        }

    }
}
