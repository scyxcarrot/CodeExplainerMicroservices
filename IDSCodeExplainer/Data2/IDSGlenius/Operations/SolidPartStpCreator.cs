using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Query;
using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    //This class require the execution command to have [CommandStyle(Style.ScriptRunner)] enabled
    //TODO: Tech Debt this also export parts, need to re-purpose this class
    public class SolidPartStpCreator
    {
        private readonly GleniusImplantDirector director;
        private readonly GleniusObjectManager objectManager;
        private readonly Curve basePlateTopContour;
        private readonly Brep[] screwMantles;
        private readonly Plane basePlateTopContourCs;
        private readonly ImplantDerivedEntities derivedEntityProvider;
        private readonly Brep productionRod;
        private readonly bool productionRodWithChamfer;
        private readonly double modelAbsoluteTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        private Brep productionRodChamferPart;

        private readonly ObjectExporter exporter;

        public SolidPartStpCreator(GleniusImplantDirector director, bool productionRodWithChamfer, string outputDirectory)
        {

            this.director = director;
            objectManager = new GleniusObjectManager(director);
            basePlateTopContour = objectManager.GetBuildingBlock(IBB.BasePlateTopContour).Geometry as Curve;
            screwMantles = objectManager.GetAllBuildingBlocks(IBB.ScrewMantle).Select(x => x.Geometry as Brep).ToArray();
            derivedEntityProvider = new ImplantDerivedEntities(director);
            this.productionRod = objectManager.GetBuildingBlock(IBB.ProductionRod).Geometry as Brep;
            this.productionRodWithChamfer = productionRodWithChamfer;

            PlateDrawingPlaneGenerator generator = new PlateDrawingPlaneGenerator(director);
            basePlateTopContourCs = generator.GenerateTopPlane();

            exporter = new ObjectExporter(director.Document);
            exporter.ExportDirectory = outputDirectory;
        }

        private bool IsParametersValid()
        {
            return basePlateTopContour != null && screwMantles != null && productionRod != null;
        }

        private Brep GenerateBasePlatePlanePlanarSurface()
        {
            var planarSurfaces = Brep.CreatePlanarBreps(basePlateTopContour).ToList();

            var planarSurface = new Brep();
            planarSurfaces.ForEach(x => planarSurface.Append(x));
            return planarSurface;
        }

        private Brep CreateBasePlateExtrude(bool roundEdges, double roundingRadius)
        {
            try
            {
                var basePlate_plane_planarSurface = GenerateBasePlatePlanePlanarSurface();

                var normal = basePlate_plane_planarSurface.Faces[0].NormalAt(1.0, 1.0);

                double offset = Glenius.Constants.Plate.BasePlateThickness;

                if (Vector3d.Multiply(normal, basePlateTopContourCs.ZAxis) < 0)
                {
                    offset = -offset;
                }

                if (!roundEdges)
                {
                    return Brep.CreateFromOffsetFace(basePlate_plane_planarSurface.Faces[0], offset, 0.01, false, true);
                }
                else
                {
                    //Method 1
                    var basePlateRounded = GenerateFilletedBasePlateMethod1(basePlateTopContour, roundingRadius);

                    //Method 2 if 1 fails
                    if (basePlateRounded == null)
                    {
                        basePlateRounded = GenerateFilletedBasePlateMethod2(basePlateTopContour, roundingRadius);
                    }

                    return basePlateRounded;
                }
            }
            catch (System.NullReferenceException ex)
            {
                return null;
            }
        }

        private Plane GenerateBasePlatePlane()
        {
            return new Plane(derivedEntityProvider.GetMetalBackingPlane());
        }

        private Plane GenerateBasePlatePlane0dot5MmMedial()
        {
            var metalBackingPlane = GenerateBasePlatePlane();

            var translationVector = metalBackingPlane.ZAxis;
            translationVector.Unitize();
            translationVector *= 0.5;

            var plane = new Plane(basePlateTopContourCs);
            plane.Translate(translationVector);

            return plane;
        }

        private Brep GenerateBasePlateExtrudeLatOffset(double lateralOffset)
        {
            var metalBackingPlane = GenerateBasePlatePlane();

            var translationVector = -metalBackingPlane.ZAxis; //going lateral
            translationVector.Unitize();
            translationVector *= lateralOffset;

            var basePlate_plane_planarSurface = GenerateBasePlatePlanePlanarSurface();
            basePlate_plane_planarSurface.Translate(translationVector);

            var normal = basePlate_plane_planarSurface.Faces[0].NormalAt(1.0, 1.0);
            var extrudeDistance = Glenius.Constants.Plate.BasePlateThickness + lateralOffset;

            if (Vector3d.Multiply(normal, basePlateTopContourCs.ZAxis) < 0)
            {
                extrudeDistance = -extrudeDistance;
            }

            var res = Brep.CreateFromOffsetFace(basePlate_plane_planarSurface.Faces[0], extrudeDistance, modelAbsoluteTolerance, false, true);

            return res;
        }

        private Brep CreateSmoothCylinderToBasePlateTransitionForProductionReal(double radius, Brep basePlate)
        {
            var cylinder = objectManager.GetBuildingBlock(IBB.CylinderHat).Geometry as Brep;

            var contactEdges = BrepUtilities.GetContactEdges(cylinder, basePlate.DuplicateBrep(), 1.0);
            var edge1 = contactEdges[contactEdges.Count - 1];
            var edge2 = contactEdges[contactEdges.Count - 2];

            var filletCurveBase = Curve.JoinCurves(
                new[] { edge1.ToNurbsCurve().DuplicateCurve(), edge2.ToNurbsCurve().DuplicateCurve() }).FirstOrDefault();
            filletCurveBase.MakeClosed(0.1);

            filletCurveBase = Curve.ProjectToPlane(filletCurveBase, basePlateTopContourCs);

            var ptCenter = CurveUtilities.GetCurveCentroid(filletCurveBase);

            var filletCurveBottom = filletCurveBase.DuplicateCurve().
                Offset(basePlateTopContourCs, radius + 0.1, modelAbsoluteTolerance, CurveOffsetCornerStyle.None).FirstOrDefault();

            var filletCurveTop = filletCurveBase.DuplicateCurve();
            var translationVector = -basePlateTopContourCs.ZAxis;
            translationVector.Unitize();
            filletCurveTop.Translate(translationVector * (radius + 0.1));

            var connectorCurve = CurveUtilities.BuildCurve(
                new List<Point3d>() { filletCurveTop.PointAtStart, filletCurveBase.PointAtStart, filletCurveBottom.PointAtStart },
                1, false);

            var arc = Curve.CreateFillet(connectorCurve, connectorCurve, radius, 0, 1);

            var idConnector = director.Document.Objects.Add(arc.ToNurbsCurve());
            var idTop = director.Document.Objects.Add(filletCurveTop);
            var idBottom = director.Document.Objects.Add(filletCurveBottom);

            var result = BrepUtilities.ScriptedSweep2(director.Document, idTop, idBottom, new[] { idConnector }).FirstOrDefault();
            var transitionBrep = (director.Document.Objects.Find(result).Geometry as Brep).DuplicateBrep();

            //Clean Up
            director.Document.Objects.Delete(idConnector, true);
            director.Document.Objects.Delete(idTop, true);
            director.Document.Objects.Delete(idBottom, true);
            director.Document.Objects.Delete(result, true);

            transitionBrep = transitionBrep.CapPlanarHoles(modelAbsoluteTolerance);
            var metalBackingPlane = GenerateBasePlatePlane();

            transitionBrep.Rotate(RhinoMath.ToRadians(7), metalBackingPlane.ZAxis, ptCenter);

            if (transitionBrep.SolidOrientation == BrepSolidOrientation.Inward)
            {
                transitionBrep.Flip();
            }

            return transitionBrep;
        }

        public bool CreateForProductionReal(out Brep baseplate, out Dictionary<string, Brep> intermediates)
        {
            // Initialize
            baseplate = null;
            intermediates = new Dictionary<string, Brep>();
            // Parameter check
            if (!IsParametersValid())
            {
                return false;
            }

            try
            {
                // Export production rod
                intermediates.Add("Production_Rod", productionRod );

                // Create rounded baseplate
                const double basePlateEdgeRoundingRadius = 0.45;
                var basePlatePlane0Dot5MmMed = GenerateBasePlatePlane0dot5MmMedial();

                // Cut screwmantles
                var screwmantlesAsOne = new Brep();
                screwMantles.ToList().ForEach(x => screwmantlesAsOne.Append(x.DuplicateBrep()));
                intermediates.Add("Screw_Mantles", screwmantlesAsOne);
                var screwMantleMeds = screwMantles.Select(sm => BrepUtilities.Trim(sm.DuplicateBrep(), basePlatePlane0Dot5MmMed, true)).ToList();
                var screwMantleMedsAsOne = new Brep();
                screwMantleMeds.ForEach(x => screwMantleMedsAsOne.Append(x.DuplicateBrep()));
                intermediates.Add("Screw_Mantles_Medial", screwMantleMedsAsOne);

                // Get cylinder
                var cylinder = objectManager.GetBuildingBlock(IBB.CylinderHat).Geometry as Brep;
                if (cylinder == null)
                {
                    return false;
                }
                // Correct cylinder normals if necessary
                if (cylinder.SolidOrientation == BrepSolidOrientation.Inward)
                {
                    cylinder.Flip();
                }
                intermediates.Add("Cylinder", cylinder );

                //Get other parts
                var derivedEntity = new ImplantDerivedEntities(director);
                var transitionBrep = derivedEntity.GetCylinderWithTransition();
                intermediates.Add("Cylinder_With_Transition", transitionBrep);
                var taperMantle = derivedEntityProvider.GetTaperMantle().DuplicateBrep();
                intermediates.Add("Taper_Mantle", taperMantle);
                var taperBooleanReal = derivedEntityProvider.GetTaperBooleanReal().DuplicateBrep();
                intermediates.Add("Taper_Hole_Boolean", taperBooleanReal );
                var connectionScrewHoleProduction = derivedEntityProvider.GetM4ConnectionScrewHoleProduction().DuplicateBrep();
                intermediates.Add("M4_HoleBoolean", connectionScrewHoleProduction );
                if (productionRodWithChamfer)
                {
                    CreateProductionRodChamferPartIfNotCreated();
                    intermediates.Add("Production_Rod_Chamfer", productionRodChamferPart );
                }

                //Create BasePlate extrude
                var basePlatePlaneExtrude = CreateBasePlateExtrude(true, basePlateEdgeRoundingRadius); //BasePlateReal
                intermediates.Add("Base_Plate", basePlatePlaneExtrude );

                // Union baseplate and cylinder with transition to baseplate
                var basePlateExtrudeWCylinder = Brep.CreateBooleanUnion(new[] { basePlatePlaneExtrude, transitionBrep }, modelAbsoluteTolerance);
                if (basePlateExtrudeWCylinder == null || !basePlateExtrudeWCylinder.Any())
                {
                    return false;
                }

                // Union all parts
                var unionPart1Parts = new List<Brep>(screwMantleMeds) { basePlateExtrudeWCylinder.FirstOrDefault(), productionRod, taperMantle };
                var unionPart1SArray = Brep.CreateBooleanUnion(unionPart1Parts, modelAbsoluteTolerance);
                if (unionPart1SArray == null || !unionPart1SArray.Any())
                {
                    return false;
                }

                var unionPart1 = unionPart1SArray.ToList().FirstOrDefault();

                // Subtract screwholes
                var screwHoleProduction = new Brep();
                objectManager.GetAllBuildingBlocks(IBB.Screw).Select(x => x as Screw).ToList().
                    ForEach(x => screwHoleProduction.Append(derivedEntityProvider.GetScrewHoleProduction(x).DuplicateBrep()));
                intermediates.Add("Screw_Holes_Production", screwHoleProduction);

                // Subtract taper and connection screw holes
                var sp4Subtractions = Brep.CreateBooleanDifference(new[] { unionPart1 },
                    new[] { screwHoleProduction, taperBooleanReal, connectionScrewHoleProduction }, modelAbsoluteTolerance);
                if (sp4Subtractions == null || !sp4Subtractions.Any())
                {
                    return false;
                }
                var sp4Subtraction = sp4Subtractions.ToList().FirstOrDefault();

                // Add chamfered part of production rod
                baseplate = sp4Subtraction.DuplicateBrep();
                baseplate = AddTaperedPartOfProductionRodToImplant(baseplate);
            }
            catch (IDSException ex)
            {
                return false;
            }

            return true;
        }

        public bool CreateForProductionOffset(out Brep baseplate, out Dictionary<string, Brep> intermediates)
        {
            baseplate = null;
            intermediates = new Dictionary<string, Brep>();

            if (!IsParametersValid())
            {
                return false;
            }

            try
            {
                var configurationInfoProvider = new ConfigurationInfoProvider();
                var lateralOffset = configurationInfoProvider.GetBasePlateOffsetValue();
                var basePlate_extrude_LateralOffset = GenerateBasePlateExtrudeLatOffset(lateralOffset);
                intermediates.Add("Base_Plate_Offset", basePlate_extrude_LateralOffset);

                var cylinderOffset = derivedEntityProvider.GetCylinderOffset().DuplicateBrep();
                intermediates.Add("Cylinder_Offset", cylinderOffset);

                var referenceBlock = derivedEntityProvider.GetReferenceBlock().DuplicateBrep();
                intermediates.Add("Reference_Block", referenceBlock);

                var taperBooleanProduction = derivedEntityProvider.GetTaperBooleanProduction().DuplicateBrep();
                intermediates.Add("Taper_Hole_Boolean_Production", taperBooleanProduction);

                var basePlateCylinderOffsetRefBlocks =
                    Brep.CreateBooleanUnion(new[] { referenceBlock, basePlate_extrude_LateralOffset, cylinderOffset },
                        modelAbsoluteTolerance); //Need this kind of tolerance, since it overlaps with each other

                var basePlateCylinderOffsetRefBlock = basePlateCylinderOffsetRefBlocks.FirstOrDefault();

                //UnionPart2
                var taperMantle = derivedEntityProvider.GetTaperMantle().DuplicateBrep();

                var unionParts2s = Brep.CreateBooleanUnion(
                    new[] { basePlateCylinderOffsetRefBlock, taperMantle, productionRod }, modelAbsoluteTolerance);

                var unionPart2 = unionParts2s.FirstOrDefault();

                var screwMantleMeds = new List<Brep>();
                screwMantles.Select(sm => BrepUtilities.Trim(sm.DuplicateBrep(), GenerateBasePlatePlane0dot5MmMedial(), true))
                    .ToList().ForEach(x => screwMantleMeds.Add(x));

                foreach (var scMed in screwMantleMeds)
                {
                    unionPart2 = Brep.CreateBooleanUnion(new[] { unionPart2, scMed }, modelAbsoluteTolerance).ToList().FirstOrDefault();
                }

                //SP5_subtraction
                var taperBooleanProductions =
                    Brep.CreateBooleanDifference(new[] { unionPart2 }, new[] { taperBooleanProduction }, modelAbsoluteTolerance).ToList();

                var sp5Subtraction = taperBooleanProductions.FirstOrDefault();

                var screws = director.ScrewObjectManager.GetAllScrews().ToList();
                foreach (var s in screws)
                {
                    var screwHoleProductionOffset = derivedEntityProvider.GetScrewHoleProductionOffset(s);
                    sp5Subtraction = Brep.CreateBooleanDifference(sp5Subtraction,
                        screwHoleProductionOffset, modelAbsoluteTolerance).FirstOrDefault();
                }

                baseplate = sp5Subtraction.DuplicateBrep();
                baseplate = AddTaperedPartOfProductionRodToImplant(baseplate);
            }
            catch (IDSException e)
            {
                return false;
            }

            return true;
        }

        private Brep AddTaperedPartOfProductionRodToImplant(Brep implant)
        {
            if (productionRodWithChamfer)
            {
                CreateProductionRodChamferPartIfNotCreated();
                var unionChamfer = Brep.CreateBooleanUnion(new[] { implant, productionRodChamferPart }, modelAbsoluteTolerance);

                if (unionChamfer == null || !unionChamfer.Any())
                {
                    return null;
                }

                var final = new Brep();
                unionChamfer.ToList().ForEach(x => final.Append(x.DuplicateBrep()));
                return final.DuplicateBrep();
            }
            else
            {
                return implant;
            }
        }

        private void CreateProductionRodChamferPartIfNotCreated()
        {
            if (productionRodChamferPart == null)
            {
                var productionRodCreator = new ProductionRodCreator(director);
                var productionRodChamfer = productionRodCreator.CreateProductionRodChamferPart();
                productionRodChamferPart = productionRodChamfer;
            }
        }


        private Brep GenerateFilletedBasePlateMethod1(Curve basePlateTopContour, double roundingRadius)
        {
            try
            {
                var basePlate_plane_planarSurface = GenerateBasePlatePlanePlanarSurface();

                //Try first algorithm
                var body = Extrusion.CreateExtrusion(basePlateTopContour,
                    basePlateTopContourCs.ZAxis * Constants.Plate.BasePlateThickness).ToBrep();

                var filletResults = BrepUtilities.ScriptedFilletSurface(director.Document,
                    basePlate_plane_planarSurface, body, roundingRadius);

                var plate = filletResults.FirstOrDefault();
                plate = plate.CapPlanarHoles(modelAbsoluteTolerance); //Makes the bottom surface

                if (plate.SolidOrientation == BrepSolidOrientation.Inward)
                {
                    plate.Flip();
                }

                return plate;
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        private Brep GenerateFilletedBasePlateMethod2(Curve basePlateTopContour, double roundingRadius)
        {
            try
            {
                //Basis
                var basePlate_plane_planarSurface = GenerateBasePlatePlanePlanarSurface();

                //Create curves to regenerate the baseplate
                var innerOffsetbasePlateTopContour = basePlateTopContour.DuplicateCurve();
                innerOffsetbasePlateTopContour = innerOffsetbasePlateTopContour.OffsetOnSurface(basePlate_plane_planarSurface.Faces[0], roundingRadius,
                    modelAbsoluteTolerance).ToList().FirstOrDefault();

                var outerOffsetbasePlateTopContour = basePlateTopContour.DuplicateCurve();
                outerOffsetbasePlateTopContour.Translate(basePlateTopContourCs.ZAxis * roundingRadius);

                //Recreation of baseplate
                var trimedBasePlateTop = Brep.CreatePlanarBreps(new[] { innerOffsetbasePlateTopContour }).FirstOrDefault();
                var trimmedBasePlateBody = Extrusion.CreateExtrusion(outerOffsetbasePlateTopContour,
                    basePlateTopContourCs.ZAxis * (Constants.Plate.BasePlateThickness - roundingRadius)).ToBrep();

                //Filleting
                var body = Extrusion.CreateExtrusion(basePlateTopContour,
                    basePlateTopContourCs.ZAxis * Constants.Plate.BasePlateThickness).ToBrep();

                var roudedBasePlateTopCorner = Surface.CreateRollingBallFillet(basePlate_plane_planarSurface.Surfaces.FirstOrDefault(),
                    body.Surfaces.FirstOrDefault(), roundingRadius, modelAbsoluteTolerance).ToList().FirstOrDefault().ToBrep();

                //Stich all together
                var roundedBasePlate = trimedBasePlateTop.DuplicateBrep();
                roundedBasePlate.Join(roudedBasePlateTopCorner, modelAbsoluteTolerance, true);
                roundedBasePlate.Join(trimmedBasePlateBody, modelAbsoluteTolerance, true);

                var finalBasePlate = roundedBasePlate.CapPlanarHoles(modelAbsoluteTolerance);

                if (finalBasePlate.SolidOrientation == BrepSolidOrientation.Inward)
                {
                    finalBasePlate.Flip();
                }

                return finalBasePlate;
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }
    }
}
