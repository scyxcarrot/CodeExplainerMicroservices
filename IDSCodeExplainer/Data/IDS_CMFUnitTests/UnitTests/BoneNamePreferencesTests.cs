using IDS.CMF.FileSystem;
using IDS.CMF.Quality;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class BoneNamePreferencesTests
    {
        private BoneNamePreferencesManager _boneNamePreferencesManagerTestPurpose = null;

        public BoneNamePreferencesTests()
        {
            var resources = new TestResources();
            _boneNamePreferencesManagerTestPurpose = new BoneNamePreferencesManager(resources.BoneNamePreferencesTestPurposePath);
        }

        [TestMethod]
        public void Compare_Bone_Name_Preferences_Config_Blocks()
        {
            var testResources = new TestResources();
            var boneNamePreferencesConfigBlocksTestPurpose = 
                BoneNamePreferencesManager.ParseBoneNamePreferencesConfigBlocks(testResources.BoneNamePreferencesTestPurposePath);
            var cmfResources = new CMFResources();
            var boneNamePreferencesConfigBlocksOriginal =
                BoneNamePreferencesManager.ParseBoneNamePreferencesConfigBlocks(cmfResources.BoneNamePreferencesJsonFile);

            // "boneNamePreferencesConfigBlocksOriginal" should be subset of "boneNamePreferencesConfigBlocksTestPurpose"
            foreach (var configBlockOrg in boneNamePreferencesConfigBlocksOriginal)   
            {
                Assert.IsTrue(boneNamePreferencesConfigBlocksTestPurpose.ContainsKey(configBlockOrg.Key), 
                    $"Part, \"{configBlockOrg.Key}\" in \"BoneNamePreferences.json\" is a new part, please revise the unit test again");
                var configBlockTestValue = boneNamePreferencesConfigBlocksTestPurpose[configBlockOrg.Key];
                var configBlockOrgValue = configBlockOrg.Value;
                
                Assert.AreEqual(configBlockTestValue.SubLayer, configBlockOrgValue.SubLayer,
                    $"Sub layer ({configBlockTestValue.SubLayer} != {configBlockOrgValue.SubLayer}) of part, \"{configBlockOrg.Key}\" have changed, please revise the unit test again");

                Assert.AreEqual(configBlockTestValue.OptionalSegmentation, configBlockOrgValue.OptionalSegmentation,
                    $"\"OptionalSegmentation\" ({configBlockTestValue.OptionalSegmentation} != {configBlockOrgValue.OptionalSegmentation}) of part, \"{configBlockOrg.Key}\" have changed, please revise the unit test again");

                Assert.AreEqual(configBlockTestValue.Name, configBlockOrgValue.Name,
                    $"Expected name ({configBlockTestValue.Name} != {configBlockOrgValue.Name}) of part, \"{configBlockOrg.Key}\" have changed, please revise the unit test again");
                
                Assert.AreEqual(configBlockTestValue.OptionalNames != null, configBlockOrgValue.OptionalNames != null,
                    $"\"OptionalNames\" of part, \"{configBlockOrg.Key}\" have changed, please revise the unit test again");
                
                if (configBlockOrgValue.OptionalNames != null)
                {
                    var optionalNamesOrg = configBlockOrgValue.OptionalNames;
                    var optionalNamesTest = configBlockTestValue.OptionalNames;

                    Assert.AreEqual(optionalNamesTest.Count, optionalNamesOrg.Count,
                        $"Content of \"OptionalNames\" of part, \"{configBlockOrg.Key}\" have changed, please revise the unit test again");

                    for (var i = 0; i < optionalNamesOrg.Count; i++)
                    {
                        Assert.AreEqual(optionalNamesTest[i].Name, optionalNamesOrg[i].Name,
                            $"Name of \"OptionalNames\" of part, \"{configBlockOrg.Key}\" in index {i} have changed, please revise the unit test again");

                        Assert.AreEqual(optionalNamesTest[i].MustHave.Count, optionalNamesOrg[i].MustHave.Count,
                            $"\"MustHave\" of \"OptionalNames\" of part, \"{configBlockOrg.Key}\" in index {i} have changed, please revise the unit test again");
                        for (var j = 0; j < optionalNamesTest[i].MustHave.Count; j++)
                        {
                            Assert.AreEqual(optionalNamesTest[i].MustHave[j], optionalNamesOrg[i].MustHave[j],
                                $"\"MustHave\" of \"OptionalNames\" of part, \"{configBlockOrg.Key}\" in index {i} have changed, please revise the unit test again");

                        }

                        Assert.AreEqual(optionalNamesTest[i].MustNotHave.Count, optionalNamesOrg[i].MustNotHave.Count,
                            $"\"MustNotHave\" of \"OptionalNames\" of part, \"{configBlockOrg.Key}\" in index {i} have changed, please revise the unit test again");
                        for (var j = 0; j < optionalNamesTest[i].MustNotHave.Count; j++)
                        {
                            Assert.AreEqual(optionalNamesTest[i].MustNotHave[j], optionalNamesOrg[i].MustNotHave[j],
                                $"\"MustNotHave\" of \"OptionalNames\" of part, \"{configBlockOrg.Key}\" in index {i} have changed, please revise the unit test again");

                        }

                    }
                }
            }
        }

        [TestMethod]
        public void Not_Found_Name_Preferences_Test()
        {
            var boneName = "NOT EXIST";
            var layerName = "DUMMY LAYER NAME";
            var relatedBoneName = new List<string>();
            var preferenceBoneName = _boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual(layerName, preferenceBoneName);
        }

        [TestMethod]
        public void RAM_R_Name_Preferences_Test()
        {
            var boneName = "RAM_R";
            var layerName = "Rami";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Ramus Right", preferenceBoneName);
        }

        [TestMethod]
        public void RAM_L_Name_Preferences_Test()
        {
            var boneName = "RAM_L";
            var layerName = "Rami";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Ramus Left", preferenceBoneName);
        }

        [TestMethod]
        public void GEN_Name_Preferences_Test()
        {
            var boneName = "GEN";
            var layerName = "Genio";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Chin", preferenceBoneName);
        }

        [TestMethod]
        public void GEN1_Name_Preferences_Test()
        {
            var boneName = "GEN1";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() { "GEN1", "GEN2" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Chin Left", preferenceBoneName);
        }

        [TestMethod]
        public void GEN2_2_Segmentation_Name_Preferences_Test()
        {
            var boneName = "GEN2";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() { "GEN1", "GEN2" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Chin Right", preferenceBoneName);
        }

        [TestMethod]
        public void GEN2_3_Segmentation_Name_Preferences_Test()
        {
            var boneName = "GEN2";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() { "GEN1", "GEN2", "GEN3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Chin Middle", preferenceBoneName);
        }

        [TestMethod]
        public void GEN2_4_Segmentation_Name_Preferences_Test()
        {
            var boneName = "GEN2";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() { "GEN1", "GEN2", "GEN3", "GEN4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Chin Middle-Left", preferenceBoneName);
        }

        [TestMethod]
        public void GEN2_2_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "GEN2";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() { "GEN2" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Chin Right", preferenceBoneName);
        }

        [TestMethod]
        public void GEN2_3_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "GEN2";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() { "GEN2", "GEN3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Chin Middle", preferenceBoneName);
        }

        [TestMethod]
        public void GEN2_4_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "GEN2";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() {  "GEN2", "GEN4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Chin Middle-Left", preferenceBoneName);
        }

        [TestMethod]
        public void GEN3_3_Segmentation_Name_Preferences_Test()
        {
            var boneName = "GEN3";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() { "GEN1", "GEN2", "GEN3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Chin Right", preferenceBoneName);
        }

        [TestMethod]
        public void GEN3_4_Segmentation_Name_Preferences_Test()
        {
            var boneName = "GEN3";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() { "GEN1", "GEN2", "GEN3", "GEN4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Chin Middle-Right", preferenceBoneName);
        }

        [TestMethod]
        public void GEN3_3_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "GEN3";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() { "GEN1", "GEN3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Chin Middle", preferenceBoneName);
        }

        [TestMethod]
        public void GEN3_4_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "GEN3";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() { "GEN3", "GEN4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Chin Middle-Right", preferenceBoneName);
        }

        [TestMethod]
        public void GEN4_Name_Preferences_Test()
        {
            var boneName = "GEN4";
            var layerName = "Genio";
            var relatedBoneName = new List<string>() {};
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Chin Right", preferenceBoneName);
        }

        [TestMethod]
        public void FIB_part1_Name_Preferences_Test()
        {
            var boneName = "FIB_part1";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 1", preferenceBoneName);
        }

        [TestMethod]
        public void FIB_part2_Name_Preferences_Test()
        {
            var boneName = "FIB_part2";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 2", preferenceBoneName);
        }

        [TestMethod]
        public void FIB_part3_Name_Preferences_Test()
        {
            var boneName = "FIB_part3";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 3", preferenceBoneName);
        }

        [TestMethod]
        public void FIB_part4_Name_Preferences_Test()
        {
            var boneName = "FIB_part4";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 4", preferenceBoneName);
        }

        [TestMethod]
        public void FIB_part5_Name_Preferences_Test()
        {
            var boneName = "FIB_part5";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 5", preferenceBoneName);
        }

        [TestMethod]
        public void HIP_part1_Name_Preferences_Test()
        {
            var boneName = "HIP_part1";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 1", preferenceBoneName);
        }

        [TestMethod]
        public void HIP_part2_Name_Preferences_Test()
        {
            var boneName = "HIP_part2";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 2", preferenceBoneName);
        }

        [TestMethod]
        public void HIP_part3_Name_Preferences_Test()
        {
            var boneName = "HIP_part3";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 3", preferenceBoneName);
        }

        [TestMethod]
        public void HIP_part4_Name_Preferences_Test()
        {
            var boneName = "HIP_part4";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 4", preferenceBoneName);
        }

        [TestMethod]
        public void HIP_part5_Name_Preferences_Test()
        {
            var boneName = "HIP_part5";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 5", preferenceBoneName);
        }

        [TestMethod]
        public void SCA_part1_Name_Preferences_Test()
        {
            var boneName = "SCA_part1";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 1", preferenceBoneName);
        }

        [TestMethod]
        public void SCA_part2_Name_Preferences_Test()
        {
            var boneName = "SCA_part2";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 2", preferenceBoneName);
        }

        [TestMethod]
        public void SCA_part3_Name_Preferences_Test()
        {
            var boneName = "SCA_part3";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 3", preferenceBoneName);
        }

        [TestMethod]
        public void SCA_part4_Name_Preferences_Test()
        {
            var boneName = "SCA_part4";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 4", preferenceBoneName);
        }

        [TestMethod]
        public void SCA_part5_Name_Preferences_Test()
        {
            var boneName = "SCA_part5";
            var layerName = "Graft";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Graft 5", preferenceBoneName);
        }

        [TestMethod]
        public void MAX1_Name_Preferences_Test()
        {
            var boneName = "MAX1";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX1", "MAX2" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Maxilla Left", preferenceBoneName);
        }

        [TestMethod]
        public void MAX2_2_Segmentation_Name_Preferences_Test()
        {
            var boneName = "MAX2";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX1", "MAX2" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Maxilla Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAX2_3_Segmentation_Name_Preferences_Test()
        {
            var boneName = "MAX2";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX1", "MAX2", "MAX3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Maxilla Middle", preferenceBoneName);
        }

        [TestMethod]
        public void MAX2_4_Segmentation_Name_Preferences_Test()
        {
            var boneName = "MAX2";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX1", "MAX2", "MAX3", "MAX4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Maxilla Middle-Left", preferenceBoneName);
        }

        [TestMethod]
        public void MAX2_2_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "MAX2";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX2" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Maxilla Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAX2_3_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "MAX2";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX2", "MAX3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Maxilla Middle", preferenceBoneName);
        }

        [TestMethod]
        public void MAX2_4_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "MAX2";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX2", "MAX4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Maxilla Middle-Left", preferenceBoneName);
        }

        [TestMethod]
        public void MAX3_3_Segmentation_Name_Preferences_Test()
        {
            var boneName = "MAX3";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX1", "MAX2", "MAX3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Maxilla Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAX3_4_Segmentation_Name_Preferences_Test()
        {
            var boneName = "MAX3";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX1", "MAX2", "MAX3", "MAX4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Maxilla Middle-Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAX3_3_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "MAX3";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX1", "MAX3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Maxilla Middle", preferenceBoneName);
        }

        [TestMethod]
        public void MAX3_4_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "MAX3";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { "MAX3", "MAX4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Maxilla Middle-Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAX4_Name_Preferences_Test()
        {
            var boneName = "MAX4";
            var layerName = "Maxilla";
            var relatedBoneName = new List<string>() { };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Maxilla Right", preferenceBoneName);
        }

        [TestMethod]
        public void ZYG1_Name_Preferences_Test()
        {
            var boneName = "ZYG1";
            var layerName = "Skull Reposition";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Zygoma 1", preferenceBoneName);
        }

        [TestMethod]
        public void ZYG2_Name_Preferences_Test()
        {
            var boneName = "ZYG2";
            var layerName = "Skull Reposition";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Zygoma 2", preferenceBoneName);
        }

        [TestMethod]
        public void ZYG3_Name_Preferences_Test()
        {
            var boneName = "ZYG3";
            var layerName = "Skull Reposition";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Zygoma 3", preferenceBoneName);
        }

        [TestMethod]
        public void ZYG4_Name_Preferences_Test()
        {
            var boneName = "ZYG4";
            var layerName = "Skull Reposition";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Zygoma 4", preferenceBoneName);
        }

        [TestMethod]
        public void ZYG5_Name_Preferences_Test()
        {
            var boneName = "ZYG5";
            var layerName = "Skull Reposition";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Zygoma 5", preferenceBoneName);
        }

        [TestMethod]
        public void ORB1_Name_Preferences_Test()
        {
            var boneName = "ORB1";
            var layerName = "Skull Reposition";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Orbit 1", preferenceBoneName);
        }

        [TestMethod]
        public void ORB2_Name_Preferences_Test()
        {
            var boneName = "ORB2";
            var layerName = "Skull Reposition";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Orbit 2", preferenceBoneName);
        }

        [TestMethod]
        public void ORB3_Name_Preferences_Test()
        {
            var boneName = "ORB3";
            var layerName = "Skull Reposition";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Orbit 3", preferenceBoneName);
        }

        [TestMethod]
        public void ORB4_Name_Preferences_Test()
        {
            var boneName = "ORB4";
            var layerName = "Skull Reposition";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Orbit 4", preferenceBoneName);
        }

        [TestMethod]
        public void ORB5_Name_Preferences_Test()
        {
            var boneName = "ORB5";
            var layerName = "Skull Reposition";
            var relatedBoneName = new List<string>();
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Orbit 5", preferenceBoneName);
        }

        [TestMethod]
        public void MAN1_Name_Preferences_Test()
        {
            var boneName = "MAN1";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN1", "MAN2" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Mandible Body Left", preferenceBoneName);
        }

        [TestMethod]
        public void MAN2_2_Segmentation_Name_Preferences_Test()
        {
            var boneName = "MAN2";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN1", "MAN2" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Mandible Body Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAN2_3_Segmentation_Name_Preferences_Test()
        {
            var boneName = "MAN2";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN1", "MAN2", "MAN3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Mandible Body Middle", preferenceBoneName);
        }

        [TestMethod]
        public void MAN2_4_Segmentation_Name_Preferences_Test()
        {
            var boneName = "MAN2";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN1", "MAN2", "MAN3", "MAN4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Mandible Body Middle-Left", preferenceBoneName);
        }

        [TestMethod]
        public void MAN2_2_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "MAN2";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN2" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Mandible Body Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAN2_3_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "MAN2";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN2", "MAN3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Mandible Body Middle", preferenceBoneName);
        }

        [TestMethod]
        public void MAN2_4_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "MAN2";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN2", "MAN4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Mandible Body Middle-Left", preferenceBoneName);
        }

        [TestMethod]
        public void MAN3_3_Segmentation_Name_Preferences_Test()
        {
            var boneName = "MAN3";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN1", "MAN2", "MAN3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Mandible Body Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAN3_4_Segmentation_Name_Preferences_Test()
        {
            var boneName = "MAN3";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN1", "MAN2", "MAN3", "MAN4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Mandible Body Middle-Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAN3_3_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "MAN3";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN1", "MAN3" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Mandible Body Middle", preferenceBoneName);
        }

        [TestMethod]
        public void MAN3_4_Segmentation_Name_Preferences_Fault_Test()
        {
            var boneName = "MAN3";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { "MAN3", "MAN4" };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Mandible Body Middle-Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAN4_Name_Preferences_Test()
        {
            var boneName = "MAN4";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Mandible Body Right", preferenceBoneName);
        }

        [TestMethod]
        public void MAN_body_remaining_Name_Preferences_Test()
        {
            var boneName = "MAN_body_remaining";
            var layerName = "Mandible Body Remaining";
            var relatedBoneName = new List<string>() { };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Mandible Body", preferenceBoneName);
        }

        [TestMethod]
        public void MAN_remaining_Name_Preferences_Test()
        {
            var boneName = "MAN_remaining";
            var layerName = "Mandible Remaining";
            var relatedBoneName = new List<string>() { };
            var preferenceBoneName =_boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Mandible Body", preferenceBoneName);
        }

        [TestMethod]
        public void Empty_MushHave_Name_Preferences_Test()
        {
            var boneName = "Empty_MustHave_Test";
            var layerName = "DUMMY LAYER NAME";
            var relatedBoneName = new List<string>() { "NOT MUSTNOTHAVE DUMPY PART"};
            var preferenceBoneName = _boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreEqual("Empty_MustHave_Test_Result", preferenceBoneName);
        }

        [TestMethod]
        public void Empty_MushHave_Name_Preferences_Fault_Test()
        {
            var boneName = "Empty_MustHave_Test";
            var layerName = "DUMMY LAYER NAME";
            var relatedBoneName = new List<string>() { "DUMMY PART 1" };
            var preferenceBoneName = _boneNamePreferencesManagerTestPurpose.GetPreferenceBoneName(boneName, layerName, relatedBoneName);
            Assert.AreNotEqual("Empty_MustHave_Test_Result", preferenceBoneName);
        }
    }
}
