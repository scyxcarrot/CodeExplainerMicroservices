using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.LogicContext;
using IDS.CMF.TestLib.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Loader;
using IDS.CMF.V2.Logics;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Loader;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ImportPreopsLogicTests
    {
        #region Mock Object 
        // Create this mock class due to some problem using moq
        private class MockImportPreopsLogic: ImportPreopsLogic
        {
            private IPreopLoader _mockLoader;
            public MockImportPreopsLogic(IConsole console, IPreopLoader mockLoader) : base(console)
            {
                _mockLoader = mockLoader;
            }

            protected override IPreopLoader GetLoader(string filePath)
            {
                var extension = Path.GetExtension(filePath);
                Assert.AreEqual(".sppc", extension.ToLower());
                return _mockLoader;
            }
        }

        private class MockPreopLoadResult : IPreopLoadResult
        {
            public string Name { get; }

            public string FilePath { get; }

            public ITransform TransformationMatrix { get; }

            public bool IsReferenceObject { get; }

            public IMesh Mesh { get; }

            public MockPreopLoadResult(string name, IPreopLoadResult result)
            {
                Name = name;
                FilePath = result.FilePath;
                TransformationMatrix = result.TransformationMatrix;
                IsReferenceObject = result.IsReferenceObject;
                Mesh = result.Mesh;
            }
        }

        #endregion

        [TestMethod]
        public void Basic_Load_Sppc_Test()
        {
            #region Arrage
            // Create a folder for load sppc
            var resource = new TestResources();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            var newSppcFilePath = Path.Combine(tempDirectory, "UnitTest.sppc");
            File.Copy(resource.SPPCFilePath, newSppcFilePath);

            // Create context and logic
            const EScrewBrand screwBrand = EScrewBrand.MtlsStandardPlus;
            const ESurgeryType surgeryType = ESurgeryType.Reconstruction;

            var console = new TestConsole();
            var context = new BlankImportPreopsContext(console,
                newSppcFilePath, screwBrand, surgeryType);
            var logic = new ImportPreopsLogic(console);

            // Assert variable
            Exception exception = null;
            LogicStatus status = LogicStatus.Failure;
            #endregion

            #region Act
            try
            {
                status = logic.Execute(context);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
            #endregion

            #region Assert
            Assert.AreEqual(LogicStatus.Success, status);
            var screwBrandSurgery = context.ConfirmationScrewBrandSurgery;

            Assert.AreEqual(LogicStatus.Success, screwBrandSurgery.Status);
            Assert.AreEqual(screwBrand, screwBrandSurgery.Parameter.ScrewBrand);
            Assert.AreEqual(surgeryType, screwBrandSurgery.Parameter.SurgeryType);
            Assert.IsNull(exception, $"Unhandled exception: {exception?.Message}");
            #endregion
        }

        [TestMethod]
        public void Load_Sppc_With_Sub_Folder_Exist_Test()
        {
            #region Arrage
            // Create a folder for load sppc
            var resource = new TestResources();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            var subDirectory = Path.Combine(tempDirectory, Path.GetRandomFileName());
            Directory.CreateDirectory(subDirectory);
            var newSppcFilePath = Path.Combine(tempDirectory, "UnitTest.sppc");
            File.Copy(resource.SPPCFilePath, newSppcFilePath);

            // Create context and logic
            const EScrewBrand screwBrand = EScrewBrand.MtlsStandardPlus;
            const ESurgeryType surgeryType = ESurgeryType.Reconstruction;

            var console = new TestConsole();
            var context = new BlankImportPreopsContext(console,
                newSppcFilePath, screwBrand, surgeryType);
            var logic = new ImportPreopsLogic(console);

            // Assert variable
            Exception exception = null;
            LogicStatus status = LogicStatus.Success;
            #endregion

            #region Act
            try
            {
                status = logic.Execute(context);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
            #endregion

            #region Assert
            Assert.AreEqual(LogicStatus.Failure, status);
            Assert.IsNull(exception, $"Unhandled exception: {exception?.Message}");
            #endregion
        }

        private void Load_Sppc_With_Given_Options_Exist_Test(ProplanBoneType[] options, LogicStatus expectedStatus)
        {
            #region Arrage
            // Create a folder for load sppc
            var resource = new TestResources();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            var newSppcFilePath = Path.Combine(tempDirectory, "UnitTest.sppc");
            File.Copy(resource.SPPCFilePath, newSppcFilePath);

            // Create context and logic
            const EScrewBrand screwBrand = EScrewBrand.Synthes;
            const ESurgeryType surgeryType = ESurgeryType.Orthognathic;

            var console = new TestConsole();
            var context = new BlankImportPreopsContext(console,
                newSppcFilePath, screwBrand, surgeryType);
            var proPlanLoader = new ProplanLoader(console, newSppcFilePath);
            var mockLoader = new Mock<IPreopLoader>();

            mockLoader.Setup(l => l.PreLoadPreop()).Returns(() =>
            {
                var preLoadData = proPlanLoader.PreLoadPreop();
                var processedPreLoadData = new List<IPreopLoadResult>();

                if (options.Contains(ProplanBoneType.Preop))
                {
                    processedPreLoadData.AddRange(preLoadData.Where(T => ProPlanPartsUtilitiesV2.IsPreopPart(T.Name)));
                }

                if (options.Contains(ProplanBoneType.Original))
                {
                    processedPreLoadData.AddRange(preLoadData.Where(T => ProPlanPartsUtilitiesV2.IsOriginalPart(T.Name)));
                }

                if (options.Contains(ProplanBoneType.Planned))
                {
                    processedPreLoadData.AddRange(preLoadData.Where(T => ProPlanPartsUtilitiesV2.IsPlannedPart(T.Name)));
                }

                return processedPreLoadData;
            });
            mockLoader.Setup(l => l.CleanUp()).Callback(proPlanLoader.CleanUp);
            mockLoader.Setup(l => l.ImportPreop()).Returns(proPlanLoader.ImportPreop);
            IPlane sagittalPlane = IDSPlane.Zero;
            IPlane axialPlane = IDSPlane.Zero;
            IPlane coronalPlane = IDSPlane.Zero;
            IPlane midSagittalPlane = IDSPlane.Zero;
            mockLoader.Setup(l => l.GetPlanes(out sagittalPlane, out axialPlane, out coronalPlane, out midSagittalPlane)).Returns(true);

            var logic = new MockImportPreopsLogic(console, mockLoader.Object);

            // Assert variable
            Exception exception = null;
            LogicStatus status = LogicStatus.Failure;

            #endregion

            #region Act
            try
            {
                status = logic.Execute(context);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
            #endregion

            #region Assert
            Assert.AreEqual(expectedStatus, status);
            Assert.IsNull(exception, $"Unhandled exception: {exception?.Message}");
            #endregion
        }

        [TestMethod]
        public void Failed_To_Load_Sppc_Without_Preop_Exist_Test()
        {
            Load_Sppc_With_Given_Options_Exist_Test(new [] { ProplanBoneType.Original , ProplanBoneType.Planned}, LogicStatus.Failure);
        }

        [TestMethod]
        public void Failed_To_Load_Sppc_Without_Original_Exist_Test()
        {
            Load_Sppc_With_Given_Options_Exist_Test(new[] { ProplanBoneType.Preop, ProplanBoneType.Planned }, LogicStatus.Failure);
        }

        [TestMethod]
        public void Failed_To_Load_Sppc_Without_Planned_Exist_Test()
        {
            Load_Sppc_With_Given_Options_Exist_Test(new[] { ProplanBoneType.Preop, ProplanBoneType.Original }, LogicStatus.Failure);
        }

        [TestMethod]
        public void Success_To_Load_Sppc_With_All_Exist_Test()
        {
            Load_Sppc_With_Given_Options_Exist_Test(new[] { ProplanBoneType.Preop, ProplanBoneType.Original, ProplanBoneType.Planned }, LogicStatus.Success);
        }

        [TestMethod]
        public void Basic_Load_Sppc_To_Director_Test()
        {
            #region Arrage
            // Create a folder for load sppc
            var resource = new TestResources();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            var newSppcFilePath = Path.Combine(tempDirectory, "UnitTest.sppc");
            File.Copy(resource.SPPCFilePath, newSppcFilePath);

            // Create context and logic
            const EScrewBrand screwBrand = EScrewBrand.MtlsStandardPlus;
            const ESurgeryType surgeryType = ESurgeryType.Reconstruction;

            var director = CMFImplantDirectorUtilities.CreateHeadlessCMFImplantDirector();
            var console = new TestConsole();
            var context = new BackEndImportPreopsContext(director, director.Document, console,
                newSppcFilePath, screwBrand, surgeryType);
            var logic = new ImportPreopsLogic(console);

            // Assert variable
            Exception exception = null;
            LogicStatus status = LogicStatus.Failure;
            #endregion

            #region Act
            try
            {
                status = logic.Execute(context);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
            #endregion

            #region Assert
            Assert.IsNull(exception, $"Unhandled exception: {exception?.Message}");
            Assert.AreEqual(LogicStatus.Success, status);
            Assert.AreEqual(screwBrand, director.CasePrefManager.SurgeryInformation.ScrewBrand);
            Assert.AreEqual(surgeryType, director.CasePrefManager.SurgeryInformation.SurgeryType);
            #endregion
        }

        [TestMethod]
        public void Load_Preop_Will_Filter_Preop_CT_if_Composite_Model_Exist_Test()
        {
            #region Arrage
            // Create a folder for load sppc
            var resource = new TestResources();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            var newSppcFilePath = Path.Combine(tempDirectory, "UnitTest.sppc");
            File.Copy(resource.SPPCFilePath, newSppcFilePath);

            // Create context and logic
            const EScrewBrand screwBrand = EScrewBrand.Synthes;
            const ESurgeryType surgeryType = ESurgeryType.Orthognathic;

            var director = CMFImplantDirectorUtilities.CreateHeadlessCMFImplantDirector();
            var console = new TestConsole();
            var context = new BackEndImportPreopsContext(director, director.Document, console,
                newSppcFilePath, screwBrand, surgeryType);
            var proPlanLoader = new ProplanLoader(console, newSppcFilePath);
            var mockLoader = new Mock<IPreopLoader>();

            mockLoader.Setup(l => l.PreLoadPreop()).Returns(() =>
            {
                var preLoadData = proPlanLoader.PreLoadPreop();
                var item = preLoadData.First(i => i.Name.StartsWith("00"));
                preLoadData.Add(new MockPreopLoadResult("00SKU", item));
                preLoadData.Add(new MockPreopLoadResult("00SKU_comp", item));
                preLoadData.Add(new MockPreopLoadResult("00MAN", item));
                preLoadData.Add(new MockPreopLoadResult("00MAN_comp", item));
                return preLoadData;
            });
            mockLoader.Setup(l => l.CleanUp()).Callback(proPlanLoader.CleanUp);
            mockLoader.Setup(l => l.ImportPreop()).Returns(proPlanLoader.ImportPreop);
            IPlane sagittalPlane = IDSPlane.Zero;
            IPlane axialPlane = IDSPlane.Zero;
            IPlane coronalPlane = IDSPlane.Zero;
            IPlane midSagittalPlane = IDSPlane.Zero;
            mockLoader.Setup(l => l.GetPlanes(out sagittalPlane, out axialPlane, out coronalPlane, out midSagittalPlane)).Returns(true);

            var logic = new MockImportPreopsLogic(console, mockLoader.Object);

            // Assert variable
            Exception exception = null;
            LogicStatus status = LogicStatus.Failure;

            #endregion

            #region Act
            try
            {
                status = logic.Execute(context);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
            #endregion

            #region Assert
            Assert.IsNull(exception, $"Unhandled exception: {exception?.Message}");
            Assert.AreEqual(LogicStatus.Success, status);

            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();
            var part00SKU = objectManager.GetBuildingBlock(proPlanImportComponent.GetProPlanImportBuildingBlock("00SKU"));
            var part00SKU_comp = objectManager.GetBuildingBlock(proPlanImportComponent.GetProPlanImportBuildingBlock("00SKU_comp"));
            var part00MAN = objectManager.GetBuildingBlock(proPlanImportComponent.GetProPlanImportBuildingBlock("00MAN"));
            var part00MAN_comp = objectManager.GetBuildingBlock(proPlanImportComponent.GetProPlanImportBuildingBlock("00MAN_comp"));

            Assert.IsNull(part00SKU);
            Assert.IsNotNull(part00SKU_comp);
            Assert.IsNull(part00MAN);
            Assert.IsNotNull(part00MAN_comp);
            #endregion
        }
    }
}
