using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.SplashScreen;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.Testing.Commands
{
    [System.Runtime.InteropServices.Guid("D2CFC327-8D39-4574-BE92-C0356FB3C8E1")]
    public class TestMedialBumpCriterion : Command
    {
        public TestMedialBumpCriterion()
        {
            Instance = this;
        }

        public static TestMedialBumpCriterion Instance { get; private set; }

        public override string EnglishName => "TestMedialBumpCriterion";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var everythingSucceeded = RunFullTest(doc);

            return everythingSucceeded ? Result.Success : Result.Failure;
        }

        public static bool RunFullTest(RhinoDoc doc)
        {
            var everythingSucceeded = true;

            var director = new ImplantDirector(doc, new PluginInfoModel());

            everythingSucceeded &= Creator_Returns_MedialBump_When_NoIntersect_And_BelowHorizontalBorder_For_V1Cup(director, ScrewPosition.Cup);
            everythingSucceeded &= Creator_Returns_MedialBump_When_NoIntersect_And_AboveHorizontalBorder_For_V1Cup(director, ScrewPosition.Cup);
            everythingSucceeded &= Creator_Returns_MedialBump_When_Intersect_And_BelowHorizontalBorder_For_V1Cup(director, ScrewPosition.Cup);
            everythingSucceeded &= Creator_Returns_HorizontalBorderBump_When_Intersect_And_AboveHorizontalBorder_For_V1Cup(director, ScrewPosition.Cup);
            everythingSucceeded &= Creator_Returns_MedialBump_When_NoIntersect_For_V2Cup(director, ScrewPosition.Cup);
            everythingSucceeded &= Creator_Returns_HorizontalBorderBump_When_Intersect_For_V2Cup(director, ScrewPosition.Cup);

            everythingSucceeded &= Creator_Returns_MedialBump_When_NoIntersect_And_BelowHorizontalBorder_For_V1Cup(director, ScrewPosition.Flange);
            everythingSucceeded &= Creator_Returns_MedialBump_When_NoIntersect_And_AboveHorizontalBorder_For_V1Cup(director, ScrewPosition.Flange);
            everythingSucceeded &= Creator_Returns_MedialBump_When_Intersect_And_BelowHorizontalBorder_For_V1Cup(director, ScrewPosition.Flange);
            everythingSucceeded &= Creator_Returns_MedialBump_When_Intersect_And_AboveHorizontalBorder_For_V1Cup(director, ScrewPosition.Flange);
            everythingSucceeded &= Creator_Returns_MedialBump_When_NoIntersect_For_V2Cup(director, ScrewPosition.Flange);
            everythingSucceeded &= Creator_Returns_HorizontalBorderBump_When_Intersect_For_V2Cup(director, ScrewPosition.Flange);

            everythingSucceeded &= Creator_Returns_NoMedialBump_When_Screw_Is_ScrewAlignmentFloating_And_ScrewPostionFlange(director);

            Reporting.ShowResultsInCommandLine(everythingSucceeded, "Medial Bump Criterion");
            return everythingSucceeded;
        }

        private static bool Creator_Returns_MedialBump_When_NoIntersect_And_BelowHorizontalBorder_For_V1Cup(ImplantDirector director, ScrewPosition screwPosition)
        {
            //Arrange
            var screw = new ScrewMock(director, new Point3d(0, 0, 15), new Point3d(0, 0, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1, screwPosition);
            var cup = new Cup(Point3d.Origin, new CupType(4, 1, CupDesign.v1), 0, 0, 170, 54, Plane.WorldXY,
                false);
            var screwAideManager = new ScrewAideManager(screw, director.ScrewDatabase);
            var medialBump = screwAideManager.GetMedialBumpMesh();

            //Act
            var creator = new ScrewMedialBumpCreator(director.ScrewDatabase, cup);
            var hasMedialBump = creator.ScrewShouldHaveMedialBump(screw);
            var bump = creator.CreateMedialBumpForScrewWithMedialBump(screw);
            //AddToDocumentForV1Cup(director.document, screw, cup, bump);

            //Assert
            var shouldHaveMedialBump = hasMedialBump;
            var matching = (medialBump.Vertices.Count == bump.Vertices.Count) && (medialBump.Faces.Count == bump.Faces.Count);
            return matching && shouldHaveMedialBump;
        }

        private static bool Creator_Returns_MedialBump_When_NoIntersect_And_AboveHorizontalBorder_For_V1Cup(ImplantDirector director, ScrewPosition screwPosition)
        {
            //Arrange
            var screw = new ScrewMock(director, new Point3d(0, 0, -15), new Point3d(0, 0, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1, screwPosition);
            var cup = new Cup(Point3d.Origin, new CupType(4, 1, CupDesign.v1), 0, 0, 170, 54, Plane.WorldXY,
                false);
            var screwAideManager = new ScrewAideManager(screw, director.ScrewDatabase);
            var medialBump = screwAideManager.GetMedialBumpMesh();

            //Act
            var creator = new ScrewMedialBumpCreator(director.ScrewDatabase, cup);
            var hasMedialBump = creator.ScrewShouldHaveMedialBump(screw);
            var bump = creator.CreateMedialBumpForScrewWithMedialBump(screw);
            //AddToDocumentForV1Cup(director.document, screw, cup, bump);

            //Assert
            var shouldHaveMedialBump = hasMedialBump;
            var matching = (medialBump.Vertices.Count == bump.Vertices.Count) && (medialBump.Faces.Count == bump.Faces.Count);
            return matching && shouldHaveMedialBump;
        }

        private static bool Creator_Returns_MedialBump_When_Intersect_And_BelowHorizontalBorder_For_V1Cup(ImplantDirector director, ScrewPosition screwPosition)
        {
            //Arrange
            var screw = new ScrewMock(director, new Point3d(0, 27, 15), new Point3d(0, 27, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1, screwPosition);
            var cup = new Cup(Point3d.Origin, new CupType(4, 1, CupDesign.v1), 0, 0, 170, 54, Plane.WorldXY,
                false);
            var screwAideManager = new ScrewAideManager(screw, director.ScrewDatabase);
            var medialBump = screwAideManager.GetMedialBumpMesh();

            //Act
            var creator = new ScrewMedialBumpCreator(director.ScrewDatabase, cup);
            var hasMedialBump = creator.ScrewShouldHaveMedialBump(screw);
            var bump = creator.CreateMedialBumpForScrewWithMedialBump(screw);
            //AddToDocumentForV1Cup(director.document, screw, cup, bump);

            //Assert
            var shouldHaveMedialBump = hasMedialBump;
            var matching = (medialBump.Vertices.Count == bump.Vertices.Count) && (medialBump.Faces.Count == bump.Faces.Count);
            return matching && shouldHaveMedialBump;
        }

        private static bool Creator_Returns_HorizontalBorderBump_When_Intersect_And_AboveHorizontalBorder_For_V1Cup(ImplantDirector director, ScrewPosition screwPosition)
        {
            //Arrange
            var screw = new ScrewMock(director, new Point3d(0, 27, -15), new Point3d(0, 27, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1, screwPosition);
            var cup = new Cup(Point3d.Origin, new CupType(4, 1, CupDesign.v1), 0, 0, 170, 54, Plane.WorldXY,
                false);
            var screwAideManager = new ScrewAideManager(screw, director.ScrewDatabase);
            var horizontalBorderBump = screwAideManager.GetHorizontalBorderBumpMesh();

            //Act
            var creator = new ScrewMedialBumpCreator(director.ScrewDatabase, cup);
            var hasMedialBump = creator.ScrewShouldHaveMedialBump(screw);
            var bump = creator.CreateMedialBumpForScrewWithMedialBump(screw);
            //AddToDocumentForV1Cup(director.document, screw, cup, bump);

            //Assert
            var shouldHaveMedialBump = hasMedialBump;
            var matching = (horizontalBorderBump.Vertices.Count == bump.Vertices.Count) && (horizontalBorderBump.Faces.Count == bump.Faces.Count);
            return matching && shouldHaveMedialBump;
        }

        private static bool Creator_Returns_MedialBump_When_NoIntersect_For_V2Cup(ImplantDirector director, ScrewPosition screwPosition)
        {
            //Arrange
            var screw = new ScrewMock(director, new Point3d(0, 0, 0), new Point3d(0, 0, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1, screwPosition);
            var cup = new Cup(Point3d.Origin, new CupType(4, 1, CupDesign.v2), 0, 0, 170, 54, Plane.WorldXY,
                false);
            var screwAideManager = new ScrewAideManager(screw, director.ScrewDatabase);
            var medialBump = screwAideManager.GetMedialBumpMesh();

            //Act
            var creator = new ScrewMedialBumpCreator(director.ScrewDatabase, cup);
            var hasMedialBump = creator.ScrewShouldHaveMedialBump(screw);
            var bump = creator.CreateMedialBumpForScrewWithMedialBump(screw);
            //AddToDocumentForV2Cup(director.document, screw, cup, bump);

            //Assert
            var shouldHaveMedialBump = hasMedialBump;
            var matching = (medialBump.Vertices.Count == bump.Vertices.Count) && (medialBump.Faces.Count == bump.Faces.Count);
            return matching && shouldHaveMedialBump;
        }

        private static bool Creator_Returns_HorizontalBorderBump_When_Intersect_For_V2Cup(ImplantDirector director, ScrewPosition screwPosition)
        {
            //Arrange
            var screw = new ScrewMock(director, new Point3d(0, 27, 0), new Point3d(0, 27, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1, screwPosition);
            var cup = new Cup(Point3d.Origin, new CupType(4, 1, CupDesign.v2), 0, 0, 170, 54, Plane.WorldXY,
                false);
            var screwAideManager = new ScrewAideManager(screw, director.ScrewDatabase);
            var horizontalBorderBump = screwAideManager.GetHorizontalBorderBumpMesh();

            //Act
            var creator = new ScrewMedialBumpCreator(director.ScrewDatabase, cup);
            var hasMedialBump = creator.ScrewShouldHaveMedialBump(screw);
            var bump = creator.CreateMedialBumpForScrewWithMedialBump(screw);
            //AddToDocumentForV2Cup(director.document, screw, cup, bump);

            //Assert
            var shouldHaveMedialBump = hasMedialBump;
            var matching = (horizontalBorderBump.Vertices.Count == bump.Vertices.Count) && (horizontalBorderBump.Faces.Count == bump.Faces.Count);
            return matching && shouldHaveMedialBump;
        }

        private static bool Creator_Returns_MedialBump_When_Intersect_And_AboveHorizontalBorder_For_V1Cup(ImplantDirector director, ScrewPosition screwPosition)
        {
            //Arrange
            var screw = new ScrewMock(director, new Point3d(0, 27, -15), new Point3d(0, 27, 100), ScrewType.AO_D65,
                ScrewAlignment.Sunk, 1, screwPosition);
            var cup = new Cup(Point3d.Origin, new CupType(4, 1, CupDesign.v1), 0, 0, 170, 54, Plane.WorldXY,
                false);
            var screwAideManager = new ScrewAideManager(screw, director.ScrewDatabase);
            var medialBump = screwAideManager.GetMedialBumpMesh();

            //Act
            var creator = new ScrewMedialBumpCreator(director.ScrewDatabase, cup);
            var hasMedialBump = creator.ScrewShouldHaveMedialBump(screw);
            var bump = creator.CreateMedialBumpForScrewWithMedialBump(screw);
            //AddToDocumentForV1Cup(director.document, screw, cup, bump);

            //Assert
            var shouldHaveMedialBump = hasMedialBump;
            var matching = (medialBump.Vertices.Count == bump.Vertices.Count) && (medialBump.Faces.Count == bump.Faces.Count);
            return matching && shouldHaveMedialBump;
        }

        private static bool Creator_Returns_NoMedialBump_When_Screw_Is_ScrewAlignmentFloating_And_ScrewPostionFlange(ImplantDirector director)
        {
            //Arrange
            var screw = new ScrewMock(director, new Point3d(0, 0, 15), new Point3d(0, 0, 100), ScrewType.AO_D65,
                ScrewAlignment.Floating, 1, ScrewPosition.Flange);
            var cup = new Cup(Point3d.Origin, new CupType(4, 1, CupDesign.v1), 0, 0, 170, 54, Plane.WorldXY,
                false);

            //Act
            var creator = new ScrewMedialBumpCreator(director.ScrewDatabase, cup);
            var hasMedialBump = creator.ScrewShouldHaveMedialBump(screw);
            var bump = creator.CreateMedialBumpForScrewWithMedialBump(screw);

            //Assert
            var shouldNotHaveMedialBump = !hasMedialBump;
            var matching = (bump == null);
            return matching && shouldNotHaveMedialBump;
        }

        private static void AddToDocumentForV1Cup(RhinoDoc doc, Screw screw, Cup cup, Mesh bump)
        {
            var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);
            var medialBump = screwAideManager.GetMedialBumpMesh();
            var trimmerCup = cup.innerReamingVolumeMesh;

            var cupRimCenter = cup.cupRimCenter;
            var cupDir = cup.orientation;

            doc.Objects.AddRhinoObject(screw);
            doc.Objects.AddMesh(medialBump);
            doc.Objects.AddMesh(trimmerCup);
            doc.Objects.AddMesh(bump);
            doc.Objects.AddPoint(cupRimCenter);
            doc.Objects.AddLine(new Line(cupRimCenter, Point3d.Add(cupRimCenter, Vector3d.Multiply(cupDir, 100))));
        }

        private static void AddToDocumentForV2Cup(RhinoDoc doc, Screw screw, Cup cup, Mesh bump)
        {
            var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);
            var screwHole = screwAideManager.GetSubtractorMesh();
            var cupRing = cup.GetCupRing();

            doc.Objects.AddRhinoObject(screw);
            doc.Objects.AddMesh(screwHole);
            doc.Objects.AddBrep(cupRing);
            doc.Objects.AddMesh(bump);
        }
    }
}
