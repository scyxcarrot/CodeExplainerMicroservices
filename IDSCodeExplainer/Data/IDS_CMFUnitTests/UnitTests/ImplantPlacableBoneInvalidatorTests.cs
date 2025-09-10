using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Invalidation;
using IDS.CMF.V2.CasePreferences;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class ImplantPlacableBoneInvalidatorTests
    {
        [TestMethod]
        public void Invalidator_Can_Accept_New_ImplantPlacableBone()
        {
            //Bug 1076603: C: Null Exception - Import recut (outdated implant support mechanism) when import a new implant placeable part

            //arrange
            const string implantType = "Lefort";
            const string screwType = "Matrix Orthognathic Ø1.85";
            const int caseNum = 1;

            CasePreferencesDataModelHelper.CreateSingleSimpleImplantCaseWithBoneAndSupport(EScrewBrand.Synthes, ESurgeryType.Orthognathic, implantType,
                screwType, caseNum, out var director, out var implantPreferenceModel);
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();            

            //act
            var invalidator = new ImplantPlacableBoneInvalidator(director);
            invalidator.SetInternalGraph();

            var newImplantPlacablePartName = "05RAM_L";
            var newImplantPlacableMesh = Mesh.CreateFromSphere(new Sphere(new Point3d(-3, -3, 0), 2.5), 10, 10);
            var newImplantPlacableBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(newImplantPlacablePartName);
            var guid = objectManager.AddNewBuildingBlockWithTransform(newImplantPlacableBlock, newImplantPlacableMesh, Transform.Identity);

            invalidator.Invalidate(new List<PartProperties>
            {
                new PartProperties(guid, newImplantPlacablePartName, IBB.ProPlanImport)
            });

            //assert
            Assert.AreNotEqual(Guid.Empty, guid);
            var newPart = objectManager.GetBuildingBlock(newImplantPlacableBlock);
            Assert.IsNotNull(newPart);
        }
    }

#endif
}