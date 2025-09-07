using IDS.Core.Operations;
using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    //Fast Track
    public class ProductionRodChamferCreator
    {
        private readonly HeadAlignment headAlignment;
        private readonly GleniusObjectManager objectManager;

        public ProductionRodChamferCreator(GleniusObjectManager objectManager, HeadAlignment headAlignment)
        {
            this.objectManager = objectManager;
            this.headAlignment = headAlignment;
        }

        private Mesh CreateScaffoldShape()
        {
            var scTop = objectManager.GetBuildingBlock(IBB.ScaffoldTop).Geometry as Mesh;
            var scSide = objectManager.GetBuildingBlock(IBB.ScaffoldSide).Geometry as Mesh;
            var scBottom = objectManager.GetBuildingBlock(IBB.ScaffoldBottom).Geometry as Mesh;

            if (scTop != null && scSide != null && scBottom != null)
            {
                return MeshUtilities.AppendMeshes(new List<Mesh>() { scTop, scSide, scBottom });
            }

            return null;
        }

        private Brep ImportProductionRodChamferedStepFile() //Should be in WCS
        {
            Resources resource = new Resources();

            ImporterViaRunScript importer = new ImporterViaRunScript();
            var guid = importer.Import(resource.CommonProductionRodChamferStepFile).FirstOrDefault();
            var rhObj = RhinoDoc.ActiveDoc.Objects.Find(guid).Geometry as Brep;

            if(rhObj != null)
            {
                var dupe = rhObj.DuplicateBrep();
                RhinoDoc.ActiveDoc.Objects.Delete(guid, true);
                return dupe;
            }

            if(guid != Guid.Empty)
            {
                RhinoDoc.ActiveDoc.Objects.Delete(guid, true);
            }

            return null;
        } 

        private Plane GetProductionRodCS()
        {
            Plane prodRodCS;
            if (objectManager.GetBuildingBlockCoordinateSystem(IBB.ProductionRod, out prodRodCS))
            {
                return prodRodCS;
            }
            else
            {
                var headCoordinateSystem = headAlignment.GetHeadCoordinateSystem();
                return new Plane(headCoordinateSystem.Origin, headCoordinateSystem.XAxis, headCoordinateSystem.YAxis);
            }
        }

        private double DistanceBrep2Mesh(Brep from, Mesh to)
        {
            return MeshUtilities.Mesh2MeshDistance(to, Mesh.CreateFromBrep(from).FirstOrDefault()).Min();
        }

        public Brep Create()
        {
            var scaffoldShape = CreateScaffoldShape();

            if (scaffoldShape == null)
            {
                return null;
            }

            var productionRodChamferedPart = ImportProductionRodChamferedStepFile();
            var wcs = new Plane(new Point3d(0, 0, 0), new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));
            var prodRodCs = GetProductionRodCS();

            if (!prodRodCs.IsValid)
            {
                return null;
            }

            var translationDir = -prodRodCs.ZAxis; //Go towards the Scaffold
            translationDir.Unitize();

            //Transform to existing production rod position
            var fullTransform = MathUtilities.CreateTransformation(wcs, prodRodCs);
            productionRodChamferedPart.Transform(fullTransform);
            productionRodChamferedPart.Transform(Transform.Translation(translationDir * -20)); //Put it far from scaffold first

            //Move productionRodChamferedPart
            var currDistance = DistanceBrep2Mesh(productionRodChamferedPart, scaffoldShape);

            double translationSteps = 0.1;
            const double minDistance = 3.0;
            const double maxDistance = 3.2;
            while (currDistance > maxDistance || currDistance < minDistance)
            {
                if (currDistance > maxDistance)
                {
                    var xform = Transform.Translation(translationDir * translationSteps);
                    productionRodChamferedPart.Transform(xform);
                    currDistance = DistanceBrep2Mesh(productionRodChamferedPart, scaffoldShape);
                }
                else
                {
                    var xform = Transform.Translation(translationDir * -translationSteps);
                    productionRodChamferedPart.Transform(xform);
                    currDistance = DistanceBrep2Mesh(productionRodChamferedPart, scaffoldShape);
                }
            }

            //Cut Off Plane
            var scaffoldShapeExtremePt = PointUtilities.FindFurthermostPointAlongVector(scaffoldShape.Vertices.ToPoint3dArray(), -translationDir);
            var cutOffPlaneOrigin = scaffoldShapeExtremePt - (translationDir*35.0);
            var cutOffPlane = new Plane(cutOffPlaneOrigin, -translationDir);

            //Trimming
            var planeSurface = PlaneSurface.CreateThroughBox(cutOffPlane, productionRodChamferedPart.GetBoundingBox(false));
            var br = Brep.CreateFromOffsetFace(Brep.CreateFromSurface(planeSurface).Faces[0], 100, 0.01, false, true);

            var productionRodChamferedCuttedOffPart =
                Brep.CreateBooleanDifference(productionRodChamferedPart, br, 0.01).OrderBy(x => x.Curves3D.Count).LastOrDefault(); //Most Curve3D is the one we want.

            return productionRodChamferedCuttedOffPart;
        }
    }
}
