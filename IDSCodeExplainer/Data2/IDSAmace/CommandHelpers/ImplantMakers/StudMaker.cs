using IDS.Amace;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;

namespace IDS.Operations.CupPositioning
{
    /*
     * StudMaker provides functionality for creation of the cup studs
     */

    public class StudMaker
    {
        /**
        * Create the studs and add them to the document
        */

        public static bool GenerateAmaceStuds(ImplantDirector director)
        {
            var objManager = new AmaceObjectManager(director);
            // Check building block exists
            var oldid = objManager.GetBuildingBlockId(IBB.CupStuds);
            if (oldid != Guid.Empty)
            {
                return true;
            }

            // Get all stuff from director
            Cup cup = director.cup;
            ScrewManager screwManager = new ScrewManager(director.Document);
            Mesh studDeletors = screwManager.GetAllStudDeletorsUnion();
            StudParameters amaceParams = GetAmaceStudParams();

            // Generate CupStuds
            Mesh studMesh = new Mesh();
            bool success = GenerateStuds(cup, studDeletors, amaceParams, out studMesh);
            if (!success)
            {
                return false;
            }

            objManager.SetBuildingBlock(IBB.CupStuds, studMesh, oldid);

            // Success
            return true;
        }

        /**
         * Generates studs with given cup, studDeletors and studparameters
         */

        public static bool GenerateStuds(Cup cup, Mesh studDeletors, StudParameters studparam, out Mesh studMesh)
        {
            // init
            studMesh = new Mesh();

            // Cup parameters
            double apertureangle = cup.apertureAngle;
            double lateraldiameter = cup.innerCupDiameter;
            // Derived parameters
            double betaFromTop = 360 * studparam.arcBetaDistFromTop / (Math.PI * lateraldiameter);
            double betaMax = apertureangle / 2 - betaFromTop;
            double betaStep = 360 * studparam.arcBetaSpace / (Math.PI * lateraldiameter);

            // Generate stud position points alpha = radial angle for position of CupStuds in one row
            // beta = angle for positions of stud rows
            List<Point3d> studcoords = new List<Point3d>();
            // Stud location
            studcoords.Add(new Point3d(lateraldiameter / 2 * Math.Cos((0 - 90) * Math.PI / 180) * Math.Cos(0 * Math.PI / 180),
                                        lateraldiameter / 2 * Math.Cos((0 - 90) * Math.PI / 180) * Math.Sin(0 * Math.PI / 180),
                                        lateraldiameter / 2 * Math.Sin((0 - 90) * Math.PI / 180)));
            List<Vector3d> studdirections = new List<Vector3d>();
            studdirections.Add(Vector3d.YAxis);
            double circArcLength, alphaStep, nstuds, a;
            double b = betaStep;
            double alphaMax = 360;
            Brep originstudBrep = GenerateStudBrep(studparam.diameter, studparam.height, studparam.roundRad, studparam.overlap);
            MeshingParameters meshparameters = MeshParameters.IDS();
            Mesh originstud = Mesh.CreateFromBrep(originstudBrep, meshparameters)[0];

            // Global transform, used for every stud
            Transform rotation_global = Transform.Rotation(Vector3d.ZAxis, cup.orientation, Point3d.Origin);
            Transform translation_global = Transform.Translation(cup.centerOfRotation - Point3d.Origin);

            // Local transform variables, changes for every stud
            Transform rotation_local = new Transform();
            Transform translation_local = new Transform();

            // Create studds at their position and check if they do not intersect the stud deletor
            rotation_local = Transform.Rotation(studdirections[0], Vector3d.ZAxis, Point3d.Origin);
            translation_local = Transform.Translation(studcoords[0] - Point3d.Origin);
            Mesh newstud = originstud.DuplicateMesh();
            newstud.Transform(rotation_local);
            newstud.Transform(translation_local);
            newstud.Transform(rotation_global);
            newstud.Transform(translation_global);

            // Add the first stud if it needs to be there
            Line[] intersectionLines = Intersection.MeshMeshFast(newstud, studDeletors);
            if (intersectionLines.Length == 0 && !studDeletors.IsPointInside(newstud.Vertices[0], 0.01, false))
            {
                studMesh.Append(newstud);
            }

            // Add the rest of the studs by looping over a and b
            while (b <= betaMax)
            {
                // calculate arc length for current row on thisBeta
                circArcLength = 2 * Math.PI * lateraldiameter / 2 * (Math.Sin(b * Math.PI / 180));

                // calculate where to place the CupStuds
                nstuds = Math.Floor(circArcLength / studparam.studArcSpace);
                alphaStep = 360 / nstuds;
                a = 0;
                while (a < alphaMax - 0.5 * alphaStep)
                {
                    // Stud location
                    Point3d studpoint = new Point3d(lateraldiameter / 2 * Math.Cos((b - 90) * Math.PI / 180) * Math.Cos(a * Math.PI / 180),
                                 lateraldiameter / 2 * Math.Cos((b - 90) * Math.PI / 180) * Math.Sin(a * Math.PI / 180),
                                 lateraldiameter / 2 * Math.Sin((b - 90) * Math.PI / 180));
                    studcoords.Add(studpoint);

                    // Stud direction
                    Vector3d direction = new Vector3d(-studpoint);
                    direction.Unitize();
                    studdirections.Add(direction);

                    // Set stud at correct position
                    rotation_local = Transform.Rotation(Vector3d.YAxis, direction, Point3d.Origin);
                    translation_local = Transform.Translation(studpoint - Point3d.Origin);

                    // Transform Brep
                    newstud = originstud.DuplicateMesh();
                    newstud.Transform(rotation_local);
                    newstud.Transform(translation_local);
                    newstud.Transform(rotation_global);
                    newstud.Transform(translation_global);

                    // Test if stud has to be generated (i.e. no intersection with studDeletor)
                    intersectionLines = Intersection.MeshMeshFast(newstud, studDeletors);

                    // No intersections and studvertex not in studDeletors
                    if (intersectionLines.Length == 0 && !studDeletors.IsPointInside(newstud.Vertices[0], 0.01, false))
                    {
                        studMesh.Append(newstud);
                    }
                    a += alphaStep;
                }
                b += betaStep;
            }

            // Success
            return true;
        }

        /**
         * Create a Brep for a single stud
         */

        public static Brep GenerateStudBrep(double diameter, double height, double roundRad, double overlap)
        {
            // Bottom line
            List<Point3d> line1points = new List<Point3d>(2);
            line1points.Add(new Point3d(0, -overlap, 0));
            line1points.Add(new Point3d(diameter / 2, -overlap, 0));
            PolylineCurve line1 = new PolylineCurve(line1points);

            // Side line
            List<Point3d> line2points = new List<Point3d>(2);
            line2points.Add(new Point3d(diameter / 2, -overlap, 0));
            line2points.Add(new Point3d(diameter / 2, height - roundRad, 0));
            PolylineCurve line2 = new PolylineCurve(line2points);

            // Top line
            List<Point3d> line3points = new List<Point3d>(2);
            line3points.Add(new Point3d(diameter / 2 - roundRad, height, 0));
            line3points.Add(new Point3d(0, height, 0));
            PolylineCurve line3 = new PolylineCurve(line3points);

            // Create curved corner using bézier
            List<Point3d> controlpoints = new List<Point3d>(6);
            controlpoints.Add(new Point3d(diameter / 2, height - roundRad, 0));
            controlpoints.Add(new Point3d(diameter / 2, height, 0));
            controlpoints.Add(new Point3d(diameter / 2 - roundRad, height, 0));
            BezierCurve cornercurve = new BezierCurve(controlpoints);

            // Join all curves
            List<Curve> allcurves = new List<Curve>(4);
            allcurves.Add(line1);
            allcurves.Add(line2);
            allcurves.Add(cornercurve.ToNurbsCurve());
            allcurves.Add(line3);
            Curve[] studcurves = Curve.JoinCurves(allcurves);
            Curve studcurve = studcurves[0];

            // Revolve curve to create surface
            Line revolveaxis = new Line(0, 0, 0, 0, 1, 0); // Y-axis
            Brep studsurface = RevSurface.Create(studcurve, revolveaxis).ToBrep();

            // Return stud brep
            return studsurface;
        }

        // Method that defines the stud paramters for an amace implant
        public static StudParameters GetAmaceStudParams()
        {
            StudParameters amaceParams = new StudParameters(diameter: 2.0, height: 0.5, roundRad: 0.2, overlap: 0.2,
                                                    studArcSpace: 3.0, arcBetaSpace: 3.0, arcBetaDistFromTop: 5.0);
            return amaceParams;
        }
    }
}