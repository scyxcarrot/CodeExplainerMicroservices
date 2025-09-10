using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.GUI;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;

namespace IDS.Amace.Operations
{
    /// <summary>
    /// Provides functionality to create implant geometry
    /// </summary>
    public class PlateMaker
    {
        /// <summary>
        /// Creates the implant for quality control.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool CreateImplantForQualityControl(ImplantDirector director)
        {
            // init
            bool success;

            // Loader
            var waitbar = new frmWaitbar();
            waitbar.Title = "Creating flanges...";
            waitbar.FixedStep = 20;

            try
            {
                waitbar.Show();
                waitbar.Increment();

                // Create the flanges
                var wrapBumpsInTopResolution = 1.0;
                success = CreateFlanges(director, wrapBumpsInTopResolution);
                if (!success)
                {
                    waitbar.ReportError("Could not create flanges.");
                    return false;
                }

                // Create the flat plate
                waitbar.Increment("Creating solid plate");
                success = CreateCupWithFlanges(director);
                if (!success)
                {
                    waitbar.ReportError("Could not create flat plate.");
                    return false;
                }


                //Make this if missing, important for optimization
                //as Plate Holes is not recreated for ImplantQC export.
                 var screwManager = new ScrewManager(director.Document);
                 screwManager.CreateMissingTrimmedMedialBumps();

                // Create the plate with screw  bumps
                waitbar.Increment("Creating bump plate");
                success = CreatePlateWithScrewBumps(director);
                if (!success)
                {
                    waitbar.ReportError("Could not create bumps plate.");
                    return false;
                }

                // Plate with screw bumps and screw holes
                waitbar.Increment("Creating hole plate");
                success = CreatePlateWithScrewBumpsAndHoles(director);
                if (!success)
                {
                    waitbar.ReportError("Could not create holes plate.");
                    return false;
                }

                // Success
                waitbar.Increment();
                waitbar.Close();
                return true;
            }
            catch
            {
                waitbar.ReportError("Could not create plates.");
                return false;
            }
        }

        /// <summary>
        /// Creates the cup with flanges.
        /// </summary>
        /// <param name="flanges">The flanges.</param>
        /// <param name="filledCup">The filled cup.</param>
        /// <param name="cupSubtractor">The cup subtractor.</param>
        /// <returns></returns>
        /// <exception cref="IDSOperationFailed">Could not create cup with flanges.</exception>
        private static Mesh CreateCupWithFlanges(Mesh flanges, Mesh filledCup, Mesh cupSubtractor)
        {
            // Union filled cup and solid plate
            Mesh filledCupWithFlanges = null;
            Mesh cupWithFlanges = null;
            try
            {
                // Union filled cup and flanges
                filledCupWithFlanges = UnionFilledCupAndFlanges(filledCup, flanges);
                // Subtract inner cup
                cupWithFlanges = SubtractInnerCup(filledCupWithFlanges, cupSubtractor);
            }
            catch (IDSOperationFailed e)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, e.Message);
                throw new IDSOperationFailed("Could not create cup with flanges.");
            }

            // Success
            return cupWithFlanges;
        }

        /// <summary>
        /// Creates the plate with screw bumps.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        private static bool CreatePlateWithScrewBumps(ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);

            // If everything exists, nothing needs to be calculated
            if (Guid.Empty != objectManager.GetBuildingBlockId(IBB.PlateBumps))
            {
                return true;
            }

            // Gather all inputs
            var filledCup = director.cup.filledCupMesh;
            var cupSubtractor = director.cup.innerReamingVolumeMesh;
            var solidPlate = objectManager.GetBuildingBlock(IBB.SolidPlate).Geometry as Mesh;
            var boolBumps = GetBooleanBumps(director);

            // init out variables
            Mesh bumpsPlate;

            // Create the flat plate
            var success = CreatePlateWithScrewBumps(solidPlate, filledCup, cupSubtractor, boolBumps, out bumpsPlate);
            if (!success)
            {
                return false;
            }

            // Add to document
            Guid bumpsPlateId = objectManager.GetBuildingBlockId(IBB.PlateBumps);
            objectManager.SetBuildingBlock(IBB.PlateBumps, bumpsPlate, bumpsPlateId);

            // success
            return true;
        }

        private static bool CreatePlateWithScrewBumps(Mesh flanges, Mesh filledCup, Mesh innerCupSubtractor, List<Mesh> trimmedBumps, out Mesh cupWithFlangesAndBumps)
        {
            // Union filled cup and solid plate
            Mesh filledCupAndFlanges = null;
            Mesh filledCupAndFlangesAndBumps = null;
            cupWithFlangesAndBumps = null;
            try
            {
                filledCupAndFlanges = UnionFilledCupAndFlanges(filledCup, flanges);
                filledCupAndFlangesAndBumps = UnionBumpsInImplant(trimmedBumps, filledCupAndFlanges);
                cupWithFlangesAndBumps = SubtractInnerCup(filledCupAndFlangesAndBumps, innerCupSubtractor);
            }
            catch (IDSOperationFailed e)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, e.Message);
                return false;
            }

            // success
            return true;
        }

        /// <summary>
        /// Cuts the flange bottom from bottom wrap.
        /// </summary>
        /// <param name="bottomContour">The bottom contour.</param>
        /// <param name="bottomWrap">The bottom wrap.</param>
        /// <param name="bottomSurface">The bottom surface.</param>
        /// <returns></returns>
        private static bool CutFlangeBottomFromBottomWrap(Curve bottomContour, Mesh bottomWrap, out Mesh bottomSurface)
        {
            bool success = MeshOperations.SplitMeshWithCurveMdck(bottomWrap, bottomContour, out bottomSurface);

            if (!success)
            {
                return false;
            }

            bottomSurface.Flip(true, true, true);
            return true;
        }

        /// <summary>
        /// Creates the flanges.
        /// </summary>
        /// <param name="bottomContour">The bottom contour.</param>
        /// <param name="bottomWrap">The bottom wrap.</param>
        /// <param name="topContour">The top contour.</param>
        /// <param name="topWrap">The top wrap.</param>
        /// <param name="wrapBumps">The wrap bumps.</param>
        /// <param name="wrapBumpsInTopResolution">The wrap bumps in top resolution.</param>
        /// <param name="topSurface">The top surface.</param>
        /// <param name="bottomSurface">The bottom surface.</param>
        /// <param name="sideSurface">The side surface.</param>
        /// <param name="solidPlate">The solid plate.</param>
        /// <exception cref="IDSOperationFailed">
        /// Cutting bottom mesh patch using MatSDK failed.
        /// or
        /// Cutting top mesh patch using MatSDK failed.
        /// or
        /// Something went wrong during stitching! Aborting.
        /// </exception>
        private static void CreateFlanges(Curve bottomContour, Mesh bottomWrap, Curve topContour, Mesh topWrap, List<Mesh> wrapBumps, double wrapBumpsInTopResolution, out Mesh topSurface, out Mesh bottomSurface, out Mesh sideSurface, out Mesh solidPlate)
        {
            // init
            topSurface = null;
            bottomSurface = null;
            sideSurface = null;
            solidPlate = null;

            // Cut out bottom surface and flip normals
            bool createdSolidPlateBottom = CutFlangeBottomFromBottomWrap(bottomContour, bottomWrap, out bottomSurface);
            if (!createdSolidPlateBottom)
            {
                throw new IDSOperationFailed("Cutting bottom mesh patch using MatSDK failed.");
            }

            // Cut out top surface
            bool createdSolidPlateTop = CutFlangeTopFromTopWrap(topContour, topWrap, wrapBumps, wrapBumpsInTopResolution, out topSurface);
            if (!createdSolidPlateTop)
            {
                throw new IDSOperationFailed("Cutting top mesh patch using MatSDK failed.");
            }

            // Create the side by stiching top and bottom
            sideSurface = StitchFlangeTopToBottom(topSurface, bottomSurface);

            // Merge the top,side and bottom into the solidPlate
            solidPlate = MergeFlangeSurfaces(topSurface, bottomSurface, sideSurface);
        }

        /// <summary>
        /// Cuts the flange top from top wrap.
        /// </summary>
        /// <param name="topContour">The top contour.</param>
        /// <param name="topWrap">The top wrap.</param>
        /// <param name="wrapBumps">The wrap bumps.</param>
        /// <param name="wrapBumpsInTopResolution">The wrap bumps in top resolution.</param>
        /// <param name="topSurface">The top surface.</param>
        /// <returns></returns>
        private static bool CutFlangeTopFromTopWrap(Curve topContour, Mesh topWrap, List<Mesh> wrapBumps, double wrapBumpsInTopResolution, out Mesh topSurface)
        {
            topSurface = null;
            bool success = false;

            try
            {
                Mesh topWrapWithBumps = WrapBumpsInTopWrap(wrapBumps, topWrap, wrapBumpsInTopResolution);
                success = MeshOperations.SplitMeshWithCurveMdck(topWrapWithBumps, topContour, out topSurface);
            }
            catch (IDSOperationFailed e)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, e.Message);
            }

            return success;
        }

        /// <summary>
        /// Creates the cup with flanges.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        private static bool CreateCupWithFlanges(ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);

            // If everything exists, nothing needs to be calculated
            if (Guid.Empty != objectManager.GetBuildingBlockId(IBB.PlateFlat))
            {
                return true;
            }

            // Gather all inputs
            Mesh filledCup = director.cup.filledCupMesh;
            Mesh cupSubtractor = director.cup.innerReamingVolumeMesh;
            Mesh flanges = objectManager.GetBuildingBlock(IBB.SolidPlate).Geometry as Mesh;

            // Create the flat plate
            Mesh cupWithFlanges = null;
            try
            {
                cupWithFlanges = CreateCupWithFlanges(flanges, filledCup, cupSubtractor);
            }
            catch (IDSOperationFailed e)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, e.Message);
                return false;
            }

            // Add to document
            Guid flatPlateId = objectManager.GetBuildingBlockId(IBB.PlateFlat);
            objectManager.SetBuildingBlock(IBB.PlateFlat, cupWithFlanges, flatPlateId);

            // success
            return true;
        }

        /// <summary>
        /// Creates the plate with screw bumps and holes.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        private static bool CreatePlateWithScrewBumpsAndHoles(ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);

            // If everything exists, nothing needs to be calculated
            if (Guid.Empty != objectManager.GetBuildingBlockId(IBB.PlateHoles))
            {
                return true;
            }

            // Gather all inputs
            var bumpsPlate = objectManager.GetBuildingBlock(IBB.PlateBumps).Geometry as Mesh;
            var screwManager = new ScrewManager(director.Document);
            var screwHoleSubtractors = screwManager.GetAllScrewHoleSubtractorsUnion();

            // init out variables
            Mesh holesPlate;

            // Create the plate with holes
            var success = CreatePlateWithScrewBumpsAndHoles(bumpsPlate, screwHoleSubtractors, out holesPlate);
            if (!success)
            {
                return false;
            }

            // Add to document
            var holesPlateId = objectManager.GetBuildingBlockId(IBB.PlateHoles);
            objectManager.SetBuildingBlock(IBB.PlateHoles, holesPlate, holesPlateId);

            // success
            return true;
        }

        /// <summary>
        /// Creates the plate with screw bumps and holes.
        /// </summary>
        /// <param name="bumpsPlate">The bumps plate.</param>
        /// <param name="screwHoleSubtractors">The screw hole subtractors.</param>
        /// <param name="holesPlate">The holes plate.</param>
        /// <returns></returns>
        private static bool CreatePlateWithScrewBumpsAndHoles(Mesh bumpsPlate, Mesh screwHoleSubtractors, out Mesh holesPlate)
        {
            // Subtract screw hole subtractors from plate
            holesPlate = Booleans.PerformBooleanSubtraction(bumpsPlate, screwHoleSubtractors);
            if (!holesPlate.IsValid)
            {
                return false;
            }

            // success
            return true;
        }

        /// <summary>
        /// Creates the qc approved plate.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool CreateQcApprovedPlate(ImplantDirector director)
        {
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // If everything exists, nothing needs to be calculated
            bool allThere = Guid.Empty != objectManager.GetBuildingBlockId(IBB.PlateSmoothBumps) && Guid.Empty != objectManager.GetBuildingBlockId(IBB.PlateSmoothHoles);
            if (allThere)
            {
                return true;
            }

            // Gather all inputs
            Curve topContour = objectManager.GetBuildingBlock(IBB.PlateContourTop).Geometry as Curve;
            Mesh topBallpark = objectManager.GetBuildingBlock(IBB.WrapTop).Geometry as Mesh;
            Curve bottomContour = objectManager.GetBuildingBlock(IBB.PlateContourBottom).Geometry as Curve;
            Mesh bottomBallpark = objectManager.GetBuildingBlock(IBB.WrapBottom).Geometry as Mesh;
            ScrewManager screwManager = new ScrewManager(director.Document);
            Mesh screwHoleSubtractors = screwManager.GetAllScrewHoleSubtractorsUnion();
            Mesh filledCup = director.cup.filledCupMesh;
            Mesh cupSubtractor = director.cup.innerReamingVolumeMesh;

            List<Mesh> boolBumps = GetBooleanBumps(director);
            List<Mesh> wrapBumps = GetWrapBumps(director);

            // init out variables
            Mesh smoothPlateBumps;
            Mesh smoothPlateHoles;
            Mesh solidPlateRounded;

            // Create the smooth plate
            double smoothingInfluenceDistance = GetSmoothingInfluenceDistance(director.PlateThickness);
            bool success = CreateSmoothPlates(topContour,
                                        topBallpark,
                                        bottomContour,
                                        bottomBallpark,
                                        filledCup,
                                        cupSubtractor,
                                        screwHoleSubtractors,
                                        wrapBumps,
                                        boolBumps,
                                        smoothingInfluenceDistance,
                                        out smoothPlateBumps,
                                        out smoothPlateHoles,
                                        out solidPlateRounded);

            // Add plate entities to project if created correctly
            if (success)
            {
                // Add to document
                var smoothPlateBumpsId = objectManager.GetBuildingBlockId(IBB.PlateSmoothBumps);
                objectManager.SetBuildingBlock(IBB.PlateSmoothBumps, smoothPlateBumps, smoothPlateBumpsId);
                var smoothPlateHolesId = objectManager.GetBuildingBlockId(IBB.PlateSmoothHoles);
                objectManager.SetBuildingBlock(IBB.PlateSmoothHoles, smoothPlateHoles, smoothPlateHolesId);
                var filledCupId = objectManager.GetBuildingBlockId(IBB.FilledSolidCup);
                objectManager.SetBuildingBlock(IBB.FilledSolidCup, filledCup, filledCupId);
                var solidPlateRoundedId = objectManager.GetBuildingBlockId(IBB.SolidPlateRounded);
                objectManager.SetBuildingBlock(IBB.SolidPlateRounded, solidPlateRounded, solidPlateRoundedId);
            }

            return success;
        }

        /// <summary>
        /// Gets the smoothing influence distance.
        /// </summary>
        /// <param name="plateThickness">The plate thickness.</param>
        /// <returns></returns>
        private static double GetSmoothingInfluenceDistance(double plateThickness)
        {
            double smoothingInfluenceDistance = 1.0;

            if (plateThickness <= 3.0)
            {
                smoothingInfluenceDistance = 1.0;
            }
            else if (plateThickness <= 4.0)
            {
                smoothingInfluenceDistance = 1.5;
            }
            else
            {
                smoothingInfluenceDistance = 1.5;
            }

            return smoothingInfluenceDistance;
        }

        /// <summary>
        /// Gets the wrap bumps.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        private static List<Mesh> GetWrapBumps(ImplantDirector director)
        {
            List<Mesh> wrapBumps = new List<Mesh>();
            ScrewManager screwManager = new ScrewManager(director.Document);
            wrapBumps.AddRange(screwManager.GetLateralBumps());
            wrapBumps.AddRange(screwManager.GetMedialBumpsInRegion(ScrewPosition.Cup, fillEmpty: false));

            return wrapBumps;
        }

        /// <summary>
        /// Gets the boolean bumps.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        private static List<Mesh> GetBooleanBumps(ImplantDirector director)
        {
            List<Mesh> booleanBumps = new List<Mesh>();
            ScrewManager screwManager = new ScrewManager(director.Document);
            booleanBumps.AddRange(screwManager.GetTrimmedMedialBumpsInRegion(ScrewPosition.Flange, fillEmpty: false));
            booleanBumps.AddRange(screwManager.GetMedialBumpsInRegion(ScrewPosition.Cup, fillEmpty: false));

            return booleanBumps;
        }

        /// <summary>
        /// Creates the smooth plates.
        /// </summary>
        /// <param name="topContour">The top contour.</param>
        /// <param name="topWrap">The top wrap.</param>
        /// <param name="bottomContour">The bottom contour.</param>
        /// <param name="bottomWrap">The bottom wrap.</param>
        /// <param name="filledCup">The filled cup.</param>
        /// <param name="innerCupSubtractor">The inner cup subtractor.</param>
        /// <param name="screwHoleSubtractors">The screw hole subtractors.</param>
        /// <param name="wrapBumps">The wrap bumps.</param>
        /// <param name="bumps">The bumps.</param>
        /// <param name="edgeRadiusTop">The edge radius top.</param>
        /// <param name="smoothPlateBumps">The smooth plate bumps.</param>
        /// <param name="smoothPlateHoles">The smooth plate holes.</param>
        /// <param name="solidPlateSmooth">The solid plate smooth.</param>
        /// <returns></returns>
        private static bool CreateSmoothPlates( Curve topContour, 
                                                Mesh topWrap, 
                                                Curve bottomContour, 
                                                Mesh bottomWrap, 
                                                Mesh filledCup, 
                                                Mesh innerCupSubtractor, 
                                                Mesh screwHoleSubtractors, 
                                                List<Mesh> wrapBumps, 
                                                List<Mesh> bumps, 
                                                double edgeRadiusTop,
                                                out Mesh smoothPlateBumps, 
                                                out Mesh smoothPlateHoles, 
                                                out Mesh solidPlateSmooth)
        {
            // Initialize
            Mesh solidTop;
            Mesh solidBottom;
            Mesh solidSide;
            Mesh solidPlate;
            smoothPlateBumps = null;
            smoothPlateHoles = null;
            solidPlateSmooth = null;

            // Loader
            frmWaitbar waitbar = new frmWaitbar();
            waitbar.Title = "Creating smooth plate...";
            waitbar.FixedStep = 15;

            try
            {
                waitbar.Show();
                waitbar.Increment(5);

                // Cutout the solid plate
                double wrapBumpsInTopResolution = 0.3;
                CreateFlanges(bottomContour, bottomWrap, topContour, topWrap, wrapBumps, wrapBumpsInTopResolution, out solidTop, out solidBottom, out solidSide, out solidPlate);
                waitbar.Increment();

                // Round the edges
                solidPlateSmooth = SmoothFlangeEdges(solidTop, solidSide, solidBottom, edgeRadiusTop);
                waitbar.Increment();

                // Union filled cup and rounded solid plate
                Mesh smoothPlateAndFilledCup = UnionFilledCupAndFlanges(filledCup, solidPlateSmooth);
                waitbar.Increment();

                // Union bumps
                smoothPlateBumps = UnionBumpsInImplant(bumps, smoothPlateAndFilledCup);
                waitbar.Increment();

                // Subtract inner cup
                smoothPlateBumps = SubtractInnerCup(smoothPlateBumps, innerCupSubtractor);
                waitbar.Increment();

                // Subtract screw hole subtractors from plate
                smoothPlateHoles = SubtractScrewHoles(smoothPlateBumps, screwHoleSubtractors);
                waitbar.Increment();

                // Success
                waitbar.Close();
                return true;
            }
            catch (IDSOperationFailed e)
            {
                waitbar.ReportError(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Creates the flanges.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="wrapBumpsInTopResolution">The wrap bumps in top resolution.</param>
        /// <returns></returns>
        public static bool CreateFlanges(ImplantDirector director, double wrapBumpsInTopResolution)
        {
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // If everything exists, nothing needs to be calculated
            bool allRequiredBlocksExist = objectManager.GetBuildingBlockId(IBB.SolidPlateBottom) != Guid.Empty &&
                                            objectManager.GetBuildingBlockId(IBB.SolidPlateTop) != Guid.Empty &&
                                            objectManager.GetBuildingBlockId(IBB.SolidPlateSide) != Guid.Empty &&
                                            objectManager.GetBuildingBlockId(IBB.SolidPlate) != Guid.Empty;
            if (allRequiredBlocksExist)
            {
                return true;
            }

            // Gather all inputs from director
            Curve contourTop = objectManager.GetBuildingBlock(IBB.PlateContourTop).Geometry as Curve;
            Mesh wrapTop = objectManager.GetBuildingBlock(IBB.WrapTop).Geometry as Mesh;
            Curve contourBottom = objectManager.GetBuildingBlock(IBB.PlateContourBottom).Geometry as Curve;
            Mesh wrapBottom = objectManager.GetBuildingBlock(IBB.WrapBottom).Geometry as Mesh;
            List<Mesh> wrapBumps = GetWrapBumps(director);

            // init out variables
            Mesh flangesTop = null;
            Mesh flangesBottom = null;
            Mesh flangesSide = null;
            Mesh flanges = null;

            // Create the solid plate
            try
            {
                CreateFlanges(contourBottom, wrapBottom, contourTop, wrapTop, wrapBumps, wrapBumpsInTopResolution, out flangesTop, out flangesBottom, out flangesSide, out flanges);
            }
            catch (IDSOperationFailed e)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, e.Message);
                return false;
            }

            // Add solid plate parts to document
            Guid bottomId = objectManager.GetBuildingBlockId(IBB.SolidPlateBottom);
            objectManager.SetBuildingBlock(IBB.SolidPlateBottom, flangesBottom, bottomId);
            Guid topId = objectManager.GetBuildingBlockId(IBB.SolidPlateTop);
            objectManager.SetBuildingBlock(IBB.SolidPlateTop, flangesTop, topId);
            Guid sideId = objectManager.GetBuildingBlockId(IBB.SolidPlateSide);
            objectManager.SetBuildingBlock(IBB.SolidPlateSide, flangesSide, sideId);
            Guid solidId = objectManager.GetBuildingBlockId(IBB.SolidPlate);
            objectManager.SetBuildingBlock(IBB.SolidPlate, flanges, solidId);

            // success
            return true;
        }

        /// <summary>
        /// Merges the flange surfaces.
        /// </summary>
        /// <param name="flangesTop">The flanges top.</param>
        /// <param name="flangesBottom">The flanges bottom.</param>
        /// <param name="flangesSide">The flanges side.</param>
        /// <returns></returns>
        private static Mesh MergeFlangeSurfaces(Mesh flangesTop, Mesh flangesBottom, Mesh flangesSide)
        {
            Mesh flanges = flangesTop.DuplicateMesh();
            flanges.Append(flangesBottom);
            flanges.Append(flangesSide);
            flanges.Vertices.CombineIdentical(true, true);
            flanges.Normals.ComputeNormals();
            flanges.Compact();

            return flanges;
        }

        /// <summary>
        /// Smoothes the flange edges.
        /// </summary>
        /// <param name="flangesTop">The flanges top.</param>
        /// <param name="flangesSide">The flanges side.</param>
        /// <param name="flangesBottom">The flanges bottom.</param>
        /// <param name="edgeRadiusTop">The edge radius top.</param>
        /// <returns></returns>
        /// <exception cref="IDSOperationFailed">Could not smooth flange edges.</exception>
        private static Mesh SmoothFlangeEdges(Mesh flangesTop, Mesh flangesSide, Mesh flangesBottom, double edgeRadiusTop)
        {
            // Initialize
            double edgeRadiusBottom = 1;
            double topMinEdgeLength = 0.25;
            double topMaxEdgeLength = 0.5;
            double bottomMinEdgeLength = 0.125;
            double bottomMaxEdgeLength = 0.25;

            var flangesRounded = ExternalToolInterop.SmoothImplantEdges(flangesTop, flangesSide,
                flangesBottom, edgeRadiusTop, edgeRadiusBottom, topMinEdgeLength,
                topMaxEdgeLength, bottomMinEdgeLength, bottomMaxEdgeLength);

            if (flangesRounded == null)
            {
                throw new IDSOperationFailed("Could not smooth flange edges.");
            }

            return flangesRounded;
        }

        /// <summary>
        /// Stitches the flange top to bottom.
        /// </summary>
        /// <param name="flangesTop">The flanges top.</param>
        /// <param name="flangesBottom">The flanges bottom.</param>
        /// <param name="flangesSide">The flanges side.</param>
        /// <returns></returns>
        private static Mesh StitchFlangeTopToBottom(Mesh flangesTop, Mesh flangesBottom)
        {
            return MeshOperations.StitchMeshSurfaces(flangesTop, flangesBottom, false);
        }

        /// <summary>
        /// Subtracts the inner cup.
        /// </summary>
        /// <param name="implant">The implant.</param>
        /// <param name="innerCupSubtractor">The inner cup subtractor.</param>
        /// <returns></returns>
        /// <exception cref="IDSOperationFailed">Could not subtract inner cup.</exception>
        private static Mesh SubtractInnerCup(Mesh implant, Mesh innerCupSubtractor)
        {
            Mesh implantMinusInnerCup = null;

            implantMinusInnerCup = Booleans.PerformBooleanSubtraction(implant, innerCupSubtractor);
            if (!implantMinusInnerCup.IsValid)
            {
                throw new IDSOperationFailed("Could not subtract inner cup.");
            }

            return implantMinusInnerCup;
        }

        /// <summary>
        /// Subtracts the screw holes.
        /// </summary>
        /// <param name="implant">The implant.</param>
        /// <param name="screwHoleSubtractors">The screw hole subtractors.</param>
        /// <returns></returns>
        /// <exception cref="IDSOperationFailed">Could not subtract screwholes.</exception>
        private static Mesh SubtractScrewHoles(Mesh implant, Mesh screwHoleSubtractors)
        {
            Mesh smoothPlateHoles = null;

            smoothPlateHoles = Booleans.PerformBooleanSubtraction(implant, screwHoleSubtractors);
            if (!smoothPlateHoles.IsValid)
            {
                throw new IDSOperationFailed("Could not subtract screwholes.");
            }

            return smoothPlateHoles;
        }

        /// <summary>
        /// Unions the bumps in implant.
        /// </summary>
        /// <param name="bumps">The bumps.</param>
        /// <param name="implant">The implant.</param>
        /// <returns></returns>
        /// <exception cref="IDSOperationFailed">Could not union bumps in implant.</exception>
        private static Mesh UnionBumpsInImplant(List<Mesh> bumps, Mesh implant)
        {
            if (bumps.Count <= 0)
            {
                return null;
            }

            Mesh implantAndBumps = null;

            List<Mesh> boolParts = new List<Mesh>();
            boolParts.AddRange(bumps);
            boolParts.Add(implant);

            bool unionedBumps = Booleans.PerformBooleanUnion(out implantAndBumps, boolParts.ToArray());
            if (!unionedBumps)
            {
                throw new IDSOperationFailed("Could not union bumps in implant.");
            }

            return implantAndBumps;
        }

        /// <summary>
        /// Unions the filled cup and flanges.
        /// </summary>
        /// <param name="filledCup">The filled cup.</param>
        /// <param name="flanges">The flanges.</param>
        /// <returns></returns>
        /// <exception cref="IDSOperationFailed">Could not union filled cup and flanges.</exception>
        private static Mesh UnionFilledCupAndFlanges(Mesh filledCup, Mesh flanges)
        {
            Mesh smoothPlateBumps;

            bool unionedFilledCup = Booleans.PerformBooleanUnion(out smoothPlateBumps, new Mesh[] { filledCup, flanges });
            if (!unionedFilledCup)
            {
                throw new IDSOperationFailed("Could not union filled cup and flanges.");
            }

            return smoothPlateBumps;
        }

        /// <summary>
        /// Wraps the bumps in top wrap.
        /// </summary>
        /// <param name="wrapBumps">The wrap bumps.</param>
        /// <param name="topWrap">The top wrap.</param>
        /// <param name="resolution">The resolution.</param>
        /// <returns></returns>
        /// <exception cref="IDSOperationFailed">Could not wrap bumps in top wrap.</exception>
        private static Mesh WrapBumpsInTopWrap(List<Mesh> wrapBumps, Mesh topWrap, double resolution)
        {
            // Initialize
            Mesh topWrapWithBumpsWrapped;
            // Combine wrap and bumps
            List<Mesh> topWrapWithBumps = wrapBumps;
            topWrapWithBumps.Add(topWrap);
            // Set wrap parameters
            //MDCKShrinkWrapParameters opparams = new MDCKShrinkWrapParameters(resolution, 4.0, 0.0, false, true, false, false);
            // Wrap
            bool bumpsWrappedInTopWrap = Wrap.PerformWrap(wrapBumps.ToArray(), resolution, 4.0, 0.0, false, true, false, false, out topWrapWithBumpsWrapped);
            if (!bumpsWrappedInTopWrap)
            {
                throw new IDSOperationFailed("Could not wrap bumps in top wrap.");
            }
            
            return topWrapWithBumpsWrapped;
        }
    }
}