using IDS.CMF.TestLib;
using IDS.CMF.TestLib.Components;
using IDS.CMF.TestLib.Utilities;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using JsonUtilities = IDS.Core.V2.Utilities.JsonUtilities;
using MeshType = IDS.CMF.TestLib.Components.MeshType;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ImplantDirectorConverterTests
    {
        [TestMethod]
        public void TestConvertConfigToImplantDirector()
        {
            // Arrange
            var config = new CaseConfig
            {
                OverallInfo =
                {
                    ScrewBrand = EScrewBrand.MtlsStandardPlus,
                    SurgeryType = ESurgeryType.Orthognathic
                }
            };

            config.ProPlanComponents.MedicalCoordinateSystem.SagittalPlane = new IDSPlane(
                RhinoPoint3dConverter.ToIPoint3D(Point3d.Origin),
                RhinoVector3dConverter.ToIVector3D(Vector3d.YAxis)
            );
            config.ProPlanComponents.MedicalCoordinateSystem.CoronalPlane = new IDSPlane(
                RhinoPoint3dConverter.ToIPoint3D(Point3d.Origin),
                RhinoVector3dConverter.ToIVector3D(Vector3d.XAxis)
            );
            config.ProPlanComponents.MedicalCoordinateSystem.AxialPlane = new IDSPlane(
            RhinoPoint3dConverter.ToIPoint3D(Point3d.Origin),
                RhinoVector3dConverter.ToIVector3D(Vector3d.ZAxis)
            );

            var transform = IDSTransform.Identity;
            config.ProPlanComponents.PreopComponents.Add(new ProPlanComponent()
            {
                TransformMatrix = transform,
                PartName = "00SKU_full",
                MeshConfig = new MeshComponent()
                {
                    Type = MeshType.FromBox,
                    Config = JsonUtilities.Serialize(new MeshComponent.ConfigFromBox()
                    {
                        MinPoint3d =(IDSPoint3D)RhinoPoint3dConverter.ToIPoint3D(new Point3d(-10,-10,-10)),
                        MaxPoint3d = (IDSPoint3D)RhinoPoint3dConverter.ToIPoint3D(new Point3d(10,10,10)),
                        Resolution = 5
                    })
                }
            });

            transform = IDSTransform.Identity;
            config.ProPlanComponents.OriginalComponents.Add(new ProPlanComponent()
            {
                TransformMatrix = transform,
                PartName = "01SKU_remaining",
                MeshConfig = new MeshComponent()
                {
                    Type = MeshType.FromBox,
                    Config = JsonUtilities.Serialize(new MeshComponent.ConfigFromBox()
                    {
                        MinPoint3d = (IDSPoint3D)RhinoPoint3dConverter.ToIPoint3D(new Point3d(0, 0, 0)),
                        MaxPoint3d = (IDSPoint3D)RhinoPoint3dConverter.ToIPoint3D(new Point3d(10, 10, 10)),
                        Resolution = 5
                    })
                }
            });

            // Act
            var director = CMFImplantDirectorUtilities.CreateHeadlessCMFImplantDirector();
            config.ParseComponentsToDirector(director, "");

            // Assert

            // Check the screw brand match with the the config in arrange
            Assert.AreEqual(EScrewBrand.MtlsStandardPlus, director.CasePrefManager.SurgeryInformation.ScrewBrand);
            // Check the screw surgery type with the the config in arrange
            Assert.AreEqual(ESurgeryType.Orthognathic, director.CasePrefManager.SurgeryInformation.SurgeryType);

            var preops = ProPlanImportUtilities.GetAllProPlanObjects(director.Document, ProplanBoneType.Preop);
            // Check only 1 Preops part that call 00SKU_full
            Assert.AreEqual(1, preops.Count);
            Assert.IsTrue(preops[0].Name.Contains("00SKU_full"));

            // Check the 00SKU_full is create as we define in arrange or not
            var boundingBoxExpected = new BoundingBox(new Point3d(-10, -10, -10), new Point3d(10, 10, 10));
            var boundingBoxActual = preops[0].Geometry.GetBoundingBox(false);
            Assert.AreEqual(boundingBoxExpected, boundingBoxActual);

            var original = ProPlanImportUtilities.GetAllProPlanObjects(director.Document, ProplanBoneType.Original);
            // Check only 1 Original part that call 01SKU_remaining
            Assert.AreEqual(1, original.Count);
            Assert.IsTrue(original[0].Name.Contains("01SKU_remaining"));

            // Check the 01SKU_remaining is create as we define in arrange or not
            boundingBoxExpected = new BoundingBox(new Point3d(0, 0, 0), new Point3d(10, 10, 10));
            boundingBoxActual = original[0].Geometry.GetBoundingBox(false);
            Assert.AreEqual(boundingBoxExpected, boundingBoxActual);
        }
    }
}
