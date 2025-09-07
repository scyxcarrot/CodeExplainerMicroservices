using IDS.Amace.CommandHelpers;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Preferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Amace.Relations
{
    /// <summary>
    /// Dependency handling functions
    /// </summary>
    public partial class Dependencies : Core.Relations.Dependencies<ImplantDirector>
    {
        private Dictionary<IBB, IBB[]> deleteableIBBs = new Dictionary<IBB, IBB[]>
        {
            {IBB.BoneGraft, new IBB[] { IBB.BoneGraftRemaining,IBB.AdditionalRbvGraft, IBB.CupRbvGraft}},
            {IBB.Cup, new IBB[] { IBB.CupStuds,IBB.SkirtGuide }},
            {IBB.CupReamingEntity, new IBB[] { IBB.ReamedPelvis, IBB.CupReamedPelvis, IBB.CupRbv,
                IBB.AdditionalRbv, IBB.OriginalReamedPelvis, IBB.TotalRbv, IBB.AdditionalRbvGraft,
                IBB.CupRbvGraft, IBB.BoneGraftRemaining}},
            {IBB.DesignPelvis, new IBB[] {IBB.ReamedPelvis, IBB.CupRbv, IBB.AdditionalRbv,
                IBB.SkirtMesh, IBB.OriginalReamedPelvis, IBB.TotalRbv}},
            {IBB.ExtraReamingEntity, new IBB[] {IBB.ReamedPelvis, IBB.AdditionalRbv,
                IBB.AdditionalRbvGraft, IBB.OriginalReamedPelvis, IBB.TotalRbv}},
            {IBB.LateralCupSubtractor, new IBB[] { IBB.WrapSunkScrew}},
            {IBB.PlateContourBottom, new IBB[] {IBB.SolidPlateBottom}},
            {IBB.PlateContourTop, new IBB[] {IBB.SolidPlateTop}},
            {IBB.ReamedPelvis, new IBB[] {IBB.ScaffoldSupport}},
            {IBB.ScaffoldSupport, new IBB[] {IBB.ScaffoldBottom}},
            {IBB.SkirtMesh, new IBB[] {IBB.ScaffoldTop}},
            {IBB.ScaffoldTop, new IBB[] {IBB.ScaffoldVolume}},
            {IBB.ScaffoldBottom, new IBB[] {IBB.ScaffoldVolume}},
            {IBB.ScaffoldVolume, new IBB[] {IBB.WrapBottom, IBB.WrapTop, IBB.WrapScrewBump,
                IBB.WrapSunkScrew, IBB.ScaffoldFinalized}},
            {IBB.Screw, new IBB[] {IBB.SolidPlateTop, IBB.ScaffoldFinalized, IBB.CupStuds}},
            {IBB.SkirtBoneCurve, new IBB[] {IBB.SkirtMesh}},
            {IBB.SkirtCupCurve, new IBB[] {IBB.SkirtMesh}},
            {IBB.SolidPlate, new IBB[] {IBB.PlateFlat}},
            {IBB.PlateFlat, new IBB[] {IBB.PlateBumps}},
            {IBB.PlateBumps, new IBB[] {IBB.PlateHoles}},
            {IBB.PlateHoles, new IBB[] {IBB.PlateSmoothBumps, IBB.PlateSmoothHoles, IBB.FilledSolidCup, IBB.SolidPlateRounded, IBB.TransitionPreview}},
            {IBB.SolidPlateBottom, new IBB[] {IBB.SolidPlateSide, IBB.PlateClearance}},
            {IBB.SolidPlateSide, new IBB[] {IBB.SolidPlate}},
            {IBB.SolidPlateTop, new IBB[] {IBB.SolidPlateSide}},
            {IBB.WrapBottom, new IBB[] {IBB.SolidPlateBottom, IBB.IntersectionEntity}},
            {IBB.WrapTop, new IBB[] {IBB.SolidPlateTop}},

            // Handled by other functions
            // Keep these as comments so that the dependency graph script can generate the complete graph
            
            // Function: Delete screw dependencies
            //{IBB.Screw, new IBB[] {IBB.MedialBump, IBB.LateralBump, IBB.ScrewContainer, IBB.ScrewHoleSubtractor, IBB.ScrewCushionSubtractor, IBB.ScrewMbvSubtractor}},
            
            // Function: Delete trimmed screw bumps
            //{IBB.LateralBump, new IBB[] {IBB.LateralBumpTrim}},
            //{IBB.MedialBump, new IBB[] {IBB.MedialBumpTrim}},

            // Function: Delete disconnected skirt guides
            //{IBB.SkirtCupCurve, new IBB[] {IBB.SkirtGuide}},
            //{IBB.SkirrtBoneCurve, new IBB[] {IBB.SkirtGuide}},
            
            // Function: Transform skirt cup curve
            //{IBB.Cup, new IBB[] {IBB.SkirtCupCurve}},
            
            // Function: project contours to bottom
            //{IBB.WrapBottom, new IBB[] {IBB.PlateContourBottom}},
            //{IBB.WrapTop, new IBB[] {IBB.PlateContourTop}},
        };

        public Dependencies()
        {
            // Automatically deleted
            deleteableDependencies = deleteableIBBs.ToDictionary(ibb => ibb.Key.ToString(), ibb => ibb.Value.Select(val => val.ToString()).ToArray());
        }

        protected override void DeleteBlockObjectDependencies(ImplantDirector director, string block)
        {
            if (block == IBB.PlateHoles.ToString() || block == IBB.TransitionPreview.ToString())
            {
                director.InvalidateFea();
            }
        }

        /// <summary>
        /// Deletes the trimmed bumps.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public bool DeleteTrimmedBumps(ImplantDirector director)
        {
            // Remove all screw aides from the document
            var screwManager = new ScrewManager(director.Document);
            var objectManager = new AmaceObjectManager(director);
            foreach (var screw in screwManager.GetAllScrews())
            {
                if (screw.ScrewAides.ContainsKey(ScrewAideType.LateralBumpTrim))
                {
                    objectManager.DeleteObject(screw.ScrewAides[ScrewAideType.LateralBumpTrim]);
                    screw.ScrewAides.Remove(ScrewAideType.LateralBumpTrim);
                }

                if (screw.ScrewAides.ContainsKey(ScrewAideType.MedialBumpTrim))
                {
                    objectManager.DeleteObject(screw.ScrewAides[ScrewAideType.MedialBumpTrim]);
                    screw.ScrewAides.Remove(ScrewAideType.MedialBumpTrim);
                }
            }

            return true;
        }

        /// <summary>
        /// Delete disconnected skirt guides when the cup-skirt or bone-skirt curve is edited
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public bool DeleteDisconnectedSkirtGuides(ImplantDirector director)
        {
            return DeleteDisconnectedGuides(director, BuildingBlocks.Blocks[IBB.SkirtCupCurve], BuildingBlocks.Blocks[IBB.SkirtBoneCurve], BuildingBlocks.Blocks[IBB.SkirtGuide]);
        }

        /// <summary>
        /// Scale the cup-skirt curve. Useful to make it match the cup
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="radiusDifference">The radius difference.</param>
        /// <param name="curveOffset">The curve offset.</param>
        /// <returns></returns>
        public bool ScaleCupSkirtCurve(ImplantDirector director, double radiusDifference, Vector3d curveOffset)
        {
            var objectManager = new AmaceObjectManager(director);

            // Variables
            var cupSkirtCurveId = objectManager.GetBuildingBlockId(IBB.SkirtCupCurve);
            if (cupSkirtCurveId == Guid.Empty)
            {
                return false;
            }

            var cupSkirtCurve = director.Document.Objects.Find(cupSkirtCurveId).Geometry as Curve;

            // Translate
            director.Document.Objects.Transform(cupSkirtCurveId, Transform.Translation(curveOffset), true);

            // Expand / contract curve
            var curvePlane = director.cup.GetRimPlaneAtAxialOffset(director.PlateThickness + director.PlateClearance);
            var expandedCurve = CurveUtilities.ExpandPlanarCurve(cupSkirtCurve, radiusDifference, curvePlane);

            // Update building block
            objectManager.SetBuildingBlock(IBB.SkirtCupCurve, expandedCurve, cupSkirtCurveId);

            // Snap to constraining curves
            SnapCupSkirtCurveToConstraints(director);

            // Delete dependencies
            DeleteBlockDependencies(director, IBB.SkirtCupCurve);

            return true;
        }

        /// <summary>
        /// Attract control points to the inner or outer limiting curves (the black curves that
        /// are shown when indicating the cup-skirt curve)
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public void SnapCupSkirtCurveToConstraints(ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);

            // Variables
            var cupSkirtCurveId = objectManager.GetBuildingBlockId(IBB.SkirtCupCurve);
            if (cupSkirtCurveId != Guid.Empty)
            {
                // Cup-skirt as nurbs
                var cupSkirtCurve = (Curve)director.Document.Objects.Find(cupSkirtCurveId).Geometry;
                var nurbs = cupSkirtCurve.ToNurbsCurve();
                // Inner and outer curves
                var innerConstraintCurve = director.cup.cupSkirtInnerCurve;
                var outerConstraintCurve = director.cup.cupSkirtOuterCurve;
                // Initialize point list
                var pointList = new List<Point3d>();
                // Find nearest constraining curve point for each cup-skirt curve point and attract
                for (var i = cupSkirtCurve.Degree - 1; i < nurbs.Knots.Count - (cupSkirtCurve.Degree - 1); i++)
                {
                    var point = nurbs.PointAt(nurbs.Knots[i]);
                    // Inner distance
                    double innerDistance;
                    var innerCurvePoint = GetClosestCurvePoint(innerConstraintCurve, point, out innerDistance);
                    // Outer distance
                    double outerDistance;
                    var outerCurvePoint = GetClosestCurvePoint(outerConstraintCurve, point, out outerDistance);
                    // Add nearest point
                    point = outerDistance < innerDistance ? outerCurvePoint : innerCurvePoint;

                    pointList.Add(point);
                }
                // Build the curve of attracted points
                pointList[pointList.Count - 1] = pointList[0]; // to avoid rounding errors
                var snappedCurve = CurveUtilities.BuildCurve(pointList, 3, true);

                // Update building block
                objectManager.SetBuildingBlock(IBB.SkirtCupCurve, snappedCurve, cupSkirtCurveId);
            }
            else
            {
                // do nothing
            }
        }

        /// <summary>
        /// Gets the closest curve point.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="point">The point.</param>
        /// <param name="distance">The distance.</param>
        /// <returns></returns>
        private static Point3d GetClosestCurvePoint(Curve curve, Point3d point, out double distance)
        {
            // Get closest point parameter
            double outerT;
            curve.ClosestPoint(point, out outerT);
            // Get actual point
            var curvePoint = curve.PointAt(outerT);
            // Get distance to actual point
            distance = (curvePoint - point).Length;

            return curvePoint;
        }

        /// <summary>
        /// Projects the plate contours to wraps.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public bool ProjectPlateContoursToWraps(ImplantDirector director)
        {
            ProjectBottomCurveToBottomWrap(director);
            ProjectTopCurveToTopWrap(director);

            // Success
            return true;
        }

        /// <summary>
        /// Projects the bottom curve to bottom wrap.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        private bool ProjectBottomCurveToBottomWrap(ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);

            if (objectManager.GetBuildingBlockId(IBB.PlateContourBottom) == Guid.Empty)
            {
                return false;
            }

            IDSPluginHelper.WriteLine(LogCategory.Default, "Updating existing plate bottom curve...");
            var wrapBottomObj = objectManager.GetBuildingBlock(IBB.WrapBottom) as MeshObject;
            var wrapBottom = wrapBottomObj.MeshGeometry;
            var contourBottomId = objectManager.GetBuildingBlockId(IBB.PlateContourBottom);
            var contourBottom = objectManager.GetBuildingBlock(IBB.PlateContourBottom).Geometry as Curve;
            var contourBottomNew = CurveUtilities.ProjectContourToMesh(wrapBottom, contourBottom);
            objectManager.SetBuildingBlock(IBB.PlateContourBottom, contourBottomNew, contourBottomId);
            DeleteBlockDependencies(director, IBB.PlateContourBottom);

            return true;
        }

        /// <summary>
        /// Projects the top curve to top wrap.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        private bool ProjectTopCurveToTopWrap(ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);

            if (objectManager.GetBuildingBlockId(IBB.PlateContourTop) == Guid.Empty)
            {
                return false;
            }

            IDSPluginHelper.WriteLine(LogCategory.Default, "Updating existing plate top curve.");
            var wrapTopObj = objectManager.GetBuildingBlock(IBB.WrapTop) as MeshObject;
            var wrapTop = wrapTopObj.MeshGeometry;
            var contourTopId = objectManager.GetBuildingBlockId(IBB.PlateContourTop);
            var contourTop = objectManager.GetBuildingBlock(IBB.PlateContourTop).Geometry as Curve;
            var contourTopNew = CurveUtilities.ProjectContourToMesh(wrapTop, contourTop);
            objectManager.SetBuildingBlock(IBB.PlateContourBottom, contourTopNew, contourTopId);
            DeleteBlockDependencies(director, IBB.PlateContourTop);

            return true;
        }

        public bool DeleteBlockDependencies(ImplantDirector director, IBB block)
        {
            return DeleteBlockDependencies(director, block.ToString());
        }

        public bool UpdateTransitionPreview(ImplantDirector director, bool invalidateFea)
        {
            //Make the Transition Preview
            var flangeTranstionParam = AmacePreferences.GetTransitionPreviewParameters();

            var flangeTransitionHelper = new FlangeTransitionCreationCommandHelper(director)
            {
                TransitionWrapResolution = flangeTranstionParam.FlangesTransitionParams.WrapOperationSmallestDetails,
                TransitionWrapOffset = flangeTranstionParam.FlangesTransitionParams.WrapOperationOffset,
                TransitionWrapGapClosingDistance = flangeTranstionParam.FlangesTransitionParams.WrapOperationGapClosingDistance,
                IsDoPostProcessing = flangeTranstionParam.IsDoPostProcessing
            };

            var objectManager = new AmaceObjectManager(director);
            var solidPlateMesh = (Mesh)objectManager.GetBuildingBlock(IBB.SolidPlate).Geometry;

            var updated = flangeTransitionHelper.CreateFlangeTransition(new[] { solidPlateMesh, director.cup.filledCupMesh }, IBB.TransitionPreview);

            if (updated && invalidateFea)
            {
                director.InvalidateFea();
            }

            return updated;
        }

        public void UpdateRoiContour(ImplantDirector director, Transform xform)
        {
            var objectManager = new AmaceObjectManager(director);
            var roiContourId = objectManager.GetBuildingBlockId(IBB.ROIContour);
            if (roiContourId == Guid.Empty)
            {
                return;
            }

            var roiContour = (Curve)objectManager.GetBuildingBlock(IBB.ROIContour).Geometry;

            var helper = new RegionOfInterestCommandHelper(objectManager);
            var currentPlane = helper.GetContourPlaneBasedOnRoiCurve(roiContourId, director.ContourPlane);
            currentPlane.Transform(xform);
            roiContour.Transform(xform);

            director.ContourPlane = currentPlane;
            helper.SetRoiContour(roiContour, director.ContourPlane, roiContourId);

            if (!objectManager.HasBuildingBlock(IBB.TransitionPreview))
            {
                return;
            }

            UpdateTransitionPreview(director, true);
        }
    }
}