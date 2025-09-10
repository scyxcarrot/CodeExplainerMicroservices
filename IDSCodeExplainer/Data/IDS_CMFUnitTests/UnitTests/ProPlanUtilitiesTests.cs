using System;
using System.Collections.Generic;
using System.Drawing;
using IDS.CMF.Constants;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Logics;
using IDS.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ProPlanUtilitiesTests
    {
        private RhinoDoc AddOsteotomyToLayer(string proPlanStage)
        {
            var rhinoDoc = RhinoDoc.CreateHeadless(null);
            var mesh = Mesh.CreateFromBox(new BoundingBox(new Point3d(1, 1, 1),
                new Point3d(3, 3, 3)), 1, 1, 1);
            
            if (rhinoDoc.Layers.Find(proPlanStage, false) < 0)
            {
                rhinoDoc.Layers.Add(proPlanStage, Color.Firebrick);
            }

            var objectAtt = new ObjectAttributes
            {
                LayerIndex = rhinoDoc.GetLayerWithPath(proPlanStage + "::Osteotomy Planes"),
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromMaterial,
                Name = "Osteotomy Planes"
            };

            rhinoDoc.Objects.AddMesh(mesh, objectAtt);

            return rhinoDoc;
        }

        [Obsolete("Rhino Decoupling - This unit test should be deleted once rhino has been decoupled")]
        [TestMethod]
        public void Pro_Plan_Is_Part_Of_Bone_Type_Test()
        {
            var preOpPartName = "00MAN";
            var originalPartName = "01MAN";
            var plannedPartName = "03MAN";

            Assert.IsTrue(ProPlanImportUtilities.IsPartOfBoneType(preOpPartName, ProplanBoneType.Preop),
                "Preop Part checking is incorrect!");

            Assert.IsTrue(ProPlanImportUtilities.IsPartOfBoneType(originalPartName, ProplanBoneType.Original),
                "Original Part checking is incorrect!");

            Assert.IsTrue(ProPlanImportUtilities.IsPartOfBoneType(plannedPartName, ProplanBoneType.Planned),
                "Planned Part checking is incorrect!");
        }

        [TestMethod]
        public void Pro_Plan_Is_Part_Of_Bone_Type_TestV2()
        {
            var preOpPartName = "00MAN";
            var originalPartName = "01MAN";
            var plannedPartName = "03MAN";

            Assert.IsTrue(ProPlanPartsUtilitiesV2.IsPreopPart(preOpPartName),
                "Preop Part checking is incorrect!");

            Assert.IsTrue(ProPlanPartsUtilitiesV2.IsOriginalPart(originalPartName),
                "Original Part checking is incorrect!");

            Assert.IsTrue(ProPlanPartsUtilitiesV2.IsPlannedPart(plannedPartName),
                "Planned Part checking is incorrect!");
        }

        [TestMethod]
        public void Pro_Plan_Get_Part_Name_Without_Surgery_Stage_Test()
        {
            var testName = "01MAN";
            var expectedName = "MAN";

            Assert.IsTrue(ProPlanPartsUtilitiesV2.GetPartNameWithoutSurgeryStage(testName) == expectedName,
                "Output for GetPartNameWithoutSurgeryStage function is incorrect!");
        }

        [TestMethod]
        public void Pro_Plan_Is_Match_With_Import_Json_Test()
        {
            var passName = "05MAN";
            var failName = "01MEN";

            Assert.IsTrue(ProPlanPartsUtilitiesV2.IsNameMatchWithProPlanImportJson(passName),
                "Output for IsNameMatchWithProPlanImportJson is incorrect!");

            Assert.IsFalse(ProPlanPartsUtilitiesV2.IsNameMatchWithProPlanImportJson(failName),
                "Output for IsNameMatchWithProPlanImportJson is incorrect!");
        }

        [TestMethod]
        public void Pro_Plan_Has_Matching_Import_Parts_Test()
        {
            var proPlanParts = new List<string>
            {
                "01MAN",
                "05MAX"
            };

            Assert.IsTrue(ProPlanPartsUtilitiesV2.HasMatchingProPlanImportParts(proPlanParts, "02MAN"),
                "02MAN should find matching part in original!");

            Assert.IsTrue(ProPlanPartsUtilitiesV2.HasMatchingProPlanImportParts(proPlanParts, "01MAX"),
                "01MAX should find matching part in planned!");
        }

        [TestMethod]
        public void Pro_Plan_Get_All_Original_Mesh_Osteotomy_Tests()
        {
            var doc = AddOsteotomyToLayer(ProPlanImport.OriginalLayer);
            var osteotomyParts = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(doc);

            Assert.IsTrue(osteotomyParts.Count > 0, 
                "Could not find osteotomy part in original layer!");

            foreach (var osteotomy in osteotomyParts)
            {
                Assert.IsInstanceOfType(osteotomy, typeof(Mesh),
                    "Return type of GetAllOriginalOsteotomyParts is wrong!");
            }
        }

        [TestMethod]
        public void Pro_Plan_Get_All_Planned_Mesh_Osteotomy_Tests()
        {
            var doc = AddOsteotomyToLayer(ProPlanImport.PlannedLayer);
            var osteotomyParts = ProPlanImportUtilities.GetAllPlannedOsteotomyParts(doc);

            Assert.IsTrue(osteotomyParts.Count > 0,
                "Could not find osteotomy part in planned layer!");

            foreach (var osteotomy in osteotomyParts)
            {
                Assert.IsInstanceOfType(osteotomy, typeof(Mesh),
                    "Return type of GetAllPlannedOsteotomyParts is wrong!");
            }
        }

        [TestMethod]
        public void Pro_Plan_Get_All_Original_Rhino_Object_Osteotomy_Tests()
        {
            var doc = AddOsteotomyToLayer(ProPlanImport.OriginalLayer);
            var osteotomyParts = ProPlanImportUtilities.GetAllOriginalOsteotomyPartsRhinoObjects(doc);

            Assert.IsTrue(osteotomyParts.Count > 0,
                "Could not find osteotomy part in original layer!");

            foreach (var osteotomy in osteotomyParts)
            {
                Assert.IsInstanceOfType(osteotomy, typeof(RhinoObject),
                    "Return type of GetAllOriginalOsteotomyPartsRhinoObjects is wrong!");
            }
        }

        [TestMethod]
        public void Pro_Plan_Get_All_Planned_Rhino_Object_Osteotomy_Tests()
        {
            var doc = AddOsteotomyToLayer(ProPlanImport.PlannedLayer);
            var osteotomyParts = ProPlanImportUtilities.GetAllPlannedOsteotomyPartsRhinoObjects(doc);

            Assert.IsTrue(osteotomyParts.Count > 0,
                "Could not find osteotomy part in planned layer!");

            foreach (var osteotomy in osteotomyParts)
            {
                Assert.IsInstanceOfType(osteotomy, typeof(RhinoObject),
                    "Return type of GetAllPlannedOsteotomyPartsRhinoObjects is wrong!");
            }
        }
    }
}
