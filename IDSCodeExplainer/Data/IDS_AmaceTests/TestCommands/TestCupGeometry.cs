using IDS.Amace;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.SplashScreen;
using IDS.Core.Utilities;
using IDS.Operations.CupPositioning;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
//using RhinoMatSDKOperations.Fix;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Testing.Commands
{
    /// <summary>
    /// TestCupGeometry - this is a command to run test case Cup Geometry for aMace
    /// The goal of this command is to test if the cup dimensions in latest release of IDS match the requirements (based on the hardware requirements).
    /// 
    /// 462128 A: Cup Phase: Cup Geometry
    /// 611263 Cup Geometry (optimized)
    /// 
    /// ***NOTE: Test case uses exported cup in STL output from Export ImplantQC
    /// ***Test command here uses the brep form of the cup (except check parts where they do no contain overlapping triangles, bad edges, inverted normal triangle)
    /// Below are the testing steps:
    /// 
    /// For each CupType:
    /// 
    /// Part [Cup Geometry]
    /// 1. Check CupDesign - v1=>no ring; v2=>got ring
    /// 2. If cup is v2 cup: Measure the ring width of the lower segment (R - R') - 2+1=3.2/3+1|4+1=4.2 (== Nominal ring width of lower segment + polishing offset) (+- 0.05mm)
    /// 3. Measure diameter - given inner diameter
    /// 4. Measure cup thickness - given X in cup type (X+Y)
    /// 5. Check parts contain no overlapping triangles, bad edges, inverted normal triangle
    ///
    /// Part [Porous Layer]
    /// 6. Measure porous layer outer diameter - inner diameter + (2*X) + (2*Y)
    /// 7. Measure porous layer thickness - Y + overlap
    /// 8. Measure porous layer overlap - 0.25 for v1 and 0.1 for v2
    ///
    /// Part [Cup Stud]
    /// 9. Measure cup stud diameter - 2
    /// 10. Measure cup stud height - 0.7
    /// 11. Measure cup stud height above cup surface - 0.5
    /// 12. Measure cup stud rounding radius - 0.2
    /// 13. Measure distance between 2 cup studs - 3
    /// 14. Measure smallest distance between cup stud surface and cup border - 5 to 8
    /// </summary>
    [System.Runtime.InteropServices.Guid("E23A9A34-BC9B-4A28-83DD-92DF27D4F196")]
    [CommandStyle(Style.ScriptRunner)]
    public class TestCupGeometry : Command
    {
        public TestCupGeometry()
        {
            Instance = this;
        }

        public static TestCupGeometry Instance { get; private set; }

        public override string EnglishName => "TestCupGeometry";

        /// <summary>
        /// Tolerances set are as below:
        /// <param name="AssertionTolerance"></param><value>0.05</value>
        /// <param name="AssertionToleranceToCheck"></param><value>1.0</value>
        /// <param name="IntersectionTolerance"></param><value>0.00001</value>
        /// </summary>
        private const double AssertionTolerance = 0.05;
        private const double AssertionToleranceToCheck = 1.0;
        private const double IntersectionTolerance = 0.00001;

        private bool _invokeAssertion = true;

        /// <summary>
        /// Option available:
        /// Invoke assertion
        /// - when this is set to true, the command will exit upon assertion error
        /// - default value is true
        /// 
        /// Required to choose a folder
        /// - generated .3dm result files will be located in this folder
        /// </summary>
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //inputs
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Invoke Assertion");
            getOption.AcceptNothing(true);
            var optInvokeAssertion = new OptionToggle(true, "False", "True");
            getOption.AddOptionToggle("Assert", ref optInvokeAssertion);
            
            const string minimalCheck = "MinimalCheck";
            const string varyingInnerDiameterCheck = "VaryingInnerDiameterCheck";
            const string fullCheck = "FullCheck";
            var checkOptions = new List<string> { minimalCheck, varyingInnerDiameterCheck, fullCheck };
            var checkOptionsId = getOption.AddOptionList("CheckType", checkOptions, 0);

            var selectedCheck = minimalCheck;
            while (true)
            {
                var result = getOption.Get();
                if (result == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (result == GetResult.Option)
                {
                    var optId = getOption.OptionIndex();
                    if (optId == checkOptionsId)
                    {
                        selectedCheck = checkOptions[getOption.Option().CurrentListOptionIndex];
                    }
                }
                else if (result == GetResult.Nothing)
                {
                    break;
                }
            }

            _invokeAssertion = optInvokeAssertion.CurrentValue;

            var dialog = new FolderBrowserDialog {Description = "Please select a folder for result outputs"};
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Failure;
            }

            var resultDirectory = Path.GetFullPath(dialog.SelectedPath);

            var everythingIsOk = false;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (selectedCheck)
            {
                case minimalCheck:
                    everythingIsOk = MinimalCheck(resultDirectory);
                    break;
                case varyingInnerDiameterCheck:
                    everythingIsOk = VaryingInnerDiameterCheck(resultDirectory);
                    break;
                case fullCheck:
                    everythingIsOk = FullCheck(resultDirectory);
                    break;
            }

            return everythingIsOk ? Result.Success : Result.Failure;
        }

        /// <summary>
        /// MinimalCheck only checks 2 cups:
        /// InnerDiameter = 46
        /// CupType = 4+1(v1) & 4+1(v2)
        /// </summary>
        private bool MinimalCheck(string resultDirectory)
        {
            const double innerDiameter = 46.0;
            var cupTypeV1 = new CupType(4, 1, CupDesign.v1);
            var everythingIsOk = Check(resultDirectory, innerDiameter, cupTypeV1);

            var cupTypeV2 = new CupType(4, 1, CupDesign.v2);
            everythingIsOk &= Check(resultDirectory, innerDiameter, cupTypeV2);
            return everythingIsOk;
        }

        /// <summary>
        /// VaryingInnerDiameterCheck checks all 6 CupTypes with varying inner diameter:
        /// CupType = 2+1(v1) & 2+2(v1) & 4+1(v1) & 3+1(v2) & 4+1(v2) & 2+1(v2)
        /// InnerDiameter = every increment of 10.0 starting from 40.0 to 80.0
        /// </summary>
        private bool VaryingInnerDiameterCheck(string resultDirectory)
        {
            var varyingInnerDiameter = MathUtilities.Range(Cup.innerDiameterMin, Cup.innerDiameterMax, 10).ToList();

            var cupTypes = new List<CupType>
            {
                new CupType(2, 1, CupDesign.v1),
                new CupType(2, 2, CupDesign.v1),
                new CupType(4, 1, CupDesign.v1),
                new CupType(3, 1, CupDesign.v2),
                new CupType(4, 1, CupDesign.v2),
                new CupType(2, 1, CupDesign.v2)
            };

            var everythingIsOk = true;

            foreach (var innerDiameter in varyingInnerDiameter)
            {
                foreach (var cupType in cupTypes)
                {
                    everythingIsOk &= Check(resultDirectory, innerDiameter, cupType);
                }
            }

            return everythingIsOk;
        }

        /// <summary>
        /// FullCheck checks combinations of varying inner diameter, varying thickness, varying porous and varying cup design
        /// InnerDiameter = every increment of 10.0 starting from 40.0 to 80.0
        /// Thickness = every increment of 1.0 starting from 2.0 to 4.0
        /// Porous = every increment of 1.0 starting from 1.0 to 2.0
        /// CupDesign = v1 & v2
        /// </summary>
        private bool FullCheck(string resultDirectory)
        {
            var varyingInnerDiameter = MathUtilities.Range(Cup.innerDiameterMin, Cup.innerDiameterMax, 10).ToList();
            var varyingThickness = MathUtilities.Range(2.0, 4.0, 1).ToList();
            var varyingPorous = MathUtilities.Range(1.0, 2.0, 1).ToList();

            var everythingIsOk = true;
            var exceptionCupTypes = new List<CupType>
            {
                new CupType(2, 2, CupDesign.v2),
                new CupType(3, 2, CupDesign.v2)
            };

            foreach (var innerDiameter in varyingInnerDiameter)
            {
                foreach (var cupThickness in varyingThickness)
                {
                    foreach (var porousThickness in varyingPorous)
                    {
                        foreach (var cupDesign in new List<CupDesign> {CupDesign.v1, CupDesign.v2 })
                        {
                            var cupType = new CupType(cupThickness, porousThickness, cupDesign);

                            //skip certain cupType
                            if (exceptionCupTypes.Contains(cupType))
                            {
                                continue;
                            }

                            everythingIsOk &= Check(resultDirectory, innerDiameter, cupType);
                        }
                    }
                }
            }

            return everythingIsOk;
        }

        /// <summary>
        /// For every combination, test will be run and 
        /// a 3dm file with format: {cupThickness}_{porousThickness}_{cupDesign}_{innerDiameter}D.3dm
        /// will be generated based on output results
        /// </summary>
        private bool Check(string resultDirectory, double innerDiameter, CupType cupType)
        {
            const double studDiameter = 2.0;
            const double studHeight = 0.7;
            const double studHeightAboveCupSurface = 0.5;
            const double studRoundingRadius = 0.2;
            const double distanceBetweenStuds = 3.0;
            const double minDistanceBetweenStudSurfaceAndCupBorder = 5.0;
            const double maxDistanceBetweenStudSurfaceAndCupBorder = 8.0;

            var cupThickness = cupType.CupThickness;
            var porousThickness = cupType.PorousThickness;
            var cupDesign = cupType.CupDesign;

            RhinoApp.WriteLine($"CupThickness: {cupThickness}, PorousThickness: {porousThickness}, CupDesign: {cupDesign}, InnerDiameter: {innerDiameter}");

            var everythingIsOk = CheckGeometry(cupThickness, porousThickness, cupDesign,
                innerDiameter, studDiameter, studHeight, studHeightAboveCupSurface,
                studRoundingRadius, distanceBetweenStuds,
                minDistanceBetweenStudSurfaceAndCupBorder,
                maxDistanceBetweenStudSurfaceAndCupBorder);

            var workFileName = $"{resultDirectory}\\{cupThickness}_{porousThickness}_{cupDesign}_{innerDiameter}D.3dm";
            var opts = new FileWriteOptions();
            RhinoDoc.ActiveDoc.WriteFile(workFileName, opts);

            var command = "-_Open No \"" + workFileName + "\"";
            RhinoApp.RunScript(command, false);

            command = "-_New N";
            RhinoApp.RunScript(command, false);

            return everythingIsOk;
        }

        /// <summary>
        /// CheckGeometry run 3 test parts:
        /// - CheckCupGeometry
        /// - CheckPorousLayerGeometry
        /// - CheckCupStudGeometry
        /// Each cup is initialized with 
        /// - center of rotation at (0, 0, 0),
        /// - anteversion = 20 (default),
        /// - inclination = 40 (default),
        /// - apertureAngle at 170 (default),
        /// - coordinateSystem = Plane.WorldXY, 
        /// - defectIsLeft = false
        /// </summary>
        private bool CheckGeometry(double cupThickness, double porousThickness, CupDesign cupDesign,
            double innerDiameter, double studDiameter, double studHeight, double studHeightAboveCupSurface, double studRoundingRadius, double distanceBetweenStuds,
            double minDistanceBetweenStudSurfaceAndCupBorder, double maxDistanceBetweenStudSurfaceAndCupBorder)
        {
            try
            {
                var centerOfRotation = Point3d.Origin;
                AddPoint(centerOfRotation, "CenterOfRotation");

                var cup = new Cup(centerOfRotation, new CupType(cupThickness, porousThickness, cupDesign), Cup.anteversionDefault, Cup.inclinationDefault, Cup.apertureAngleDefault,
                    innerDiameter, Plane.WorldXY, false);

                RhinoApp.WriteLine("CheckCupGeometry");
                CheckCupGeometry(cup, innerDiameter, cupThickness);

                RhinoApp.WriteLine("CheckPorousLayerGeometry");
                CheckPorousLayerGeometry(cup, innerDiameter, cupThickness, porousThickness);

                RhinoApp.WriteLine("CheckCupStudGeometry");
                CheckCupStudGeometry(cup, studDiameter, studHeight, studHeightAboveCupSurface, studRoundingRadius, distanceBetweenStuds, minDistanceBetweenStudSurfaceAndCupBorder, maxDistanceBetweenStudSurfaceAndCupBorder);
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.Message);
                throw;
            }
            return true;
        }

        /// <summary>
        /// CheckCupGeometry runs 4 checks:
        /// - CheckCupDesignAndCupRingWidthIfApplicable
        /// - CheckCupDiameter
        /// - CheckCupThickness
        /// - CheckCupFixes
        /// </summary>
        private void CheckCupGeometry(Cup cup, double diameter, double thickness)
        {
            AddBrepEntity(cup.BrepGeometry, "Cup");

            RhinoApp.WriteLine("CheckCupDesignAndCupRingWidthIfApplicable");
            CheckCupDesignAndCupRingWidthIfApplicable(cup, diameter);

            RhinoApp.WriteLine("CheckCupDiameter");
            CheckCupDiameter(cup, diameter);

            RhinoApp.WriteLine("CheckCupThickness");
            CheckCupThickness(cup, thickness);

            RhinoApp.WriteLine("CheckCupFixes");
            CheckCupFixes(cup);
        }

        /// <summary>
        /// CheckCupDesignAndCupRingWidthIfApplicable checks:
        /// - CupDesign
        /// - CupRingWidth (if cup is v2)
        /// 
        /// Assumption made:
        /// CupDesign v1 has no ring
        /// CupDesign v2 has a ring
        /// 
        /// If the cup has a ring
        /// (i) the thickest outer cup radius will be larger than the calculated outer cup radius
        /// (ii) the cup ring width can be derived from the thickest outer cup radius
        /// </summary>
        private void CheckCupDesignAndCupRingWidthIfApplicable(Cup cup, double diameter)
        {
            //Arrange and Act
            var centerOfRotation = cup.centerOfRotation;
            var curves3D = cup.BrepGeometry.Curves3D;
            var thickestPoint = centerOfRotation;
            var thickestLength = 0.0;

            foreach (var curve in curves3D)
            {
                var controlPoints = curve.ToNurbsCurve().Points;
                foreach (var point in controlPoints)
                {
                    var curInt = Intersection.CurveCurve(curve,
                        NurbsCurve.CreateFromLine(new Line(centerOfRotation, point.Location)), IntersectionTolerance,
                        IntersectionTolerance);
                    if (curInt.Count <= 0) continue;
                    var p = curInt.OrderBy(cint => centerOfRotation.DistanceTo(cint.PointA)).Last().PointA;
                    var length = centerOfRotation.DistanceTo(p);
                    if (!(length > thickestLength) && Math.Abs(thickestLength) > AssertionTolerance) continue;
                    thickestLength = length;
                    thickestPoint = p;
                }
            }

            AddPoint(thickestPoint, "ThickestPoint");
            var thickestDimension = CreateLinearDimension(centerOfRotation, thickestPoint);
            AddLinearDimension(thickestDimension, "Thickest");

            //Assert
            var cupOuterRadius = cup.InnerCupRadius + cup.cupType.CupThickness;
            switch (cup.cupType.CupDesign)
            {
                case CupDesign.v1:
                    TestAssert.IsTrue(_invokeAssertion, thickestLength < (cupOuterRadius + AssertionTolerance), "Incorrect CupDesign");
                    break;
                case CupDesign.v2:
                    TestAssert.IsTrue(_invokeAssertion, thickestLength > (cupOuterRadius - AssertionTolerance), "Incorrect CupDesign");

                    var vector = thickestPoint - centerOfRotation;
                    vector.Unitize();
                    var radiusPoint = Point3d.Add(centerOfRotation, Vector3d.Multiply(vector, (diameter / 2)));
                    var ringWidthDimension = CreateLinearDimension(radiusPoint, thickestPoint);
                    AddLinearDimension(ringWidthDimension, "CupRingWidth");

                    switch ((int) cup.cupType.CupThickness)
                    {
                        case 2:
                            TestAssert.AreEqual(_invokeAssertion, ringWidthDimension.NumericValue, 3.2, AssertionTolerance, "Incorrect CupRingWidth");
                            break;
                        case 3:
                        case 4:
                            TestAssert.AreEqual(_invokeAssertion, ringWidthDimension.NumericValue, 4.2, AssertionTolerance, "Incorrect CupRingWidth");
                            break;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// CheckCupDiameter checks:
        /// - CupDiameter
        /// 
        /// Assumption made:
        /// The smallest distance from inner cup to center of rotation will be the cup radius
        /// </summary>
        private void CheckCupDiameter(Cup cup, double diameter)
        {
            //Arrange and Act
            var centerOfRotation = cup.centerOfRotation;
            var curves3D = cup.BrepGeometry.Curves3D;
            var radiusPoint = centerOfRotation;
            var radiusLength = 0.0;

            foreach (var curve in curves3D)
            {
                var controlPoints = curve.ToNurbsCurve().Points;
                foreach (var point in controlPoints)
                {
                    var length = centerOfRotation.DistanceTo(point.Location);
                    if (!(length < radiusLength) && Math.Abs(radiusLength) > AssertionTolerance) continue;
                    radiusLength = length;
                    radiusPoint = point.Location;
                }
            }

            AddPoint(radiusPoint, "RadiusPoint");
            var radiusDimension = CreateLinearDimension(centerOfRotation, radiusPoint);
            AddLinearDimension(radiusDimension, "CupRadius");

            //Assert
            TestAssert.AreEqual(_invokeAssertion, radiusDimension.NumericValue, diameter / 2, AssertionTolerance, "Incorrect CupDiameter");
        }

        /// <summary>
        /// CheckCupThickness checks:
        /// - CupThickness
        /// 
        /// Assumption made:
        /// Cup thickness can be derived from the two intersection points when projecting the center of rotation to the cup
        /// </summary>
        private void CheckCupThickness(Cup cup, double thickness)
        {
            //Arrange and Act
            var centerOfRotation = cup.centerOfRotation;
            var intersectionForwards = Intersection.ProjectPointsToBreps(new List<Brep> {cup.Geometry as Brep},
                new List<Point3d> {centerOfRotation}, new Vector3d(0, 0, 1), IntersectionTolerance);
            if (intersectionForwards.Length < 2)
            {
                throw new Exception("Exception in CheckCupThickness");
            }

            var pt1 = intersectionForwards[0];
            var pt2 = intersectionForwards[1];

            var thicknessDimension = CreateLinearDimension(pt1, pt2);
            AddLinearDimension(thicknessDimension, "CupThickness");

            //Assert
            TestAssert.AreEqual(_invokeAssertion, thicknessDimension.NumericValue, thickness, AssertionTolerance, "Incorrect CupThickness");
        }

        /// <summary>
        /// CheckCupFixes checks:
        /// - NumberOfIntersectingTriangles == 0
        /// - NumberOfDoubleTriangles == 0
        /// - NumberOfBadEdges == 0
        /// - NumberOfInvertedNormals == 0
        /// 
        /// Method:
        /// Uses MDCKFixQuery.MeshFixQueryStl to get the numbers
        /// Brep is converted to Mesh using MeshParameters.IDS() 
        /// </summary>
        private void CheckCupFixes(Cup cup)
        {
            //Arrange and Act
            var exportMesh = new Mesh();
            var mp = MeshParameters.IDS();
            var tempBrep = (Brep) cup.Geometry;
            exportMesh.Append(tempBrep.GetCollisionMesh(mp));
            if (exportMesh.Faces.QuadCount > 0)
            {
                exportMesh.Faces.ConvertQuadsToTriangles();
            }
            exportMesh.FaceNormals.ComputeFaceNormals();

            //Dictionary<string, ulong> fixQueryDict;
            //var queried = MDCKFixQuery.MeshFixQueryStl(exportMesh, out fixQueryDict);

            ////Assert
            //if (!queried)
            //{
            //    throw new Exception("Exception in CheckCupFixes");
            //}

            //TestAssert.IsTrue(_invokeAssertion, fixQueryDict["NumberOfIntersectingTriangles"] == 0, "Cup's NumberOfIntersectingTriangles != 0");
            //TestAssert.IsTrue(_invokeAssertion, fixQueryDict["NumberOfDoubleTriangles"] == 0, "Cup's NumberOfDoubleTriangles != 0");
            //TestAssert.IsTrue(_invokeAssertion, fixQueryDict["NumberOfBadEdges"] == 0, "Cup's NumberOfBadEdges != 0");
            //TestAssert.IsTrue(_invokeAssertion, fixQueryDict["NumberOfInvertedNormals"] == 0, "Cup's NumberOfInvertedNormals != 0");
        }

        /// <summary>
        /// CheckPorousLayerGeometry runs 3 checks:
        /// - CheckPorousLayerOuterDiameter
        /// - CheckPorousLayerThickness
        /// - CheckPorousLayerOverlap
        /// </summary>
        private void CheckPorousLayerGeometry(Cup cup, double diameter, double cupThickness, double porousThickness)
        {
            AddBrepEntity(cup.porousShell, "PorousLayer");

            RhinoApp.WriteLine("CheckPorousLayerOuterDiameter");
            CheckPorousLayerOuterDiameter(cup, diameter + (2 * cupThickness) + (2 * porousThickness));

            var porousLayerOverlap = (cup.cupType.CupDesign == CupDesign.v1) ? 0.25 : 0.1;
            RhinoApp.WriteLine("CheckPorousLayerThickness");
            CheckPorousLayerThickness(cup, porousThickness + porousLayerOverlap);

            RhinoApp.WriteLine("CheckPorousLayerOverlap");
            CheckPorousLayerOverlap(cup, porousLayerOverlap);
        }

        /// <summary>
        /// CheckPorousLayerOuterDiameter checks:
        /// - PorousLayerOuterDiameter
        /// 
        /// Assumption made:
        /// Porous layer outer diameter can be derived from the furtherest intersection point when projecting the center of rotation to the cup's porous shell
        /// </summary>
        private void CheckPorousLayerOuterDiameter(Cup cup, double diameter)
        {
            //Arrange and Act
            var centerOfRotation = cup.centerOfRotation;
            var intersectionForwards = Intersection.ProjectPointsToBreps(new List<Brep> {cup.porousShell},
                new List<Point3d> {centerOfRotation}, new Vector3d(0, 0, 1), IntersectionTolerance);
            if (intersectionForwards.Length < 2)
            {
                throw new Exception("Exception in CheckPorousLayerOuterDiameter");
            }

            var furtherest = intersectionForwards.OrderBy(pt => (centerOfRotation - pt).Length).Last();

            AddPoint(furtherest, "PorousLayerFurtherestPoint");
            var porousLayerRadius = CreateLinearDimension(centerOfRotation, furtherest);
            AddLinearDimension(porousLayerRadius, "PorousLayerOuterRadius");

            //Assert
            TestAssert.AreEqual(_invokeAssertion, porousLayerRadius.NumericValue, diameter / 2, AssertionTolerance, "Incorrect PorousLayerOuterDiameter");
        }

        /// <summary>
        /// CheckPorousLayerThickness checks:
        /// - PorousLayerThickness
        /// 
        /// Assumption made:
        /// Porous layer thickness can be derived from the two intersection points when projecting the center of rotation to the cup's porous shell
        /// </summary>
        private void CheckPorousLayerThickness(Cup cup, double thickness)
        {
            //Arrange and Act
            var centerOfRotation = cup.centerOfRotation;
            var intersectionForwards = Intersection.ProjectPointsToBreps(new List<Brep> {cup.porousShell},
                new List<Point3d> {centerOfRotation}, new Vector3d(0, 0, 1), IntersectionTolerance);
            if (intersectionForwards.Length < 2)
            {
                throw new Exception("Exception in CheckPorousLayerThickness");
            }

            var pt1 = intersectionForwards[0];
            var pt2 = intersectionForwards[1];

            var porousLayerThickness = CreateLinearDimension(pt1, pt2);
            AddLinearDimension(porousLayerThickness, "PorousLayerThickness");

            //Assert
            TestAssert.AreEqual(_invokeAssertion, porousLayerThickness.NumericValue, thickness, AssertionTolerance, "Incorrect PorousLayerThickness");
        }

        /// <summary>
        /// CheckPorousLayerOverlap checks:
        /// - PorousLayerOverlap
        /// 
        /// Assumption made:
        /// Porous layer overlap can be derived from 
        /// (i) the nearest intersection point when projecting the center of rotation to the cup's porous shell
        ///  and (ii) the furtherest intersection point when projecting the center of rotation to the cup
        /// </summary>
        private void CheckPorousLayerOverlap(Cup cup, double overlap)
        {
            //Arrange and Act
            var centerOfRotation = cup.centerOfRotation;
            var intersectionOnPorous = Intersection.ProjectPointsToBreps(new List<Brep> {cup.porousShell},
                new List<Point3d> {centerOfRotation}, new Vector3d(0, 0, 1), IntersectionTolerance);
            var intersectionOnCup = Intersection.ProjectPointsToBreps(new List<Brep> {cup.Geometry as Brep},
                new List<Point3d> {centerOfRotation}, new Vector3d(0, 0, 1), IntersectionTolerance);

            if (intersectionOnPorous.Length < 2 || intersectionOnCup.Length < 2)
            {
                throw new Exception("Exception in CheckPorousLayerOverlap");
            }
            var nearestToPorous = intersectionOnPorous.OrderBy(pt => (centerOfRotation - pt).Length).First();
            var furtherestToCup = intersectionOnCup.OrderBy(pt => (centerOfRotation - pt).Length).Last();

            var overlapDimension = CreateLinearDimension(nearestToPorous, furtherestToCup);
            AddLinearDimension(overlapDimension, "PorousLayerOverlap");

            //Assert
            TestAssert.AreEqual(_invokeAssertion, overlapDimension.NumericValue, overlap, AssertionTolerance, "Incorrect PorousLayerOverlap");
        }

        /// <summary>
        /// CheckCupStudGeometry runs 6 checks:
        /// - CheckStudDiameter
        /// - CheckStudHeight
        /// - CheckStudHeightAboveCupSurface
        /// - CheckStudRoundingRadius
        /// - CheckDistanceBetweenStuds
        /// - CheckSmallestDistanceBetweenStudSurfaceAndCupBorder
        /// 
        /// Method:
        /// - Split disjoint pieces to get individual studs from IBB.CupStuds
        /// - using Random to pick a stud to test
        /// </summary>
        private void CheckCupStudGeometry(Cup cup, double diameter, double height, double heightAboveCupSurface, double roundingRadius, double distanceBetweenStuds, double minDistanceBetweenStudSurfaceAndCupBorder, double maxDistanceBetweenStudSurfaceAndCupBorder)
        {
            var doc = RhinoDoc.ActiveDoc;
            var director = new ImplantDirector(doc, new PluginInfoModel());
            var objectManager = new AmaceObjectManager(director);
            var cupId = objectManager.AddNewBuildingBlock(IBB.Cup, cup);
            if (!StudMaker.GenerateAmaceStuds(director)) return;
            var cupStuds = objectManager.GetBuildingBlock(IBB.CupStuds).Geometry as Mesh;
            var studs = cupStuds.SplitDisjointPieces();

            var random = new Random();
            var stud = studs[random.Next(0, studs.Length - 1)];
            if (stud.Faces.QuadCount > 0)
            {
                stud.Faces.ConvertQuadsToTriangles();
            }
            AddMeshEntity(stud, "Stud");
                
            var normal = stud.Normals.GroupBy(n => n).OrderBy(group => @group.Count()).Last().Key;
            var volMassProp = VolumeMassProperties.Compute(stud);
            var centroid = volMassProp.Centroid;
            AddPoint(centroid, "StudCentroid");

            RhinoApp.WriteLine("CheckStudDiameter");
            CheckStudDiameter(stud, normal, centroid, diameter);

            RhinoApp.WriteLine("CheckStudHeight");
            CheckStudHeight(stud, normal, centroid, height);

            doc.Objects.Unlock(cupId, true);
            var cupMesh = MeshUtilities.ConvertBrepToMesh(cup.Geometry as Brep);
            if (cupMesh.Faces.QuadCount > 0)
            {
                cupMesh.Faces.ConvertQuadsToTriangles();
            }
            AddMeshEntity(cupMesh, "CupMesh");

            RhinoApp.WriteLine("CheckStudHeightAboveCupSurface");
            CheckStudHeightAboveCupSurface(stud, cupMesh, normal, centroid, heightAboveCupSurface);

            RhinoApp.WriteLine("CheckStudRoundingRadius");
            CheckStudRoundingRadius(stud, normal, centroid, roundingRadius);

            RhinoApp.WriteLine("CheckDistanceBetweenStuds");
            CheckDistanceBetweenStuds(studs, distanceBetweenStuds);

            RhinoApp.WriteLine("CheckSmallestDistanceBetweenStudSurfaceAndCupBorder");
            CheckSmallestDistanceBetweenStudSurfaceAndCupBorder(studs, cupMesh, cup.centerOfRotation, minDistanceBetweenStudSurfaceAndCupBorder, maxDistanceBetweenStudSurfaceAndCupBorder);
        }

        /// <summary>
        /// CheckStudDiameter checks:
        /// - StudDiameter
        /// </summary>
        private void CheckStudDiameter(Mesh stud, Vector3d normal, Point3d centroid, double diameter)
        {
            //Arrange and Act
            var studPlane = new Plane(centroid, normal);
            var outlines = stud.GetOutlines(studPlane);
            var line = outlines[0];
            AddCurve(line.ToNurbsCurve(), "StudDiameterOutline");

            var count = line.Count / 3;
            var circle = new Circle(line[0], line[0 + count], line[0 + count + count]);

            //var studDiameterDimension = new RadialDimension(circle, line[0], circle.Diameter);
            var studDiameterDimension = RadialDimension.Create(new DimensionStyle(), AnnotationType.Radius, studPlane,
                line[0], line[0 + count], line[0 + count + count]); //TODO Not sure if this is correct?
            AddRadialDimension(studDiameterDimension, "StudDiameter");

            //Assert
            TestAssert.AreEqual(_invokeAssertion, studDiameterDimension.NumericValue, diameter, AssertionTolerance, "Incorrect StudDiameter");
        }

        /// <summary>
        /// CheckStudHeight checks:
        /// - StudHeight
        /// </summary>
        private void CheckStudHeight(Mesh stud, Vector3d normal, Point3d centroid, double height)
        {
            //Arrange and Act
            var moveOutside = Point3d.Add(centroid, Vector3d.Multiply(normal, 10));
            var normalReverse = new Vector3d(normal);
            normalReverse.Reverse();
            var intersectionStud = Intersection.ProjectPointsToMeshes(new List<Mesh> {stud}, new List<Point3d> {moveOutside}, normalReverse, IntersectionTolerance);
            if (intersectionStud.Length < 2)
            {
                throw new Exception("Exception in CheckStudHeight");
            }

            var pt1 = intersectionStud[0];
            var pt2 = intersectionStud[1];
            AddPoint(pt1, "StudHeight1");
            AddPoint(pt2, "StudHeight2");

            var studHeight = CreateLinearDimension(pt1, pt2);
            AddLinearDimension(studHeight, "StudHeight");

            //Assert
            TestAssert.AreEqual(_invokeAssertion, studHeight.NumericValue, height, AssertionTolerance, "Incorrect StudHeight");
        }

        /// <summary>
        /// CheckStudHeightAboveCupSurface checks:
        /// - StudHeightAboveCupSurface
        /// </summary>
        private void CheckStudHeightAboveCupSurface(Mesh stud, Mesh cup, Vector3d normal, Point3d centroid, double height)
        {
            //Arrange and Act
            var subtracted = Mesh.CreateBooleanDifference(new List<Mesh> {stud}, new List<Mesh> {cup});
            var studAboveCup = subtracted[0];
            AddMeshEntity(studAboveCup, "StudAboveCup");

            var moveOutside = Point3d.Add(centroid, Vector3d.Multiply(normal, 10));
            var normalReverse = new Vector3d(normal);
            normalReverse.Reverse();
            var intersectionStudAboveCup = Intersection.ProjectPointsToMeshes(new List<Mesh> {studAboveCup}, new List<Point3d> {moveOutside}, normalReverse, IntersectionTolerance);
            if (intersectionStudAboveCup.Length < 2)
            {
                throw new Exception("Exception in CheckStudHeightAboveCupSurface");
            }

            var pt1 = intersectionStudAboveCup[0];
            var pt2 = intersectionStudAboveCup[1];
            AddPoint(pt1, "StudAboveCupHeight1");
            AddPoint(pt2, "StudAboveCupHeight2");

            var studAboveCupHeight = CreateLinearDimension(pt1, pt2);
            AddLinearDimension(studAboveCupHeight, "StudHeightAboveCupSurface");

            //Assert
            TestAssert.AreEqual(_invokeAssertion, studAboveCupHeight.NumericValue, height, AssertionTolerance, "Incorrect StudHeightAboveCupSurface");
        }

        /// <summary>
        /// CheckStudRoundingRadius checks:
        /// - StudRoundingRadius
        /// </summary>
        private void CheckStudRoundingRadius(Mesh stud, Vector3d normal, Point3d centroid, double radius)
        {
            //Arrange and Act
            var studPlane = new Plane(centroid, normal);
            var studRoundingPlane = new Plane(centroid, studPlane.YAxis, studPlane.ZAxis);
            var roundingOutlines = stud.GetOutlines(studRoundingPlane);
            var rounding = roundingOutlines[0];
            AddCurve(rounding.ToNurbsCurve(), "StudRounding");

            var roundingLines = new List<Curve>();
            var xAxis = studRoundingPlane.XAxis;
            var yAxis = studRoundingPlane.YAxis;

            var segments = rounding.GetSegments();
            var lines = new List<Line>();
            foreach (var segment in segments)
            {
                var diff = segment.From - segment.To;
                if (!diff.IsPerpendicularTo(xAxis) && !diff.IsPerpendicularTo(yAxis))
                {
                    lines.Add(segment);
                }
                else
                {
                    CreateCurveFromLines(lines, roundingLines);
                }
            }

            CreateCurveFromLines(lines, roundingLines);

            if (roundingLines.Count > 2)
            {
                foreach (var line in roundingLines)
                {
                    var connectedLines = roundingLines.Where(ln => ln.PointAtStart == line.PointAtEnd && ln != line);
                    var enumerable = connectedLines as Curve[] ?? connectedLines.ToArray();
                    if (!enumerable.Any()) continue;
                    var connected = enumerable.First();
                    roundingLines.Remove(connected);
                    roundingLines.Remove(line);
                    roundingLines.Add(Curve.JoinCurves(new List<Curve> {connected, line}).First());
                    break;
                }
            }

            for (var r = 0; r < roundingLines.Count; r++)
            {
                var roundingLine = roundingLines[r];
                AddCurve(roundingLine, $"RoundingCurve{r}");

                var xLinePlane = new Plane(roundingLine.PointAtStart, xAxis);
                var yLinePlane = new Plane(roundingLine.PointAtStart, yAxis);
                //var distanceToXPlane = Math.Abs(xLinePlane.DistanceTo(roundingLine.PointAtEnd));
                //var distanceToYPlane = Math.Abs(yLinePlane.DistanceTo(roundingLine.PointAtEnd));

                AddPoint(roundingLine.PointAtStart, $"RoundingPointStart{r}");
                AddPoint(roundingLine.PointAtEnd, $"RoundingPointEnd{r}");

                var ptX = xLinePlane.ClosestPoint(roundingLine.PointAtEnd);
                AddPoint(ptX, $"RoundingPointX{r}");
                var roundingXDimension = CreateLinearDimension(roundingLine.PointAtEnd, ptX);
                AddLinearDimension(roundingXDimension, $"RoundingRadiusX{r}");

                var ptY = yLinePlane.ClosestPoint(roundingLine.PointAtEnd);
                AddPoint(ptY, $"RoundingPointY{r}");
                var roundingYDimension = CreateLinearDimension(roundingLine.PointAtEnd, ptY);
                AddLinearDimension(roundingYDimension, $"RoundingRadiusY{r}");

                //Assert
                TestAssert.AreEqual(_invokeAssertion, roundingXDimension.NumericValue, radius, AssertionTolerance, "Incorrect StudRoundingRadius");
                TestAssert.AreEqual(_invokeAssertion, roundingYDimension.NumericValue, radius, AssertionTolerance, "Incorrect StudRoundingRadius");
            }
        }

        /// <summary>
        /// CheckDistanceBetweenStuds checks:
        /// - DistanceBetweenStud
        /// </summary>
        private void CheckDistanceBetweenStuds(Mesh[] studs, double distanceBetweenStuds)
        {
            //Arrange and Act
            var centers = new List<Point3d>();
            foreach (var stud in studs)
            {
                var centroidPoint = VolumeMassProperties.Compute(stud).Centroid;
                var norm = stud.Normals.GroupBy(n => n).OrderBy(group => group.Count()).Last().Key;
                norm.Reverse();
                var outside = Point3d.Add(centroidPoint, Vector3d.Multiply(norm, 10));
                norm.Reverse();
                var inter = Intersection.ProjectPointsToMeshes(new List<Mesh> { stud }, new List<Point3d> { outside }, norm, IntersectionTolerance);
                if (inter.Length <= 0) continue;
                var pt1 = inter[0];
                AddPoint(pt1, "StudCenter");
                centers.Add(pt1);
            }

            var pointA = Point3d.Origin;
            var pointB = Point3d.Origin;
            var distance = 0.0;
            foreach (var c1 in centers)
            {
                foreach (var c2 in centers)
                {
                    if (c1 == c2) continue;
                    var length = (c1 - c2).Length;
                    if (!(length < distance) && Math.Abs(distance) > AssertionTolerance) continue;
                    distance = length;
                    pointA = c1;
                    pointB = c2;
                }
            }

            var distanceDimension = CreateLinearDimension(pointA, pointB);
            AddLinearDimension(distanceDimension, "DistanceBetweenStud");

            //Assert
            TestAssert.AreEqual(_invokeAssertion, distanceDimension.NumericValue, distanceBetweenStuds, AssertionToleranceToCheck, "Incorrect DistanceBetweenStud"); //2.9383895909106656
        }

        /// <summary>
        /// CheckSmallestDistanceBetweenStudSurfaceAndCupBorder checks:
        /// - SmallestDistanceBetweenStudSurfaceAndCupBorder
        /// </summary>
        private void CheckSmallestDistanceBetweenStudSurfaceAndCupBorder(Mesh[] studs, Mesh cup, Point3d centerOfRotation, double minDistance,
            double maxDistance)
        {
            //Arrange and Act
            var closestFromCupBorder = 0.0;
            Mesh closestFromCupBorderMesh = null;
            var cupBorder = new Vector3d(cup.Normals.GroupBy(n => n).OrderBy(group => group.Count()).Last().Key);
            var cupBorderPlane = new Plane(centerOfRotation, cupBorder);
            
            var r = 0;
            foreach (var std in studs)
            {
                var centroidPoint = VolumeMassProperties.Compute(std).Centroid;
                var dist = Math.Abs(cupBorderPlane.DistanceTo(centroidPoint));

                if (!(dist < closestFromCupBorder) && Math.Abs(closestFromCupBorder) > AssertionTolerance) continue;
                closestFromCupBorder = dist;
                closestFromCupBorderMesh = std;
            }
            
            AddMeshEntity(closestFromCupBorderMesh, "ClosestFromCupBorderStud");

            var interLines = Intersection.MeshMeshFast(closestFromCupBorderMesh, cup);
            var interCurves = Curve.JoinCurves(interLines.Select(NurbsCurve.CreateFromLine));
            var interLine = interCurves.First();
            AddCurve(interLine, "IntersectLine");
            
            var cupBorderMesh = cup.DuplicateMesh();
            cupBorderMesh.FaceNormals.ComputeFaceNormals();
            if (cupBorderMesh.FaceNormals.Count != cupBorderMesh.Faces.Count)
            {
                throw new Exception("Exception in CheckSmallestDistanceBetweenStudSurfaceAndCupBorder");
            }

            var cupBorderFaces = new List<int>();
            for (var i = 0; i < cupBorderMesh.FaceNormals.Count; i++)
            {
                if (cupBorder.IsParallelTo(cupBorderMesh.FaceNormals[i]) == 0)
                {
                    cupBorderFaces.Add(i);
                }
            }
            cupBorderMesh.Faces.DeleteFaces(cupBorderFaces);
            AddMeshEntity(cupBorderMesh, "CupBorderMesh");

            var pointA = Point3d.Origin;
            var pointB = Point3d.Origin;
            var smallestDist = 0.0;
            var controlPoints = interLine.ToNurbsCurve().Points;
            foreach (var t in controlPoints)
            {
                var point = t.Location;
                var closestPoint = cupBorderMesh.ClosestPoint(point);
                var length = closestPoint.DistanceTo(point);
                if (!(length < smallestDist) && Math.Abs(smallestDist) > AssertionTolerance) continue;
                smallestDist = length;
                pointA = point;
                pointB = closestPoint;
            }

            var smallestDistanceDimension = CreateLinearDimension(pointA, pointB);
            AddLinearDimension(smallestDistanceDimension, "SmallestDistanceBetweenStudSurfaceAndCupBorder");

            //Assert
            smallestDist = smallestDistanceDimension.NumericValue;
            TestAssert.IsTrue(_invokeAssertion, smallestDist > (minDistance - AssertionToleranceToCheck), "Incorrect SmallestDistanceBetweenStudSurfaceAndCupBorder");
            TestAssert.IsTrue(_invokeAssertion, smallestDist < (maxDistance + AssertionToleranceToCheck), "Incorrect SmallestDistanceBetweenStudSurfaceAndCupBorder");
        }

        private LinearDimension CreateLinearDimension(Point3d pointStart, Point3d pointEnd)
        {
            var normal = pointEnd - pointStart;
            var originPlane = new Plane(pointStart, normal);
            var plane = new Plane(originPlane.Origin, originPlane.ZAxis, originPlane.XAxis);

            double u, v;
            plane.ClosestParameter(pointStart, out u, out v);
            var ext1 = new Point2d(u, v);

            plane.ClosestParameter(pointEnd, out u, out v);
            var ext2 = new Point2d(u, v);

            var pointOnDimensionLine = new Point3d((pointStart.X + pointEnd.X) / 2, (pointStart.Y + pointEnd.Y) / 2,
                (pointStart.Z + pointEnd.Z) / 2);
            plane.ClosestParameter(pointOnDimensionLine, out u, out v);
            var linePt = new Point2d(u, v);

            var dimension = new LinearDimension(plane, ext1, ext2, linePt);
            return dimension;
        }

        private void CreateCurveFromLines(List<Line> lines, List<Curve> curves)
        {
            if (!lines.Any()) return;
            var curve = Curve.JoinCurves(lines.Select(NurbsCurve.CreateFromLine)).First();
            curves.Add(curve);
            lines.Clear();
        }

        private Guid AddLinearDimension(LinearDimension dimension, string objName)
        {
            if (dimension == null)
                return Guid.Empty;

            var doc = RhinoDoc.ActiveDoc;

            var layering = "Dimensions::" + objName;

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layering),
                Name = objName
            };

            return doc.Objects.AddLinearDimension(dimension, oa);
        }

        private Guid AddRadialDimension(RadialDimension dimension, string objName)
        {
            if (dimension == null)
                return Guid.Empty;

            var doc = RhinoDoc.ActiveDoc;

            var layering = "Dimensions::" + objName;

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layering),
                Name = objName
            };

            return doc.Objects.AddRadialDimension(dimension, oa);
        }

        private Guid AddPoint(Point3d point, string objName)
        {
            const string layerName = "Points";
            var color = Color.Blue;

            if (!point.IsValid)
                return Guid.Empty;

            var doc = RhinoDoc.ActiveDoc;

            var layering = layerName + "::" + objName;

            var oa = new ObjectAttributes();
            oa.LayerIndex = doc.GetLayerWithPath(layering);
            oa.ObjectColor = color;
            oa.MaterialSource = ObjectMaterialSource.MaterialFromObject;
            oa.ColorSource = ObjectColorSource.ColorFromObject;
            oa.Name = objName;

            return doc.Objects.AddPoint(point, oa);
        }

        private Guid AddCurve(Curve curve, string objName)
        {
            const string layerName = "Curves";
            var color = Color.Red;

            if (curve == null)
                return Guid.Empty;

            var doc = RhinoDoc.ActiveDoc;

            var layering = layerName + "::" + objName;

            if (doc.Objects.FindByLayer(objName) != null)
            {
                var obj = doc.Objects.FindByLayer(objName).ToList();
                obj.ForEach(x => doc.Objects.Delete(x.Id, true));
            }

            if (doc.GetLayerWithPath(layering) > 0)
                doc.Layers.Delete(doc.GetLayerWithPath(layering), true);

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layering),
                ObjectColor = color,
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromObject,
                Name = objName
            };

            return doc.Objects.AddCurve(curve, oa);
        }

        private Guid AddBrepEntity(Brep brep, string objName)
        {
            const string layerName = "BrepEntities";

            if (brep == null)
                return Guid.Empty;

            var doc = RhinoDoc.ActiveDoc;

            var layering = layerName + "::" + objName;

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layering),
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromMaterial,
                Name = objName
            };

            return doc.Objects.AddBrep(brep, oa);
        }

        private Guid AddMeshEntity(Mesh mesh, string objName)
        {
            const string layerName = "MeshEntities";

            if (mesh == null)
                return Guid.Empty;

            var doc = RhinoDoc.ActiveDoc;

            var layering = layerName + "::" + objName;

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layering),
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromMaterial,
                Name = objName
            };

            return doc.Objects.AddMesh(mesh, oa);
        }
    }
}
