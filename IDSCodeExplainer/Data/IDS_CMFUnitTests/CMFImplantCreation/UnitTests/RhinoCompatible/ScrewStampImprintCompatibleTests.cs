using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using IDS.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class ScrewStampImprintCompatibleTests
    {
        [TestMethod]
        public void Check_ScrewStampImprintComponent_Returns_Same_Mesh_As_Original_Algo()
        {
            // Arrange
            var idsScrewHeadPoint = new IDSPoint3D(0, 1, 1);
            var idsScrewDirection = new IDSVector3D(0, 0, 1);
            var console = new TestConsole();

            var screwTypes = new List<string>
            {
                "Matrix Mandible Ø2.0",
                "Matrix Mandible Ø2.4",
                "Matrix Midface Ø1.55",
                "Matrix Orthognathic Ø1.85",
                "Mini Slotted",
                "Mini Slotted Self-Tapping",
                "Mini Slotted Self-Drilling",
                "Micro Slotted",
                "Mini Crossed",
                "Mini Crossed Self-Tapping",
                "Mini Crossed Self-Drilling",
                "Micro Crossed"
            };

            foreach (var screwType in screwTypes)
            {
                // Act
                var originalScrewStampImprint =
                    CreateOldScrewStampImprintMesh(idsScrewHeadPoint, idsScrewDirection, screwType);

                var newScrewStampImprint = CreateNewScrewStampImprintMesh(console, idsScrewHeadPoint, idsScrewDirection, screwType);

                // Assert
                double[] vertexDistances;
                double[] triangleCenterDistances;
                TriangleSurfaceDistanceV2.DistanceBetween(console, 
                    originalScrewStampImprint.Faces.ToFacesArray2D(), originalScrewStampImprint.Vertices.ToVerticesArray2D(),
                    newScrewStampImprint.Faces.ToFacesArray2D(), newScrewStampImprint.Vertices.ToVerticesArray2D(),
                    out vertexDistances, out triangleCenterDistances);
            
                Assert.IsTrue(vertexDistances.Max() < 0.01);
                Assert.IsTrue(triangleCenterDistances.Max() < 0.01);
            }
        }

        private static ScrewStampImprintComponentInfo GetScrewStampImprintComponentInfo(
            IPoint3D screwHeadPoint, IVector3D screwDirection, string screwType)
        {
            var component = new ScrewStampImprintComponentInfo
            {
                DisplayName = $"ForUnitTest",
                IsActual = true,
                ScrewType = screwType,
                ScrewHeadPoint = screwHeadPoint,
                ScrewDirection = screwDirection,
            };

            return component;
        }

        private IMesh CreateNewScrewStampImprintMesh(IConsole console,
            IPoint3D screwHeadPoint, IVector3D screwDirection, string screwType)
        {
            var componentInfo = GetScrewStampImprintComponentInfo(screwHeadPoint, screwDirection, screwType);
            var factory = new ImplantFactory(console);
            var taskResult = factory.CreateImplantAsync(componentInfo).Result;
            var screwStampImprint = taskResult.IntermediateMeshes["StampImprint"];

            return screwStampImprint;
        }

        private IMesh CreateOldScrewStampImprintMesh(IPoint3D screwHeadPoint, IVector3D screwDirection, string screwType)
        {
            var rhinoScrewHeadPoint = RhinoPoint3dConverter.ToPoint3d(screwHeadPoint);
            var rhinoScrewDirection = RhinoVector3dConverter.ToVector3d(screwDirection);
            var shapeOffset = Queries.GetStampImprintShapeOffset(screwType);
            var shapeWidth = Queries.GetStampImprintShapeWidth(screwType);
            var shapeHeight = Queries.GetStampImprintShapeHeight(screwType);
            var shapeSectionHeightRatio = Queries.GetStampImprintShapeSectionHeightRatio(screwType);

            var screwStampImprint =
                ImplantPastilleCreationUtilities.GenerateStampImprintShapeMesh(
                    rhinoScrewHeadPoint,
                    rhinoScrewDirection,
                    shapeOffset,
                    shapeWidth,
                    shapeHeight,
                    shapeSectionHeightRatio);

            return RhinoMeshConverter.ToIDSMesh(screwStampImprint);
        }
    }
}
