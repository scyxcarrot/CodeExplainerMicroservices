using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;


namespace IDS.Amace.Quality
{
    /// <summary>
    /// ScrewAnalysis manages the calculations for the screw qc checks
    /// </summary>
    public class ScrewAnalysis : Common.Quality.ScrewAnalysis
    {
        /// <summary>
        /// Performs the slow screw checks.
        /// </summary>
        /// <param name="screws">The screws.</param>
        /// <param name="director">The director.</param>
        /// <param name="screwHoleBumpIntersections">The bump problems other screws.</param>
        /// <param name="cupZoneBumpsDestroyed">The bump problems cup zone.</param>
        /// <param name="insertProblems">The insert problems.</param>
        /// <param name="shaftProblems">The shaft problems.</param>
        public static void PerformSlowScrewChecks(List<Screw> screws, Mesh plateBumps, Cup cup,
                                                    out Dictionary<int, List<int>> screwHoleBumpIntersections,
                                                    out List<int> cupZoneBumpsDestroyed,
                                                    out List<int> insertProblems,
                                                    out List<int> shaftProblems)
        {
            // Calculate the complex screw analyses
            screwHoleBumpIntersections = PerformScrewHoleBumpIntersectionCheck(screws);
            cupZoneBumpsDestroyed = PerformBumpIntegrityInCupZoneCheck(screws, cup);
            insertProblems = PerformInsertTrajectoryCheck(screws, plateBumps);
            shaftProblems = PerformShaftTrajectoryCheck(screws, plateBumps);
        }

        /// <summary>
        /// Performs the screw intersection check.
        /// </summary>
        /// <param name="sourceScrew">The source screw.</param>
        /// <param name="targetScrew">The target screw.</param>
        /// <returns></returns>
        protected override bool PerformScrewIntersectionCheck(Screw sourceScrew, Screw targetScrew, double margin)
        {
            bool bodyBodyIntersection = PerformBodyBodyIntersectionCheck(sourceScrew, targetScrew, margin);
            bool headHeadIntersection = PerformHeadHeadIntersectionCheck(sourceScrew, targetScrew, margin);
            bool headBodyIntersection = PerformBodyHeadIntersectionCheck(sourceScrew, targetScrew, margin);
            bool bodyHeadIntersection = PerformBodyHeadIntersectionCheck(targetScrew, sourceScrew, margin);

            return bodyBodyIntersection || headHeadIntersection || headBodyIntersection || bodyHeadIntersection;
        }

        /// <summary>
        /// Performs the body body intersection check.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        private static bool PerformBodyBodyIntersectionCheck(Screw source, Screw target, double margin)
        {
            return source.centerLine.MinimumDistanceTo(target.centerLine) < (source.radius + target.radius + margin);
        }

        /// <summary>
        /// Performs the head head intersection check.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        private static bool PerformHeadHeadIntersectionCheck(Screw source, Screw target, double margin)
        {
            return (source.headCenter - target.headCenter).Length < (source.headRadius + target.headRadius + margin);
        }

        /// <summary>
        /// Performs the body head intersection check.
        /// </summary>
        /// <param name="headScrew">The head screw.</param>
        /// <param name="bodyScrew">The body screw.</param>
        /// <returns></returns>
        private static bool PerformBodyHeadIntersectionCheck(Screw headScrew, Screw bodyScrew, double margin)
        {
            return bodyScrew.centerLine.MinimumDistanceTo(headScrew.headCenter) < (bodyScrew.radius + headScrew.headRadius + margin);
        }

        /// <summary>
        /// Performs the bump integrity in cup zone check.
        /// </summary>
        /// <param name="screws">The screws.</param>
        /// <param name="cup">The cup.</param>
        /// <returns></returns>
        private static List<int> PerformBumpIntegrityInCupZoneCheck(List<Screw> screws, Cup cup)
        {
            List<int> intersections = new List<int>();

            foreach (Screw screw in screws)
            {
                // Ignore screws without trimmed lateral bumps
                if (screw.lateralTrimmedBump == null)
                    continue;

                // Check other screws
                Line[] intersection = Intersection.MeshMeshFast(screw.lateralTrimmedBump, cup.innerReamingVolumeMesh);
                if (intersection != null && intersection.Length > 0)
                    intersections.Add(screw.index);
            }

            return intersections;
        }

        /// <summary>
        /// Performs the screw hole bump intersection check.
        /// </summary>
        /// <param name="screws">The screws.</param>
        /// <returns></returns>
        private static Dictionary<int, List<int>> PerformScrewHoleBumpIntersectionCheck(List<Screw> screws)
        {
            // Init dict, key = sourceScrew, value = list of problematic targetScrews
            Dictionary<int, List<int>> intersections = new Dictionary<int, List<int>>();

            // Loop over sourceScrews and targetScrews
            foreach (Screw sourceScrew in screws) // source
            {
                foreach (Screw targetScrew in screws) // target
                {
                    // Do not test self-inflicted problems
                    if (sourceScrew.index == targetScrew.index)
                        continue;

                    // Check distance between body lines
                    bool lateralIntersection = PerformFastMeshIntersectionCheck(sourceScrew.screwHoleSubtractor, targetScrew.lateralTrimmedBump);
                    bool medialIntersection = PerformFastMeshIntersectionCheck(sourceScrew.screwHoleSubtractor, targetScrew.medialTrimmedBump);

                    if (lateralIntersection || medialIntersection)
                    {
                        // screw i destroys bump of screw j
                        if (intersections.ContainsKey(sourceScrew.index))
                        {
                            List<int> temp = intersections[sourceScrew.index];
                            temp.Add(targetScrew.index);
                            intersections[sourceScrew.index] = temp;
                        }
                        else
                        {
                            List<int> temp = new List<int>() { targetScrew.index };
                            intersections.Add(sourceScrew.index, temp);
                        }
                    }
                }
            }

            // return dict
            return intersections;
        }

        /// <summary>
        /// Performs the fast mesh intersection check.
        /// </summary>
        /// <param name="mesh1">The mesh1.</param>
        /// <param name="mesh2">The mesh2.</param>
        /// <returns></returns>
        private static bool PerformFastMeshIntersectionCheck(Mesh mesh1, Mesh mesh2)
        {
            bool areIntersecting = false;

            if (mesh1 != null && mesh2!= null)
            {
                Line[] intersectionLateral = null;
                intersectionLateral = Intersection.MeshMeshFast(mesh1, mesh2);
                areIntersecting = intersectionLateral != null && intersectionLateral.Length > 0;
            }

            return areIntersecting;
        }

        /// <summary>
        /// Performs the insert trajectory check.
        /// </summary>
        /// <param name="screws">The screws.</param>
        /// <param name="intersectMesh">The intersect mesh.</param>
        /// <returns></returns>
        private static List<int> PerformInsertTrajectoryCheck(List<Screw> screws, Mesh intersectMesh)
        {
            // init, list of screws that will have insert problems
            List<int> intersections = new List<int>();

            // loop over all screws
            foreach (Screw theScrew in screws)
            {
                if (PerformInsertTrajectoryCheck(theScrew, intersectMesh))
                    intersections.Add(theScrew.index);
            }

            // return list
            return intersections;
        }

        /// <summary>
        /// Performs the shaft trajectory check.
        /// </summary>
        /// <param name="screws">The screws.</param>
        /// <param name="intersectMesh">The intersect mesh.</param>
        /// <returns></returns>
        private static List<int> PerformShaftTrajectoryCheck(List<Screw> screws, Mesh intersectMesh)
        {
            // init, list of screws that will have shaft trajectory problems
            List<int> intersections = new List<int>();

            // loop over all screws
            foreach (Screw theScrew in screws) // source
            {
                if (PerformShaftTrajectoryCheck(theScrew, intersectMesh))
                    intersections.Add(theScrew.index);
            }

            // return list
            return intersections;
        }

        /// <summary>
        /// Performs the insert trajectory check.
        /// </summary>
        /// <param name="screw">The screw.</param>
        /// <param name="plate">The plate.</param>
        /// <returns></returns>
        private static bool PerformInsertTrajectoryCheck(Screw screw, Mesh plate)
        {
            // circle of rays pointing away from screw tip
            Circle headCircle = new Circle(new Plane(screw.headPoint, screw.direction), screw.screwHoleTopRadius);
            NurbsCurve rayOriginCurve = headCircle.ToNurbsCurve();
            Point3d[] rayOrigins = rayOriginCurve.DivideEquidistant(1.0);
            Vector3d rayDirection = -screw.direction;

            return PerformTrajectoryCheck(rayOrigins, rayDirection, plate, screw.screwAlignment == ScrewAlignment.Sunk);
        }

        /// <summary>
        /// Performs the shaft trajectory check.
        /// </summary>
        /// <param name="screw">The screw.</param>
        /// <param name="plate">The plate.</param>
        /// <returns></returns>
        private static bool PerformShaftTrajectoryCheck(Screw screw, Mesh plate)
        {
            // circle of rays pointing towards from screw tip
            Circle bodyOriginCircle = new Circle(new Plane(screw.bodyOrigin, screw.direction), screw.screwHoleBottomRadius);
            NurbsCurve rayOriginCurve = bodyOriginCircle.ToNurbsCurve();
            Point3d[] rayOrigins = rayOriginCurve.DivideEquidistant(1.0);
            Vector3d rayDirection = screw.direction;

            return PerformTrajectoryCheck(rayOrigins, rayDirection, plate, screw.screwAlignment == ScrewAlignment.Floating);
        }

        /// <summary>
        /// Performs the trajectory check.
        /// Used by other methods to check specific trajectories
        /// </summary>
        /// <param name="rayOrigins">The ray origins.</param>
        /// <param name="rayDirection">The ray direction.</param>
        /// <param name="collisionMesh">The collision mesh.</param>
        /// <param name="outFirst">if set to <c>true</c> [out first].</param>
        /// <returns></returns>
        private static bool PerformTrajectoryCheck(Point3d[] rayOrigins, Vector3d rayDirection, Mesh collisionMesh, bool outFirst)
        {
            // Make sure facenormals exist
            collisionMesh.FaceNormals.ComputeFaceNormals();

            // Intersect every ray and evaluate cut points
            foreach (Point3d origin in rayOrigins)
            {
                // Create a line along the ray and calculate intersections
                Line ray = new Line(origin, origin + 200 * rayDirection);
                int[] faceIds;
                Point3d[] intersections = Intersection.MeshLine(collisionMesh, ray, out faceIds);
                bool outFirstDetected = false;

                // Sort intersections by hit distance
                List<double> distances = new List<double>();
                foreach (Point3d intersection in intersections)
                    distances.Add((intersection - origin).Length);
                Array.Sort(distances.ToArray(), intersections);
                Array.Sort(distances.ToArray(), faceIds);

                // Loop over intersection points and evaluate
                if (outFirst && intersections.Count() > 1)
                {
                    for (int i = 0; i < intersections.Count(); i++)
                    {
                        double cos = collisionMesh.FaceNormals[faceIds[i]] * rayDirection;

                        if (!outFirstDetected && cos > 0)
                            // normal in direction of ray (break out)
                            outFirstDetected = true;
                        else if (outFirstDetected && cos < 0)
                            // normal in opposite direction (break in) after a break out of the mesh
                            // has been detected
                            return true;
                    }
                }
                else if (!outFirst && intersections.Count() > 0)
                {
                    for (int i = 0; i < intersections.Count(); i++)
                    {
                        double cos = collisionMesh.FaceNormals[faceIds[i]] * rayDirection;

                        if (cos < 0)
                            // normal in opposite direction of ray (break in)
                            return true;
                    }
                }
            }

            // No cut points found, so no intersection problem
            return false;
        }
        
        private static bool PerformGuideHoleBooleanIntersectionCheck(Brep guideHoleBoolean, Brep guideHoleSafetyZone)
        {
            var areIntersecting = false;

            var tolerance = 0.001;
            Curve[] intersectionCurves;
            Point3d[] intersectionPoints;
            if (guideHoleBoolean != null && guideHoleSafetyZone != null && Intersection.BrepBrep(guideHoleBoolean, guideHoleSafetyZone, tolerance, out intersectionCurves, out intersectionPoints))
            { 
                areIntersecting = (intersectionCurves != null && intersectionCurves.Length > 0) || (intersectionPoints != null && intersectionPoints.Length > 0);
            }

            return areIntersecting;
        }
        
        private static void GetGuideHoleBreps(Screw screw, double drillBitRadius, out Brep guideHoleBoolean, out Brep guideHoleSafetyZone)
        {
            var screwGuideCreator = new ScrewGuideCreator();
            guideHoleBoolean = screwGuideCreator.GetGuideHoleBoolean(screw, drillBitRadius);
            guideHoleSafetyZone = screwGuideCreator.GetGuideHoleSafetyZone(screw, drillBitRadius);
        }
    }
}