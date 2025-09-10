using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class ObjectManagerTests
    {
        private IImplantDirector _director;

        [TestInitialize]
        public void TestInitialize()
        {
            var helper = new ImplantDirectorHelper();
            helper.Initialize();

            _director = helper.Director;
        }

        [TestMethod]
        public void New_Building_Block_Tests()
        {
            var buildingBlock = BuildingBlockHelper.CreateBuildingBlock("BuildingBlockA");

            New_Building_Block_Is_Not_Exist(buildingBlock);

            var guid = Added_New_Building_Block_Has_Non_Empty_Identifier(buildingBlock);

            Added_New_Building_Block_Has_Same_Identifier_When_Queried(buildingBlock, guid);

            Added_New_Building_Block_Exists(buildingBlock, guid);

            Find_Object_By_Full_Path_In_Layers(buildingBlock, guid);

            Added_Building_Block_Can_Be_Deleted(guid);

            Deleted_Building_Block_Is_Not_Exist(buildingBlock, guid);
        }

        [TestMethod]
        public void Existing_Building_Block_Tests()
        {
            var buildingBlock = BuildingBlockHelper.CreateBuildingBlock("BuildingBlockB");
            var mesh = BuildingBlockHelper.CreateSphereMesh(5.0);
            var objectManager = new ObjectManager(_director);
            var guid = BuildingBlockHelper.AddNewBuildingBlock(buildingBlock, mesh, objectManager);

            Set_Building_Block_Will_Replace_Existing_Geometry(buildingBlock, mesh);

            //Replacing building block will remove attributes
        }

        [TestMethod]
        public void Add_Extended_Building_Block_Test()
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var casePreferenceData = casePreferencesHelper.AddNewImplantCase();

            var implantComponent = new ImplantCaseComponent();
            var supportMesh = BuildingBlockHelper.CreateSphereMesh(3);

            //act
            var objectManager = new CMFObjectManager(director);
            var implantSupportBb = implantComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreferenceData);
            var guid = objectManager.AddNewBuildingBlock(implantSupportBb, supportMesh);

            //assert
            Assert.IsTrue(guid != Guid.Empty);

            var newSupportMesh = objectManager.GetBuildingBlock(implantSupportBb).Geometry;
            Assert.IsTrue(supportMesh.GetBoundingBox(true).Equals(newSupportMesh.GetBoundingBox(true)));
        }

        private void New_Building_Block_Is_Not_Exist(ImplantBuildingBlock buildingBlock)
        {
            var objectManager = new ObjectManager(_director);
            var hasBuildingBlock = objectManager.HasBuildingBlock(buildingBlock);
            var guid = objectManager.GetBuildingBlockId(buildingBlock);

            Assert.IsFalse(hasBuildingBlock);
            Assert.IsTrue(guid == Guid.Empty);
        }

        private Guid Added_New_Building_Block_Has_Non_Empty_Identifier(ImplantBuildingBlock buildingBlock)
        {
            var objectManager = new ObjectManager(_director);
            var addedGuid = BuildingBlockHelper.AddNewBuildingBlock(buildingBlock, objectManager);

            Assert.IsTrue(addedGuid != Guid.Empty);

            return addedGuid;
        }

        private void Added_New_Building_Block_Has_Same_Identifier_When_Queried(ImplantBuildingBlock buildingBlock, Guid addedGuid)
        {
            var objectManager = new ObjectManager(_director);
            var result = objectManager.GetBuildingBlockId(buildingBlock);

            Assert.IsTrue(addedGuid == result);
        }

        private void Added_New_Building_Block_Exists(ImplantBuildingBlock buildingBlock, Guid addedGuid)
        {
            var objectManager = new ObjectManager(_director);
            var isPresent = objectManager.IsObjectPresent(addedGuid);
            var hasBuildingBlock = objectManager.HasBuildingBlock(buildingBlock);

            Assert.IsTrue(isPresent);
            Assert.IsTrue(hasBuildingBlock);
        }

        private void Added_Building_Block_Can_Be_Deleted(Guid addedGuid)
        {
            var objectManager = new ObjectManager(_director);
            var isdeleted = objectManager.DeleteObject(addedGuid);

            Assert.IsTrue(isdeleted);
        }

        private void Deleted_Building_Block_Is_Not_Exist(ImplantBuildingBlock buildingBlock, Guid addedGuid)
        {
            var objectManager = new ObjectManager(_director);
            var isPresent = objectManager.IsObjectPresent(addedGuid);
            var hasBuildingBlock = objectManager.HasBuildingBlock(buildingBlock);

            Assert.IsFalse(isPresent);
            Assert.IsFalse(hasBuildingBlock);
        }

        private void Set_Building_Block_Will_Replace_Existing_Geometry(ImplantBuildingBlock buildingBlock, Mesh existingMesh)
        {
            var objectManager = new ObjectManager(_director);
            var rhinoObject = objectManager.GetBuildingBlock(buildingBlock);

            Assert.IsNotNull(rhinoObject);

            var guid = rhinoObject.Id;
            var oldGeometry = (Mesh)rhinoObject.Geometry;
            //here we use boundingbox to check for equality
            var isSameAsExisting = oldGeometry.GetBoundingBox(true).Equals(existingMesh.GetBoundingBox(true));

            Assert.IsTrue(isSameAsExisting);

            var mesh = BuildingBlockHelper.CreateSphereMesh(10.0);
            var replacedGuid = objectManager.SetBuildingBlock(buildingBlock, mesh, guid);

            Assert.AreEqual(guid, replacedGuid);

            var replacedRhinoObject = objectManager.GetBuildingBlock(buildingBlock);
            var newGeometry = (Mesh)replacedRhinoObject.Geometry;

            isSameAsExisting = oldGeometry.GetBoundingBox(true).Equals(newGeometry.GetBoundingBox(true));

            Assert.IsFalse(isSameAsExisting);
        }

        private void Find_Object_By_Full_Path_In_Layers(ImplantBuildingBlock buildingBlock, Guid guid)
        {
            var objectManager = new ObjectManager(_director);
            var foundObjects = objectManager.FindLayerObjectsByFullPath(buildingBlock);

            Assert.IsNotNull(foundObjects);

            var objectId = foundObjects.First().Id;
            Assert.AreEqual(objectId, guid);
        }
    }

#endif
}
