using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Query;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImplantCreationErrorUtilities = IDS.CMFImplantCreation.Configurations.ErrorUtilities;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Utilities
{
    public static class ImplantPastilleCreationUtilities
    {
        public static List<int> GetPastilleIndexesThatUsesNonPrimaryCreationAlgo(List<IDot> dots)
        {
            var res = new List<int>();

            if (!dots.Any())
            {
                return res;
            }

            for (var i = 0; i < dots.Count; i++)
            {
                var currDot = dots[i];
                if (currDot is DotPastille pastille)
                {
                    if (pastille.CreationAlgoMethod == DotPastille.CreationAlgoMethods[1])
                    {
                        res.Add(i);
                    }
                }
            }

            return res;
        }

        public static ImplantDataModel AdjustPastilles(ImplantDataModel implantDataModel, Mesh supportMesh, IEnumerable<Screw> screws, double pastillePlacementModifier)
        {
            var implant = (ImplantDataModel)implantDataModel.Clone();
            if (pastillePlacementModifier <= 0)
            {
                return implant;
            }

            //update pastille info: location, direction, landmark point   
            var dotList = implant.DotList;
            foreach (var dot in dotList)
            {
                if (dot is DotPastille currentPastille)
                {
                    var adjustedPastille = AdjustPastille(currentPastille, supportMesh, screws, pastillePlacementModifier);
                    currentPastille.Location = adjustedPastille.Location;
                    currentPastille.Direction = adjustedPastille.Direction;
                    currentPastille.Landmark = adjustedPastille.Landmark;
                    currentPastille.CreationAlgoMethod = adjustedPastille.CreationAlgoMethod;
                }
            }

            return implant;
        }

        public static DotPastille AdjustPastille(DotPastille originalPastille, Mesh supportMesh, IEnumerable<Screw> screws, double pastillePlacementModifier)
        {
            var pastille = (DotPastille)originalPastille.Clone();

            if (pastillePlacementModifier <= 0)
            {
                return pastille;
            }

            if (pastille?.Screw != null)
            {
                var screw = screws.First(s => s.Id == pastille.Screw.Id);
                var pointOnMesh = RhinoPoint3dConverter.ToPoint3d(pastille.Location);
                var direction = RhinoVector3dConverter.ToVector3d(pastille.Direction);

                var closestPointOnMesh = supportMesh.ClosestPoint(screw.HeadPoint);
                var closestPointAverageNormal = VectorUtilities.FindAverageNormal(supportMesh, closestPointOnMesh, pastille.Diameter / 2);
                closestPointAverageNormal += direction;
                closestPointAverageNormal /= 2;
                closestPointAverageNormal.Unitize();

                var midPoint = Point3d.Add(pointOnMesh, new Vector3d(closestPointOnMesh - pointOnMesh) * pastillePlacementModifier);
                var points = Intersection.ProjectPointsToMeshes(new List<Mesh> { supportMesh }, new List<Point3d> { midPoint }, -closestPointAverageNormal, 0.0);
                if (points != null && points.Any())
                {
                    var projectedPoint = points.OrderBy(point => point.DistanceTo(midPoint)).First();
                    var translation = projectedPoint - pointOnMesh;

                    pastille.Location = RhinoPoint3dConverter.ToIPoint3D(projectedPoint);
                    var averageNormal = VectorUtilities.FindAverageNormal(supportMesh, projectedPoint, pastille.Diameter / 2);
                    pastille.Direction = RhinoVector3dConverter.ToIVector3D(averageNormal);

                    if (pastille.Landmark != null)
                    {
                        var landmarkPoint = RhinoPoint3dConverter.ToPoint3d(pastille.Landmark.Point);
                        landmarkPoint.Transform(Transform.Translation(translation));
                        pastille.Landmark.Point = RhinoPoint3dConverter.ToIPoint3D(landmarkPoint);
                    }
                }
            }

            return pastille;
        }

        public static Mesh GeneratePastilleCylinderIntersectionMesh(IndividualImplantParams individualImplantParams, DotPastille pastille)
        {
            var wrapRatio = individualImplantParams.WrapOperationOffsetInDistanceRatio;
            double compensatePastille;
            double wrapValue;
            ImplantCreationUtilities.CalculatePastilleParameters(pastille, wrapRatio, out wrapValue, out compensatePastille);
            return ImplantCreationUtilities.GeneratePastilleCylinderIntersectionMesh(pastille, compensatePastille, 0.5);
        }

        public static Mesh GeneratePastilleExtrudeCylinderIntersectionMesh(IndividualImplantParams individualImplantParams, DotPastille pastille)
        {
            var wrapRatio = individualImplantParams.WrapOperationOffsetInDistanceRatio;
            double compensatePastille;
            double wrapValue;
            ImplantCreationUtilities.CalculatePastilleParameters(pastille, wrapRatio, out wrapValue, out compensatePastille);
            return ImplantCreationUtilities.GeneratePastilleCylinderIntersectionMesh(pastille, compensatePastille, 0.025);
        }

        public static Mesh GeneratePastilleSphereIntersectionMesh(IndividualImplantParams individualImplantParams, DotPastille pastille)
        {
            var wrapRatio = individualImplantParams.WrapOperationOffsetInDistanceRatio;
            double compensatePastille;
            double wrapValue;
            ImplantCreationUtilities.CalculatePastilleParameters(pastille, wrapRatio, out wrapValue, out compensatePastille);
            return ImplantCreationUtilities.GeneratePastilleSphereIntersectionMesh(pastille, compensatePastille, 0.5);
        }

        public static Mesh GeneratePastilleExtrudeSphereIntersectionMesh(IndividualImplantParams individualImplantParams, DotPastille pastille)
        {


            var wrapRatio = individualImplantParams.WrapOperationOffsetInDistanceRatio;
            double compensatePastille;
            double wrapValue;
            ImplantCreationUtilities.CalculatePastilleParameters(pastille, wrapRatio, out wrapValue, out compensatePastille);
            return ImplantCreationUtilities.GeneratePastilleSphereIntersectionMesh(pastille, compensatePastille, 0.025);
        }
        /// <summary>
        /// Steps to generate implant pastille and landmark
        /// 1. Create initial sphere to get intersection curves between tube and support mesh. 
        /// If pastille radius are half smaller than connection thickness, wrap ratio value to based on connection thickness.
        /// Calculation for this tube radius = radius - wrap ratio*connection(to compensate wrap later).
        /// pastille radius = [ sphere radius/2] - [wrap ratio*radius or wrap ratio*thickness]
        /// 2. Get intersection curves between create sphere and support mesh.
        /// 3. Call "GenerateImplantComponent", refer comment to add that function.
        /// 4. If landmark exist, call "GenerateImplantComponent" as well, refer comment to add that function.
        /// </summary>
        public static bool GenerateImplantPastillesAndLandmarks(ref ImplantDataModel implantDataModel, CasePreferenceDataModel casePreferencesData,
            IndividualImplantParams individualImplantParams, LandmarkImplantParams landmarkImplantParams, Mesh supportMeshRoI, Mesh supportMeshFull, IEnumerable<Screw> screws,
            out List<Mesh> implantPastilleMeshes, out Mesh cylinderMeshes, out List<Mesh> pastilleLandmarkMeshes)
        {
            implantPastilleMeshes = new List<Mesh>();
            cylinderMeshes = new Mesh();
            pastilleLandmarkMeshes = new List<Mesh>();

            var errorMessages = new List<string>();
            foreach (var connection_pt in implantDataModel.DotList)
            {
                if (connection_pt is DotPastille pastille)
                {
                    var currPastille = pastille;

                    Mesh pastilleCylinder;
                    Mesh implantPastilleMesh;
                    Mesh pastilleLandmarkMesh;
                    if (!GenerateImplantPastilleAndLandmark(ref currPastille, casePreferencesData, individualImplantParams, landmarkImplantParams,
                        supportMeshRoI, supportMeshFull, screws, out implantPastilleMesh, out pastilleLandmarkMesh, out pastilleCylinder, ref errorMessages))
                    {
                        foreach (var errorMessage in errorMessages)
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Error, errorMessage);
                        }
                        return false;
                    }

                    implantPastilleMeshes.Add(implantPastilleMesh);
                    cylinderMeshes.Append(pastilleCylinder);
                    pastilleLandmarkMeshes.Add(pastilleLandmarkMesh);
                }
            }
            return true;
        }

        public static bool GenerateImplantPastilleAndLandmark(ref DotPastille pastille,
            CasePreferenceDataModel casePreferencesData,
            IndividualImplantParams individualImplantParams, LandmarkImplantParams landmarkImplantParams,
            Mesh supportMeshRoI,
            Mesh supportMeshFull, IEnumerable<Screw> screws, out Mesh implantPastilleMesh,
            out Mesh pastilleLandmarkMesh,
            out Mesh pastilleCylinder, ref List<string> errorMessages)
        {
            var entityName = "Pastille";
            pastilleLandmarkMesh = null;
            pastilleCylinder = GeneratePastilleCylinderIntersectionMesh(individualImplantParams, pastille);
            var pastilleExtrudeCylinder =
                GeneratePastilleExtrudeCylinderIntersectionMesh(individualImplantParams, pastille);
            var currPastille = pastille;
            var currScrew = screws.First(s => s.Id == currPastille.Screw.Id);

            try
            {
                Mesh top;
                Mesh bottom;
                Mesh stitched;
                GenerateImplantPastille(pastille, currScrew, casePreferencesData.NCase, individualImplantParams,
                    supportMeshRoI, supportMeshFull, 0,
                    out implantPastilleMesh, pastilleCylinder, pastilleExtrudeCylinder, out top, out bottom,
                    out stitched);

                pastille.CreationAlgoMethod = DotPastille.CreationAlgoMethods[0];

                entityName = "Landmark";
                pastilleLandmarkMesh = CreateLandmarkBrep(pastille, casePreferencesData, individualImplantParams,
                    landmarkImplantParams,
                    supportMeshRoI, supportMeshFull, new List<Mesh> { implantPastilleMesh });
            }
            catch (Exception e)
            {
                try
                {
                    pastilleCylinder = GeneratePastilleSphereIntersectionMesh(individualImplantParams, pastille);
                    var extrudeSphere =
                        GeneratePastilleExtrudeSphereIntersectionMesh(individualImplantParams, pastille);
                    Mesh top;
                    Mesh bottom;
                    Mesh stitched;
                    GenerateImplantPastille(pastille, currScrew, casePreferencesData.NCase, individualImplantParams,
                        supportMeshRoI, supportMeshFull, 0,
                        out implantPastilleMesh, extrudeSphere, null, out top, out bottom, out stitched);

                    pastille.CreationAlgoMethod = DotPastille.CreationAlgoMethods[1];

                    entityName = "Landmark";
                    pastilleLandmarkMesh = CreateLandmarkBrep(pastille, casePreferencesData,
                        individualImplantParams, landmarkImplantParams,
                        supportMeshRoI, supportMeshFull, new List<Mesh> { implantPastilleMesh });

                    return true;
                }
                catch (Exception)
                {
                    errorMessages.AddRange(ReportScrewRelatedException(entityName, pastille, screws,
                        casePreferencesData.NCase, e));
                    implantPastilleMesh = null;
                    return false;
                }
            }

            return true;
        }

        public static bool GenerateImplantPastille(DotPastille pastille, Screw screw, int implantNum,
            IndividualImplantParams individualImplantParams, Mesh supportMeshRoI, Mesh supportMeshFull, int a,
            out Mesh implantPastilleMesh, Mesh cylinderMesh, Mesh extrudeCylinderMesh,
            out Mesh top, out Mesh bottom, out Mesh stitched)
        {
            var wrapRatio = individualImplantParams.WrapOperationOffsetInDistanceRatio;

            double compensatePastille;
            double wrapValue;
            ImplantCreationUtilities.CalculatePastilleParameters(pastille, wrapRatio, out wrapValue, out compensatePastille);

            top = new Mesh();
            bottom = new Mesh();
            stitched = new Mesh();

            return GenerateImplantPastille(pastille, screw, implantNum,
                individualImplantParams, supportMeshRoI, supportMeshFull, a, compensatePastille,
                wrapValue, out implantPastilleMesh, cylinderMesh, extrudeCylinderMesh,
                out top, out bottom, out stitched);
        }

        private static bool GenerateImplantPastille(DotPastille pastille, Screw screw, int implantNum,
            IndividualImplantParams individualImplantParams, Mesh supportMeshRoI, Mesh supportMeshFull, int a, double compensatePastille,
            double wrapValue, out Mesh implantPastilleMesh, Mesh cylinderMesh, Mesh extrudeCylinderMesh,
            out Mesh top, out Mesh bottom, out Mesh stitched)
        {
            implantPastilleMesh = new Mesh();

            Mesh extrusion = null;
            if (extrudeCylinderMesh != null)
            {
                var extrudeIntersectionCurve = ImplantCreationUtilities.GetIntersectionCurveForPastille(extrudeCylinderMesh,
                    RhinoPoint3dConverter.ToPoint3d(pastille.Location), supportMeshRoI, supportMeshFull, RhinoVector3dConverter.ToVector3d(pastille.Direction), implantNum, a);
                var pastilleDirection = RhinoVector3dConverter.ToVector3d(pastille.Direction);
                pastilleDirection.Unitize();
                extrusion = Extrude(extrudeIntersectionCurve, pastilleDirection * pastille.Thickness);
            }

#if (INTERNAL)
            InternalUtilities.AddObject(cylinderMesh, $"Cylinder of {a}", $"Test Implant::Implant {implantNum}");
#endif


#if (INTERNAL)
            InternalUtilities.AddObject(cylinderMesh, $"MeshPastille of {a}",
                $"Test Implant::Implant {implantNum}");
#endif

            Curve intersectionCurve = null;
            try
            {
                intersectionCurve = ImplantCreationUtilities.GetIntersectionCurveForPastille(cylinderMesh,
                    RhinoPoint3dConverter.ToPoint3d(pastille.Location), supportMeshRoI, supportMeshFull,
                    RhinoVector3dConverter.ToVector3d(pastille.Direction), implantNum, a);
#if (INTERNAL)
                InternalUtilities.AddCurve(intersectionCurve, $"Curve Pastille of {a}",
                    $"Test Implant::Implant {implantNum}", Color.Magenta);
#endif

                Mesh finalTop;
                Mesh finalBottom;
                Mesh finalStitched;
                Mesh implantPastille = null;
                implantPastille =
                    GenerateImplantPastilleComponent(intersectionCurve, screw, supportMeshRoI, pastille.Thickness, wrapValue,
                        individualImplantParams, RhinoPoint3dConverter.ToPoint3d(pastille.Location), extrusion, out finalTop,
                        out finalBottom, out finalStitched);

                top = new Mesh();
                bottom = new Mesh();
                stitched = new Mesh();
                top.Append(finalTop);
                bottom.Append(finalBottom);
                stitched.Append(finalStitched);

                intersectionCurve.Dispose();
#if (INTERNAL)
                InternalUtilities.AddObject(implantPastille, $"Pastille of {a}",
                    $"Test Implant::Implant {implantNum}");
#endif

                implantPastilleMesh = implantPastille;

                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public static Mesh CreateLandmarkBrep(DotPastille pastille, CasePreferenceDataModel casePreferencesData,
            IndividualImplantParams individualImplantParams, LandmarkImplantParams landmarkImplantParams, Mesh supportMeshRoI, Mesh supportMeshFull,
            List<Mesh> pastilleMeshes)
        {
            var a = 0;

            if (pastille != null && pastille.Landmark != null)
            {
                var wrapOffset = pastille.Thickness * 0.25; //Y

                //Step 1 - Create a base of the landmark for Step 2 to take place.
                var landmarkBase = CreateBaseLandmarkBrep(landmarkImplantParams, pastille, wrapOffset);
                var landmarkBaseMesh = MeshUtilities.AppendMeshes(Mesh.CreateFromBrep(landmarkBase));
                landmarkBaseMesh.Compact();

                var intersectionBaseCurve = ImplantCreationUtilities.GetIntersectionCurveForPastille(
                    landmarkBaseMesh, RhinoPoint3dConverter.ToPoint3d(pastille.Location), supportMeshRoI, supportMeshFull,
                    RhinoVector3dConverter.ToVector3d(pastille.Direction), casePreferencesData.NCase, a);

                //Step 2 - To maintain Shape, create the one with actual size compensated by the offset.
                var landmark = CreateLandmarkBrep(landmarkImplantParams, pastille, wrapOffset);
                var landmarkMesh = MeshUtilities.AppendMeshes(Mesh.CreateFromBrep(landmark));
                landmarkMesh.Compact();

                var intersectionCurve = ImplantCreationUtilities.GetIntersectionCurveForPastille(
                    landmarkMesh, RhinoPoint3dConverter.ToPoint3d(pastille.Location), supportMeshRoI, supportMeshFull,
                    RhinoVector3dConverter.ToVector3d(pastille.Direction), casePreferencesData.NCase, a);

                var pastilleDirection = RhinoVector3dConverter.ToVector3d(pastille.Direction);
                pastilleDirection.Unitize();

                var extrusion = Extrude(
                    intersectionCurve, 
                    pastilleDirection * pastille.Thickness);
                var finalTop = new Mesh();
                var finalBottom = new Mesh();
                var finalStitched = new Mesh();

                Mesh pastilleMesh = null;
                double dist = Double.MaxValue;
                pastilleMeshes.ForEach(x =>
                {
                    var pastilleLoc = RhinoPoint3dConverter.ToPoint3d(pastille.Location);
                    var tmpPt = x.ClosestPoint(RhinoPoint3dConverter.ToPoint3d(pastille.Location));
                    if (tmpPt.DistanceTo(pastilleLoc) < dist)
                    {
                        pastilleMesh = x;
                        dist = tmpPt.DistanceTo(pastilleLoc);
                    }
                });

                return GenerateImplantLandmarkComponent(intersectionBaseCurve, supportMeshRoI,
                    pastille.Thickness, wrapOffset, individualImplantParams, RhinoPoint3dConverter.ToPoint3d(pastille.Location),
                    extrusion, pastilleMesh, out finalTop, out finalBottom, out finalStitched);
            }

            return null;
        }

        public static List<string> ReportScrewRelatedException(string entityName, DotPastille pastille, IEnumerable<Screw> screws, int implantNum, Exception e)
        {
            var errorMessages = new List<string>();

            Msai.TrackException(e, "CMF");
            if (pastille?.Screw != null)
            {
                var screw = screws.First(s => s.Id == pastille.Screw.Id);
                errorMessages.Add($"{entityName} for screw {screw.Index}.I{implantNum} could not be created.");
            }

            if (e is IDSException idsException)
            {
                errorMessages.Add($"{idsException.Message}");
            }
            else if (e is MtlsException exception)
            {
                errorMessages.Add($"Operation {exception.OperationName} failed to complete.\n{exception.Message}");
            }
            else
            {
                errorMessages.Add($"The following unknown exception was thrown. Please report this to the development team.\n{e}");
            }

            return errorMessages;
        }

        private static Brep CreateBaseLandmarkBrep(LandmarkImplantParams landmarkImplantParams, DotPastille pastille, double intendedWrapOffset)
        {
            var radius = pastille.Diameter * 0.5;
            var landmarkBrepFactory = new LandmarkBrepFactory(landmarkImplantParams);
            var transform = landmarkBrepFactory.GetTransform(pastille.Landmark.LandmarkType,
                RhinoPoint3dConverter.ToPoint3d(pastille.Location), RhinoVector3dConverter.ToVector3d(pastille.Direction),
                RhinoPoint3dConverter.ToPoint3d(pastille.Landmark.Point), pastille.Diameter / 2);

            var baseLandmark = landmarkBrepFactory.CreateBaseLandmark(pastille.Landmark.LandmarkType, pastille.Thickness * 2, radius, intendedWrapOffset);
            baseLandmark.Transform(transform);
            return baseLandmark;
        }

        public static Brep CreateLandmarkBrep(LandmarkImplantParams landmarkImplantParams, DotPastille pastille, double wrapOffsetValue)
        {
            if (pastille.Landmark == null)
            {
                return null;
            }

            var radius = pastille.Diameter * 0.5;
            var wrapOffset = wrapOffsetValue;

            var landmarkBrepFactory = new LandmarkBrepFactory(landmarkImplantParams);
            var landmarkBrep = landmarkBrepFactory.CreateLandmarkAdjustedByWrapOffset(pastille.Landmark.LandmarkType, pastille.Diameter, radius, wrapOffset); //increase thickness (extrude) in order to make sure intersection happens with support mesh
            var transform = landmarkBrepFactory.GetTransform(pastille.Landmark.LandmarkType, RhinoPoint3dConverter.ToPoint3d(pastille.Location),
                RhinoVector3dConverter.ToVector3d(pastille.Direction), RhinoPoint3dConverter.ToPoint3d(pastille.Landmark.Point), pastille.Diameter / 2);
            landmarkBrep.Transform(transform);

            return landmarkBrep;
        }

        /// <summary>
        /// 1. Calculate wrap offset value which slightly larger than create sphere wrap offset to make sure there intersection afterwards with support mesh.
        /// 2. If radius smaller than thickness, offset distance of upper and lower surface will be different. or else it will have same offset distance.
        /// 3. Offset been done 2 time in order to create "radial" effect which bottom implant facing towards inside.
        /// 4. Offset direction will based on average direction of closest vertex.
        /// 5. Wrap final connection with wrap value that been compensate at beginning(wrap ratio*connection width or wrap ratio*connection thickness).
        /// </summary>
        /// <param name="interCurve"></param>
        /// <param name="supportMesh"></param>
        /// <param name="thickness"></param>
        /// <param name="wrapValue"></param>
        /// <param name="individualImplantParams"></param>
        /// <param name="pastilleCenter"></param>
        /// <returns></returns>
        private static Mesh GenerateImplantPastilleComponent(Curve interCurve, Screw screw, Mesh supportMesh, double thickness, double wrapValue,
            IndividualImplantParams individualImplantParams, Point3d pastilleCenter, Mesh extrusion, out Mesh top, out Mesh bottom, out Mesh stitched)
        {
            var finalWrapOffset = wrapValue * individualImplantParams.WrapOperationOffsetInDistanceRatio;
            var offsetDistance = (thickness - finalWrapOffset) / 2;
            var offsetDistanceUpper = thickness - finalWrapOffset;
            if (offsetDistanceUpper < 0.00)
            {
                throw new IDSException("Implant Pastille thickness and diameter ratio invalid.");
            }

            if (!interCurve.IsClosed)
            {
                throw new IDSException(ImplantCreationErrorUtilities.ImplantCreationErrorCurveNotClosed);
            }

            var connectionSurface = SurfaceUtilities.GetPatch(supportMesh, interCurve);

            var doUniformOffset = extrusion == null;
            var pointOnMesh = supportMesh.ClosestPoint(pastilleCenter);
            var norm = doUniformOffset ? Vector3d.Unset : VectorUtilities.FindNormalAtPoint(pointOnMesh, supportMesh, 2.0);
            norm.Unitize();

            var vertexOffsettedLower = new List<Point3d>();
            var vertexOffsettedUpper = new List<Point3d>();
            foreach (var vertex in connectionSurface.Vertices)
            {
                if (doUniformOffset)
                {
                    var ptOnSpt = supportMesh.ClosestPoint(vertex);
                    norm = VectorUtilities.FindNormalAtPoint(ptOnSpt, supportMesh, 2.0);
                    norm.Unitize();
                }

                var ptUpper = new Point3d(vertex) + offsetDistanceUpper * norm;
                ptUpper = ImplantCreationUtilities.EnsureVertexIsOnSameLevelAsThickness(supportMesh, ptUpper, offsetDistanceUpper);
                vertexOffsettedUpper.Add(ptUpper);

                var pointOnSupport = supportMesh.ClosestPoint(ptUpper);
                var normal = VectorUtilities.FindNormalAtPoint(pointOnSupport, supportMesh, 2.0);
                normal.Unitize();

                var ptLower = new Point3d(ptUpper) - (offsetDistanceUpper - offsetDistance) * normal;
                vertexOffsettedLower.Add(ptLower);
            }

            var smallestDetail = individualImplantParams.WrapOperationSmallestDetails;
            var gapClosingDistance = individualImplantParams.WrapOperationGapClosingDistance;

            var offsetMesh = ImplantCreationUtilities.OptimizeOffsetForPastille(new List<List<Point3d>>() { vertexOffsettedLower, vertexOffsettedUpper },
                connectionSurface, extrusion, out top, out bottom, out stitched);

            var pastilleMeshes = new List<Mesh>();
            pastilleMeshes.Add(offsetMesh);

            //Add Screw Stamp Imprint Smarties /////////////////////////////////////////////////////////////
            var screwMgr = new ScrewManager(screw.Director);
            var cp = screwMgr.GetImplantPreferenceTheScrewBelongsTo(screw);

            var shapeOffset = Queries.GetStampImprintShapeOffset(cp.CasePrefData.ScrewTypeValue);
            var shapeWidth = Queries.GetStampImprintShapeWidth(cp.CasePrefData.ScrewTypeValue);
            var shapeHeight = Queries.GetStampImprintShapeHeight(cp.CasePrefData.ScrewTypeValue);
            var shapeSectionHeightRatio = Queries.GetStampImprintShapeSectionHeightRatio(cp.CasePrefData.ScrewTypeValue);
            var shapeCreationMaxPastilleThickness =
                Queries.GetStampImprintShapeCreationMaxPastilleThickness(cp.CasePrefData.ScrewTypeValue); //Only create stamp imprint if lower than specified pastille thickness.

            if (IsNeedToAddStampImprintShape(thickness, shapeCreationMaxPastilleThickness))
            {
                var stampImprintShapeMesh = GenerateStampImprintShapeMesh(screw.HeadPoint, screw.Direction,
                    shapeOffset, shapeWidth, shapeHeight, shapeSectionHeightRatio);

                if (stampImprintShapeMesh != null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Implant Screw {cp.CasePrefData.ScrewTypeValue}'s Stamp Imprint Shape downward offset value {StringUtilities.DoubleStringify(shapeOffset, 2)}");

                    pastilleMeshes.Add(stampImprintShapeMesh);
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic,
                        $"Implant Screw {cp.CasePrefData.ScrewTypeValue}'s Stamp Imprint Shape has Width or Height with 0 value, hence it is skipped");
                }
            }

            var wrappedMesh = ImplantCreationUtilities.WrapOffset(MeshUtilities.AppendMeshes(pastilleMeshes), smallestDetail, gapClosingDistance, finalWrapOffset);

            connectionSurface.Dispose();

            vertexOffsettedLower.Clear();
            vertexOffsettedUpper.Clear();

            return wrappedMesh;
        }

        public static bool IsNeedToAddStampImprintShape(double pastilleThickness, double shapeCreationMaxPastilleThickness)
        {
            return pastilleThickness <= shapeCreationMaxPastilleThickness;
        }

        /// <summary>
        /// 1. Calculate wrap offset value which slightly larger than create sphere wrap offset to make sure there intersection afterwards with support mesh.
        /// 2. If radius smaller than thickness, offset distance of upper and lower surface will be different. or else it will have same offset distance.
        /// 3. Offset been done 2 time in order to create "radial" effect which bottom implant facing towards inside.
        /// 4. Offset direction will based on average direction of closest vertex.
        /// 5. Wrap final connection with wrap value that been compensate at beginning(wrap ratio*connection width or wrap ratio*connection thickness).
        /// </summary>
        /// <param name="interCurve"></param>
        /// <param name="supportMesh"></param>
        /// <param name="thickness"></param>
        /// <param name="wrapValue"></param>
        /// <param name="individualImplantParams"></param>
        /// <param name="pastilleCenter"></param>
        /// <returns></returns>
        private static Mesh GenerateImplantLandmarkComponent(Curve interCurve, Mesh supportMesh, double thickness, double wrapValue,
            IndividualImplantParams individualImplantParams, Point3d pastilleCenter, Mesh extrusion, Mesh pastille, out Mesh top, out Mesh bottom, out Mesh stitched)
        {
            var finalWrapOffset = wrapValue;
            var offsetDistance = (wrapValue - 0.1);
            var offsetDistanceUpper = thickness - finalWrapOffset;

            if (!interCurve.IsClosed)
            {
#if (INTERNAL)
                InternalUtilities.AddCurve(interCurve, "ErrorAnalysis", $"Test Implant", Color.Red);
#endif

                throw new IDSException(ImplantCreationErrorUtilities.ImplantCreationErrorCurveNotClosed);
            }

            var connectionSurface = SurfaceUtilities.GetPatch(supportMesh, interCurve);

            var pointOnMesh = supportMesh.ClosestPoint(pastilleCenter);
            var norm = VectorUtilities.FindNormalAtPoint(pointOnMesh, supportMesh, 2.0);
            norm.Unitize();

            var vertexOffsettedLower = new List<Point3d>();
            var vertexOffsettedUpper = new List<Point3d>();
            foreach (var vertex in connectionSurface.Vertices)
            {
                var ptUpper = new Point3d(vertex) + offsetDistanceUpper * norm;
                ptUpper = ImplantCreationUtilities.EnsureVertexIsOnSameLevelAsThickness(supportMesh, ptUpper, offsetDistanceUpper);
                vertexOffsettedUpper.Add(ptUpper);

                var ptLower = new Point3d(vertex) + offsetDistance * norm;
                vertexOffsettedLower.Add(ptLower);
            }

            var smallestDetail = individualImplantParams.WrapOperationSmallestDetails;
            var gapClosingDistance = individualImplantParams.WrapOperationGapClosingDistance;

            var offsetMesh = ImplantCreationUtilities.OptimizeOffsetForLandmark(new List<List<Point3d>>() { vertexOffsettedLower, vertexOffsettedUpper },
                connectionSurface, extrusion, supportMesh, offsetDistanceUpper, out top, out bottom, out stitched);

            var wrappedMesh = ImplantCreationUtilities.WrapOffset(offsetMesh, smallestDetail, gapClosingDistance, finalWrapOffset);
            connectionSurface.Dispose();

            vertexOffsettedLower.Clear();
            vertexOffsettedUpper.Clear();

            var unioned = new Mesh();
            Booleans.PerformBooleanUnion(out unioned, new[] { pastille.DuplicateMesh(), wrappedMesh.DuplicateMesh() });

            var combined = ImplantCreationUtilities.WrapOffset(unioned, smallestDetail, 2, 0);

            return combined;
        }

        private static Mesh Extrude(Curve connectionCurve, Vector3d extrudeDirection)
        {
            connectionCurve.MakeClosed(1);
            var extrusion = MeshUtilities.ConvertBrepToMesh(Surface.CreateExtrusion(connectionCurve, extrudeDirection).ToBrep(), true);

            return extrusion;
        }

        public static void UpdatePastilleAlgo(List<IDot> dotList, Guid objId, string algo)
        {
            var pastille = (DotPastille)dotList.FirstOrDefault(
                dot => (dot as DotPastille)?.Screw != null && objId == (dot as DotPastille).Screw.Id);

            pastille.CreationAlgoMethod = algo;
        }

        public static Mesh GenerateStampImprintShapeMesh(Point3d screwHeadPoint, Vector3d screwDirection,
            double shapeOffset, double shapeWidth, double shapeHeight, double sectionHeightRatio)
        {
            var shapeHalfWidth = shapeWidth / 2;

            const double epsilon = 0.001;
            if (shapeHalfWidth > epsilon && shapeHeight > epsilon)
            {
                var scaleRatio = shapeHeight / (shapeHalfWidth * 2);

                var translatedHeadPt = screwHeadPoint + screwDirection * (shapeOffset + shapeHeight / 2);

                var squishTransform = new Transform(1)
                {
                    M22 = 1 * scaleRatio
                };

                var stampImprintShape = new Sphere(Point3d.Origin, shapeHalfWidth);
                var stampImprintShapeMesh = Mesh.CreateFromSphere(stampImprintShape, 50, 50);
                stampImprintShapeMesh.Transform(squishTransform);

                if (sectionHeightRatio > 0 && sectionHeightRatio <= 1)
                {
                    var topPt = PointUtilities.FindFurthermostPointAlongVector(stampImprintShapeMesh.Vertices.ToPoint3dArray(),
                        -Vector3d.ZAxis);

                    var translatedTopPt = topPt + Vector3d.ZAxis * (shapeHeight * sectionHeightRatio);

                    var trimPlane = new Plane(translatedTopPt, Vector3d.ZAxis);
                    var cylMesh = CreateCylinderMesh(trimPlane, (shapeWidth / 2) * 1.5, shapeHeight * 1.5);

                    var topSideSmartie = Booleans.PerformBooleanSubtraction(stampImprintShapeMesh, new List<Mesh>(){ cylMesh });

                    stampImprintShapeMesh = topSideSmartie.DuplicateMesh();
                }

                var rotateTransform = Transform.Rotation(Vector3d.ZAxis, screwDirection, Point3d.Origin);
                stampImprintShapeMesh.Transform(rotateTransform);

                var translateTransform = Transform.Translation(translatedHeadPt - Point3d.Origin);
                stampImprintShapeMesh.Transform(translateTransform);

                return stampImprintShapeMesh;
            }
            else
            {
                return null;
            }
        }

        public static Mesh CreateCylinderMesh(Plane plane, double radius, double height)
        {
            var circle = new Circle(plane, plane.Origin, radius);
            var cylinder = new Cylinder(circle, height);
            var mesh = Mesh.CreateFromCylinder(cylinder, 50, 50);
            mesh.UnifyNormals();
            return mesh;
        }

        public static Mesh GetScrewStamps(IEnumerable<Screw> allScrewsAvailable, CasePreferenceDataModel casePreferenceData)
        {
            var pastilles = new List<DotPastille>();
            var implant = casePreferenceData.ImplantDataModel;
            foreach (var dot in implant.DotList)
            {
                if (dot is DotPastille)
                {
                    pastilles.Add(dot as DotPastille);
                }
            }

            return GetScrewStamps(allScrewsAvailable, pastilles);
        }

        public static Mesh GetScrewStamps(IEnumerable<Screw> allScrewsAvailable, List<DotPastille> pastilles)
        {
            var stamps = new List<Mesh>();

            foreach (var pastille in pastilles)
            {
                if (pastille?.Screw != null)
                {
                    var screw = allScrewsAvailable.First(s => s.Id == pastille.Screw.Id);
                    var screwStamp = screw.GetScrewStamp();
                    stamps.AddRange(Mesh.CreateFromBrep(screwStamp));
                    screwStamp.Dispose();
                }
            }

            var screwStamps = MeshUtilities.AppendMeshes(stamps);

            if (screwStamps == null)
            {
                throw new IDSException("Missing screw.");
            }

            if (screwStamps.Faces.QuadCount > 0)
            {
                screwStamps.Faces.ConvertQuadsToTriangles();
            }

            return screwStamps;
        }

        public static Mesh GetScrewStamp(IEnumerable<Screw> allScrewsAvailable, DotPastille pastille)
        {
            return GetScrewStamps(allScrewsAvailable, new List<DotPastille> { pastille });
        }

        public static Mesh SubstractPastilleWithScrew(Mesh pastilleMesh, Mesh screwStamp)
        {
            var subtractedScrewsStamp = Booleans.PerformBooleanSubtraction(pastilleMesh, screwStamp);
            if (!subtractedScrewsStamp.IsValid)
            {
                throw new IDSException("Screws stamp and implant subtractions failed.");
            }

            return subtractedScrewsStamp;
        }

        public static IMesh SubtractPastilleWithScrew(IMesh pastilleMesh, IMesh screwStamp)
        {
            var console = new IDSRhinoConsole();
            var subtractedScrewsStamp = BooleansV2.PerformBooleanSubtraction(console, pastilleMesh, screwStamp);
            if (!subtractedScrewsStamp.Vertices.Any())
            {
                throw new IDSException("Screws stamp and implant subtractions failed.");
            }

            return subtractedScrewsStamp;
        }
    }
}
