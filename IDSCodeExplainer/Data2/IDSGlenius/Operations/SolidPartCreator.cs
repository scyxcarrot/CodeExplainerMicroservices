using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Query;
using Rhino;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class SolidPartCreator
    {
        private readonly GleniusImplantDirector director;
        private readonly GleniusObjectManager objectManager;
        private readonly ImplantDerivedEntities implantDerivedEntities;
        private readonly double tolerance;
        private readonly bool productionRodWithChamfer;
        private Mesh solidPartForReportingAndFinalization;
        private Mesh derivedBasePlate;
        private Mesh derivedBasePlateCylinder;
        private Mesh derivedSolidEntitiesMed;
        private Mesh derivedSolidEntitiesMedWithSolidWalls;
        private Mesh derivedSolidEntitiesMedWithSolidWallWraps;
        private Mesh derivedScaffoldShape;
        private Mesh derivedBone;
        private Brep productionRodChamferPart;

        public Mesh SolidPartForReportingBeforeSubtraction { get; private set; }
        public Mesh SolidPartForFinalizationBeforeSubtraction { get; private set; }

        public SolidPartCreator(GleniusImplantDirector director, bool productionRodWithChamfer)
        {
            this.director = director;
            this.objectManager = new GleniusObjectManager(director);
            implantDerivedEntities = new ImplantDerivedEntities(director);
            tolerance = 0.001;
            SolidPartForReportingBeforeSubtraction = null;
            SolidPartForFinalizationBeforeSubtraction = null;
            this.productionRodWithChamfer = productionRodWithChamfer;
        }

        public bool GetSolidPartForReporting(out Mesh output)
        {
            var success = true;
            SolidPartForReportingBeforeSubtraction = null;
            Locking.UnlockImplantCreation(director.Document);

            output = null;
            try
            {
                var solidPart = CreateMergedSolidPartForReportingAndFinalization();
                output = solidPart.DuplicateMesh();

                var solidPartWrapped = WrapAndCutSolidPart(solidPart);
                output = solidPartWrapped.DuplicateMesh();

                var final = RemeshSmoothUnion(solidPartWrapped, solidPart);
                output = final.DuplicateMesh();

                var clear = BoneClearance(final);
                output = clear.DuplicateMesh();
                SolidPartForReportingBeforeSubtraction = clear.DuplicateMesh();

                var subtract = SubtractPartsForReporting(clear);
                subtract = MeshUtilities.ExtendedAutoFix(subtract);
                output = subtract.DuplicateMesh();
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.Message);
                success = false;
            }

            Locking.LockAll(director.Document);
            return success;
        }

        //Plate for Finalization
        public bool GetSolidPartForFinalization(out Mesh output)
        {
            var success = true;
            SolidPartForFinalizationBeforeSubtraction = null;
            Locking.UnlockImplantCreation(director.Document);

            output = null;
            try
            {
                var solidPart = CreateMergedSolidPartForReportingAndFinalization();
                var transition = GetSolidPartTransitionForFinalization(solidPart);
                output = transition.DuplicateMesh();

                var final = Union(transition, solidPart);
                output = final.DuplicateMesh();

                var productionRod = GetProductionRod(true);
                var solidPartShape = Union(final, productionRod);
                output = solidPartShape.DuplicateMesh();
                SolidPartForFinalizationBeforeSubtraction = solidPartShape.DuplicateMesh();

                var subtract = SubtractPartsForFinalization(solidPartShape);
                output = subtract.DuplicateMesh();

                var unionChamfer = AddTaperedPartOfProductionRodToImplant(subtract);
                unionChamfer = MeshUtilities.ExtendedAutoFix(unionChamfer);
                output = unionChamfer.DuplicateMesh();
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.Message);
                success = false;
            }

            Locking.LockAll(director.Document);
            return success;
        }

        public bool GetSolidPartForProductionOffset(out Mesh output)
        {
            var success = true;
            Locking.UnlockImplantCreation(director.Document);

            output = null;
            try
            {
                var solidPart = CreateMergedSolidPartForProductionOffset();
                output = solidPart.DuplicateMesh();

                var productionRod = GetProductionRod(false);
                output = productionRod.DuplicateMesh();

                var unionSolidPartAndProductionRod = Union(solidPart, productionRod);
                output = unionSolidPartAndProductionRod.DuplicateMesh();

                var solidPartFinalization = CreateMergedSolidPartForReportingAndFinalization();
                var transition = GetSolidPartTransitionForFinalization(solidPartFinalization);
                output = transition.DuplicateMesh();

                var final = Union(transition, unionSolidPartAndProductionRod);
                output = final.DuplicateMesh();

                var solidPartShape = Union(final, productionRod);
                output = solidPartShape.DuplicateMesh();

                var subtract = SubtractPartsForProductionOffset(solidPartShape);
                output = subtract.DuplicateMesh();

                var unionChamfer = AddTaperedPartOfProductionRodToImplant(subtract);
                unionChamfer = MeshUtilities.ExtendedAutoFix(unionChamfer);
                output = unionChamfer.DuplicateMesh();
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.Message);
                success = false;
            }

            Locking.LockAll(director.Document);
            return success;
        }

        public Mesh GetMacroShape()
        {
            var solidEntityMed = CreateSolidEntitiesMed();
            var scaffoldShape = GetScaffoldShape();
            var basePlateCylinder = CreateBasePlateCylinder();
            var merged = new Mesh();

            if (Booleans.PerformBooleanUnion(out merged, solidEntityMed, scaffoldShape, basePlateCylinder))
            {
                merged = MeshUtilities.FixMesh(merged);

                //var wrapParams = new MDCKShrinkWrapParameters(0.1, 0.0, 0.0, false, true, true, false);
                Mesh marcoShapeWrap;
                if (Wrap.PerformWrap(new[] { merged }, 0.1, 0.0, 0.0, false, true, true, false, out marcoShapeWrap))
                {
                    return marcoShapeWrap;
                }
            }

            throw new NullReferenceException("Fail to wrap MarcoShape");
        }

        public Mesh GetMacroShapeMed(Mesh marcoShape)
        {
            var plane = GetMetalBackingPlaneWithMedOffset(1.4);
            var macroShapeWrapCut = Cut(new List<Mesh> { marcoShape }, plane);
            if (macroShapeWrapCut == null)
            {
                throw new NullReferenceException("Fail to cut MarcoShapeWrap");
            }
            return macroShapeWrapCut;
        }

        public Mesh BoneClearance(Mesh solidPart)
        {
            var inMesh = solidPart.DuplicateMesh();
            if (derivedBone == null)
            {
                derivedBone = implantDerivedEntities.GetScapulaReamedWithWrap();
            }
            var scapula = derivedBone;

            var subtract = Booleans.PerformBooleanSubtraction(inMesh, scapula);
            if (subtract.IsValid)
            {
                return subtract;
            }
            throw new NullReferenceException("Fail to make Clearance");
        }

        public bool GetPlatePreview(out Mesh output)
        {
            var success = true;
            Locking.UnlockImplantCreation(director.Document);

            output = null;
            try
            {
                var solidPart = MergeSolidPartForPlatePreview();
                output = solidPart.DuplicateMesh();

                var subtract = SubtractPartsForReporting(solidPart);
                output = subtract.DuplicateMesh();
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.Message);
                success = false;
            }

            Locking.LockAll(director.Document);
            return success;
        }

        #region Helpers

        public Mesh CreateMergedSolidPartForReportingAndFinalization()
        {
            if (solidPartForReportingAndFinalization == null)
            {
                var basePlateCylinder = CreateBasePlateCylinder();
                var solidEntityMed = CreateSolidEntitiesMedWithSolidWallWraps();
                Mesh merged;
                if (Booleans.PerformBooleanUnion(out merged, basePlateCylinder, solidEntityMed))
                {
                    solidPartForReportingAndFinalization = MeshUtilities.FixMesh(merged);
                }
                else
                {
                    throw new NullReferenceException("Fail to union SmoothBasePlate and SolidEntityMed");
                }
            }
            return solidPartForReportingAndFinalization;
        }

        public Mesh CreateMergedSolidPartForProductionOffset()
        {
            var basePlate = objectManager.GetBuildingBlock(IBB.PlateBasePlate).Geometry.Duplicate() as Mesh;
            var extrude = GetBasePlateOffset();
            var cylinderOffset = implantDerivedEntities.GetCylinderOffset();
            var refBlock = implantDerivedEntities.GetReferenceBlock();
            var solidEntityMed = CreateSolidEntitiesMedWithSolidWallWraps();

            var brep = cylinderOffset;
            brep.Append(refBlock);
            var merged = MeshUtilities.ConvertBrepToMesh(brep, true);

            Mesh unioned;
            if (Booleans.PerformBooleanUnion(out unioned, merged, basePlate, extrude, solidEntityMed) && unioned.IsValid)
            {
                return MeshUtilities.FixMesh(unioned);
            }
            throw new NullReferenceException("Fail to merge SolidPart For ProductionOffset");
        }

        private Mesh MergeSolidPartForPlatePreview()
        {
            var cylinderHat = objectManager.GetBuildingBlock(IBB.CylinderHat).Geometry.Duplicate() as Brep;
            var cylinder = MeshUtilities.ConvertBrepToMesh(cylinderHat, true);
            var basePlate = objectManager.GetBuildingBlock(IBB.PlateBasePlate).Geometry.Duplicate() as Mesh;
            var solidEntityMed = CreateSolidEntitiesMedWithSolidWalls();

            Mesh merged;
            if(Booleans.PerformBooleanUnion(out merged, cylinder, basePlate, solidEntityMed))
            {
                return MeshUtilities.FixMesh(merged);
            }
            else
            {
                throw new NullReferenceException("Fail to union cylinder, basePlate, and solidEntityMed");
            }
        }

        private Mesh CreateBasePlateCylinder()
        {
            if (derivedBasePlateCylinder == null)
            {
                var basePlateMesh = objectManager.GetBuildingBlock(IBB.PlateBasePlate).Geometry as Mesh;
                var unroundedCylinder = implantDerivedEntities.GetUnroundedCylinder();
                var unroundedCylinderMesh = MeshUtilities.ConvertBrepToMesh(unroundedCylinder, true);
                var union = Union(basePlateMesh, unroundedCylinderMesh);
                if (union == null)
                {
                    throw new NullReferenceException("Fail to create BasePlateCylinder");
                }

                derivedBasePlateCylinder = union;
                return derivedBasePlateCylinder;
            }
            return derivedBasePlateCylinder;
        }

        private Mesh CreateSolidWalls()
        {
            var solidWallWraps = CreateSolidWallWraps();
            if (solidWallWraps != null)
            {
                var scaffoldShape = GetScaffoldShape();
                var intersected = Booleans.PerformBooleanIntersection(solidWallWraps, scaffoldShape);
                if (intersected.IsValid)
                {
                    return intersected;
                }
                throw new NullReferenceException("Fail to create SolidWalls");
            }
            return null;
        }

        private Mesh CreateSolidWallWraps()
        {
            if (objectManager.HasBuildingBlock(IBB.SolidWallWrap))
            {
                var solidWallWraps = objectManager.GetAllBuildingBlocks(IBB.SolidWallWrap).Select(wrap => wrap.Geometry.Duplicate() as Mesh);
                Mesh unioned;
                if (Booleans.PerformBooleanUnion(out unioned, solidWallWraps.ToArray()) && unioned.IsValid)
                {
                    return unioned;
                }
                throw new NullReferenceException("Fail to create SolidWallWraps");
            }
            return null;
        }

        private Mesh CreateSolidEntitiesMed()
        {
            if (derivedSolidEntitiesMed == null)
            {
                var plane = GetMetalBackingPlaneWithMedOffset(1.0);
                var solidEntityList = GetSolidEntitiesInList();
                var solidEntitiesMed = Cut(solidEntityList, plane);
                if (solidEntitiesMed == null)
                {
                    throw new NullReferenceException("Fail to create SolidEntitiesMed");
                }
                derivedSolidEntitiesMed = solidEntitiesMed;
            }
            return derivedSolidEntitiesMed;
        }

        private Mesh CreateSolidEntitiesMedWithSolidWalls()
        {
            return derivedSolidEntitiesMedWithSolidWalls ?? (derivedSolidEntitiesMedWithSolidWalls =
                       HandleCreateSolidEntitiesMed(CreateSolidWalls(), "SolidEntitiesMedWithSolidWalls"));
        }

        private Mesh CreateSolidEntitiesMedWithSolidWallWraps()
        {
            return derivedSolidEntitiesMedWithSolidWallWraps ?? (derivedSolidEntitiesMedWithSolidWallWraps =
                       HandleCreateSolidEntitiesMed(CreateSolidWallWraps(), "SolidEntitiesMedWithSolidWallWraps"));
        }

        private Mesh HandleCreateSolidEntitiesMed(Mesh additionalSolidWallTypeMesh, string partName)
        {
            var plane = GetMetalBackingPlaneWithMedOffset(1.0);
            var solidEntityList = GetSolidEntitiesInList();

            if (additionalSolidWallTypeMesh != null)
            {
                solidEntityList.Add(additionalSolidWallTypeMesh);
            }

            var solidEntitiesMed = Cut(solidEntityList, plane);
            if (solidEntitiesMed == null)
            {
                throw new NullReferenceException($"Fail to create {partName}");
            }
            return solidEntitiesMed;
        }

        private List<Mesh> GetSolidEntitiesInList()
        {
            var meshList = new List<Mesh>();
            var taperMantle = implantDerivedEntities.GetTaperMantle();
            var taperMantleMesh = MeshUtilities.ConvertBrepToMesh(taperMantle, true);
            meshList.Add(taperMantleMesh);

            if (objectManager.HasBuildingBlock(IBB.ScrewMantle))
            {
                var screwMantles = objectManager.GetAllBuildingBlocks(IBB.ScrewMantle).Select(wrap => wrap.Geometry as Brep);
                foreach (var brep in screwMantles)
                {
                    var screwMantleMesh = MeshUtilities.ConvertBrepToMesh(brep, true);
                    meshList.Add(screwMantleMesh);
                }
            }

            return meshList;
        }

        private Plane GetMetalBackingPlaneWithMedOffset(double offset)
        {
            var plane = implantDerivedEntities.GetMetalBackingPlane();
            var translationDirection = plane.ZAxis;
            translationDirection.Unitize();
            var translationVector = Vector3d.Multiply(translationDirection, offset);
            plane.Translate(translationVector);
            return plane;
        }

        private Mesh Cut(List<Mesh> list, Plane plane)
        {
            Mesh unioned;
            if (Booleans.PerformBooleanUnion(out unioned, list.ToArray()) && unioned.IsValid)
            {
                var planeSurface = PlaneSurface.CreateThroughBox(plane, unioned.GetBoundingBox(false));
                var planeSurfaceFace = Brep.CreateFromSurface(planeSurface).Faces[0];
                var offsetDistance = 100;
                double u, v;
                if (planeSurfaceFace.ClosestPoint(plane.Origin, out u, out v))
                {
                    var direction = planeSurfaceFace.NormalAt(u, v);
                    if (direction.IsParallelTo(plane.ZAxis, tolerance) == 1)
                    {
                        offsetDistance = -offsetDistance;
                    }
                }
                var brep = Brep.CreateFromOffsetFace(planeSurfaceFace, offsetDistance, tolerance, false, true);
                if (brep.SolidOrientation == BrepSolidOrientation.Inward)
                {
                    brep.Flip();
                }
                var cutting = MeshUtilities.ConvertBrepToMesh(brep, true);

                var subtract = Booleans.PerformBooleanSubtraction(unioned, cutting);
                if (subtract.IsValid)
                {
                    return subtract;
                }
            }
            return null;
        }

        private Mesh GetBasePlateOffset()
        {
            var configurationInfoProvider = new ConfigurationInfoProvider();
            var lateralOffset = configurationInfoProvider.GetBasePlateOffsetValue();

            var plane = implantDerivedEntities.GetMetalBackingPlane();
            var translationDirection = -plane.ZAxis;
            translationDirection.Unitize();
            var translationVector = Vector3d.Multiply(translationDirection, lateralOffset);

            var topContour = objectManager.GetBuildingBlock(IBB.BasePlateTopContour).Geometry.Duplicate() as Curve;
            topContour.Translate(translationVector);

            var brepFace = Brep.CreatePlanarBreps(topContour)[0].Faces[0];
            var extrudeDistance = lateralOffset + 0.1;
            double u, v;
            if (brepFace.ClosestPoint(plane.Origin, out u, out v))
            {
                var direction = brepFace.NormalAt(u, v);
                if (direction.IsParallelTo(plane.ZAxis, 0.001) == -1) //vectors are anti-parallel
                {
                    extrudeDistance = -extrudeDistance;
                }
            }
            var brep = Brep.CreateFromOffsetFace(brepFace, extrudeDistance, 0.01, false, true);
            return MeshUtilities.ConvertBrepToMesh(brep, true);
        }

        private Mesh WrapAndCutSolidPart(Mesh solidPart)
        {
            //var wrapParams = new MDCKShrinkWrapParameters(0.5, 10.0, -0.1, false, true, true, false);
            Mesh solidPartWrap;
            if (Wrap.PerformWrap(new[] { solidPart }, 0.5, 10.0, -0.1, false, true, true, false, out solidPartWrap))
            {
                var plane = GetMetalBackingPlaneWithMedOffset(1.0);
                var solidPartWrapCut = Cut(new List<Mesh> { solidPartWrap }, plane);
                if (solidPartWrapCut == null)
                {
                    throw new NullReferenceException("Fail to cut SolidPartWrap");
                }
                return solidPartWrapCut;
            }
            throw new NullReferenceException("Fail to wrap SolidPart");
        }
        private Mesh RemeshSmooth(Mesh toProcess)
        {
            Mesh remeshed;
            if (MeshUtilities.Remesh(toProcess, 0.7, out remeshed))
            {
                remeshed = MeshUtilities.FixMesh(remeshed);

                //var opParams = new MDCKSmoothParameters();
                //opParams.Compensation = true;
                //opParams.SmoothenAlgorithm = SmoothenAlgorithm.FirstOrderLaplacian;
                //opParams.SmoothenFactor = 0.8;
                //opParams.SmoothenIterations = 25;
                var smoothen = Smooth.PerformSmoothing(remeshed, iterations:25);
                if (smoothen.IsValid)
                {
                    return smoothen;
                }
                throw new NullReferenceException("Fail to Smooth");
            }
            throw new NullReferenceException("Fail to Remesh");
        }

        private Mesh RemeshSmoothUnion(Mesh toProcess, Mesh toUnion)
        {
            var remeshedAndsmoothen = RemeshSmooth(toProcess);
            var unioned = Union(remeshedAndsmoothen, toUnion);
            if (unioned == null)
            {
                throw new NullReferenceException("Fail to union Remeshed and Smoothen");
            }
            return unioned;
        }

        private Mesh GetSolidPartTransitionForFinalization(Mesh solidPart)
        {
            var productionRod = GetProductionRod(true);
            var unionSolidPartAndProductionRod = Union(solidPart, productionRod);
            var solidPartWrapped = WrapAndCutSolidPart(unionSolidPartAndProductionRod);
            var transition = RemeshSmooth(solidPartWrapped);
            return transition;
        }

        private Mesh SubtractPartsForReporting(Mesh parts)
        {
            var subtracting = new List<Mesh>();
            var screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(sc => sc as Screw);
            foreach (var screw in screws)
            {
                var brep = implantDerivedEntities.GetScrewHoleReal(screw);
                subtracting.Add(MeshUtilities.ConvertBrepToMesh(brep, true));
            }

            var conn = implantDerivedEntities.GetM4ConnectionScrewHoleReal();
            subtracting.Add(MeshUtilities.ConvertBrepToMesh(conn, true));

            var taper = implantDerivedEntities.GetTaperBooleanReal();
            subtracting.Add(MeshUtilities.ConvertBrepToMesh(taper, true));

            Mesh unioned;
            if (Booleans.PerformBooleanUnion(out unioned, subtracting.ToArray()) && unioned.IsValid)
            {
                var subtract = Booleans.PerformBooleanSubtraction(parts, unioned);
                if (subtract.IsValid)
                {
                    return subtract;
                }
            }
            throw new NullReferenceException("Fail to Subtract parts for Reporting");
        }

        private Mesh SubtractPartsForFinalization(Mesh parts)
        {
            var subtracting = new List<Mesh>();

            var conn = implantDerivedEntities.GetM4ConnectionScrewHoleProduction();
            subtracting.Add(MeshUtilities.ConvertBrepToMesh(conn, true));

            var taper = implantDerivedEntities.GetTaperBooleanReal();
            subtracting.Add(MeshUtilities.ConvertBrepToMesh(taper, true));

            Mesh unioned;
            if (Booleans.PerformBooleanUnion(out unioned, subtracting.ToArray()) && unioned.IsValid)
            {
                var subtract = Booleans.PerformBooleanSubtraction(parts, unioned);
                if (subtract.IsValid)
                {
                    return subtract;
                }
            }
            throw new NullReferenceException("Fail to Subtract parts for Finalization");
        }

        private Mesh SubtractPartsForProductionOffset(Mesh parts)
        {
            var taper = implantDerivedEntities.GetTaperBooleanProduction();

            var subtract = Booleans.PerformBooleanSubtraction(parts, MeshUtilities.ConvertBrepToMesh(taper, true));
            if (subtract.IsValid)
            {
                return subtract;
            }
            throw new NullReferenceException("Fail to Subtract parts for Production Offset");
        }

        private Mesh GetProductionRod(bool needToSubtract)
        {
            var productionRod = (objectManager.GetBuildingBlock(IBB.ProductionRod).Geometry as Brep).DuplicateBrep();

            if (needToSubtract)
            {
                var subtractingList = new List<Brep>();
                var screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(sc => sc as Screw);
                foreach (var screw in screws)
                {
                    var brep = implantDerivedEntities.GetScrewHoleScaffold(screw);
                    subtractingList.Add(brep);
                }

                var subtracted = Brep.CreateBooleanDifference(new List<Brep> { productionRod }, subtractingList, tolerance);
                if (subtracted.Length > 0)
                {
                    productionRod = subtracted.First();
                }
            }

            var prodrodmesh = MeshUtilities.ConvertBrepToMesh(productionRod, true);
            return prodrodmesh;
        }

        private Mesh Union(Mesh mesh1, Mesh mesh2)
        {
            Mesh unioned;
            if (Booleans.PerformBooleanUnion(out unioned, mesh1, mesh2) && unioned.IsValid)
            {
                return unioned;
            }
            return null;
        }

        private Mesh GetScaffoldShape()
        {
            if (derivedScaffoldShape == null)
            {
                derivedScaffoldShape = implantDerivedEntities.GetScaffoldShape();
            }
            return derivedScaffoldShape.DuplicateMesh();
        }

        private bool IsBrepFaceParallelTo(Brep brep, Point3d testPoint, Vector3d vector)
        {
            var brepFace = brep.Faces[0];
            double u, v;
            if (brepFace.ClosestPoint(testPoint, out u, out v))
            {
                var direction = brepFace.NormalAt(u, v);
                if (direction.IsParallelTo(vector, 0.001) == 1)
                {
                    return true;
                }
            }
            return false;
        }

        private Mesh AddTaperedPartOfProductionRodToImplant(Mesh implant)
        {
            if (productionRodWithChamfer)
            {
                CreateProductionRodChamferPartIfNotCreated();
                var chamferMesh = MeshUtilities.ConvertBrepToMesh(productionRodChamferPart, true);
                var unionWithChamfer = Union(implant, chamferMesh);
                return unionWithChamfer;
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

        #endregion
    }
}
