using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Invalidation;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class ImplantSupportGuidingOutlineInvalidationHelperTests
    {
        [TestMethod]
        public void OsteotomiesPreop_Is_Updated_After_Regenerate_ImplantSupportGuidingOutlines_NeedToMaintainExistingOutline()
        {
            //OsteotomiesPreop is updated by ImplantSupportGuidingOutlineInvalidationHelper
            OsteotomiesPreop_Is_Updated_After_Regenerate_ImplantSupportGuidingOutlines(true, true);
        }

        [TestMethod]
        public void OsteotomiesPreop_Is_Updated_After_Regenerate_ImplantSupportGuidingOutlines_NoNeedToMaintainExistingOutline()
        {
            //OsteotomiesPreop is updated by ProPlanImportUtilities
            OsteotomiesPreop_Is_Updated_After_Regenerate_ImplantSupportGuidingOutlines(true, false);
        }

        [TestMethod]
        public void OsteotomiesPreop_Is_Updated_After_Regenerate_ImplantSupportGuidingOutlines_HasNoOutlineDependantParts()
        {
            //OsteotomiesPreop is updated by ProPlanImportUtilities
            OsteotomiesPreop_Is_Updated_After_Regenerate_ImplantSupportGuidingOutlines(false, false);
        }

        [TestMethod]
        public void Correct_Touching_Part_Is_Updated_After_Regenerate_ImplantSupportGuidingOutlines()
        {
            //Bug 1077273: C: Null exception when create margin

            //Notes:
            //Bug happens when a search for part with "01GEN", returns part with "01Geniocut"
            //This is because the regex for search pattern is not anchored with ^ and $, and the first result in the list is returned
            //This can only happen if 01Geniocut is added into the document first, then 01GEN

            var director = OsteotomiesPreop_Is_Updated_After_Regenerate_ImplantSupportGuidingOutlines(true, true, new List<string> { "01Geniocut" });

            var objectManager = new CMFObjectManager(director);
            var implantSupportGuidingBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantSupportGuidingOutline);
            var proPlanImportComponent = new ProPlanImportComponent();
            foreach (var implantSupportGuidingBlock in implantSupportGuidingBlocks)
            {
                var hasOriginalPart = ImplantSupportGuidingOutlineHelper.ExtractTouchingOriginalPartId(implantSupportGuidingBlock, out var originalPartGuid);
                var originalPart = director.Document.Objects.Find(originalPartGuid);

                Assert.IsTrue(hasOriginalPart);
                Assert.IsNotNull(originalPart);
                Assert.AreEqual("01GEN", proPlanImportComponent.GetPartName(originalPart.Name));
            }
        }

        private CMFImplantDirector OsteotomiesPreop_Is_Updated_After_Regenerate_ImplantSupportGuidingOutlines(bool hasOutlineDependantParts, bool needToMaintainExistingOutline, List<string> partsToReplace = null)
        {
            //Bug 1073556: C: Margin created is distorted after import recut/update anatomy

            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();

            var preOpPartName = "00MAN_comp";
            var originalPartName = "01GEN";
            var plannedPartName = "05GEN";
            var osteotomyPartName = "01Geniocut";

            var plane = Plane.WorldXY;
            var size = 6.0;
            var osteotomyMesh = Mesh.CreateFromBox(new Box(plane, new Interval(-size, size), new Interval(-size, size), new Interval(-1.0, 1.0)), 10, 10, 10);
            var osteotomyBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(osteotomyPartName);
            objectManager.AddNewBuildingBlockWithTransform(osteotomyBlock, osteotomyMesh, Transform.Identity);

            var preOpMesh = Mesh.CreateFromSphere(new Sphere(new Point3d(3, 3, 0), 2.5), 10, 10);
            var preOpBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(preOpPartName);
            objectManager.AddNewBuildingBlockWithTransform(preOpBlock, preOpMesh, Transform.Identity);

            var originalMesh = preOpMesh.DuplicateMesh();
            var originalBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(originalPartName);
            objectManager.AddNewBuildingBlockWithTransform(originalBlock, originalMesh, Transform.Identity);

            var plannedMesh = originalMesh.DuplicateMesh();
            var plannedBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(plannedPartName);
            objectManager.AddNewBuildingBlockWithTransform(plannedBlock, plannedMesh, Transform.Identity);

            ProPlanImportUtilities.RegenerateImplantSupportGuidingOutlines(objectManager);
            var originalOsteotomiesPreop = director.OsteotomiesPreop.DuplicateMesh();

            var implantSupportGuidingBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantSupportGuidingOutline);
            var outlineIds = implantSupportGuidingBlocks.Select(b => b.Id);

            //act
            var guidingOutlineHelper = new ImplantSupportGuidingOutlineInvalidationHelper(director);
            guidingOutlineHelper.SetPreGuidingOutlineInfo();

            var newPreOpPartName = "00SKU_comp";
            var newPreOpMesh = Mesh.CreateFromSphere(new Sphere(new Point3d(-3, -3, 0), 2.5), 10, 10);
            var newPreOpBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(newPreOpPartName);
            objectManager.AddNewBuildingBlockWithTransform(newPreOpBlock, newPreOpMesh, Transform.Identity);

            if (partsToReplace != null)
            {
                foreach (var partName in partsToReplace)
                {
                    var replacementBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(partName); 
                    var existingPart = objectManager.GetBuildingBlock(replacementBlock);
                    var replacementMesh = ((Mesh)existingPart.Geometry).DuplicateMesh();
                    objectManager.SetBuildingBlock(replacementBlock, replacementMesh, existingPart.Id);
                }
            }

            if (hasOutlineDependantParts)
            {
                foreach (var outline in outlineIds)
                {
                    guidingOutlineHelper.IsGuidingOutlineChanged(outline);
                    if (needToMaintainExistingOutline)
                    {
                        break;
                    }
                }
            }

            guidingOutlineHelper.UpdateImplantSupportGuidingOutlines();

            //assert
            var updatedOsteotomiesPreop = director.OsteotomiesPreop.DuplicateMesh();
            //here we use boundingbox to check for equality
            var isSameAsExisting = updatedOsteotomiesPreop.GetBoundingBox(true).Equals(originalOsteotomiesPreop.GetBoundingBox(true));
            Assert.IsFalse(isSameAsExisting);

            return director;
        }
    }

#endif
}