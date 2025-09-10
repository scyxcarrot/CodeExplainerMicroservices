using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.Commands
{
    [System.Runtime.InteropServices.Guid("37C53AAF-C681-45ED-9D64-AC571F862A6B")]
    public class TestFea : Command
    {
        public TestFea()
        {
            Instance = this;
        }

        ///<summary>The only instance of the TestFea command.</summary>
        public static TestFea Instance { get; private set; }

        public override string EnglishName => "TestFea";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var everythingSucceeded = RunFullTest();

            return everythingSucceeded ? Result.Success : Result.Failure;
        }

        public static bool RunFullTest()
        {
            var everythingIsOk = true;

            everythingIsOk &= TestLoadSurfaceCalculation();
            Reporting.ShowResultsInCommandLine(everythingIsOk, "Fea: Load Surface Calculation");

            everythingIsOk &= TestLoadDirectionCalculation();
            Reporting.ShowResultsInCommandLine(everythingIsOk, "Fea: Load Direction Calculation");

            everythingIsOk &= TestBoundaryConditionCalculation();
            Reporting.ShowResultsInCommandLine(everythingIsOk, "Fea: Boundary Condition Calculation");

            Reporting.ShowResultsInCommandLine(everythingIsOk, "Fea");

            return everythingIsOk;
        }

        private static bool TestBoundaryConditionCalculation()
        {
            var everythingOk = true;

            everythingOk &= TestNoiseShellRemoval();
            everythingOk &= TestMeshToMeshSelection();

            return everythingOk;
        }

        private static bool TestMeshToMeshSelection()
        {
            var orientationPlane = Plane.WorldXY;
            var largeSpan = new Interval(-200, 200);
            var smallSpan = new Interval(-50, 50);

            var targetSurface = new PlaneSurface(orientationPlane, largeSpan, largeSpan);
            var targetSurfaceMesh = targetSurface.ToBrep().GetCollisionMesh(MeshParameters.IDS());

            const double maxDistance = 0.5;

            const int precision = 2;
            for (double z = 0; z <= 2 * maxDistance; z += Math.Pow(10, -precision))
            {
                var safeZ = Math.Round(z, precision); // To avoid numerical errors

                var sourcePlane = new Plane(new Point3d(0, 0, safeZ), orientationPlane.Normal);
                var sourceSurface = new PlaneSurface(sourcePlane, smallSpan, smallSpan);
                var sourceSurfaceMesh = sourceSurface.ToBrep().GetCollisionMesh(MeshParameters.IDS());

                var selectedMesh = MeshUtilities.SelectFromMeshToMesh(sourceSurfaceMesh, targetSurfaceMesh, maxDistance);

                if(safeZ > maxDistance && selectedMesh.Faces.Count != 0)
                {
                    RhinoApp.WriteLine($"Mesh unexpectedly NOT REMOVED at z = {z:F4}, with maxDistance={maxDistance:F4}");
                }
                else if(safeZ <= maxDistance && selectedMesh.Faces.Count == 0)
                {
                    RhinoApp.WriteLine($"Mesh unexpectedly REMOVED at z = {z:F4}, with maxDistance={maxDistance:F4}");
                }
            }

            return true;
        }

        private static bool TestNoiseShellRemoval()
        {
            const double noiseShellThreshold = 25;

            for (double sideLength = 1; sideLength <= noiseShellThreshold; sideLength++)
            {
                var span = new Interval(0, sideLength);
                var box = new Box(Plane.WorldXY, span, span, span);

                var surfaceArea = box.Area;
                var expectedRemoved = surfaceArea <= noiseShellThreshold;

                var boundaryConditionMeshFiltered = MeshUtilities.RemoveNoiseShells(box.ToBrep().GetCollisionMesh(MeshParameters.IDS()), noiseShellThreshold);

                if (boundaryConditionMeshFiltered.DisjointMeshCount == 0 && !expectedRemoved)
                {
                    RhinoApp.WriteLine("Cube with surface area {0:F4} (sidelength {1:F4}) unexpectedly removed", surfaceArea, sideLength);
                    return false;
                }
                if (boundaryConditionMeshFiltered.DisjointMeshCount == 0 || !expectedRemoved) continue;
                RhinoApp.WriteLine("Cube with surface area {0:F4} (sidelength {1:F4}) unexpectedly not removed", surfaceArea, sideLength);
                return false;
            }
            return true;
        }

        private static bool TestLoadDirectionCalculation()
        {
            var cupCor = new List<Point3d>
            {
                new Point3d(63.434131826765295, 3.843433495283022, -94.32060171698107),
                new Point3d(28.24606839543542, 37.752801684820405, -58.13855044995422),
                new Point3d(19.39714087484028, 63.43000500903664, 55.99109099237254),
                new Point3d(-10.194915512460856, 11.153845494172865, -21.92415456392254),
                new Point3d(-87.43348047310239, 17.626709071435307, -65.90200176539898),
                new Point3d(-42.20036913724676, -33.97552853880414, 23.317484570917447),
                new Point3d(-56.3099050963928, 92.91949245103892, 87.51033268712285),
                new Point3d(-19.039177781274663, 5.141659338739957, -17.58168594797198),
                new Point3d(55.89336109264629, -4.682410176177565, -29.97470726980196),
                new Point3d(70.09704791950074, -3.2738960048134924, -66.04865960510615)
            };
            var cupOrientation = new List<Vector3d>
            {
                new Vector3d(-0.6186739438490404, 0.14067143936630913, 0.7729515491600266),
                new Vector3d(0.632522725559578, -0.732236142659327, -0.25247818328337246),
                new Vector3d(-0.24793468726018128, -0.8450034818897879, -0.4738116782513184),
                new Vector3d(0.5085958091693139, -0.358312895063379, -0.7829062345688088),
                new Vector3d(-0.4685848957852621, 0.40288865097177906, 0.7861990399129566),
                new Vector3d(-0.038645532238208255, 0.2101492498761009, -0.9769052234554475),
                new Vector3d(0.7987634108558433, 0.1436707499689818, -0.584239444989201),
                new Vector3d(0.3306316392752772, -0.9437443786958394, -0.005409878940151311),
                new Vector3d(0.671448495191002, 0.6166215803733669, -0.4110167209781014),
                new Vector3d(-0.6300615518516345, -0.4753951037707021, -0.6140211203119595)
            };
            var pcsZaxis = new List<Vector3d>
            {
                new Vector3d(0.7782136345819137, 0.5549902770238213, -0.29388659608738493),
                new Vector3d(0.5917888963842725, -0.5139168159514506, -0.6210277034067062),
                new Vector3d(-0.7388463452229916, 0.6635137903949481, 0.11770950728958433),
                new Vector3d(-0.8074490586239653, 0.44375165618326795, -0.3887293214588771),
                new Vector3d(-0.8231211805560227, -0.05375277963579704, -0.5653159831470226),
                new Vector3d(-0.23409613960442546, 0.8312118900867342, -0.5042675789704748),
                new Vector3d(-0.6527077528710017, 0.600588077589444, 0.4618079150464003),
                new Vector3d(-0.8217520826530292, -0.3533830463899702, -0.44703907791104036),
                new Vector3d(-0.6308247985317771, 0.7695248846649047, -0.09945614832079575),
                new Vector3d(0.5323523385612616, -0.8221408491617437, 0.20170625118712376)
            };
            var expectedLoadVector = new List<Vector3d>
            {
                new Vector3d(0.7910534618550382, 0.4869612901890928, -0.3702743879132525),
                new Vector3d(-0.41727095858369345, 0.7710731133996978, -0.4809690228224085),
                new Vector3d(-0.4886362040081782, 0.8308452534871135, 0.26632841547685504),
                new Vector3d(-0.8528413330583423, 0.5090856997441697, 0.11616114213995456),
                new Vector3d(-0.27931353803317194, -0.30003707434670385, -0.9121193460775089),
                new Vector3d(-0.17230980668568985, 0.512904765085866, 0.8409744541138886),
                new Vector3d(-0.6542067764103517, 0.598070083906756, 0.4629532032879153),
                new Vector3d(-0.8311066685745365, 0.45987899720364744, -0.3126867656008218),
                new Vector3d(-0.9665170293803662, 0.07022340839916465, 0.24680661423583597),
                new Vector3d(0.7922787335968869, -0.2696073925945556, 0.5473630076544067)
            };

            for (var i = 0; i < cupCor.Count; i++)
            {
                var actualLoadVector = IDS.Amace.Fea.AmaceFea.CalculateFdaConstructLoadVector(cupCor[i], cupOrientation[i], pcsZaxis[i]);

                const int decimalDigits = 7;
                if (AreVectorsEqual(expectedLoadVector[i], actualLoadVector, decimalDigits)) continue;
                RhinoApp.WriteLine($"Expected {expectedLoadVector.ToString()} does not match {actualLoadVector.ToString()}");
                return false;
            }

            return true;
        }

        private static bool AreVectorsEqual(Vector3d v1, Vector3d v2, int decimalDigits)
        {
            var accuracyFactor = Math.Pow(10, decimalDigits);

            return Math.Abs(Math.Round(v1.X * accuracyFactor) - Math.Round(v2.X * accuracyFactor)) < 0.001 &&
                Math.Abs(Math.Round(v1.Y * accuracyFactor) - Math.Round(v2.Y * accuracyFactor)) < 0.001 &&
                Math.Abs(Math.Round(v1.Z * accuracyFactor) - Math.Round(v2.Z * accuracyFactor)) < 0.001;
        }

        private static bool TestLoadSurfaceCalculation()
        {
            // Variation ranges for the x, y and z coordinates
            const double resolution = 0.2;
            var coordinateRange = MathUtilities.Range(-1, 1, resolution).ToList();

            // Create a sphere as target mesh
            var mp = MeshParameters.IDS();
            var targetMesh = (new Sphere(Point3d.Origin, 32)).ToBrep().GetCollisionMesh(mp);
            targetMesh.Flip(true,true,true);

            // Subset selection parameters
            const double thresholdAngle = 32;
            var threshold = Math.Cos(thresholdAngle / 180 * Math.PI);
            const double meshTolerance = 0.1;

            foreach (var x in coordinateRange)
            {
                foreach (var y in coordinateRange)
                {
                    foreach (var z in coordinateRange)
                    {
                        // Invalid x,y,z combination
                        if (Math.Abs(x) < 0.001 && Math.Abs(y) < 0.001 && Math.Abs(z) < 0.001)
                            continue;

                        // Set the direction of the vector that is used to select the mesh subset
                        var direction = new Vector3d(x, y, z);
                        direction.Unitize();

                        // Select subset
                        var subset = MeshUtilities.SelectMeshSubSetByNormalDirection(direction, targetMesh, threshold, meshTolerance);
                        subset.FaceNormals.ComputeFaceNormals();
                        subset.Flip(true, true, true);

                        // Select the part of the mesh outside the subset
                        var outsideSubset = targetMesh;
                        var deleteFaceIndices = new List<int>();
                        for (var i = 0; i < subset.Faces.Count; i++)
                        {
                            var meshPoint = outsideSubset.ClosestMeshPoint(subset.Faces.GetFaceCenter(i), 0.1);
                            deleteFaceIndices.Add(meshPoint.FaceIndex);
                        }
                        outsideSubset.Faces.DeleteFaces(deleteFaceIndices);
                        outsideSubset.Compact();
                        outsideSubset.Flip(true, true, true);

                        // Check if all faces INSIDE the selected subset have normals that form an angle with the direction 
                        // that is LARGER THAN OR EQUAL TO the threshold angle.
                        foreach (Vector3d faceNormal in subset.FaceNormals)
                        {
                            var testVector = faceNormal;
                            testVector.Unitize();
                            var testAngle = Vector3d.VectorAngle(direction, testVector) / Math.PI * 180;
                            if (testAngle > thresholdAngle)
                            {
                                RhinoApp.WriteLine("Test angle {0:F4} inside subset smaller than threshold angle {1:F4}", testAngle, thresholdAngle);
                            }
                        }

                        // Check if all faces OUTSIDE the selected subset have normals that form an angle with the direction 
                        // that is SMALLER THAN the threshold angle.
                        foreach (Vector3d faceNormal in outsideSubset.FaceNormals)
                        {
                            var testVector = faceNormal;
                            testVector.Unitize();
                            var testAngle = Vector3d.VectorAngle(direction, testVector) / Math.PI * 180;
                            if (testAngle <= thresholdAngle)
                            {
                                RhinoApp.WriteLine("Test angle {0:F4} outside larger than or equal to threshold angle {1:F4}", testAngle, thresholdAngle);
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}
