using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.Testing.UnitTests
{
    // NOTES: in rhino unit tests, you cannot simulate undo with doc.Undo
    // so thats why this is not tested inside unit tests
    [TestClass]
    public class UserDictionaryTests
    {
        [TestMethod]
        public void ModifyUserDictionary_Adds_Dictionary_Key_And_Update_Correctly()
        {
            // arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes,
                ESurgeryType.Orthognathic);
            var objectManager = new CMFObjectManager(director);

            var pointStart = new IDSPoint3D(0, 0, 0);
            var pointEnd = new IDSPoint3D(10, 10, 10);
            var rectangleMesh = BuildingBlockHelper.CreateRectangleMesh(RhinoPoint3dConverter.ToPoint3d(pointStart),
                RhinoPoint3dConverter.ToPoint3d(pointEnd), 1);
            BuildingBlockHelper.AddNewBuildingBlock(BuildingBlocks.Blocks[IBB.OriginalNervesWrapped], rectangleMesh, objectManager);
            var rectangleRhinoObj = objectManager.GetBuildingBlock(BuildingBlocks.Blocks[IBB.OriginalNervesWrapped]);
            rectangleRhinoObj.Attributes.UserDictionary.Set("InitialKey", "InitialValue");

            // assert to check initial conditions
            Assert.IsTrue(rectangleRhinoObj.Attributes.UserDictionary.ContainsKey("InitialKey"));
            Assert.IsFalse(rectangleRhinoObj.Attributes.UserDictionary.ContainsKey("ModifiedKey"));
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["InitialKey"], "InitialValue");

            // act
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "InitialKey", "ModifiedValueFromInitial");
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "ModifiedKey", "ModifiedValue");

            // assert
            Assert.IsTrue(rectangleRhinoObj.Attributes.UserDictionary.ContainsKey("InitialKey"));
            Assert.IsTrue(rectangleRhinoObj.Attributes.UserDictionary.ContainsKey("ModifiedKey"));

            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["InitialKey"], "ModifiedValueFromInitial");
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["ModifiedKey"], "ModifiedValue");
        }

        [TestMethod]
        public void ModifyUserDictionary_Adds_And_Retains_Data()
        {
            // arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes,
                ESurgeryType.Orthognathic);
            
            var objectManager = new CMFObjectManager(director);

            var pointStart = new IDSPoint3D(0, 0, 0);
            var pointEnd = new IDSPoint3D(10, 10, 10);
            var rectangleMesh = BuildingBlockHelper.CreateRectangleMesh(RhinoPoint3dConverter.ToPoint3d(pointStart),
                RhinoPoint3dConverter.ToPoint3d(pointEnd), 1);
            BuildingBlockHelper.AddNewBuildingBlock(BuildingBlocks.Blocks[IBB.OriginalNervesWrapped], rectangleMesh, objectManager);
            var rectangleRhinoObj = objectManager.GetBuildingBlock(BuildingBlocks.Blocks[IBB.OriginalNervesWrapped]);
            rectangleRhinoObj.Attributes.UserDictionary.Set("InitialKey", "InitialValue");

            // assert to check initial conditions
            Assert.IsTrue(rectangleRhinoObj.Attributes.UserDictionary.ContainsKey("InitialKey"));
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["InitialKey"], "InitialValue");

            // act
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_boolean", true);
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_string", "sample_string");
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_int", 10);
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_double", 10.1);
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_guid", Guid.Empty);
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_bool_enumerable", new List<bool> { true, false });
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_int_enumerable", new List<int> { 1, 2 });
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_double_enumerable", new List<double> { 1.1, 2.2 });
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_guid_enumerable", new List<Guid> { Guid.Empty, Guid.Empty });
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_string_enumerable", new List<string> { "testing", "value" });
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_color", Color.AliceBlue);
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_point3d", Point3d.Origin);
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_vector3d", Vector3d.XAxis);
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_transform", Transform.Identity);

            // do this to compare the hash code to see if classes are the same
            var defaultMeshingParam = MeshingParameters.Default;
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_meshing_parameters", defaultMeshingParam);

            var emptyMesh = new Mesh();
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_geometry_base", emptyMesh);

            var doc = director.Document;
            var emptyObjRef = new ObjRef(doc, Guid.Empty);
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_obj_ref", emptyObjRef);
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_geometry_base_enumerable", new List<GeometryBase> { emptyMesh });
            UserDictionaryUtilities.ModifyUserDictionary(rectangleRhinoObj, "sample_obj_ref_enumerable", new List<ObjRef> { emptyObjRef });

            // assert
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_boolean"], true);
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_string"], "sample_string");
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_int"], 10);
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_double"], 10.1);
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_guid"], Guid.Empty);
            CollectionAssert.AreEqual((List<bool>)rectangleRhinoObj.Attributes.UserDictionary["sample_bool_enumerable"], new List<bool> { true, false });
            CollectionAssert.AreEqual((List<int>)rectangleRhinoObj.Attributes.UserDictionary["sample_int_enumerable"], new List<int> { 1, 2 });
            CollectionAssert.AreEqual((List<double>)rectangleRhinoObj.Attributes.UserDictionary["sample_double_enumerable"], new List<double> { 1.1, 2.2 });
            CollectionAssert.AreEqual((List<Guid>)rectangleRhinoObj.Attributes.UserDictionary["sample_guid_enumerable"], new List<Guid> { Guid.Empty, Guid.Empty });
            CollectionAssert.AreEqual((List<string>)rectangleRhinoObj.Attributes.UserDictionary["sample_string_enumerable"], new List<string> { "testing", "value" });
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_color"], Color.AliceBlue);
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_point3d"], Point3d.Origin);
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_vector3d"], Vector3d.XAxis);
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_transform"], Transform.Identity);
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_meshing_parameters"].GetHashCode(), defaultMeshingParam.GetHashCode());
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_geometry_base"].GetHashCode(), emptyMesh.GetHashCode());
            Assert.AreEqual(rectangleRhinoObj.Attributes.UserDictionary["sample_obj_ref"].GetHashCode(), emptyObjRef.GetHashCode());
            Assert.AreEqual(((List<GeometryBase>)rectangleRhinoObj.Attributes.UserDictionary["sample_geometry_base_enumerable"])[0].GetHashCode(), emptyMesh.GetHashCode());
            Assert.AreEqual(((List<ObjRef>)rectangleRhinoObj.Attributes.UserDictionary["sample_obj_ref_enumerable"])[0].GetHashCode(), emptyObjRef.GetHashCode());
        }
    }
}
