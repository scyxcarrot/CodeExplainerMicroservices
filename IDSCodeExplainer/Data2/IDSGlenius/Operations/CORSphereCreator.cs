using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Collections.Generic;

namespace IDS.Glenius.Operations
{
    public class CORSphereCreator
    {
        private const double sphereRadius = 2.0;
        
        private readonly GleniusObjectManager objectManager;
        private readonly Box boundaryLimitBox;
        private readonly Point3d glenoidOrigin;
        private readonly Point3d implantOrigin;
        private readonly Point3d? preopOrigin;
        private readonly AnatomicalMeasurements anatomicalMeasurements;

        public CORSphereCreator(GleniusImplantDirector director)
        {
            objectManager = new GleniusObjectManager(director);
            anatomicalMeasurements = director.AnatomyMeasurements;

            Plane headCoordSystem;
            objectManager.GetBuildingBlockCoordinateSystem(IBB.Head, out headCoordSystem);
            glenoidOrigin = anatomicalMeasurements.PlGlenoid.Origin;
            implantOrigin = headCoordSystem.Origin;
            preopOrigin = null;
            if (director.PreopCor != null)
            {
                preopOrigin = director.PreopCor.CenterPoint;
            }
            boundaryLimitBox = GetBoundaryLimitBox();
        }

        public Mesh CreateCORGlenoidSphereForSuperior()
        {
            return CreateCORSphere(glenoidOrigin, anatomicalMeasurements.AxIs);
        }

        public Mesh CreateCORGlenoidSphereForAnterior()
        {
            return CreateCORSphere(glenoidOrigin, -anatomicalMeasurements.AxAp);
        }

        public Mesh CreateCORGlenoidSphereForLateral()
        {
            return CreateCORSphere(glenoidOrigin, anatomicalMeasurements.AxMl);
        }

        public Mesh CreateCORGlenoidSphereForPosterior()
        {
            return CreateCORSphere(glenoidOrigin, anatomicalMeasurements.AxAp);
        }

        public Mesh CreateCORImplantSphereForSuperior()
        {
            return CreateCORSphere(implantOrigin, anatomicalMeasurements.AxIs);
        }

        public Mesh CreateCORImplantSphereForAnterior()
        {
            return CreateCORSphere(implantOrigin, -anatomicalMeasurements.AxAp);
        }

        public Mesh CreateCORImplantSphereForLateral()
        {
            return CreateCORSphere(implantOrigin, anatomicalMeasurements.AxMl);
        }

        public Mesh CreateCORImplantSphereForPosterior()
        {
            return CreateCORSphere(implantOrigin, anatomicalMeasurements.AxAp);
        }

        public Mesh CreateCORPreopSphereForSuperior()
        {
            return preopOrigin.HasValue ? CreateCORSphere(preopOrigin.Value, anatomicalMeasurements.AxIs) : null;
        }

        public Mesh CreateCORPreopSphereForAnterior()
        {
            return preopOrigin.HasValue ? CreateCORSphere(preopOrigin.Value, -anatomicalMeasurements.AxAp) : null;
        }

        public Mesh CreateCORPreopSphereForLateral()
        {
            return preopOrigin.HasValue ? CreateCORSphere(preopOrigin.Value, anatomicalMeasurements.AxMl) : null;
        }

        public Mesh CreateCORPreopSphereForPosterior()
        {
            return preopOrigin.HasValue ? CreateCORSphere(preopOrigin.Value, anatomicalMeasurements.AxAp) : null;
        }

        private Mesh CreateCORSphere(Point3d origin, Vector3d direction)
        {
            var offsetLength = 0.0;

            var line = new Line(origin, direction);
            Interval lineParameters;
            //sphere intersects with boundary limit box
            if (Intersection.LineBox(line, boundaryLimitBox, 0.0, out lineParameters))
            {
                var p1 = line.PointAt(lineParameters.T1);
                var length = (origin - p1).Length;
                if (length > offsetLength)
                {
                    offsetLength = length;
                }
            }

            var point = Point3d.Add(origin, Vector3d.Multiply(direction, offsetLength));
            var sphere = new Sphere(point, sphereRadius);
            var sphereMesh = Mesh.CreateFromBrep(sphere.ToBrep())[0];
            return sphereMesh;
        }

        private Box GetBoundaryLimitBox()
        {
            var combinedBlocks = new List<IBB>
            {
                IBB.ScapulaReamed,
                IBB.Head,
                IBB.CylinderHat,
                IBB.Screw,
                IBB.ScaffoldTop,
                IBB.ScaffoldSide,
                IBB.ScaffoldBottom,
                IBB.PlateBasePlate,
                IBB.Scapula,
                IBB.Humerus
            };
            combinedBlocks.AddRange(BuildingBlocks.GetAllPossibleNonConflictingConflictingEntities());
            
            //Geometry's BoundingBox is aligned to world axis
            //In order to get the BoundingBox aligned to MCS, the geometry needs to be transformed in a way where MCS axes are parallel to world axes (while maintaining it's Origin)
            var mcs = new Plane(anatomicalMeasurements.PlAxial.Origin, anatomicalMeasurements.AxMl, anatomicalMeasurements.AxAp);
            var world = new Plane(mcs.Origin, Plane.WorldXY.XAxis, Plane.WorldXY.YAxis);
            var mcsToWorldTransformation = Transform.PlaneToPlane(mcs, world);

            var bBox = BoundingBox.Empty;
            foreach (var blocks in combinedBlocks)
            {
                var rhinoObjs = objectManager.GetAllBuildingBlocks(blocks);
                foreach (var rhinoObj in rhinoObjs)
                {
                    var tempGeometry = rhinoObj.Geometry.DuplicateShallow();
                    tempGeometry.Transform(mcsToWorldTransformation);
                    bBox.Union(tempGeometry.GetBoundingBox(true));
                    tempGeometry.Dispose();
                }
            }

            //Once retrieved the BoundingBox that is aligned to MCS, a Box is created and transformed back so that it is aligned with the world axis
            //Transforming the BoundingBox itself will not produce the expected result as the BoundingBox's dimension will be re-aligned back to the world axis
            var worldToMcsTransformation = Transform.PlaneToPlane(world, mcs);
            var box = new Box(bBox);
            box.Transform(worldToMcsTransformation);
            return box;
        }
    }
}