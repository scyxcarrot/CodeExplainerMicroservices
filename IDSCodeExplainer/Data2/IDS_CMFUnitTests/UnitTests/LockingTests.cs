using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino;
using System;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class LockingTests
    {
        private RhinoDoc _rhinoDoc;
        private ObjectManager _objectManager;

        [TestInitialize]
        public void TestInitialize()
        {
            var helper = new ImplantDirectorHelper();
            helper.Initialize();

            _rhinoDoc = helper.RhinoDoc;
            _objectManager = new ObjectManager(helper.Director);

            //this need to be set for Locking
            IDSPluginHelper.SetDirector(_rhinoDoc.DocumentId, helper.Director);
        }

        [TestMethod]
        public void LockingTest()
        {
            var buildingBlocks = BuildingBlockHelper.CreateAndAddBuildingBlocks(5, _objectManager);

            All_Objects_Are_Locked_After_LockAll_Is_Called(buildingBlocks);

            Object_Is_Unlocked_After_ManageUnlocked_Is_Called(buildingBlocks);

            Other_Objects_Are_Locked_After_ManageUnlocked_Is_Called(buildingBlocks);
        }

        private void All_Objects_Are_Locked_After_LockAll_Is_Called(List<ImplantBuildingBlock> buildingBlocks)
        {
            Core.Operations.Locking.LockAll(_rhinoDoc);

            foreach (var buildingBlock in buildingBlocks)
            {
                var rhinoObj = _objectManager.GetBuildingBlock(buildingBlock);
                Assert.IsTrue(rhinoObj.IsLocked);
            }
        }

        private void Object_Is_Unlocked_After_ManageUnlocked_Is_Called(List<ImplantBuildingBlock> buildingBlocks)
        {
            var random = new Random();
            var index = random.Next(0, buildingBlocks.Count - 1);

            var blocks = new List<ImplantBuildingBlock>
            {
                buildingBlocks[index]
            };

            Core.Operations.Locking.ManageUnlocked(_rhinoDoc, blocks);

            for (var i = 0; i < buildingBlocks.Count; i++)
            {
                var rhinoObj = _objectManager.GetBuildingBlock(buildingBlocks[i]);

                if (i == index)
                {
                    Assert.IsFalse(rhinoObj.IsLocked);
                }
                else
                {
                    //the rest of the objects should remain locked
                    Assert.IsTrue(rhinoObj.IsLocked);
                }
            }
        }

        private void Other_Objects_Are_Locked_After_ManageUnlocked_Is_Called(List<ImplantBuildingBlock> buildingBlocks)
        {
            //make sure that there are enough building blocks to run this test
            Assert.IsTrue(buildingBlocks.Count >= 3);

            Core.Operations.Locking.LockAll(_rhinoDoc);

            var blocks = new List<ImplantBuildingBlock>
            {
                buildingBlocks[0],
                buildingBlocks[1]
            };

            Core.Operations.Locking.ManageUnlocked(_rhinoDoc, blocks);
            Assert.IsFalse(_objectManager.GetBuildingBlock(buildingBlocks[0]).IsLocked);
            Assert.IsFalse(_objectManager.GetBuildingBlock(buildingBlocks[1]).IsLocked);

            var index = 2;
            blocks.Clear();
            blocks.Add(buildingBlocks[index]);
            Core.Operations.Locking.ManageUnlocked(_rhinoDoc, blocks);

            for (var i = 0; i < buildingBlocks.Count; i++)
            {
                var rhinoObj = _objectManager.GetBuildingBlock(buildingBlocks[i]);

                if (i == index)
                {
                    Assert.IsFalse(rhinoObj.IsLocked);
                }
                else
                {
                    //the rest of the objects should remain locked
                    Assert.IsTrue(rhinoObj.IsLocked);
                }
            }
        }
    }

#endif
}
