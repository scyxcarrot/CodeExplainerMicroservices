#if DEBUG

using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Visualization;
using IDS.Core.PluginHelper;
using IDS.Operations.CupPositioning;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System.Linq;

namespace IDS.Commands.Hidden
{
    [System.Runtime.InteropServices.Guid("DB7D0161-93F9-4833-8AE0-B1882F49C5A7")]
    [CommandStyle(Style.ScriptRunner)]
    public class GuideCreatorTest : Command
    {
        public GuideCreatorTest()
        {
            Instance = this;
        }

        public static GuideCreatorTest Instance { get; private set; }

        public override string EnglishName => "GuideCreatorTest";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //TestCupEntitiesOnExistingProject();
            //TestScrewEntitiesOnExistingProject();
            //TestCupEntitiesWithoutProject();
            //TestScrewEntities();
            //TestCupEntityCurves();
            //TestScrewHolePlugs();
            TestExportAllEntities();
            //TestScrewEntitiesCurves();

            return Result.Success;
        }

        private static void TestScrewHolePlugs()
        {
            var screwManager = new ScrewManager(RhinoDoc.ActiveDoc);
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);
            var objectManager = new AmaceObjectManager(director);
            var flanges = objectManager.GetBuildingBlock(IBB.PlateFlat).Geometry as Mesh;
            var guideExporter = new GuideCreator(director.cup, screwManager.GetAllScrews().ToList());
            var screwHolePlugs = guideExporter.GetScrewHolePlugs(flanges);

            RhinoDoc.ActiveDoc.Objects.AddMesh(screwHolePlugs);
            RhinoDoc.ActiveDoc.Views.Redraw();

        }

        private static void TestExportAllEntities()
        {
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);
            var screwManager = new ScrewManager(director.Document);
            var objectManager = new AmaceObjectManager(director);
            var guideExporter = new GuideCreator(director.cup, screwManager.GetAllScrews().ToList());

            guideExporter.ExportAllGuideEntities(
                objectManager.GetBuildingBlock(IBB.PlateFlat).Geometry as Mesh, director.DrillBitRadius,
                System.IO.Path.GetDirectoryName(director.Document.Path), director.Inspector.CaseId, 
                director.version, director.draft);
        }

        private static void TestCupEntitiesWithoutProject()
        {
            const double aperture = 170;
            const double anteversion = 0;
            const double inclination = 180;
            const double diameter = 54;
            var cupType = new CupType(4, 1, CupDesign.v2);

            var cup = new Cup(Point3d.Origin, cupType, anteversion, inclination, aperture, diameter, Plane.WorldZX,
                false);

            RhinoDoc.ActiveDoc.Objects.AddRhinoObject(cup);
            TestCupEntities(cup);
        }

        private static void TestCupEntitiesOnExistingProject()
        {
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);
            var cup = director.cup;

            TestCupEntities(cup);
        }

        private static void TestCupEntities(Cup cup)
        {
            var screwManager = new ScrewManager(RhinoDoc.ActiveDoc);
            Mesh studs;
            StudMaker.GenerateStuds(cup, new Mesh(), StudMaker.GetAmaceStudParams(), out studs);
            var guideExporter = new GuideCreator(cup, screwManager.GetAllScrews().ToList());

            //var cupOffset = guideExporter.GetOffsetCupWithStuds();
            //RhinoDoc.ActiveDoc.Objects.AddMesh(cupOffset);

            //var fenestrationEntity = guideExporter.GetFenestrationEntity();
            //foreach (var fenestration in fenestrationEntity)
            //    RhinoDoc.ActiveDoc.Objects.AddBrep(fenestration);

            //RhinoDoc.ActiveDoc.Objects.AddBrep(guideExporter.GetGuideRing());
            //RhinoDoc.ActiveDoc.Objects.AddMesh(guideExporter.GetLiftTab());           
            RhinoDoc.ActiveDoc.Objects.AddMesh(guideExporter.GetGuideCup(true));
            RhinoDoc.ActiveDoc.Views.Redraw();

            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private static void TestCupEntityCurves()
        {
            const double aperture = 170;
            const double anteversion = 0;
            const double inclination = 180;
            const double diameter = 54;
            var cupType = new CupType(4, 1, CupDesign.v2);

            var cup = new Cup(Point3d.Origin, cupType, anteversion, inclination, aperture, diameter, Plane.WorldZX,
                false);

            var guideExporter = new GuideCreator(cup, null);

            var ringCurves = guideExporter.GetGuideRingCurves();
            foreach (var ringCurve in ringCurves)
            {
                RhinoDoc.ActiveDoc.Objects.AddCurve(ringCurve);
            }

            var liftTabCupCurves = guideExporter.GetLiftTabCurves(5,4.1);
            foreach (var liftTabCupCurve in liftTabCupCurves)
            {
                RhinoDoc.ActiveDoc.Objects.AddCurve(liftTabCupCurve);
            }

            var liftTabFlangeCurves = guideExporter.GetLiftTabCurves(7, 6.1);
            foreach (var liftTabFlangeCurve in liftTabFlangeCurves)
            {
                RhinoDoc.ActiveDoc.Objects.AddCurve(liftTabFlangeCurve);
            }

            var cupShapeCurves = guideExporter.GetGuideCupShapeCurves();
            foreach (var cupShapeCurve in cupShapeCurves)
            {
                RhinoDoc.ActiveDoc.Objects.AddCurve(cupShapeCurve);
            }
                
            const double horizontalBorderWidth = 2;
            var translation = Transform.Translation(0, -diameter / 2, 0);
            var metalAttributs = new ObjectAttributes {ObjectColor = Colors.MetalCup, ColorSource = ObjectColorSource.ColorFromObject};
            var porousAttributs = new ObjectAttributes { ObjectColor = Colors.PorousOrange, ColorSource = ObjectColorSource.ColorFromObject };

            foreach (var curve in Cup.GetRingDesignCupCurves(aperture, diameter, cupType.CupThickness,
                horizontalBorderWidth, Cup.GetPolishingOffsetValue(cupType.CupDesign)))
            {
                var alignedCurve = curve;
                alignedCurve.Transform(translation);
                RhinoDoc.ActiveDoc.Objects.AddCurve(alignedCurve, metalAttributs);
            }

            foreach (var curve in cup.GetRingDesignPorousShellCurves())
            {
                var alignedCurve = curve;
                alignedCurve.Transform(translation);
                RhinoDoc.ActiveDoc.Objects.AddCurve(alignedCurve, porousAttributs);
            }
                

            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private static void TestScrewEntitiesCurves()
        {
            var screwManager = new ScrewManager(RhinoDoc.ActiveDoc);
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);
            var guideExporter = new GuideCreator(null, screwManager.GetAllScrews().ToList());

            //foreach (var flangeCylinderUpper in guideExporter.GetUpperFlangeCylinders())
            //    RhinoDoc.ActiveDoc.Objects.AddBrep(flangeCylinderUpper);
            //foreach (var flangeCylinderLower in guideExporter.GetLowerFlangeCylinders())
            //    RhinoDoc.ActiveDoc.Objects.AddBrep(flangeCylinderLower);
            //foreach (var cupCylinder in guideExporter.GetCupCylinders())
            //    RhinoDoc.ActiveDoc.Objects.AddBrep(cupCylinder);
            //foreach (var snapFit in guideExporter.GetSnapFits())
            //    RhinoDoc.ActiveDoc.Objects.AddMesh(snapFit);
            //foreach (var snapFitSubtractor in guideExporter.GetSnapFitSubtractors())
            //    RhinoDoc.ActiveDoc.Objects.AddBrep(snapFitSubtractor);

            foreach (var guideHoleBoolean in guideExporter.GetGuideHoleBooleans(ScrewPosition.Cup, director.DrillBitRadius))
                RhinoDoc.ActiveDoc.Objects.AddBrep(guideHoleBoolean.Value);
            foreach (var guideHoleBoolean in guideExporter.GetGuideHoleBooleans(ScrewPosition.Flange, director.DrillBitRadius))
                RhinoDoc.ActiveDoc.Objects.AddBrep(guideHoleBoolean.Value);

            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private static void TestScrewEntities()
        {
            var resources = new AmaceResources();
            var screwDatabase = File3dm.Read(resources.ScrewDatabasePath);

            var screwBrandType = new ScrewBrandType("AO", 6.5, ScrewLocking.None);
            var sphereCenter = ScrewAideManager.GetHeadCenterInDatabase(screwDatabase, screwBrandType);
            const double topCylinderRadius = 6.0;
            const double topCylinderHeight = 60.0;
            const double bottomCylinderHeightFromSphereContour = 12.0;
            var sphereRadius = 6.0;
            var guideHoleBooleanCurves = ScrewAideManager.GetGuideHoleBooleanCurves(sphereCenter, sphereRadius,
                ImplantDirector.DefaultDrillBitRadius, topCylinderRadius, topCylinderHeight, bottomCylinderHeightFromSphereContour);

            foreach (var guideHoleBoolean in guideHoleBooleanCurves)
                RhinoDoc.ActiveDoc.Objects.AddCurve(guideHoleBoolean);

            RhinoDoc.ActiveDoc.Views.Redraw();
        }
    }
}

#endif