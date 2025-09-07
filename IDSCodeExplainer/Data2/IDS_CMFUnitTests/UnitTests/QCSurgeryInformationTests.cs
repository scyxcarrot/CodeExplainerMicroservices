using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.V2.CasePreferences;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class QCSurgeryInformationTests
    {
        #region Screw Brand

        [TestMethod]
        public void MtlsStandardPlus_ScrewBrand_Has_Correct_Display_Value()
        {
            ScrewBrand_Has_Correct_Display_Value(EScrewBrand.MtlsStandardPlus, "Materialise Standard+");
        }

        [TestMethod]
        public void Synthes_ScrewBrand_Has_Correct_Display_Value()
        {
            ScrewBrand_Has_Correct_Display_Value(EScrewBrand.Synthes, "Synthes");
        }

        [TestMethod]
        public void SynthesUsCanada_ScrewBrand_Has_Correct_Display_Value()
        {
            ScrewBrand_Has_Correct_Display_Value(EScrewBrand.SynthesUsCanada, "Synthes (US/Canada)");
        }

        #endregion

        #region Surgery Type

        [TestMethod]
        public void Orthognathic_SurgeryType_Has_Correct_Display_Value()
        {
            SurgeryType_Has_Correct_Display_Value(ESurgeryType.Orthognathic, "Orthognathic");
        }

        [TestMethod]
        public void Reconstruction_SurgeryType_Has_Correct_Display_Value()
        {
            SurgeryType_Has_Correct_Display_Value(ESurgeryType.Reconstruction, "Reconstruction");
        }

        #endregion

        #region Implant Support: Teeth Integration

        [TestMethod]
        public void ImplantSupport_TeethIntegration_Has_Correct_Display_Value_When_No_TeethIntegration()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasTeethIntegration = false;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("TEETH_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["TEETH_INTEG_IMPLANT_SUPPORT"], "N/A");
        }

        [TestMethod]
        public void ImplantSupport_TeethIntegration_Has_Correct_Display_Value_When_Has_TeethIntegration()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasTeethIntegration = true;
            dataModel.ResultingOffsetForTeeth = 0.5;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("TEETH_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["TEETH_INTEG_IMPLANT_SUPPORT"], "0.5");
        }

        [TestMethod]
        public void ImplantSupport_TeethIntegration_Has_Correct_Display_Value_When_Value_Has_AdditionalPrecision()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasTeethIntegration = true;
            dataModel.ResultingOffsetForTeeth = 0.65;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("TEETH_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["TEETH_INTEG_IMPLANT_SUPPORT"], "0.7");
        }

        [TestMethod]
        public void ImplantSupport_TeethIntegration_Has_Correct_Display_Value_When_Value_Has_NoDecimal()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasTeethIntegration = true;
            dataModel.ResultingOffsetForTeeth = 1;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("TEETH_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["TEETH_INTEG_IMPLANT_SUPPORT"], "1.0");
        }

        #endregion

        #region Implant Support: Metal Integration

        [TestMethod]
        public void ImplantSupport_MetalIntegration_Has_Correct_Display_Value_When_No_MetalIntegration()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasMetalIntegration = false;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMOVED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMOVED_METAL_INTEG_IMPLANT_SUPPORT"], "N/A");
            Assert.IsTrue(reportValues.ContainsKey("REMAINED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMAINED_METAL_INTEG_IMPLANT_SUPPORT"], "N/A");
        }

        [TestMethod]
        public void ImplantSupport_MetalIntegration_Has_Correct_Display_Value_When_Has_MetalIntegration_But_No_BuildingBlock()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasMetalIntegration = true;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMOVED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMOVED_METAL_INTEG_IMPLANT_SUPPORT"], "N/A");
            Assert.IsTrue(reportValues.ContainsKey("REMAINED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMAINED_METAL_INTEG_IMPLANT_SUPPORT"], "N/A");
        }

        [TestMethod]
        public void ImplantSupport_RemovedMetalIntegration_Has_Correct_Display_Value_When_Has_MetalIntegration_And_BuildingBlock()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasMetalIntegration = true;
            dataModel.ResultingOffsetForRemovedMetal = 0.5;
            dataModel.ResultingOffsetForRemainedMetal = 0.5;

            var reportValues = Get_Display_Values(dataModel, IBB.ImplantSupportRemovedMetalIntegrationRoI);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMOVED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMOVED_METAL_INTEG_IMPLANT_SUPPORT"], "0.5");
            Assert.IsTrue(reportValues.ContainsKey("REMAINED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMAINED_METAL_INTEG_IMPLANT_SUPPORT"], "N/A");
        }

        [TestMethod]
        public void ImplantSupport_RemainedMetalIntegration_Has_Correct_Display_Value_When_Has_MetalIntegration_And_BuildingBlock()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasMetalIntegration = true;
            dataModel.ResultingOffsetForRemovedMetal = 0.5;
            dataModel.ResultingOffsetForRemainedMetal = 0.5;

            var reportValues = Get_Display_Values(dataModel, IBB.ImplantSupportRemainedMetalIntegrationRoI);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMOVED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMOVED_METAL_INTEG_IMPLANT_SUPPORT"], "N/A");
            Assert.IsTrue(reportValues.ContainsKey("REMAINED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMAINED_METAL_INTEG_IMPLANT_SUPPORT"], "0.5");
        }

        [TestMethod]
        public void ImplantSupport_RemovedMetalIntegration_Has_Correct_Display_Value_When_Value_Has_AdditionalPrecision()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasMetalIntegration = true;
            dataModel.ResultingOffsetForRemovedMetal = 0.65;

            var reportValues = Get_Display_Values(dataModel, IBB.ImplantSupportRemovedMetalIntegrationRoI);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMOVED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMOVED_METAL_INTEG_IMPLANT_SUPPORT"], "0.7");
        }

        [TestMethod]
        public void ImplantSupport_RemovedMetalIntegration_Has_Correct_Display_Value_When_Value_Has_NoDecimal()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasMetalIntegration = true;
            dataModel.ResultingOffsetForRemovedMetal = 1;

            var reportValues = Get_Display_Values(dataModel, IBB.ImplantSupportRemovedMetalIntegrationRoI);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMOVED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMOVED_METAL_INTEG_IMPLANT_SUPPORT"], "1.0");
        }

        [TestMethod]
        public void ImplantSupport_RemainedMetalIntegration_Has_Correct_Display_Value_When_Value_Has_AdditionalPrecision()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasMetalIntegration = true;
            dataModel.ResultingOffsetForRemainedMetal = 0.65;

            var reportValues = Get_Display_Values(dataModel, IBB.ImplantSupportRemainedMetalIntegrationRoI);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMAINED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMAINED_METAL_INTEG_IMPLANT_SUPPORT"], "0.7");
        }

        [TestMethod]
        public void ImplantSupport_RemainedMetalIntegration_Has_Correct_Display_Value_When_Value_Has_NoDecimal()
        {
            var dataModel = new ImplantSupportRoICreationData();
            dataModel.HasMetalIntegration = true;
            dataModel.ResultingOffsetForRemainedMetal = 1;

            var reportValues = Get_Display_Values(dataModel, IBB.ImplantSupportRemainedMetalIntegrationRoI);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMAINED_METAL_INTEG_IMPLANT_SUPPORT"));
            Assert.AreEqual(reportValues["REMAINED_METAL_INTEG_IMPLANT_SUPPORT"], "1.0");
        }

        #endregion

        #region Guide Support: Teeth Integration

        [TestMethod]
        public void GuideSupport_TeethIntegration_Has_Correct_Display_Value_When_No_TeethIntegration()
        {
            var dataModel = new GuideSupportRoICreationData();
            dataModel.HasTeethIntegration = false;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("TEETH_INTEG_GUIDE_SUPPORT"));
            Assert.AreEqual(reportValues["TEETH_INTEG_GUIDE_SUPPORT"], "N/A");
        }

        [TestMethod]
        public void GuideSupport_TeethIntegration_Has_Correct_Display_Value_When_Has_TeethIntegration()
        {
            var dataModel = new GuideSupportRoICreationData();
            dataModel.HasTeethIntegration = true;
            dataModel.ResultingOffsetForTeeth = 0.5;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("TEETH_INTEG_GUIDE_SUPPORT"));
            Assert.AreEqual(reportValues["TEETH_INTEG_GUIDE_SUPPORT"], "0.5");
        }

        [TestMethod]
        public void GuideSupport_TeethIntegration_Has_Correct_Display_Value_When_Value_Has_AdditionalPrecision()
        {
            var dataModel = new GuideSupportRoICreationData();
            dataModel.HasTeethIntegration = true;
            dataModel.ResultingOffsetForTeeth = 0.65;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("TEETH_INTEG_GUIDE_SUPPORT"));
            Assert.AreEqual(reportValues["TEETH_INTEG_GUIDE_SUPPORT"], "0.7");
        }

        [TestMethod]
        public void GuideSupport_TeethIntegration_Has_Correct_Display_Value_When_Value_Has_NoDecimal()
        {
            var dataModel = new GuideSupportRoICreationData();
            dataModel.HasTeethIntegration = true;
            dataModel.ResultingOffsetForTeeth = 1;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("TEETH_INTEG_GUIDE_SUPPORT"));
            Assert.AreEqual(reportValues["TEETH_INTEG_GUIDE_SUPPORT"], "1.0");
        }

        #endregion

        #region Guide Support: Metal Integration

        [TestMethod]
        public void GuideSupport_RemovedMetalIntegration_Has_Correct_Display_Value_When_No_MetalIntegration()
        {
            var dataModel = new GuideSupportRoICreationData();
            dataModel.HasMetalIntegration = false;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMOVED_METAL_INTEG_GUIDE_SUPPORT"));
            Assert.AreEqual(reportValues["REMOVED_METAL_INTEG_GUIDE_SUPPORT"], "N/A");
        }

        [TestMethod]
        public void GuideSupport_RemovedMetalIntegration_Has_Correct_Display_Value_When_Has_MetalIntegration()
        {
            var dataModel = new GuideSupportRoICreationData();
            dataModel.HasMetalIntegration = true;
            dataModel.ResultingOffsetForMetal = 0.5;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMOVED_METAL_INTEG_GUIDE_SUPPORT"));
            Assert.AreEqual(reportValues["REMOVED_METAL_INTEG_GUIDE_SUPPORT"], "0.5");
        }

        [TestMethod]
        public void GuideSupport_RemovedMetalIntegration_Has_Correct_Display_Value_When_Value_Has_AdditionalPrecision()
        {
            var dataModel = new GuideSupportRoICreationData();
            dataModel.HasMetalIntegration = true;
            dataModel.ResultingOffsetForMetal = 0.65;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMOVED_METAL_INTEG_GUIDE_SUPPORT"));
            Assert.AreEqual(reportValues["REMOVED_METAL_INTEG_GUIDE_SUPPORT"], "0.7");
        }

        [TestMethod]
        public void GuideSupport_RemovedMetalIntegration_Has_Correct_Display_Value_When_Value_Has_NoDecimal()
        {
            var dataModel = new GuideSupportRoICreationData();
            dataModel.HasMetalIntegration = true;
            dataModel.ResultingOffsetForMetal = 1;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMOVED_METAL_INTEG_GUIDE_SUPPORT"));
            Assert.AreEqual(reportValues["REMOVED_METAL_INTEG_GUIDE_SUPPORT"], "1.0");
        }

        [TestMethod]
        public void GuideSupport_RemainedMetalIntegration_Has_Correct_Display_Value_When_Has_MetalIntegration()
        {
            var dataModel = new GuideSupportRoICreationData();
            dataModel.HasMetalIntegration = true;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMAINED_METAL_INTEG_GUIDE_SUPPORT"));
            Assert.AreEqual(reportValues["REMAINED_METAL_INTEG_GUIDE_SUPPORT"], "N/A");
        }

        [TestMethod]
        public void GuideSupport_RemainedMetalIntegration_Has_Correct_Display_Value_When_No_MetalIntegration()
        {
            var dataModel = new GuideSupportRoICreationData();
            dataModel.HasMetalIntegration = false;

            var reportValues = Get_Display_Values(dataModel);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("REMAINED_METAL_INTEG_GUIDE_SUPPORT"));
            Assert.AreEqual(reportValues["REMAINED_METAL_INTEG_GUIDE_SUPPORT"], "N/A");
        }

        #endregion

        #region Helper methods

        private void ScrewBrand_Has_Correct_Display_Value(EScrewBrand screwBrand, string expectedDisplayValue)
        {
            var reportValues = Get_Display_Values(screwBrand, ESurgeryType.Orthognathic);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("INFO_SCREW_BRAND"));
            Assert.AreEqual(reportValues["INFO_SCREW_BRAND"], expectedDisplayValue);
        }

        private void SurgeryType_Has_Correct_Display_Value(ESurgeryType surgeryType, string expectedDisplayValue)
        {
            var reportValues = Get_Display_Values(EScrewBrand.Synthes, surgeryType);

            //assert
            Assert.IsTrue(reportValues.ContainsKey("INFO_SURGERY_TYPE"));
            Assert.AreEqual(reportValues["INFO_SURGERY_TYPE"], expectedDisplayValue);
        }

        private Dictionary<string, string> Get_Display_Values(EScrewBrand screwBrand, ESurgeryType surgeryType)
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(screwBrand, surgeryType);
            var information = new QCSurgeryInformation(director);
            var reportValues = new Dictionary<string, string>();

            //act
            information.AssignQcSurgeryInformation(ref reportValues);

            return reportValues;
        }

        private Dictionary<string, string> Get_Display_Values(ImplantSupportRoICreationData dataModel, IBB addBuildingBlock = IBB.Generic)
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            director.ImplantManager.SetImplantSupportRoICreationInformation(dataModel);

            if (addBuildingBlock != IBB.Generic)
            {
                var objectManager = new CMFObjectManager(director);
                var mesh = BuildingBlockHelper.CreateRectangleMesh(new Point3d(-50, -50, -6), new Point3d(50, 50, -1), 0.5);
                var id = objectManager.AddNewBuildingBlock(addBuildingBlock, mesh);
                Assert.IsTrue(id != Guid.Empty);
            }

            var information = new QCSurgeryInformation(director);
            var reportValues = new Dictionary<string, string>();

            //act
            information.AssignQcSurgeryInformation(ref reportValues);

            return reportValues;
        }

        private Dictionary<string, string> Get_Display_Values(GuideSupportRoICreationData dataModel)
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            director.GuideManager.SetGuideSupportRoICreationInformation(dataModel);

            var information = new QCSurgeryInformation(director);
            var reportValues = new Dictionary<string, string>();

            //act
            information.AssignQcSurgeryInformation(ref reportValues);

            return reportValues;
        }

        #endregion
    }

#endif
}
