using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Helper;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;

#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.PICMF.Operations
{
    public abstract class TranslateScrewBase : IDisposable
    {
        protected readonly CMFImplantDirector director;
        protected Screw referenceScrew;
        protected Point3d movingPoint;
        protected Brep screwPreview;
        protected readonly DisplayMaterial screwMaterial;
        protected bool drawSphereConduit;
        protected readonly double length;
        public Mesh LowLoDSupportMesh { get; set; }

        protected ScrewGaugeConduit _gaugeConduit;

        protected CasePreferenceDataModel _casePreferenceDataModel = null;
        protected bool _needDoubleRecalibration = false;
        private List<NurbsCurve> _implantRoiBorders;

        protected TranslateScrewBase(Screw screw)
        {
            director = screw.Director;
            referenceScrew = screw;
            movingPoint = screw.HeadPoint;
            drawSphereConduit = true;

            screwPreview = screw.Geometry.Duplicate() as Brep;
            screwMaterial = new DisplayMaterial(Colors.ScrewTemporary, 0.75);
            length = (referenceScrew.HeadPoint - referenceScrew.TipPoint).Length;

            var gauges = ScrewGaugeUtilities.CreateScrewGauges(screw, screw.ScrewType);
            _gaugeConduit = new ScrewGaugeConduit(gauges);
        }

        public virtual Result Translate()
        {
            _gaugeConduit.Enabled = true;
            ConduitUtilities.RefeshConduit();
            var res = TranslateToPoint();
            _gaugeConduit.Enabled = false;
            return res;
        }

        private Result TranslateToPoint()
        {
            _implantRoiBorders = DisplayConduitProvider.GetConduit<ImplantSurfaceRoIVisualizer>().SelectMany(x => x.RoiSurfaceBorders).ToList();
            RhinoApp.KeyboardEvent += OnKeyboard;

            var get = new GetPoint();
            get.SetCommandPrompt("Click on a point to translate screw, + and - to adjust safety region radius.");
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Safety region radius = {StaticValues.SafetyRegionRadius}");
            get.PermitObjectSnap(false);
            get.DynamicDraw += DynamicDraw;
            get.AcceptNothing(true); // accept ENTER to confirm
            get.EnableTransparentCommands(false);
            var cancelled = false;
            while (true)
            {
                var get_res = get.Get(); // function only returns after clicking
                if (get_res == GetResult.Cancel)
                {
                    cancelled = true;
                    break;
                }

                if (get_res == GetResult.Point)
                {
                    if (UpdateScrew(get.Point()))
                    {
                        break;
                    }
                }
            }
            get.DynamicDraw -= DynamicDraw;

            RhinoApp.KeyboardEvent -= OnKeyboard;

            return cancelled ? Result.Cancel : Result.Success;
        }

        protected virtual void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            Transform transform = Transform.Unset;
            var pointOnLowLoD = GetPointOnConstraint(e.CurrentPoint, e.Viewport.CameraLocation, 
                e.Viewport.CameraDirection, LowLoDSupportMesh);
            if (pointOnLowLoD != Point3d.Unset)
            {
                var tipPoint = pointOnLowLoD + referenceScrew.Direction * length;

                var tmpScrew = new Screw(referenceScrew.Director,
                    pointOnLowLoD,
                    tipPoint,
                    referenceScrew.ScrewAideDictionary, referenceScrew.Index, referenceScrew.ScrewType, referenceScrew.BarrelType);

                var calibratedScrew = CalibratePreviewScrew(tmpScrew);
                
                if (calibratedScrew != null)
                {
                    transform = Transform.Translation(calibratedScrew.HeadPoint - movingPoint);
                    movingPoint = calibratedScrew.HeadPoint;
                    screwPreview.Transform(transform);
                }
            }

            OnScrewPreviewDynamicDrawUpdated(sender, e, transform);

            e.Display.DrawBrepShaded(screwPreview, screwMaterial);

            if (_casePreferenceDataModel != null)
            {
                DrawRoITriggerWarning(e.Display, movingPoint);
            }

            e.Display.DrawSphere(new Sphere(movingPoint, StaticValues.SafetyRegionRadius), Color.Red);
        }

        private void DrawRoITriggerWarning(DisplayPipeline p, Point3d pt)
        {
            _implantRoiBorders.ForEach(c =>
            {
                double param;
                if (c.ClosestPoint(pt, out param))
                {
                    var closestPt = c.PointAt(param);
                    var dist = closestPt.DistanceTo(pt);
                    var triggerDist =
                        ImplantCreationUtilities.GetImplantPointCheckRoICreationTriggerTolerance(
                            _casePreferenceDataModel.CasePrefData.PastilleDiameter);
                    if (dist < triggerDist)
                    {
                        var tmpVecUnitize = closestPt - pt;
                        tmpVecUnitize.Unitize();
                        var loc = closestPt + (tmpVecUnitize * 3);

                        p.DrawLineArrow(new Line(pt, closestPt), Color.Black, 7, 0.3);
                        p.DrawLineArrow(new Line(pt, closestPt), Color.Yellow, 4, 0.3);
                        p.DrawDot(loc, "Too near!\nRoI will be\nregenerated", Color.Red, Color.White);
                    }
                }
            });
        }

        protected virtual void OnScrewPreviewDynamicDrawUpdated(object sender, GetPointDrawEventArgs e, Transform calibratedTransform)
        {
            _gaugeConduit.GaugesData.ForEach(x =>
            {
                if (calibratedTransform != Transform.Unset)
                {
                    x.Gauge.Transform(calibratedTransform);
                }
            });

            if (calibratedTransform != Transform.Unset)
            {
                ConduitUtilities.RefeshConduit();
            }
        }

        protected Point3d GetPointOnConstraint(Point3d currentPoint, Point3d cameraLocation, Vector3d cameraDirection, Mesh constraintMesh)
        {
            var points = Intersection.ProjectPointsToMeshes(new List<Mesh> { constraintMesh }, new List<Point3d> { currentPoint }, cameraDirection, 0.0);
            if (points != null && points.Any())
            {
                //get the nearest point to camera
                var projectedPoint = points.OrderBy(point => point.DistanceTo(cameraLocation)).First();
                return projectedPoint;
            }
            Mouse.SetCursor(Cursors.No);
            return Point3d.Unset;
        }

        protected abstract Screw CalibratePreviewScrew(Screw originScrew);

        protected abstract Screw CalibrateActualScrew(Screw originScrew);

        protected abstract bool UpdateBuildingBlock(Screw calibratedScrew);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                screwMaterial.Dispose();
            }
        }

        protected virtual Point3d GetFinalizedPickedPoint(Point3d currentPoint, Point3d cameraLocation, Vector3d cameraDirection)
        {
            return GetPointOnConstraint(currentPoint, cameraLocation, cameraDirection, LowLoDSupportMesh);
        }

        protected virtual bool UpdateScrew(Point3d toPoint)
        {
            var updated = false;

            var finalPickedPoint = GetFinalizedPickedPoint(toPoint, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection);
            if (finalPickedPoint != Point3d.Unset)
            {
                var headPoint = finalPickedPoint;
                var tipPoint = headPoint + referenceScrew.Direction * length;

                // Check if leveling can be done before replacing the old screw by the updated screw
                var screw = new Screw(referenceScrew.Director,
                    headPoint,
                    tipPoint,
                    referenceScrew.ScrewAideDictionary, referenceScrew.Index, referenceScrew.ScrewType, referenceScrew.BarrelType);

#if (INTERNAL)
                if (CMFImplantDirector.IsDebugMode)
                {
                    InternalUtilities.AddObject(referenceScrew.BrepGeometry, "Testing::ReferenceScrew");
                    InternalUtilities.AddObject(referenceScrew.GetScrewContainer(), "Testing::ReferenceScrewContainer");
                    InternalUtilities.AddObject(screw.BrepGeometry, "Testing::TranslatedScrew");
                    InternalUtilities.AddObject(screw.GetScrewContainer(), "Testing::TranslatedScrewContainer");
                }
#endif

                var calibratedScrew = CalibrateActualScrew(screw);
                if (calibratedScrew == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Failed to calibrate");
                    return false;
                }

                updated = UpdateBuildingBlock(calibratedScrew);

#if (INTERNAL)
                if (CMFImplantDirector.IsDebugMode)
                {
                    InternalUtilities.AddObject(calibratedScrew.BrepGeometry, "Testing::CalibratedTranslatedScrew");
                    InternalUtilities.AddObject(calibratedScrew.GetScrewContainer(), "Testing::CalibratedTranslatedScrewContainer");
                }
#endif
            }

            director.Document.Views.Redraw();

            return updated;
        }

        // Get the key state
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetKeyState(int keyCode);

        // Detect if a keyboard key is down
        private static bool IsKeyDown(int key)
        {
            short retVal = GetKeyState(key);

            //If the high-order bit is 1, the key is down
            //otherwise, it is up.
            if ((retVal & 0x8000) == 0x8000)
            {
                return true;
            }
            //If the low-order bit is 1, the key is toggled.
            //if ((retVal & 1) == 1)
            return false;
        }

        protected void OnKeyboard(int key)
        {
            // Only execute if key is down (avoid triggering on key release)
            if (!IsKeyDown(key))
                return;

            switch (key)
            {
                case (187):
                case (107):
                    double max = StaticValues.SafetyRegionMaxRadius;
                    if (StaticValues.SafetyRegionRadius < max)
                    {
                        StaticValues.SafetyRegionRadius += 0.1;
                        if (StaticValues.SafetyRegionRadius > max) //Possible due to precision
                        {
                            StaticValues.SafetyRegionRadius = max;
                        }
                        IDSPluginHelper.WriteLine(LogCategory.Default, $"Safety region radius increased to { StaticValues.SafetyRegionRadius.ToString("0.0", CultureInfo.InvariantCulture) } mm");
                    }
                    else
                    {
                        StaticValues.SafetyRegionRadius = max;
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"Maximum radius { StaticValues.SafetyRegionRadius.ToString("0.0", CultureInfo.InvariantCulture) } mm reached!");
                    }

                    director.Document.Views.Redraw();
                    
                    break;
                case (189):
                case (109):
                    double min = StaticValues.SafetyRegionMinRadius;
                    if (StaticValues.SafetyRegionRadius > min)
                    {
                        StaticValues.SafetyRegionRadius -= 0.1;
                        if (StaticValues.SafetyRegionRadius < min) //Possible due to precision
                        {
                            StaticValues.SafetyRegionRadius = min;
                        }
                        IDSPluginHelper.WriteLine(LogCategory.Default, $"Safety region radius decreased to { StaticValues.SafetyRegionRadius.ToString("0.0", CultureInfo.InvariantCulture) } mm");
                    }
                    else
                    {
                        StaticValues.SafetyRegionRadius = min;
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"Minimum radius { StaticValues.SafetyRegionRadius.ToString("0.0", CultureInfo.InvariantCulture) } mm reached!");
                    }

                    director.Document.Views.Redraw();
                    
                    break;
                default:
                    return; // nothing to do
            }

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }
    }
}
