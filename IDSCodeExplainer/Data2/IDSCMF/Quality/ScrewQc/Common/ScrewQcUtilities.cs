using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.V2.MTLS.Operation;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Interface.Tools;
using IDS.RhinoInterface.Converter;
using Rhino.Geometry;
using System.Collections.Generic;
using Plane = Rhino.Geometry.Plane;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.ScrewQc
{
    public static class ScrewQcUtilities
    {
        public static ScrewQcCheckerManager CreateScrewQcManager(CMFImplantDirector director, IEnumerable<IScrewQcChecker> checkers)
        {
            var implantScrewQcManager = new ScrewQcCheckerManager(director, checkers);
            return implantScrewQcManager;
        }

        public static bool IsGuideScrew(Screw screw)
        {
            var objectManager = new CMFObjectManager(screw.Director);

            if (objectManager.IsGuideComponent(screw))
            {
                return true;
            }

            if (objectManager.IsImplantComponent(screw))
            {
                return false;
            }

            throw new IDSException("Screw is not belong to any implant or guide!");
        }

        #region Checking Utilities
        public static Brep CreateVicinityClearance(Screw screw)
        {
            var clearanceHeight = Queries.GetGuideVicinityClearanceHeight(screw.ScrewType);
            var diameter = Queries.GetGuideVicinityClearance(screw.ScrewType);

            var screwEyeRef = new Brep();
            screwEyeRef.Append(screw.ScrewAideDictionary[ScrewAide.EyeRef] as Brep);

            var eyeOffset = screwEyeRef.Curves3D[0].PointAtStart.Z;
            var centerOfCircle = new Point3d(0, 0, eyeOffset);
            centerOfCircle.Transform(screw.AlignmentTransform);

            var circlePlane = new Plane(centerOfCircle, -screw.Direction);
            var circle = new Circle(circlePlane, diameter / 2);

            return Brep.CreateFromCylinder(new Cylinder(circle, clearanceHeight), true, true);
        }

        public static Brep GenerateQcScrewCylinderBrep(Screw screw, string screwTypeValue)
        {
            var cylinderHeight = screw.BodyOrigin.DistanceTo(screw.TipPoint);
            var screwDiameter = Queries.GetScrewQCCylinderDiameter(screwTypeValue);
            var cylinder = CylinderUtilities.CreateCylinder(screwDiameter, screw.TipPoint, -screw.Direction, cylinderHeight);
            return Brep.CreateFromCylinder(cylinder, true, true);
        }

        public static Brep GenerateQcScrewCylinderBrep(Screw screw)
        {
            return GenerateQcScrewCylinderBrep(screw, screw.ScrewType);
        }

        public static Mesh GenerateQcScrewCapsuleMesh(IConsole console, Screw screw)
        {
            var screwQcData = ScrewQcData.Create(screw);
            var capsuleMesh = ScrewQcOperations.GenerateQcScrewCapsule(console, screwQcData);
            return RhinoMeshConverter.ToRhinoMesh(capsuleMesh);
        }

        public static List<Screw> PerformScrewIntersectionCheck(Screw screw, List<Screw> screws)
        {
            var list = new List<Screw>();
            var qcCylinder = GenerateQcScrewCylinderBrep(screw);

            foreach (var otherScrew in screws)
            {
                if (otherScrew.Id == screw.Id)
                {
                    continue;
                }

                var otherCylinder = GenerateQcScrewCylinderBrep(otherScrew);
                if (CheckScrewIntersectionScrew(qcCylinder, otherCylinder))
                {
                    list.Add(otherScrew);
                }
            }

            return list;
        }

        public static bool CheckScrewIntersectionScrew(Brep qcCylinder, Brep otherCylinder)
        {
            var intersected = BrepUtilities.CheckBrepIntersectionBrep(qcCylinder, otherCylinder);
#if (INTERNAL)
            if (CMFImplantDirector.IsDebugMode)
            {
                var objName = intersected ? "IntersectionCylinder" : "NotIntersectionCylinder";
                InternalUtilities.AddObject(qcCylinder, objName, "Test");
                InternalUtilities.AddObject(otherCylinder, objName, "Test");
            }
#endif
            return intersected;
        }

        #endregion
    }
}
