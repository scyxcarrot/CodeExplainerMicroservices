using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Core.GUI;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Linq;


namespace IDS.Amace.Operations
{
    /// <summary>
    /// Functionality to create wraps of the bone necessary for the implant design.
    /// </summary>
    public class WrapMaker
    {
        /// <summary>
        /// Creates all wraps.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool CreateAllWraps(ImplantDirector director)
        {
            // Parameters
            var plateThickness = director.PlateThickness;
            var edgeLength = 2.0;
            var offsetMedialTop = 1.0; // medium wrap lies 1.0 mm below top
            var cupDesign = director.cup.cupType.CupDesign;
            var offsetTop = plateThickness + director.cup.CupRingPolishingOffset;
            var offsetMedial = plateThickness - offsetMedialTop;
            var plateBoneClearance = director.PlateClearance;

            // Create the waitbar
            var waitbar = new frmWaitbar
            {
                Title = "Making bottom wrap..."
            };
            waitbar.Show();

            try
            {
                waitbar.Increment(10); // to make sure it is shown

                // Bottom wrap
                Mesh wrapBottom;
                var success = CreateBottomWrap(director, plateBoneClearance, out wrapBottom);
                if (!success)
                {
                    waitbar.ReportError("Bottom wrap creation failed.");
                    return false;
                }
                AmaceObjectManager objectManager = new AmaceObjectManager(director);
                Guid wrapBottomId = objectManager.GetBuildingBlockId(IBB.WrapBottom);
                objectManager.SetBuildingBlock(IBB.WrapBottom, wrapBottom, wrapBottomId);
                waitbar.Increment(20, "Making top wrap...");

                // Top wrap
                Mesh wrapTop;
                success = CreateOffsetWrap(wrapBottom, offsetTop, out wrapTop);
                if (!success)
                {
                    waitbar.ReportError("Top wrap creation failed.");
                    return false;
                }
                success = MeshUtilities.Remesh(wrapTop, edgeLength, out wrapTop);
                Guid wrapTopId = objectManager.GetBuildingBlockId(IBB.WrapTop);
                objectManager.SetBuildingBlock(IBB.WrapTop, wrapTop, wrapTopId);
                waitbar.Increment(20, "Making trimming wrap...");

                // Trimming wrap
                Mesh wrapScrewBump;
                success = CreateOffsetWrap(wrapBottom, offsetMedial, out wrapScrewBump);
                if (!success)
                {
                    waitbar.ReportError("Trimming wrap creation failed.");
                    return false;
                }
                Guid wrapTrimId = objectManager.GetBuildingBlockId(IBB.WrapScrewBump);
                objectManager.SetBuildingBlock(IBB.WrapScrewBump, wrapScrewBump, wrapTrimId);
                waitbar.Increment(20, "Making screws wrap...");

                // Countersunk wrap
                Mesh wrapSunkScrew;
                success = CreateSunkScrewWrap(director, out wrapSunkScrew);
                if (!success)
                {
                    waitbar.ReportError("Screw wrap creation failed.");
                    return false;
                }
                Guid wrapSunkId = objectManager.GetBuildingBlockId(IBB.WrapSunkScrew);
                objectManager.SetBuildingBlock(IBB.WrapSunkScrew, wrapSunkScrew, wrapSunkId);

                // Update plate contours
                waitbar.Increment(20, "Updating plate contours (if any)...");
                Dependencies dependencies = new Dependencies();
                dependencies.ProjectPlateContoursToWraps(director);

                // Update screws
                waitbar.Increment(5, "Updating screws (if any)...");
                UpdateScrews(director);

                // Clean up and end
                waitbar.Increment(5);
                waitbar.Close();
                return true;
            }
            catch
            {
                waitbar.ReportError("Could not create wraps.");
                return false;
            }
        }

        /// <summary>
        /// Updates the screws.
        /// </summary>
        /// <param name="director">The director.</param>
        private static void UpdateScrews(ImplantDirector director)
        {
            var screwManager = new ScrewManager(director.Document);
            var screws = screwManager.GetAllScrews().ToList();
            foreach (var screw in screws)
            {
                var axialOffset = screw.AxialOffset;
                screw.CalibrateScrewHead();
                screw.AxialOffset = axialOffset;
                screw.Update();
            }
        }

        /// <summary>
        /// Creates the bottom wrap.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="bottomWrap">The bottom wrap.</param>
        /// <returns></returns>
        public static bool CreateBottomWrap(ImplantDirector director, double offset, out Mesh bottomWrap)
        {
            // init
            RhinoDoc doc = director.Document;
            bottomWrap = new Mesh();
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // Get meshes to be wrapped
            Mesh pelvis = (Mesh)doc.Objects.Find(objectManager.GetBuildingBlockId(IBB.ReamedPelvis)).Geometry;
            Mesh scaffold = (Mesh)doc.Objects.Find(objectManager.GetBuildingBlockId(IBB.ScaffoldVolume)).Geometry;

            // Define the wrap parameters
            double detail = 0.5;
            double gapdist = 5.0;
            //MDCKShrinkWrapParameters opparams = new MDCKShrinkWrapParameters(detail, gapdist, offset, false, true, false, false);

            // Create bottom ballpark using wrap
            var success = Wrap.PerformWrap(new [] { pelvis, scaffold }, detail, gapdist, offset, false, true, false, false, out bottomWrap);

            /// \todo Is this necessary? Force garbage collection (clean up MatSDK objects)
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            if (!success)
                return false;

            /// \todo Is this necessary? Detect disjoint pieces after wrap (caused by sharp edges near transition surface - cup)
            int nb_parts = bottomWrap.DisjointMeshCount;
            if (nb_parts > 1)
            {
                RhinoApp.WriteLine("Multiple disjoint parts have been detected in the resulting mesh. Attempting to remove smaller parts...");
                Mesh[] parts = bottomWrap.SplitDisjointPieces();
                double[] areas = new double[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    AreaMassProperties areamass = AreaMassProperties.Compute(parts[i]);
                    areas[i] = areamass.Area;
                }
                int largest_idx = areas.ToList().IndexOf(areas.Max());
                bottomWrap = parts[largest_idx];
            }

            // success
            return true;
        }

        /// <summary>
        /// Creates an offset wrap.
        /// </summary>
        /// <param name="sourceMesh">The source mesh.</param>
        /// <param name="plateThi">The plate thi.</param>
        /// <param name="topWrap">The top wrap.</param>
        /// <returns></returns>
        public static bool CreateOffsetWrap(Mesh sourceMesh, double plateThi, out Mesh topWrap)
        {
            // init
            topWrap = new Mesh();

            // Define the wrap parameters
            double detail = 1.0;
            double gapdist = 5.0;
            //MDCKShrinkWrapParameters wrapParams = new MDCKShrinkWrapParameters(detail, gapdist, plateThi, false, true, false, false);

            // Do the wrap operation
            bool success = Wrap.PerformWrap(new Mesh[] { sourceMesh }, detail, gapdist, plateThi, false, true, false, false, out topWrap);
            if (!success)
            {
                return false;
            }

            // Success
            return true;
        }

        /// <summary>
        /// Creates the sunk screw wrap.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="wrapSunkScrew">The wrap sunk screw.</param>
        /// <returns></returns>
        public static bool CreateSunkScrewWrap(ImplantDirector director, out Mesh wrapSunkScrew)
        {
            // init
            wrapSunkScrew = new Mesh();
            Mesh WrapTop;
            Mesh innerCupReamer;
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // Gather objects from director
            WrapTop = objectManager.GetBuildingBlock(IBB.WrapTop).Geometry as Mesh;
            innerCupReamer = director.cup.innerReamingVolumeMesh;

            // subtract
            wrapSunkScrew = Booleans.PerformBooleanSubtraction(WrapTop, innerCupReamer);
            if (!wrapSunkScrew.IsValid)
            {
                return false;
            }

            // success
            return true;
        }
    }
}