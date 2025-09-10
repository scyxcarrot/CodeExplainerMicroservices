using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Core.Enumerators;
using IDS.Core.GUI;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;

namespace IDS.Amace.Operations
{
    /*
     * ScaffoldMaker provides functionality for scaffold creation
     */

    public class ScaffoldMaker
    {
        /**
         * Add the finalized scaffold to the document
         */

        public static bool CreateFinalizedScaffold(ImplantDirector director)
        {
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // If ScaffoldFinalized exists, nothing needs to be calculated
            if (objectManager.GetBuildingBlockId(IBB.ScaffoldFinalized) != Guid.Empty)
            {
                return true;
            }

            // Gather all inputs from the director
            Mesh scafVolume = objectManager.GetBuildingBlock(IBB.ScaffoldVolume).Geometry as Mesh;
            Mesh scafBottom = objectManager.GetBuildingBlock(IBB.ScaffoldBottom).Geometry as Mesh;
            ScrewManager screwManager = new ScrewManager(director.Document);
            Mesh mbvHoleSubtractors = screwManager.GetAllMbvHoleSubtractorsUnion();
            Mesh latCupSubtractor = director.cup.GetReamingVolumeMesh(director.cup.innerCupDiameter + 1.0); // a bit larger to avoid see through
            Mesh medCupSubtractor = director.cup.outerReamingVolumeMesh;
            Mesh reamedPelvis = objectManager.GetBuildingBlock(IBB.ReamedPelvis).Geometry as Mesh;

            // init out variables
            Mesh scaffoldFinished;

            // Create finished scaffold
            bool success = CreateFinalizedScaffold(scafVolume, scafBottom, latCupSubtractor, medCupSubtractor, reamedPelvis, mbvHoleSubtractors, out scaffoldFinished);
            if (!success)
            {
                return false;
            }

            // Add to document
            Guid scafId = objectManager.GetBuildingBlockId(IBB.ScaffoldFinalized);
            objectManager.SetBuildingBlock(IBB.ScaffoldFinalized, scaffoldFinished, scafId);

            // Success
            return true;
        }

        /**
         * Calculate the finalized scaffold
         */

        public static bool CreateFinalizedScaffold(Mesh scafVolume, Mesh scafBottom, Mesh latCupSubtractor, Mesh medCupSubtractor, Mesh reamedPelvis, Mesh mbvHoleSubtractors, out Mesh scaffoldFinished)
        {
            // Loader
            frmWaitbar waitbar = new frmWaitbar();
            waitbar.Title = "Creating final scaffold...";

            // init
            scaffoldFinished = new Mesh();
            bool success;
            Mesh wScafVolume;
            Mesh wScafBottom;
            Mesh wBoneSubtractor;

            try
            {
                waitbar.Show();
                waitbar.Increment(5);

                // -- Make the wBoneSubtractor -- \\ Wrap bottom skirt
                //opparams = new MDCKShrinkWrapParameters(0.3, 0.0, 0.7, false, true, false, false);
                success = Wrap.PerformWrap(new Mesh[] { scafBottom }, 0.3, 0.0, 0.7, false, true, false, false, out wScafBottom);
                if (!success)
                {
                    waitbar.ReportError("Scaffold bottom wrap failed");
                    return false;
                }
                waitbar.Increment(15);

                // Wrap bottom skirt and reamed bone
                //opparams = new MDCKShrinkWrapParameters(0.3, 0.0, 0.3, false, true, false, false);
                success = Wrap.PerformWrap(new Mesh[] { wScafBottom, reamedPelvis }, 0.3, 0.0, 0.3, false, true, false, false, out wBoneSubtractor);
                if (!success)
                {
                    waitbar.ReportError("Wrapping scaffold bottom and reamed bone failed.");
                    return false;
                }
                waitbar.Increment(15);

                // Subtract medCupSubtractor
                wBoneSubtractor = Booleans.PerformBooleanSubtraction(wBoneSubtractor, medCupSubtractor);
                if (!wBoneSubtractor.IsValid)
                {
                    waitbar.ReportError("Cup subtraction failed.");
                    return false;
                }
                waitbar.Increment(15);

                // -- Finish the scaffold -- \\ Wrap scafVolume
                //opparams = new MDCKShrinkWrapParameters(0.3, 0.0, 0.7, false, true, false, false);
                success = Wrap.PerformWrap(new Mesh[] { scafVolume }, 0.3, 0.0, 0.7, false, true, false, false, out wScafVolume);
                if (!success)
                {
                    waitbar.ReportError("Scaffold volume wrap failed.");
                    return false;
                }
                waitbar.Increment(15);

                // Subtract the wBoneSubtractor
                scaffoldFinished = Booleans.PerformBooleanSubtraction(wScafVolume, wBoneSubtractor);
                if (!scaffoldFinished.IsValid)
                {
                    waitbar.ReportError("Bone subtraction failed.");
                    return false;
                }
                waitbar.Increment(15);

                // Subtract the lateral cup subtractor
                scaffoldFinished = Booleans.PerformBooleanSubtraction(scaffoldFinished, latCupSubtractor);
                if (!scaffoldFinished.IsValid)
                {
                    waitbar.ReportError("Lateral cup subtraction failed.");
                    return false;
                }
                waitbar.Increment(10);

                // Subtract the mbv hole subtractor
                scaffoldFinished = Booleans.PerformBooleanSubtraction(scaffoldFinished, mbvHoleSubtractors);
                if (!scaffoldFinished.IsValid)
                {
                    waitbar.ReportError("MBV hole subtraction failed.");
                    return false;
                }
                waitbar.Increment(10);

                // success
                waitbar.Close();
                return true;
            }
            catch
            {
                waitbar.ReportError("Could not create final scaffold");
                return false;
            }
        }

        public static bool CreateScaffold(ImplantDirector director)
        {
            // Init
            Mesh scaffoldTop;
            Mesh scaffoldBottom;
            Mesh scaffoldVolume;

            // Create it
            var rc = CreateScaffold(director, out scaffoldBottom, out scaffoldTop, out scaffoldVolume);
            if (!rc)
            {
                return false;
            }

            var objectManager = new AmaceObjectManager(director);

            // Add to / replace in document
            var topId = objectManager.GetBuildingBlockId(IBB.ScaffoldTop);
            objectManager.SetBuildingBlock(IBB.ScaffoldTop, scaffoldTop, topId);
            var bottomId = objectManager.GetBuildingBlockId(IBB.ScaffoldBottom);
            objectManager.SetBuildingBlock(IBB.ScaffoldBottom, scaffoldBottom, bottomId);
            var scaffoldId = objectManager.GetBuildingBlockId(IBB.ScaffoldVolume);
            objectManager.SetBuildingBlock(IBB.ScaffoldVolume, scaffoldVolume, scaffoldId);

            // Delete dependencies
            var dependencies = new Dependencies();
            dependencies.DeleteBlockDependencies(director, IBB.ScaffoldVolume);

            // Success
            return true;
        }

        public static bool CreateScaffold(ImplantDirector director, out Mesh scaffoldBottom, out Mesh scaffoldTop, out Mesh scaffoldVolume)
        {
            // Loader
            var waitbar = new frmWaitbar
            {
                Title = "Creating scaffold..."
            };

            // Initialize outputs
            scaffoldTop = null;
            scaffoldBottom = null;
            scaffoldVolume = null;

            // Get the rhino doc
            var doc = director.Document;
            var objectManager = new AmaceObjectManager(director);

            // Get required objects
            var cup = objectManager.GetBuildingBlock(IBB.Cup) as Cup;
            var shootDir = director.InsertionDirection;
            var supportObj = objectManager.GetBuildingBlock(IBB.ScaffoldSupport) as MeshObject;
            var skirtMeshObj = objectManager.GetBuildingBlock(IBB.SkirtMesh) as MeshObject;

            // Check if all required objects are there
            if (null == supportObj || null == skirtMeshObj || null == cup)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Missing object to create scaffold.");
                return false;
            }
            var support = supportObj.MeshGeometry;
            scaffoldTop = skirtMeshObj.MeshGeometry;

            try
            {
                waitbar.Show();
                waitbar.Increment(20);

                // Compute scaffold bottom using TPS
                ////////////////////////////////////
                var rc = ComputeScaffoldBottomHoleFill(doc, cup.orientation, shootDir, support, scaffoldTop, out scaffoldBottom);
                if (!rc)
                {
                    waitbar.ReportError("Scaffold bottom calculation failed.");
                    return false;
                }
                waitbar.Increment(70);

                // Stitch top to bottom
                ///////////////////////
                scaffoldVolume = scaffoldBottom.DuplicateMesh();
                scaffoldVolume.Append(scaffoldTop);
                waitbar.Increment(10);

                // Cleanup
                waitbar.Close();
                return true;
            }
            catch
            {
                waitbar.ReportError("Could not create scaffold");
                return false;
            }
        }

        /// \todo Clean up
        /**
         * Compute the scaffold bottom surface by first hole filling the top,
         * remeshing the newly craeted surface (filled hole), and TPS
         * transforming using landmarks in the defect.
         */

        public static bool ComputeScaffoldBottomHoleFill(RhinoDoc doc, Vector3d holeFillVector, Vector3d shootDir, Mesh support, Mesh scaffoldTop, out Mesh scaffoldBottom)
        {
            const double additionalOffset = 0.0;
            return MeshOperations.ComputeBottomHoleFill(doc, holeFillVector, shootDir, additionalOffset, support, scaffoldTop, out scaffoldBottom);
        }
    }
}