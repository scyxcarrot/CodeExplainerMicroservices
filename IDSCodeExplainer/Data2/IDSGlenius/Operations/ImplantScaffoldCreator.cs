using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class ImplantScaffoldCreator
    {
        private readonly GleniusImplantDirector director;
        private readonly GleniusObjectManager objectManager;
        private readonly ImplantDerivedEntities implantDerivedEntities;
        private readonly SolidPartCreator solidPartCreator;
        private Mesh macroShape;
        private Mesh macroShapeMed;

        public ImplantScaffoldCreator(GleniusImplantDirector director, SolidPartCreator solidPartCreator)
        {
            this.director = director;
            this.solidPartCreator = solidPartCreator;
            objectManager = new GleniusObjectManager(director);
            implantDerivedEntities = new ImplantDerivedEntities(director);
        }

        private Mesh GenerateSolidPartForReportingBeforeSubtraction()
        {
            var solidPart = solidPartCreator.SolidPartForReportingBeforeSubtraction;
            if (solidPart == null)
            {
                Mesh temp;
                solidPartCreator.GetSolidPartForReporting(out temp);
                solidPart = solidPartCreator.SolidPartForReportingBeforeSubtraction;

                if (solidPart == null)
                {
                    throw new NullReferenceException("SolidPartForReportingBeforeSubtraction is null");
                }
            }

            return solidPart;
        }

        private Mesh GenerateSolidPartForFinalizationBeforeSubtraction()
        {
            var solidPart = solidPartCreator.SolidPartForFinalizationBeforeSubtraction;
            if (solidPart == null)
            {
                Mesh temp;
                solidPartCreator.GetSolidPartForFinalization(out temp);
                solidPart = solidPartCreator.SolidPartForFinalizationBeforeSubtraction;

                if (solidPart == null)
                {
                    throw new NullReferenceException("SolidPartForFinalizationBeforeSubtraction is null");
                }
            }

            return solidPart;
        }

        public bool GetScaffoldForReporting(out Mesh output)
        {
            var success = true;

            output = null;
            try
            {
                var solidPart = GenerateSolidPartForReportingBeforeSubtraction();
                output = solidPart.DuplicateMesh(); 

                Locking.UnlockImplantCreation(director.Document);

                var scaffoldCreation = ScaffoldCreation(solidPart);
                output = scaffoldCreation.DuplicateMesh();

                var subtract = SubtractPartsForReporting(scaffoldCreation);
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

        public bool GetScaffoldForFinalization(out Mesh output)
        {
            var success = true;

            output = null;
            try
            {
                var solidPart = GenerateSolidPartForFinalizationBeforeSubtraction();
                output = solidPart.DuplicateMesh();

                Locking.UnlockImplantCreation(director.Document);

                var scaffoldCreation = ScaffoldCreation(solidPart);
                output = scaffoldCreation.DuplicateMesh();

                var subtract = SubtractPartsForFinalization(scaffoldCreation);
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

        #region Helpers

        private void SetMacroShapeParts()
        {
            if (macroShape == null)
            {
                macroShape = solidPartCreator.GetMacroShape();
                if (macroShape == null)
                {
                    throw new NullReferenceException("Fail get MacroShape");
                }
            }

            if (macroShapeMed == null)
            {
                macroShapeMed = solidPartCreator.GetMacroShapeMed(macroShape);
                if (macroShapeMed == null)
                {
                    throw new NullReferenceException("Fail get MacroShapeMed");
                }
            }
        }

        private Mesh ScaffoldCreation(Mesh solidPart)
        {
            //var solidPartWrapParams = new MDCKShrinkWrapParameters(0.1, 0.0, 0.05, false, true, true, false);
            Mesh solidPartWrap;
            if (Wrap.PerformWrap(new[] { solidPart }, 0.1, 0.0, 0.05, false, true, true, false, out solidPartWrap))
            {
                SetMacroShapeParts();

                var subtract = Booleans.PerformBooleanSubtraction(macroShapeMed, solidPartWrap);
                if (subtract.IsValid)
                {
                    //var scaffoldSubtractWrapParams = new MDCKShrinkWrapParameters(0.1, 0.0, 0.25, false, true, true, false);
                    Mesh scaffoldSubtractWrap;
                    if (Wrap.PerformWrap(new[] { subtract }, 0.1, 0.0, 0.25, false, true, true, false, out scaffoldSubtractWrap))
                    {
                        return scaffoldSubtractWrap;
                    }
                    throw new NullReferenceException("Fail to wrap MacroShape subtract");
                }
                throw new NullReferenceException("Fail to Subtract MacroShape");
            }
            throw new NullReferenceException("Fail to wrap SolidPart");
        }

        private Mesh SubtractPartsForReporting(Mesh scaffoldPart)
        {
            var subtracting = new List<Mesh>();
            var screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(sc => sc as Screw);
            foreach (var screw in screws)
            {
                var brep = implantDerivedEntities.GetScrewHoleScaffold(screw);
                subtracting.Add(MeshUtilities.ConvertBrepToMesh(brep, true));
            }

            var conn = implantDerivedEntities.GetM4ConnectionScrewHoleReal();
            subtracting.Add(MeshUtilities.ConvertBrepToMesh(conn, true));

            Mesh unioned;
            if (Booleans.PerformBooleanUnion(out unioned, subtracting.ToArray()) && unioned.IsValid)
            {
                var subtract = Booleans.PerformBooleanSubtraction(scaffoldPart, unioned);
                if (subtract.IsValid)
                {
                    return subtract;
                }
            }
            throw new NullReferenceException("Fail to Subtract parts for Reporting");
        }

        private Mesh SubtractPartsForFinalization(Mesh scaffoldPart)
        {
            var subtracting = new List<Mesh>();

            var conn = implantDerivedEntities.GetM4ConnectionScrewHoleProduction();
            subtracting.Add(MeshUtilities.ConvertBrepToMesh(conn, true));

            Mesh unioned;
            if (Booleans.PerformBooleanUnion(out unioned, subtracting.ToArray()) && unioned.IsValid)
            {
                var subtract = Booleans.PerformBooleanSubtraction(scaffoldPart, unioned);
                if (subtract.IsValid)
                {
                    return subtract;
                }
            }
            throw new NullReferenceException("Fail to Subtract parts for Finalization");
        }

        #endregion
    }
}
