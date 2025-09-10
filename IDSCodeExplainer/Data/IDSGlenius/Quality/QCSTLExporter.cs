using IDS.Core.Enumerators;
using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Query;
using IDS.Glenius.Visualization;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace IDS.Glenius.Quality
{
    //TODO: Class is too big, this is a tech debt, let's make base class and child for QC Screw, QC Scaffold, QC Approved in refactoring sprint
    //TODO: QCSTLExporter is not true, it also uses StpCreator that exports the brep.
    public class QCSTLExporter : QCFileExporter
    {
        private readonly GleniusImplantDirector _director;
        private readonly GleniusObjectManager _objectManager;
        private readonly ImplantDerivedEntities _implantDerivedEntities;
        private readonly ObjectExporter _exporter;
        private readonly ImplantFileNameGenerator _generator;
        public bool UseProductionRodWithChamfer { get; set; }

        public QCSTLExporter(GleniusImplantDirector director)
        {
            this._director = director;
            this._objectManager = new GleniusObjectManager(director);
            _implantDerivedEntities = new ImplantDerivedEntities(director);
            _exporter = new ObjectExporter(director.Document);
            _generator = new ImplantFileNameGenerator(director);
            UseProductionRodWithChamfer = false;
        }

        public override int DoExport(DocumentType documentType, string outputDirectory, out List<string> failedItems)
        {
            _exporter.ExportDirectory = outputDirectory;

            var totalItems = 0;
            failedItems = new List<string>();

            switch (documentType)
            {
                case DocumentType.ScaffoldQC:
                    totalItems = ExportSTLsForScaffoldQC(ref failedItems);
                    break;
                case DocumentType.ScrewQC:
                    totalItems = ExportSTLsForScrewQC(ref failedItems);
                    break;
                case DocumentType.ApprovedQC:
                    _exporter.ExportDirectory = Path.Combine(outputDirectory, "Guide");
                    totalItems = ExportSTLsForApprovedQCGuide(ref failedItems);
                    _exporter.ExportDirectory = Path.Combine(outputDirectory, "Plastic_Models");
                    totalItems += ExportSTLsForApprovedQCPlasticModels(ref failedItems);
                    _exporter.ExportDirectory = Path.Combine(outputDirectory, "Finalization");
                    totalItems += ExportSTLsForApprovedQCFinalization(ref failedItems);
                    _exporter.ExportDirectory = Path.Combine(outputDirectory, "Reporting");
                    totalItems += ExportSTLsForApprovedQCReporting(ref failedItems);
                    break;
            }

            return totalItems;
        }

        public int DoQCApprovedExportForReporting(string outputDirectory, out List<string> failedItems)
        {
            _exporter.ExportDirectory = outputDirectory;

            var totalItems = 0;
            failedItems = new List<string>();

            _exporter.ExportDirectory = Path.Combine(outputDirectory, "Reporting");
            totalItems += ExportSTLsForApprovedQCReporting(ref failedItems);

            return totalItems;
        }

        public int DoQCApprovedExportForGuide(string outputDirectory, out List<string> failedItems)
        {
            _exporter.ExportDirectory = outputDirectory;

            var totalItems = 0;
            failedItems = new List<string>();

            _exporter.ExportDirectory = Path.Combine(outputDirectory, "Guide");
            totalItems += ExportSTLsForApprovedQCGuide(ref failedItems);

            return totalItems;
        }

        private int ExportSTLsForScrewQC(ref List<string> failedItems)
        {
            var totalItems = 0;

            var blocks = new List<IBB>
            {
                IBB.Scapula,
                IBB.ProductionRod, //Does not need to have chamfer so export it right away
                IBB.ReconstructedScapulaBone,
                IBB.ScapulaDefectRegionRemoved,
                IBB.CylinderHat,
                IBB.Head,
                IBB.RBVHead,
                IBB.ScapulaReamed,
                IBB.TaperMantleSafetyZone,
                IBB.M4ConnectionSafetyZone,
                IBB.M4ConnectionScrew,
            };
            ExportBuildingBlocks(blocks, ref totalItems, ref failedItems);

            ExportFullSphere(ref totalItems, ref failedItems);

            ExportScrewsAndComponents(ref totalItems, ref failedItems);

            ExportScapulaReamedOffset(ref totalItems, ref failedItems);

            return totalItems;
        }

        private int ExportSTLsForScaffoldQC(ref List<string> failedItems)
        {
            var totalItems = 0;

            var blocks = new List<IBB>
            {
                IBB.Scapula,
                IBB.ProductionRod, //Does not need to have chamfer so export it right away
                IBB.ReconstructedScapulaBone,
                IBB.ScapulaDefectRegionRemoved,
                IBB.CylinderHat,
                IBB.Head,
                IBB.RBVHead,
                IBB.ScapulaReamed,
                IBB.TaperMantleSafetyZone,
                IBB.M4ConnectionSafetyZone,
                IBB.M4ConnectionScrew,
                IBB.RbvScaffold
            };
            ExportBuildingBlocks(blocks, ref totalItems, ref failedItems);

            ExportFullSphere(ref totalItems, ref failedItems);

            ExportScrewsAndComponents(ref totalItems, ref failedItems);

            ExportScapulaReamedOffset(ref totalItems, ref failedItems);

            ExportScaffoldShape(ref totalItems, ref failedItems);

            ExportPlatePreview(ref totalItems, ref failedItems);

            ExportExtrusion(ref totalItems, ref failedItems);

            return totalItems;
        }

        private void ExportBuildingBlocks(List<IBB> blocks, ref int totalItems, ref List<string> failedItems)
        {
            foreach (var block in blocks)
            {
                var rhinoObjects = _objectManager.GetAllBuildingBlocks(block);
                var resultMesh = new Mesh();
                var buildingBlock = BuildingBlocks.Blocks[block];

                if (rhinoObjects == null || !rhinoObjects.Any())
                {
                    continue;
                }

                totalItems += 1;
                foreach (var rhinoObject in rhinoObjects)
                {
                    Mesh currMesh = null;
                    if (buildingBlock.GeometryType == ObjectType.Mesh)
                    {
                        currMesh = rhinoObject.Geometry as Mesh;
                    }
                    else if (buildingBlock.GeometryType == ObjectType.Brep)
                    {
                        currMesh = MeshUtilities.ConvertBrepToMesh(rhinoObject.Geometry as Brep, true);
                    }
                    else
                    {
                        LogError(buildingBlock.ExportName, ref failedItems);
                    }

                    if (currMesh != null)
                    {
                        resultMesh.Append(currMesh);
                    }
                }

                ExportSTL(resultMesh, buildingBlock.ExportName, BuildingBlocks.Blocks[block].Color, ref failedItems);
            }
        }

        private void ExportScrewsAndComponents(ref int totalItems, ref List<string> failedItems)
        {
            var screwRhinoObjects = _objectManager.GetAllBuildingBlocks(IBB.Screw);
            if (screwRhinoObjects != null)
            {
                var screwExportName = BuildingBlocks.Blocks[IBB.Screw].ExportName;
                var safetyZoneExportName = BuildingBlocks.Blocks[IBB.ScrewSafetyZone].ExportName;
                var screwHolesProduction = new List<Brep>();
                var screwHolesScaffold = new List<Brep>();
                var screwHolesScaffoldWith1dot0Offset = new List<Brep>();
                var screwHolesScaffoldWith2dot5Offset = new List<Brep>();
                foreach (var screwRhinoObject in screwRhinoObjects)
                {
                    var screw = screwRhinoObject as Screw;
                    if (screw == null)
                    {
                        continue;
                    }

                    totalItems += 1;
                    var screwWithIndex = string.Format(screwExportName, screw.Index);
                    ExportBrepAsSTL(screw.Geometry as Brep, screwWithIndex, BuildingBlocks.Blocks[IBB.Screw].Color, ref failedItems);

                    var safetyZoneId = screw.ScrewAides[ScrewAideType.SafetyZone];
                    var safetyZoneRhinoObject = _director.Document.Objects.Find(safetyZoneId);
                    totalItems += 1;
                    var safetyZoneWithIndex = string.Format(safetyZoneExportName, screw.Index);
                    ExportBrepAsSTL(safetyZoneRhinoObject.Geometry as Brep, safetyZoneWithIndex, BuildingBlocks.Blocks[IBB.ScrewSafetyZone].Color, ref failedItems);

                    screwHolesProduction.Add(_implantDerivedEntities.GetScrewHoleProduction(screw));

                    var screwHoleScaffold = _implantDerivedEntities.GetScrewHoleScaffold(screw);
                    screwHolesScaffold.Add(screwHoleScaffold);

                    var plane = new Plane(screw.HeadPoint, screw.Direction);
                    var cylinderWith1dot0Offset = CylinderUtilities.CreateCylinderFromBoundingBox(screwHoleScaffold.GetBoundingBox(true), plane, screw.HeadPoint, screw.TotalLength, 1.0);
                    screwHolesScaffoldWith1dot0Offset.Add(Brep.CreateFromCylinder(cylinderWith1dot0Offset, true, true));

                    var cylinderWith2dot5Offset = CylinderUtilities.CreateCylinderFromBoundingBox(screwHoleScaffold.GetBoundingBox(true), plane, screw.HeadPoint, screw.TotalLength, 2.5);
                    screwHolesScaffoldWith2dot5Offset.Add(Brep.CreateFromCylinder(cylinderWith2dot5Offset, true, true));
                }

                totalItems += 1;
                var screwDrillGuideCylinders = _objectManager.GetAllBuildingBlocks(IBB.ScrewDrillGuideCylinder).Select(rhinoObject => rhinoObject.Geometry as Brep).ToList();
                var screwDrillGuideCylinderBlock = BuildingBlocks.Blocks[IBB.ScrewDrillGuideCylinder];
                ExportBrepsAsSTL(screwDrillGuideCylinders, screwDrillGuideCylinderBlock.ExportName, screwDrillGuideCylinderBlock.Color, ref failedItems);

                totalItems += 1;
                var screwHolesProductionName = "Merged_implant_production_screwholes";
                ExportBrepsAsSTL(screwHolesProduction, screwHolesProductionName, Colors.GeneralGrey, ref failedItems);

                totalItems += 1;
                var screwHolesScaffoldName = "Merged_scaffold_screwholes";
                ExportBrepsAsSTL(screwHolesScaffold, screwHolesScaffoldName, Colors.GeneralGrey, ref failedItems);

                totalItems += 1;
                var screwHolesScaffoldWith1dot0OffsetName = "Screw_Holes_Scaffold_1dot0offset";
                ExportBrepsAsSTL(screwHolesScaffoldWith1dot0Offset, screwHolesScaffoldWith1dot0OffsetName, Colors.GeneralGrey, ref failedItems);

                totalItems += 1;
                var screwHolesScaffoldWith2dot5OffsetName = "Screw_Holes_Scaffold_2dot5offset";
                ExportBrepsAsSTL(screwHolesScaffoldWith2dot5Offset, screwHolesScaffoldWith2dot5OffsetName, Colors.GeneralGrey, ref failedItems);
            }

            ExportMergedImplantRealScrewHoles(ref totalItems, ref failedItems);

            ExportScrewMantles(ref totalItems, ref failedItems);
        }

        private void ExportMergedImplantRealScrewHoles(ref int totalItems, ref List<string> failedItems)
        {
            var screwRhinoObjects = _objectManager.GetAllBuildingBlocks(IBB.Screw);
            if (screwRhinoObjects == null)
            {
                return;
            }

            var screwHolesReal = new List<Brep>();
            foreach (var screwRhinoObject in screwRhinoObjects)
            {
                var screw = screwRhinoObject as Screw;
                if (screw != null)
                {
                    screwHolesReal.Add(_implantDerivedEntities.GetScrewHoleReal(screw));
                }
            }

            totalItems += 1;
            var screwHolesRealName = "Merged_implant_real_screwholes";
            ExportBrepsAsSTL(screwHolesReal, screwHolesRealName, Colors.GeneralGrey, ref failedItems);
        }

        private void ExportScrewMantles(ref int totalItems, ref List<string> failedItems)
        {
            var screwRhinoObjects = _objectManager.GetAllBuildingBlocks(IBB.Screw);
            if (screwRhinoObjects != null)
            {
                totalItems += 1;
                var screwMantles = _objectManager.GetAllBuildingBlocks(IBB.ScrewMantle).Select(rhinoObject => rhinoObject.Geometry as Brep).ToList();
                var screwMantleblock = BuildingBlocks.Blocks[IBB.ScrewMantle];
                ExportBrepsAsSTL(screwMantles, screwMantleblock.ExportName, screwMantleblock.Color, ref failedItems);
            }
        }

        private void ExportFullSphere(ref int totalItems, ref List<string> failedItems)
        {
            var rhinoObject = _objectManager.GetBuildingBlock(IBB.Head);
            if (rhinoObject == null)
            {
                return;
            }

            var head = rhinoObject as Head;
            if (head == null)
            {
                return;
            }
            totalItems += 1;

            var sphere = new Sphere(head.CoordinateSystem.Origin, HeadQueries.GetHeadDiameter(head.HeadType) / 2);
            var sphereBrep = sphere.ToBrep();
            ExportBrepAsSTL(sphereBrep, "FullSphere", Color.White, ref failedItems);
        }

        private void ExportScapulaReamedOffset(ref int totalItems, ref List<string> failedItems)
        {
            totalItems += 1;
            var bone = _implantDerivedEntities.GetScapulaReamedWithWrap();
            ExportSTL(bone, "Scapula_Reamed_0dot30offset", Colors.ScapulaReamedOffset, ref failedItems);
        }

        private void ExportScaffoldShape(ref int totalItems, ref List<string> failedItems)
        {
            totalItems += 1;
            var scaffold = _implantDerivedEntities.GetScaffoldShape();
            ExportSTL(scaffold, "Scaffold_Shape", Colors.Scaffold, ref failedItems);
        }

        private void ExportPlatePreview(ref int totalItems, ref List<string> failedItems)
        {
            totalItems += 1;
            var entityName = "Plate_Preview";
            var solidPartCreator = new SolidPartCreator(_director, UseProductionRodWithChamfer);
            Mesh plate;
            if (solidPartCreator.GetPlatePreview(out plate))
            {
                ExportSTL(plate, entityName, Colors.Metal, ref failedItems);
            }
            else
            {
                LogError(entityName, ref failedItems);
            }
        }

        private void ExportExtrusion(ref int totalItems, ref List<string> failedItems)
        {
            var offsetDistance = 1.5;
            var topContour = _objectManager.GetBuildingBlock(IBB.BasePlateTopContour).Geometry.Duplicate() as Curve;
            var extrusion = CreateExtrusion(topContour, offsetDistance, false);

            totalItems += 1;
            ExportBrepAsSTL(extrusion, "Extrusion", Colors.Metal, ref failedItems);
        }

        private void ExportGuideExtrusion(ref int totalItems, ref List<string> failedItems)
        {
            var offsetDistance = 9.0;
            var topContour = _objectManager.GetBuildingBlock(IBB.BasePlateTopContour).Geometry.Duplicate() as Curve;
            var guideExtrusion = CreateExtrusion(topContour, offsetDistance, true);

            totalItems += 1;
            ExportBrepAsSTL(guideExtrusion, "Guide_Extrusion", Colors.Metal, ref failedItems);
        }

        private Brep CreateExtrusion(Curve surfaceCurve, double inputOffsetDistance, bool towardsLateral)
        {
            Plane headCoordinateSystem;
            if (!_objectManager.GetBuildingBlockCoordinateSystem(IBB.Head, out headCoordinateSystem))
            {
                return null;
            }

            var offsetDistance = inputOffsetDistance;
            var brepFace = Brep.CreatePlanarBreps(surfaceCurve)[0].Faces[0];
            var extrusionDirection = towardsLateral ? headCoordinateSystem.ZAxis : -headCoordinateSystem.ZAxis;
            if (BrepUtilities.IsBrepFaceParallelTo(brepFace, headCoordinateSystem.Origin, extrusionDirection))
            {
                offsetDistance = -offsetDistance;
            }
            var extrusion = Brep.CreateFromOffsetFace(brepFace, offsetDistance, 0.001, false, true);
            if (extrusion.SolidOrientation == BrepSolidOrientation.Inward)
            {
                extrusion.Flip();
            }
            return extrusion;
        }

        private void ExportBrepAsSTL(Brep brep, string entityName, Color color, ref List<string> failedItems)
        {
            if (brep == null)
            {
                return;
            }

            var fileName = _generator.GenerateFileName(entityName);
            if (!_exporter.ExportStlWithColor(MeshUtilities.ConvertBrepToMesh(brep, true), fileName, color))
            {
                LogError(entityName, ref failedItems);
            }
        }

        private void ExportBrepsAsSTL(List<Brep> breps, string entityName, Color color, ref List<string> failedItems)
        {
            if (breps.Count <= 0)
            {
                return;
            }

            var component = new Brep();
            breps.ForEach(brep => component.Append(brep));
            ExportBrepAsSTL(component, entityName, color, ref failedItems);
        }

        private void ExportSTL(Mesh mesh, string entityName, Color color, ref List<string> failedItems)
        {
            if (mesh == null)
            {
                return;
            }

            var fileName = _generator.GenerateFileName(entityName);
            if (!_exporter.ExportStlWithColor(mesh, fileName, color))
            {
                LogError(entityName, ref failedItems);
            }
        }

        private void ExportBreps(List<Brep> breps, string entityName, bool addMeta, ref List<string> failedItems)
        {
            if (breps.Count <= 0)
            {
                return;
            }

            var combined = new Brep();
            breps.ForEach(x => combined.Append(x));

            var fileName = addMeta ? _generator.GenerateFileName(entityName) : entityName;

            if (!_exporter.ExportStp(combined, fileName))
            {
                LogError(entityName, ref failedItems);
            }
        }

        //QCApprovedExport, Guide //TODO: Dupe some codes so it is easier to do class separation later
        //////////////////////////////////////////////////////////////////////////

        private int ExportSTLsForApprovedQCGuide(ref List<string> failedItems)
        {
            var totalItems = 0;

            var blocks = new List<IBB>
            {
                IBB.Scapula,
                IBB.ScapulaReamed,
                IBB.M4ConnectionScrew,

            };
            ExportBuildingBlocks(blocks, ref totalItems, ref failedItems);

            ExportScapulaReamedOffset(ref totalItems, ref failedItems);

            ExportGuideExtrusion(ref totalItems, ref failedItems);

            ExportScrewsAndComponentsApprovedQCGuide(ref totalItems, ref failedItems);

            ExportM4ScrewComponentsApprovedQCGuide(ref totalItems, ref failedItems);

            totalItems += 1;
            var guideHandle = _implantDerivedEntities.GetGuideHandle();
            ExportSTL(guideHandle, "Guide_Handle", Colors.Plastic, ref failedItems);

            return totalItems;
        }

        private void ExportM4ScrewComponentsApprovedQCGuide(ref int totalItems, ref List<string> failedItems)
        {
            totalItems += 1;
            var m4ConnectionHole = _implantDerivedEntities.GetM4ConnectionHoleGuide();
            ExportBrepAsSTL(m4ConnectionHole, "M4_Hole_Guide", Colors.GeneralGrey, ref failedItems);

            totalItems += 1;
            var m4ConnectionMantle = _implantDerivedEntities.GetM4ConnectionMantle();
            ExportBrepAsSTL(m4ConnectionMantle, "M4_Mantle", Colors.Plastic, ref failedItems);
        }

        private void ExportScrewsAndComponentsApprovedQCGuide(ref int totalItems, ref List<string> failedItems)
        {
            var screwRhinoObjects = _objectManager.GetAllBuildingBlocks(IBB.Screw);
            if (screwRhinoObjects != null)
            {
                var screwExportName = BuildingBlocks.Blocks[IBB.Screw].ExportName;
                foreach (var screwRhinoObject in screwRhinoObjects)
                {
                    var screw = screwRhinoObject as Screw;
                    if (screw == null)
                    {
                        continue;
                    }

                    totalItems += 1;
                    var screwWithIndex = string.Format(screwExportName, screw.Index);
                    ExportBrepAsSTL(screw.Geometry as Brep, screwWithIndex, BuildingBlocks.Blocks[IBB.Screw].Color, ref failedItems);
                }

                totalItems += 1;
                var screwDrillGuideCylinders = _objectManager.GetAllBuildingBlocks(IBB.ScrewDrillGuideCylinder).Select(rhinoObject => rhinoObject.Geometry as Brep).ToList();
                var screwDrillGuideCylinderBlock = BuildingBlocks.Blocks[IBB.ScrewDrillGuideCylinder];
                ExportBrepsAsSTL(screwDrillGuideCylinders, screwDrillGuideCylinderBlock.ExportName, screwDrillGuideCylinderBlock.Color, ref failedItems);
            }

            ExportScrewMantles(ref totalItems, ref failedItems);
        }

        //QCApprovedExport, Finalization //TODO: Dupe some codes so it is easier to do class separation later
        //////////////////////////////////////////////////////////////////////////

        private int ExportSTLsForApprovedQCFinalization(ref List<string> failedItems)
        {
            var totalItems = 0;

            var blocks = new List<IBB>
            {
                IBB.Scapula,
                IBB.ScapulaReamed,
                IBB.ConflictingEntities,
                IBB.NonConflictingEntities
            };
            ExportBuildingBlocks(blocks, ref totalItems, ref failedItems);

            ExportScapulaReamedOffset(ref totalItems, ref failedItems);

            ExportScaffoldShape(ref totalItems, ref failedItems);

            ExportScrewsAndComponentsApprovedQCFinalization(ref totalItems, ref failedItems);

            ExportProductionRodAndImplantsApprovedQCFinalization(ref totalItems, ref failedItems);

            ExportExtrusion(ref totalItems, ref failedItems);

            totalItems += 1;
            var unRoundedCylinder = _implantDerivedEntities.GetUnroundedCylinder();
            ExportBrepAsSTL(unRoundedCylinder, "Cylinder_Unrounded", Colors.GeneralGrey, ref failedItems);

            totalItems += 1;
            var taperHoleBoolean = _implantDerivedEntities.GetTaperBooleanReal();
            ExportBrepAsSTL(taperHoleBoolean, "TaperHoleBoolean", Colors.GeneralGrey, ref failedItems);

            totalItems += 1;
            var m4HoleBoolean = _implantDerivedEntities.GetM4ConnectionScrewHoleProduction();
            ExportBrepAsSTL(m4HoleBoolean, "M4_HoleBoolean", Colors.GeneralGrey, ref failedItems);

            totalItems += 1;
            var taperBooleanProduction = _implantDerivedEntities.GetTaperBooleanProduction();
            ExportBrepAsSTL(taperBooleanProduction, "TaperBooleanProduction", Colors.GeneralGrey, ref failedItems);

            return totalItems;
        }

        private void ExportScrewsAndComponentsApprovedQCFinalization(ref int totalItems, ref List<string> failedItems)
        {
            var screwRhinoObjects = _objectManager.GetAllBuildingBlocks(IBB.Screw);
            if (screwRhinoObjects != null)
            {
                var screwHolesProduction = new List<Brep>();
                var screwHolesProductionOffset = new List<Brep>();
                var screwHolesScaffold = new List<Brep>();

                foreach (var screwRhinoObject in screwRhinoObjects)
                {
                    var screw = screwRhinoObject as Screw;
                    if (screw == null)
                    {
                        continue;
                    }

                    screwHolesProduction.Add(_implantDerivedEntities.GetScrewHoleProduction(screw));
                    screwHolesProductionOffset.Add(_implantDerivedEntities.GetScrewHoleProductionOffset(screw));

                    var screwHoleScaffold = _implantDerivedEntities.GetScrewHoleScaffold(screw);
                    screwHolesScaffold.Add(screwHoleScaffold);
                }

                totalItems += 1;
                var screwHolesProductionName = "Merged_implant_production_screwholes";
                ExportBrepsAsSTL(screwHolesProduction, screwHolesProductionName, Colors.GeneralGrey, ref failedItems);

                totalItems += 1;
                var screwHolesProductionOffsetStlName = "Merged_implant_production_screwholes_offset";
                ExportBrepsAsSTL(screwHolesProductionOffset, screwHolesProductionOffsetStlName, Colors.GeneralGrey, ref failedItems);

                totalItems += 1;
                var screwHolesProductionOffsetStpName = "Screw_Holes_Production_Offset";
                ExportBreps(screwHolesProductionOffset, screwHolesProductionOffsetStpName, false, ref failedItems);

                totalItems += 1;
                var screwHolesScaffoldName = "Merged_scaffold_screwholes";
                ExportBrepsAsSTL(screwHolesScaffold, screwHolesScaffoldName, Colors.GeneralGrey, ref failedItems);

                ExportMergedImplantRealScrewHoles(ref totalItems, ref failedItems);
            }

            ExportScrewMantles(ref totalItems, ref failedItems);
        }

        private void ExportProductionRodAndImplantsApprovedQCFinalization(ref int totalItems, ref List<string> failedItems)
        {
            var prodRodCreator = new ProductionRodCreator(_director);
            var productionRod = prodRodCreator.CreateProductionRod(UseProductionRodWithChamfer);

            //Production Rod
            totalItems += 1;
            ExportBrepAsSTL(productionRod, "Production_Rod", BuildingBlocks.Blocks[IBB.ProductionRod].Color, ref failedItems);

            //STEP File Plate
            var stpCreator = new SolidPartStpCreator(_director, UseProductionRodWithChamfer, _exporter.ExportDirectory);

            totalItems += 1;
            Brep solidPartForProductioOffset;
            Dictionary<string, Brep> productionOffsetIntermediates;
            var solidPartForProductionOffsetSuccess =
                stpCreator.CreateForProductionOffset(out solidPartForProductioOffset, out productionOffsetIntermediates);
            ExportBrepWithCheck(solidPartForProductioOffset, _generator.GeneratePlateForProductionOffsetFileName(),
                solidPartForProductionOffsetSuccess, ref failedItems);
            ExportUtilities.ExportBreps(productionOffsetIntermediates, _exporter.ExportDirectory, _director.Document);

            totalItems += 1;
            Brep solidPartForProductioReal;
            Dictionary<string, Brep> productionRealIntermediates;
            var solidPartForProductionRealtSuccess =
                stpCreator.CreateForProductionReal(out solidPartForProductioReal, out productionRealIntermediates);
            ExportBrepWithCheck(solidPartForProductioReal, _generator.GeneratePlateForProductionFileName(),
                solidPartForProductionRealtSuccess, ref failedItems);
            ExportUtilities.ExportBreps(productionRealIntermediates, _exporter.ExportDirectory, _director.Document);

            //Create STL Plate
            var solidPartCreator = new SolidPartCreator(_director, UseProductionRodWithChamfer);

            totalItems += 1;
            Mesh solidPartForFinalization;
            var solidPartForFinalizationMeshSuccess =
                solidPartCreator.GetSolidPartForFinalization(out solidPartForFinalization);
            ExportSTLWithCheck(solidPartForFinalization, _generator.GeneratePlateForFinalizationFileName(),
                Colors.Metal, solidPartForFinalizationMeshSuccess, ref failedItems);

            totalItems += 1;
            Mesh solidPartForProductionOffset;
            var solidPartForProductionOffsetMeshSuccess =
                solidPartCreator.GetSolidPartForProductionOffset(out solidPartForProductionOffset);
            ExportSTLWithCheck(solidPartForProductionOffset, _generator.GeneratePlateForProductionOffsetFileName(),
                Colors.Metal, solidPartForProductionOffsetMeshSuccess, ref failedItems);

            //SolidPart
            totalItems += 1;
            var solidPart = solidPartCreator.CreateMergedSolidPartForReportingAndFinalization();
            ExportSTL(solidPart, "SolidParts", Colors.Metal, ref failedItems);

            //MacroShape
            totalItems += 1;
            var macroShape = solidPartCreator.GetMacroShape();
            ExportSTL(macroShape, "Macroshape", Colors.GeneralGrey, ref failedItems);

            //SolidPart_ForProductionOffset
            totalItems += 1;
            var solidPartProductionOffset = solidPartCreator.CreateMergedSolidPartForProductionOffset();
            ExportSTL(solidPartProductionOffset, "SolidPart_ForProductionOffset", Colors.GeneralGrey, ref failedItems);

            //Create Scaffold
            var scaffoldCreator = new ImplantScaffoldCreator(_director, solidPartCreator);

            totalItems += 1;
            Mesh scaffoldForFinalization;
            var scaffoldForFinalizationSuccess =
                scaffoldCreator.GetScaffoldForFinalization(out scaffoldForFinalization);
            ExportSTLWithCheck(scaffoldForFinalization, _generator.GenerateScaffoldForFinalizationFileName(),
                Colors.Scaffold, scaffoldForFinalizationSuccess, ref failedItems);
        }

        private void ExportBrepWithCheck(Brep brep, string fileName, bool success, ref List<string> failedItems)
        {
            if (success && brep != null)
            {
                _exporter.ExportStp(brep, fileName);
            }
            else
            {
                LogError(fileName, ref failedItems);
            }
        }

        private void ExportSTLWithCheck(Mesh mesh, string fileName, Color color, bool success, ref List<string> failedItems)
        {
            if (success && mesh != null)
            {
                _exporter.ExportStlWithColor(mesh, fileName, color);
            }
            else
            {
                LogError(fileName, ref failedItems);
            }
        }

        //QCApprovedExport, PlasticModels //TODO: Dupe some codes so it is easier to do class separation later
        //////////////////////////////////////////////////////////////////////////

        private int ExportSTLsForApprovedQCPlasticModels(ref List<string> failedItems)
        {
            var totalItems = 0;

            var blocks = new List<IBB>
            {
                IBB.Scapula,
                IBB.ScapulaReamed,
                IBB.TaperMantleSafetyZone,
                IBB.M4ConnectionScrew
            };

            ExportBuildingBlocks(blocks, ref totalItems, ref failedItems);

            ExportScapulaReamedOffset(ref totalItems, ref failedItems);

            totalItems += 1;
            var plasticHead = _implantDerivedEntities.GetPlasticHead();
            ExportSTL(plasticHead, "Head_Plastic", Colors.Plastic, ref failedItems);

            totalItems += 1;
            var plasticTaperBoolean = _implantDerivedEntities.GetTaperBooleanPlastic();
            ExportBrepAsSTL(plasticTaperBoolean, "Taper_Boolean_Plastic", Colors.Plastic, ref failedItems);

            ExportMergedImplantRealScrewHoles(ref totalItems, ref failedItems);

            return totalItems;
        }

        private int ExportSTLsForApprovedQCReporting(ref List<string> failedItems)
        {
            var totalItems = 0;

            var blocks = new List<IBB>
            {
                IBB.Scapula,
                IBB.Head,
                IBB.RBVHead,
                IBB.ScapulaReamed,
                IBB.M4ConnectionScrew,
                IBB.RbvScaffold,
                IBB.ConflictingEntities,
                IBB.NonConflictingEntities
            };
            ExportBuildingBlocks(blocks, ref totalItems, ref failedItems);

            ExportScrews(ref totalItems, ref failedItems);

            ExportPreOpComponents(ref totalItems, ref failedItems);

            totalItems += 1;
            var implantContactCreator = new QCImplantContactCreator(_director);
            var implantContact = implantContactCreator.CreateImplantContact();
            ExportSTLWithCheck(implantContact, _generator.GenerateFileName("Implant_Contact"), Colors.Scaffold, true, ref failedItems);

            ExportCOR(ref totalItems, ref failedItems);

            return totalItems;
        }

        private void ExportScrews(ref int totalItems, ref List<string> failedItems)
        {
            var screwRhinoObjects = _objectManager.GetAllBuildingBlocks(IBB.Screw);
            if (screwRhinoObjects == null)
            {
                return;
            }

            var screwExportName = BuildingBlocks.Blocks[IBB.Screw].ExportName;
            foreach (var screwRhinoObject in screwRhinoObjects)
            {
                var screw = screwRhinoObject as Screw;
                if (screw == null)
                {
                    continue;
                }

                totalItems += 1;
                var screwWithIndex = string.Format(screwExportName, screw.Index);
                ExportBrepAsSTL(screw.Geometry as Brep, screwWithIndex, BuildingBlocks.Blocks[IBB.Screw].Color, ref failedItems);
            }
        }

        private void ExportPreOpComponents(ref int totalItems, ref List<string> failedItems)
        {
            var preOpComponents = _director.BlockToKeywordMapping;

            foreach (var keyPairValue in preOpComponents)
            {
                var rhinoObject = _objectManager.GetBuildingBlock(keyPairValue.Key);
                if (rhinoObject == null)
                {
                    continue;
                }

                totalItems += 1;
                ExportSTL(rhinoObject.Geometry as Mesh, keyPairValue.Value, BuildingBlocks.Blocks[keyPairValue.Key].Color, ref failedItems);
            }
        }

        private void ExportCOR(ref int totalItems, ref List<string> failedItems)
        {
            var sphereCreator = new CORSphereCreator(_director);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORGlenoidSphereForAnterior(), "COR_Glenoid_Ant", Colors.CoRglenoid, ref failedItems);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORGlenoidSphereForLateral(), "COR_Glenoid_Lat", Colors.CoRglenoid, ref failedItems);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORGlenoidSphereForPosterior(), "COR_Glenoid_Post", Colors.CoRglenoid, ref failedItems);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORGlenoidSphereForSuperior(), "COR_Glenoid_Sup", Colors.CoRglenoid, ref failedItems);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORImplantSphereForAnterior(), "COR_Implant_Ant", Colors.CoRimplant, ref failedItems);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORImplantSphereForLateral(), "COR_Implant_Lat", Colors.CoRimplant, ref failedItems);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORImplantSphereForPosterior(), "COR_Implant_Post", Colors.CoRimplant, ref failedItems);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORImplantSphereForSuperior(), "COR_Implant_Sup", Colors.CoRimplant, ref failedItems);

            if (_director.PreopCor == null)
            {
                return;
            }

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORPreopSphereForAnterior(), "COR_PreOp_Ant", Colors.CoRpreop, ref failedItems);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORPreopSphereForLateral(), "COR_PreOp_Lat", Colors.CoRpreop, ref failedItems);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORPreopSphereForPosterior(), "COR_PreOp_Post", Colors.CoRpreop, ref failedItems);

            totalItems += 1;
            ExportSTL(sphereCreator.CreateCORPreopSphereForSuperior(), "COR_PreOp_Sup", Colors.CoRpreop, ref failedItems);
        }
    }
}
