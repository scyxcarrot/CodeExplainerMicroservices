using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Quality;
using IDS.Core.SplashScreen;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.Commands
{
    [System.Runtime.InteropServices.Guid("DE19F33B-84B4-444D-B384-72D7847C4BC3")]
    public class TestGuideHoleBooleanIntersection : Command
    {
        public TestGuideHoleBooleanIntersection()
        {
            Instance = this;
        }

        public static TestGuideHoleBooleanIntersection Instance { get; private set; }

        public override string EnglishName => "TestGuideHoleBooleanIntersection";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var everythingSucceeded = RunFullTest(doc);

            return everythingSucceeded ? Result.Success : Result.Failure;
        }

        public static bool RunFullTest(RhinoDoc doc)
        {
            var everythingSucceeded = true;

            var director = new ImplantDirector(doc, new PluginInfoModel());

            everythingSucceeded &= Check_Returns_False_When_No_Screw_Intersect(director);
            everythingSucceeded &= Check_Returns_True_When_One_Screw_Intersects_With_Another_Screw(director);
            everythingSucceeded &= Check_Returns_True_When_Two_Screws_Intersect_Each_Other(director);
            everythingSucceeded &= Check_Returns_True_When_One_Screw_Intersects_With_Two_Other_Screws(director);
            everythingSucceeded &= Check_Returns_True_When_Two_Screws_Intersect_With_One_Screw(director);

            Reporting.ShowResultsInCommandLine(everythingSucceeded, "Guide Hole Boolean Intersection");
            return everythingSucceeded;
        }

        private static bool Check_Returns_False_When_No_Screw_Intersect(ImplantDirector director)
        {
            //Arrange
            var screws = new List<Screw>();
            const double drillBitRadius = 1.8;

            var screw1 = new ScrewMock(director, new Point3d(0, 0, 0), new Point3d(0, 0, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1);
            var screw2 = new ScrewMock(director, new Point3d(-10, -10, -10), new Point3d(-10, -10, -200), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 2);
            var screw3 = new ScrewMock(director, new Point3d(-50, 0, -5), new Point3d(-60, 10, 50), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 3);
            screws.Add(screw1);
            screws.Add(screw2);
            screws.Add(screw3);

            //AddToDocument(director.document, screws, drillBitRadius);

            //Act
            var screwAnalysis = new AmaceScrewAnalysis();
            var intersection = screwAnalysis.PerformGuideHoleBooleanIntersectionCheck(screws, drillBitRadius);

            //Assert
            var matching = (intersection == null) || (intersection.Count == 0);
            return matching;
        }

        private static bool Check_Returns_True_When_One_Screw_Intersects_With_Another_Screw(ImplantDirector director)
        {
            //Arrange
            var screws = new List<Screw>();
            const double drillBitRadius = 1.8;

            var screw1 = new ScrewMock(director, new Point3d(0, 0, 0), new Point3d(0, 0, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1);
            var screw2 = new ScrewMock(director, new Point3d(-10, -10, -10), new Point3d(-10, -10, -200), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 2);
            var screw3 = new ScrewMock(director, new Point3d(-3, 3, -26), new Point3d(-16, 45, 3), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 3);
            screws.Add(screw1);
            screws.Add(screw2);
            screws.Add(screw3);

            //AddToDocument(director.document, screws, drillBitRadius);

            //Act
            var screwAnalysis = new AmaceScrewAnalysis();
            var intersections = screwAnalysis.PerformGuideHoleBooleanIntersectionCheck(screws, drillBitRadius);

            //Assert
            var matching = intersections != null && intersections.Count == 1 && intersections.Keys.Contains(1) &&
                           intersections[1].Count == 1 && intersections[1].Contains(3);
            return matching;
        }

        private static bool Check_Returns_True_When_Two_Screws_Intersect_Each_Other(ImplantDirector director)
        {
            //Arrange
            var screws = new List<Screw>();
            const double drillBitRadius = 1.8;

            var screw1 = new ScrewMock(director, new Point3d(0, 0, 0), new Point3d(0, 0, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1);
            var screw2 = new ScrewMock(director, new Point3d(-10, -10, -10), new Point3d(-10, -10, -200), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 2);
            var screw3 = new ScrewMock(director, new Point3d(-2, 6, -5), new Point3d(-13, 45, 3), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 3);
            screws.Add(screw1);
            screws.Add(screw2);
            screws.Add(screw3);

            //AddToDocument(director.document, screws, drillBitRadius);

            //Act
            var screwAnalysis = new AmaceScrewAnalysis();
            var intersections = screwAnalysis.PerformGuideHoleBooleanIntersectionCheck(screws, drillBitRadius);

            //Assert
            var matching = intersections != null && intersections.Count == 2 && intersections.Keys.Contains(1) && intersections.Keys.Contains(3) &&
                           intersections[1].Count == 1 && intersections[1].Contains(3) && intersections[3].Count == 1 && intersections[3].Contains(1);
            return matching;
        }

        private static bool Check_Returns_True_When_Two_Screws_Intersect_With_One_Screw(ImplantDirector director)
        {
            //Arrange
            var screws = new List<Screw>();
            const double drillBitRadius = 1.8;

            var screw1 = new ScrewMock(director, new Point3d(0, 0, 0), new Point3d(0, 0, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1);
            var screw2 = new ScrewMock(director, new Point3d(1, 16, -12), new Point3d(1, 96, -69), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 2);
            var screw3 = new ScrewMock(director, new Point3d(-27, -15, -4), new Point3d(-73, -32, -2), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 3);
            screws.Add(screw1);
            screws.Add(screw2);
            screws.Add(screw3);

            //AddToDocument(director.Document, screws, drillBitRadius);

            //Act
            var screwAnalysis = new AmaceScrewAnalysis();
            var intersections = screwAnalysis.PerformGuideHoleBooleanIntersectionCheck(screws, drillBitRadius);

            //Assert
            var matching = intersections != null && intersections.Count == 2 && intersections.Keys.Contains(2) &&
                           intersections.Keys.Contains(3) &&
                           intersections[2].Count == 1 && intersections[2].Contains(1) && intersections[3].Count == 1 &&
                           intersections[3].Contains(1);
            return matching;
        }

        private static bool Check_Returns_True_When_One_Screw_Intersects_With_Two_Other_Screws(ImplantDirector director)
        {
            //Arrange
            var screws = new List<Screw>();
            const double drillBitRadius = 1.8;

            var screw1 = new ScrewMock(director, new Point3d(0, 0, 0), new Point3d(0, 0, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1);
            var screw2 = new ScrewMock(director, new Point3d(-10, -10, -10), new Point3d(-10, -10, -200), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 2);
            var screw3 = new ScrewMock(director, new Point3d(-27, -15, -4), new Point3d(-73, -32, -2), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 3);
            screws.Add(screw1);
            screws.Add(screw2);
            screws.Add(screw3);

            //AddToDocument(director.document, screws, drillBitRadius);

            //Act
            var screwAnalysis = new AmaceScrewAnalysis();
            var intersections = screwAnalysis.PerformGuideHoleBooleanIntersectionCheck(screws, drillBitRadius);

            //Assert
            var matching = intersections != null && intersections.Count == 1 && intersections.Keys.Contains(3) &&
                           intersections[3].Count == 2 && intersections[3].Contains(1) && intersections[3].Contains(2);
            return matching;
        }

        private static void AddToDocument(RhinoDoc doc, List<Screw> screws, double drillBitRadius)
        {
            var screwGuideCreator = new ScrewGuideCreator();

            foreach (var screw in screws)
            {
                var guideHoleBoolean = screwGuideCreator.GetGuideHoleBoolean(screw, drillBitRadius);
                var guideHoleSafetyZone = screwGuideCreator.GetGuideHoleSafetyZone(screw, drillBitRadius);

                doc.Objects.AddRhinoObject(screw);
                doc.Objects.AddBrep(guideHoleBoolean);
                doc.Objects.AddBrep(guideHoleSafetyZone);
            }
        }
    }

    public class ScrewMock : Screw
    {
        public override ScrewPosition positioning { get; }

        public ScrewMock()
        {
        }

        public ScrewMock(ImplantDirector director, Point3d headPoint, Point3d tipPoint, ScrewType screwType, ScrewAlignment screwAlignment, int newIndex, ScrewPosition positioning = ScrewPosition.Cup) :
            base(director, headPoint, tipPoint, screwType, screwAlignment, newIndex)
        {
            this.positioning = positioning;
        }
    }
}
