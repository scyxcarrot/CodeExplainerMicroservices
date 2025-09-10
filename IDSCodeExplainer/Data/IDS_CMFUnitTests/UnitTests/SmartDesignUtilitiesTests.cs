using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.LogicContext;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.V2.Geometry;
using IDS.Interface.Loader;
using IDS.RhinoInterface.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class SmartDesignUtilitiesTests
    {
        private const string wedgePartPostFix = "bone";

        [TestMethod]
        public void Exported_TransformationMatrices_Are_According_To_Correct_Format()
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();

            var transformForA = new Transform
            {
                M00 = 0.999357,
                M01 = -0.0270091,
                M02 = 0.0235797,
                M03 = 3.93627,
                M10 = 0.0283668,
                M11 = 0.997838,
                M12 = -0.0592828,
                M13 = -8.22746,
                M20 = -0.0219275,
                M21 = 0.0599135,
                M22 = 0.997963,
                M23 = 2.60288,
                M30 = 0.0,
                M31 = 0.0,
                M32 = 0.0,
                M33 = 1.0
            };

            var transformForB = new Transform
            {
                M00 = 0.999694,
                M01 = -0.00200257,
                M02 = 0.0246672,
                M03 = 2.07518,
                M10 = 0.00152302,
                M11 = 0.99981,
                M12 = 0.0194444,
                M13 = 1.18521,
                M20 = -0.0247014,
                M21 = -0.0194009,
                M22 = 0.999507,
                M23 = 6.26816,
                M30 = 0.0,
                M31 = 0.0,
                M32 = 0.0,
                M33 = 1.0
            };

            AddNewBuildingBlockWithTransform(objectManager, proPlanImportComponent, "01RAM_L", Transform.Identity);
            AddNewBuildingBlockWithTransform(objectManager, proPlanImportComponent, "05RAM_L", transformForA);
            AddNewBuildingBlockWithTransform(objectManager, proPlanImportComponent, "01RAM_R", Transform.Identity);
            AddNewBuildingBlockWithTransform(objectManager, proPlanImportComponent, "03RAM_R", transformForB);

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            var filePath = $@"{tempDirectory}\{SmartDesignStrings.MovementsFileName}.json";
            var expectedContent = NormalizingString(@"{
              ""ExportedParts"": [
                {
                    ""ExportedPartName"": ""05RAM_L"",
                    ""TransformationMatrix"": {
                        ""M00"": 0.999357,
                        ""M01"": -0.0270091,
                        ""M02"": 0.0235797,
                        ""M03"": 3.93627,
                        ""M10"": 0.0283668,
                        ""M11"": 0.997838,
                        ""M12"": -0.0592828,
                        ""M13"": -8.22746,
                        ""M20"": -0.0219275,
                        ""M21"": 0.0599135,
                        ""M22"": 0.997963,
                        ""M23"": 2.60288,
                        ""M30"": 0.0,
                        ""M31"": 0.0,
                        ""M32"": 0.0,
                        ""M33"": 1.0
                     }
                },
                {
                    ""ExportedPartName"": ""03RAM_R"",
                    ""TransformationMatrix"": {
                        ""M00"": 0.999694,
                        ""M01"": -0.00200257,
                        ""M02"": 0.0246672,
                        ""M03"": 2.07518,
                        ""M10"": 0.00152302,
                        ""M11"": 0.99981,
                        ""M12"": 0.0194444,
                        ""M13"": 1.18521,
                        ""M20"": -0.0247014,
                        ""M21"": -0.0194009,
                        ""M22"": 0.999507,
                        ""M23"": 6.26816,
                        ""M30"": 0.0,
                        ""M31"": 0.0,
                        ""M32"": 0.0,
                        ""M33"": 1.0
                      }
                }
              ]}");

            //act
            SmartDesignUtilities.ExportTransformationMatricesToTempFolder(new List<string> { "01RAM_L", "01RAM_R" }, director, tempDirectory);

            //assert
            Assert.IsTrue(File.Exists(filePath));
            var content = NormalizingString(File.ReadAllText(filePath));
            Assert.AreEqual(expectedContent, content);
                        
            Directory.Delete(tempDirectory, true);
        }

        [TestMethod]
        public void Exported_OsteotomyHandler_Are_According_To_Correct_Format()
        {
            // Arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var proPlanImportComponent = new ProPlanImportComponent();

            var osteotomyIdentifier = new[]
            {
                "anterior_plane_pt",
                "middle_plane_pt_1",
                "middle_plane_pt_2",
                "middle_plane_pt_3",
                "middle_plane_pt_4",
                "middle_plane_pt_5",
                "middle_plane_pt_6",
                "middle_plane_pt_7",
                "posterior_plane_pt",
                "top_plane_pt"
            };

            var osteotomyCoordinates = new[,]
            {
                {26.211120656611055, -136.04787829688686, 740.2368626055345},
                {25.565649372298182,-103.02190149578024, 780.33695762124478},
                {21.39675351447757, -126.68627554765843, 771.402371820942},
                {19.489384874410376, -133.49742465708044, 764.63351226520547},
                {18.482390415367348, -138.71071795008083, 757.71452970724351},
                {16.902293095348988, -124.32096269933942, 728.00935277778058},
                {17.609785533780773, -106.93654013751784, 730.10354956865251},
                {17.390405813817281, -90.464820972682446, 731.45795127174233},
                {15.753920075521421, -95.732642571346418, 756.49339348430658},
                {17.757093925851564, -113.84336985883441, 776.46560375890635}
            };

            var osteotomyHandler = new List<IOsteotomyHandler>
            {
                new MockOsteotomyHandler("01BSSO_L", "SSO", 0.1, osteotomyIdentifier, osteotomyCoordinates)
            };

            AddNewBuildingBlockWithOsteotomyHandler(director, proPlanImportComponent, osteotomyHandler);

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            var filePath = $@"{tempDirectory}\{SmartDesignStrings.OsteotomyHandlerFileName}.json";

            #region ExpectedFormat

            var expectedContent = NormalizingString(@"{
                ""ExportedParts"": [
                    {
                        ""OsteotomyPartName"": ""01BSSO_L"",
                        ""OsteotomyType"": ""SSO"",
                        ""OsteotomyThickness"": 0.1,
                        ""OsteotomyHandler"": {
                            ""anterior_plane_pt"": [
                            26.211120656611055,
                            -136.04787829688686,
                            740.2368626055345
                                ],
                            ""middle_plane_pt_1"": [
                            25.565649372298182,
                            -103.02190149578024,
                            780.33695762124478
                                ],
                            ""middle_plane_pt_2"": [
                            21.39675351447757,
                            -126.68627554765843,
                            771.402371820942
                                ],
                            ""middle_plane_pt_3"": [
                            19.489384874410376,
                            -133.49742465708044,
                            764.63351226520547
                                ],
                            ""middle_plane_pt_4"": [
                            18.482390415367348,
                            -138.71071795008083,
                            757.71452970724351
                                ],
                            ""middle_plane_pt_5"": [
                            16.902293095348988,
                            -124.32096269933942,
                            728.00935277778058
                                ],
                            ""middle_plane_pt_6"": [
                            17.609785533780773,
                            -106.93654013751784,
                            730.10354956865251
                                ],
                            ""middle_plane_pt_7"": [
                            17.390405813817281,
                            -90.464820972682446,
                            731.45795127174233
                                ],
                            ""posterior_plane_pt"": [
                            15.753920075521421,
                            -95.732642571346418,
                            756.49339348430658
                                ],
                            ""top_plane_pt"": [
                            17.757093925851564,
                            -113.84336985883441,
                            776.46560375890635
                                ]
                        }
                    }
                ]}");

            #endregion

            //act
            SmartDesignUtilities.ExportOsteotomyHandlerToTempFolder(new List<string>() { "01BSSO_L" }, director,
                tempDirectory);

            //assert
            Assert.IsTrue(File.Exists(filePath));
            var content = NormalizingString(File.ReadAllText(filePath));
            Assert.AreEqual(expectedContent, content);

            Directory.Delete(tempDirectory, true);
        }

        [TestMethod]
        public void WedgeBSSO_Outputs_Are_Processed_Correctly()
        {
            //recut part remains in recut folder
            //wedge moved to wedge folder

            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();

            AddNewBuildingBlockWithTransform(objectManager, proPlanImportComponent, "01RAM_L", Transform.Identity);
            AddNewBuildingBlockWithTransform(objectManager, proPlanImportComponent, "01RAM_R", Transform.Identity);

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            var sameMesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 5.0), 10, 10);
            StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(sameMesh), Path.Combine(tempDirectory, "01RAM_L.stl"));
            var differentMesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 15.0), 10, 10);
            StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(differentMesh), Path.Combine(tempDirectory, "01RAM_R.stl"));
            var bssoWedgeStlFileName = $"01BSSO_{wedgePartPostFix}.stl";
            StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(sameMesh), Path.Combine(tempDirectory, bssoWedgeStlFileName));

            //act
            var dataModel = new SmartDesignBSSORecutModel
            {
                Osteotomies = new List<string>{ "01BSSO" },
                WedgeOperation = true
            };
            var success = SmartDesignUtilities.ProcessSmartDesignOutput(dataModel, director, tempDirectory);

            //assert
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(Path.Combine(tempDirectory, "01RAM_L.stl")));
            Assert.IsTrue(File.Exists(Path.Combine(tempDirectory, "01RAM_R.stl")));
            Assert.IsFalse(File.Exists(Path.Combine(tempDirectory, bssoWedgeStlFileName)));
            Assert.IsTrue(File.Exists(Path.Combine(Path.GetTempPath(), SmartDesignStrings.WedgeFolderName, bssoWedgeStlFileName)));

            Directory.Delete(tempDirectory, true);
            Directory.Delete(Path.Combine(Path.GetTempPath(), SmartDesignStrings.WedgeFolderName), true);
        }

        [TestMethod]
        public void WedgeLefort_Outputs_Are_Processed_Correctly()
        {
            //recut part remains in recut folder
            //wedge moved to wedge folder

            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();

            AddNewBuildingBlockWithTransform(objectManager, proPlanImportComponent, "01SKU_remaining", Transform.Identity);

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            var sameMesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 15.0), 10, 10);
            StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(sameMesh), Path.Combine(tempDirectory, "01SKU_remaining.stl"));
            var bssoWedgeStlFileName = $"01LefortI_{wedgePartPostFix}_l_.stl";
            StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(sameMesh), Path.Combine(tempDirectory, bssoWedgeStlFileName));

            //act
            var dataModel = new SmartDesignBSSORecutModel
            {
                Osteotomies = new List<string> { "01LefortI" },
                WedgeOperation = true
            };
            var success = SmartDesignUtilities.ProcessSmartDesignOutput(dataModel, director, tempDirectory);

            //assert
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(Path.Combine(tempDirectory, "01SKU_remaining.stl")));
            Assert.IsFalse(File.Exists(Path.Combine(tempDirectory, bssoWedgeStlFileName)));
            Assert.IsTrue(File.Exists(Path.Combine(Path.GetTempPath(), SmartDesignStrings.WedgeFolderName, bssoWedgeStlFileName)));

            Directory.Delete(tempDirectory, true);
            Directory.Delete(Path.Combine(Path.GetTempPath(), SmartDesignStrings.WedgeFolderName), true);
        }

        [TestMethod]
        public void WedgeGenio_Outputs_Are_Processed_Correctly()
        {
            //recut part remains in recut folder
            //wedge moved to wedge folder

            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();

            AddNewBuildingBlockWithTransform(objectManager, proPlanImportComponent, "01GEN", Transform.Identity);

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            var sameMesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 15.0), 10, 10);
            StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(sameMesh), Path.Combine(tempDirectory, "01GEN.stl"));
            var bssoWedgeStlFileName = $"01Geniocut_{wedgePartPostFix}.stl";
            StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(sameMesh), Path.Combine(tempDirectory, bssoWedgeStlFileName));

            //act
            var dataModel = new SmartDesignBSSORecutModel
            {
                Osteotomies = new List<string> { "01Geniocut" },
                WedgeOperation = true
            };
            var success = SmartDesignUtilities.ProcessSmartDesignOutput(dataModel, director, tempDirectory);

            //assert
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(Path.Combine(tempDirectory, "01GEN.stl")));
            Assert.IsFalse(File.Exists(Path.Combine(tempDirectory, bssoWedgeStlFileName)));
            Assert.IsTrue(File.Exists(Path.Combine(Path.GetTempPath(), SmartDesignStrings.WedgeFolderName, bssoWedgeStlFileName)));

            Directory.Delete(tempDirectory, true);
            Directory.Delete(Path.Combine(Path.GetTempPath(), SmartDesignStrings.WedgeFolderName), true);
        }

        [TestMethod]
        public void BSSO_Split_SSO_Outputs_Are_Processed_Correctly()
        {
            //mock
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();

            AddNewBuildingBlockWithTransform(objectManager, proPlanImportComponent, "01RAM_L", Transform.Identity);
            AddNewBuildingBlockWithTransform(objectManager, proPlanImportComponent, "01RAM_R", Transform.Identity);

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            var sameMesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 5.0), 10, 10);
            var bssoLeft = "01BSSO_L_l.stl";
            StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(sameMesh), Path.Combine(tempDirectory, bssoLeft));

            var bssoRight = "01BSSO_R_r.stl";
            StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(sameMesh), Path.Combine(tempDirectory, bssoRight));

            var bssoWedgeStlFileName = $"01BSSO_L_l_{wedgePartPostFix}.stl";
            StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(sameMesh), Path.Combine(tempDirectory, bssoWedgeStlFileName));

            //act
            var dataModel = new SmartDesignBSSORecutModel
            {
                Osteotomies = new List<string> { "01BSSO_L", "01BSSO_R" },
                WedgeOperation = true,
                SplitSso = true
            };

            SmartDesignUtilities.DeleteSplitSsoOsteotomySuffix(dataModel, tempDirectory);

            //assert
            Assert.IsTrue(File.Exists(Path.Combine(tempDirectory, "01BSSO_L.stl")));
            Assert.IsFalse(File.Exists(Path.Combine(tempDirectory, bssoLeft)));

            Assert.IsTrue(File.Exists(Path.Combine(tempDirectory, "01BSSO_R.stl")));
            Assert.IsFalse(File.Exists(Path.Combine(tempDirectory, bssoRight)));

            Assert.IsTrue(File.Exists(Path.Combine(tempDirectory, bssoWedgeStlFileName)));
        }

        private void AddNewBuildingBlockWithTransform(CMFObjectManager objectManager, ProPlanImportComponent proPlanImportComponent, string partName, Transform transform)
        {
            var mesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 5.0), 10, 10);
            var buildingBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(partName);
            objectManager.AddNewBuildingBlockWithTransform(buildingBlock, mesh, transform);
        }

        private void AddNewBuildingBlockWithOsteotomyHandler(CMFImplantDirector director, ProPlanImportComponent proPlanImportComponent, 
            List<IOsteotomyHandler> osteotomyHandler)
        {
            var objectManager = new CMFObjectManager(director);
            var mesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 5.0), 10, 10);

            foreach (var handler in osteotomyHandler)
            {
                var buildingBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(handler.Name);
                objectManager.AddNewBuildingBlock(buildingBlock, mesh);
            }

            var context = new BackEndImportPreopsContext(director, director.Document, new TestConsole());
            context.AddOsteotomyHandlerToBuildingBlock(osteotomyHandler);
        }

        private string NormalizingString(string content)
        {
            var processed = content.Replace(" ", "");
            processed = processed.Replace("\r", "");
            processed = processed.Replace("\n", "");
            return processed;
        }
    }

#endif
}