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
    public class VisibilityTests
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
        }

        [TestMethod]
        public void Visibility_Tests()
        {
            var buildingBlocks = BuildingBlockHelper.CreateAndAddBuildingBlocks(5, _objectManager);

            All_Objects_Are_Not_Visible_After_HideAll_Is_Called(buildingBlocks);

            Object_Is_Visible_After_SetVisible_Is_Called(buildingBlocks);
        }

        private void All_Objects_Are_Not_Visible_After_HideAll_Is_Called(List<ImplantBuildingBlock> buildingBlocks)
        {
            foreach (var buildingBlock in buildingBlocks)
            {
                var rhinoObj = _objectManager.GetBuildingBlock(buildingBlock);
                Assert.IsTrue(rhinoObj.Visible);
            }

            Core.Visualization.Visibility.HideAll(_rhinoDoc);

            foreach (var buildingBlock in buildingBlocks)
            {
                var rhinoObj = _objectManager.GetBuildingBlock(buildingBlock);
                Assert.IsFalse(rhinoObj.Visible);
            }
        }

        private void Object_Is_Visible_After_SetVisible_Is_Called(List<ImplantBuildingBlock> buildingBlocks)
        {
            var random = new Random();
            var index = random.Next(0, buildingBlocks.Count - 1);

            var showPaths = new List<string>
            {
                buildingBlocks[index].Layer
            };

            Core.Visualization.Visibility.SetVisible(_rhinoDoc, showPaths);

            for (var i = 0; i < buildingBlocks.Count; i++)
            {
                var rhinoObj = _objectManager.GetBuildingBlock(buildingBlocks[i]);

                if (i == index)
                {
                    Assert.IsTrue(rhinoObj.Visible);
                }
                else
                {
                    //the rest of the objects should remain hidden
                    Assert.IsFalse(rhinoObj.Visible);
                }
            }
        }
    }

#endif
}
