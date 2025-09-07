using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.Helper;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Drawing
{
    public class ImplantConduit : DisplayConduit, IDisposable
    {
        private List<Brep> _dotPreviewPastilleBreps;
        private List<Brep> _dotPreviewPastilleSphereBreps;
        private List<IDot> _dotPreview;
        public List<IDot> DotPreview
        {
            get { return _dotPreview; }
            set
            {
                _dotPreview = value;

                var pastilles = _dotPreview.Where(dot => dot is DotPastille).Cast<DotPastille>();
                _dotPreviewPastilleBreps = new List<Brep>();
                _dotPreviewPastilleSphereBreps = new List<Brep>();

                foreach (var pastille in pastilles)
                {
                    var direction = DataModelUtilities.GetAverageDirection(ConnectionPreview, pastille);
                    var p = _pastilleBrepFactory.CreatePastille(pastille, direction);
                    var s = Brep.CreateFromSphere(ScrewUtilities.CreateScrewSphere(pastille, ScrewDiameter));

                    _dotPreviewPastilleBreps.Add(p);
                    _dotPreviewPastilleSphereBreps.Add(s);
                }
            }
        }

        public IDot PointPreview { get; set; }
        public IConnection LinePreview { get; set; }

        private List<Brep> _connectionBreps;
        private List<Line> _connectionPreviewLines;
        private List<IConnection> _connectionPreview;
        public List<IConnection> ConnectionPreview
        {
            get { return _connectionPreview; }
            set
            {
                _connectionPreview = value;

                _connectionPreviewLines = new List<Line>();
                _connectionBreps = new List<Brep>();
                foreach (var connection in ConnectionPreview)
                {
                    var l = DataModelUtilities.CreateLine(connection.A.Location, connection.B.Location);
                    _connectionPreviewLines.Add(l);
                    _connectionBreps.Add(ConnectionBrepFactory.CreateConnection(connection));
                }
            }
        }

        public Color LineColor { get; set; }
        public Color PointColor { get; set; }
        public int LineThickness { get; set; }
        public int ControlPointSize { get; set; }
        public double ScrewDiameter { get; set; }
        public bool IsUseLinesForConnectionPreview { get; set; }
        public double PastilleDiameter { get; set; }

        protected CMFImplantDirector implantDirector;

        private readonly PastilleBrepFactory _pastilleBrepFactory;
        private DisplayMaterial _connectionDisplayMaterial;
        private readonly DisplayMaterial _sphereDisplayMaterial;
        private readonly bool _hasImplantSupport = false;

        private readonly List<NurbsCurve> _implantRoiBorders;

        public ImplantConduit(CMFImplantDirector director)
        {
            implantDirector = director;
            LinePreview = null;
            ConnectionPreview = new List<IConnection>();
            PointPreview = null;
            DotPreview = new List<IDot>();

            SetLineColorToDefault();
            PointColor = Color.Crimson;

            LineThickness = 3;
            ControlPointSize = 5;
            ScrewDiameter = 2.0;

            _pastilleBrepFactory = new PastilleBrepFactory();
            _connectionDisplayMaterial = new DisplayMaterial(Colors.PlateTemporary);
            _sphereDisplayMaterial = new DisplayMaterial(Color.White);

            IsUseLinesForConnectionPreview = false;

            var objManager = new CMFObjectManager(implantDirector);
            _hasImplantSupport = objManager.HasBuildingBlock(IDS.CMF.ImplantBuildingBlocks.IBB.ImplantSupport);
            _implantRoiBorders = DisplayConduitProvider.GetConduit<ImplantSurfaceRoIVisualizer>().SelectMany(x => x.RoiSurfaceBorders).ToList();

        }

        public void SetLineColorToDefault()
        {
            LineColor = Color.Aqua;
        }

        public void SetConnectionColor(Color connectionColor)
        {
            _connectionDisplayMaterial = new DisplayMaterial(connectionColor);
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            if (implantDirector.CurrentDesignPhase == IDS.CMF.Enumerators.DesignPhase.Planning ||
               implantDirector.CurrentDesignPhase == IDS.CMF.Enumerators.DesignPhase.Implant)
            {
                DrawLine(e.Display);
                DrawPoint(e.Display);
                DrawTheLine(e.Display);
            }
            DrawControlPoint(e.Display);
        }
        protected override void PreDrawObjects(DrawEventArgs e)
        {
            if (implantDirector.CurrentDesignPhase == IDS.CMF.Enumerators.DesignPhase.Implant)
            {
                DrawConnectionPreview(e.Display);
            }            
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            if (implantDirector.CurrentDesignPhase == IDS.CMF.Enumerators.DesignPhase.Planning)
            {
                DrawConnections(e.Display);
                DrawDots(e.Display);
            }
        }

        private void DrawLine(DisplayPipeline p)
        {
            if (IsUseLinesForConnectionPreview && ConnectionPreview.Any())
            {
                _connectionPreviewLines.ForEach(x =>
                {
                    p.DrawLine(x, Color.Magenta, 10);
                });
            }
        }

        private void DrawTheLine(DisplayPipeline p)
        {
            if (LinePreview != null)
            {
                var line = DataModelUtilities.CreateLine(LinePreview.A.Location, LinePreview.B.Location);
                if (LinePreview is ConnectionLink)
                {
                    p.DrawDottedLine(line, LineColor);
                }
                else if (LinePreview is ConnectionPlate)
                {
                    p.DrawLine(line, LineColor, LineThickness);
                }
            }
        }

        private void DrawConnectionPreview(DisplayPipeline p)
        {
            var casePreferenceDatas = implantDirector.CasePrefManager.CasePreferences;
            foreach (var caseData in casePreferenceDatas)
            {
                if (!caseData.IsActive)
                {
                    var curves = implantDirector.ImplantManager.GetConnectionsBuildingBlockCurves(caseData);
                    foreach (var connection in caseData.ImplantDataModel.ConnectionList)
                    {
                        var trimmedCurve = CurveUtilities.Trim(curves, RhinoPoint3dConverter.ToPoint3d(connection.A.Location), RhinoPoint3dConverter.ToPoint3d(connection.B.Location));
                        if (trimmedCurve == null)
                        {
                            continue;
                        }

                        Point3d pointFrustum;
                        p.Viewport.GetFrustumCenter(out pointFrustum);
                        double pixelPerUnit;
                        p.Viewport.GetWorldToScreenScale(pointFrustum, out pixelPerUnit);
                        p.DrawCurve(trimmedCurve, Color.Red, Convert.ToInt32(connection.Width * pixelPerUnit));
                    }
                }
            }

            DrawActiveImplantConnections(p);
        }

        private void DrawActiveImplantConnections(DisplayPipeline p)
        {
            if (ConnectionPreview.Any())
            {
                var dotLists = ConnectionUtilities.CreateDotCluster(ConnectionPreview);

                foreach (var dotList in dotLists)
                {
                    var points = dotList.Select(x => RhinoPoint3dConverter.ToPoint3d(x.Location)).ToList();
                    var curve = CurveUtilities.BuildCurve(points, 3, false);
                    Point3d pointFrustum;
                    p.Viewport.GetFrustumCenter(out pointFrustum);
                    double pixelPerUnit;
                    p.Viewport.GetWorldToScreenScale(pointFrustum, out pixelPerUnit);

                    if (dotList.Count == 2)
                    {
                        foreach (var conn in ConnectionPreview)
                        {
                            if ((conn.A == dotList.First() || conn.A == dotList.Last()) && (conn.B == dotList.First() || conn.B == dotList.Last()))
                            {
                                var color = (conn is ConnectionPlate) ? Color.Blue : Color.Green;
                                p.DrawCurve(curve, color, Convert.ToInt32(conn.Width * pixelPerUnit));
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < dotList.Count - 1; i++)
                        {
                            foreach (var conn in ConnectionPreview)
                            {
                                if ((conn.A == dotList[i] || conn.A == dotList[i + 1]) && (conn.B == dotList[i] || conn.B == dotList[i + 1]))
                                {
                                    var trimmedCurve = CurveUtilities.Trim(curve, RhinoPoint3dConverter.ToPoint3d(dotList[i].Location), RhinoPoint3dConverter.ToPoint3d(dotList[i + 1].Location));
                                    var color = (conn is ConnectionPlate) ? Color.Blue : Color.Green;
                                    p.DrawCurve(trimmedCurve, color, Convert.ToInt32(conn.Width * pixelPerUnit));
                                    break;
                                }
                            }
                        }
                    }

                }
            }
        }

        private void DrawConnections(DisplayPipeline p)
        {
            if (IsUseLinesForConnectionPreview)
            {
                return;
            }

            if (ConnectionPreview.Any())
            {
                _connectionBreps.ForEach(x =>
                {
                    p.DrawBrepShaded(x, _connectionDisplayMaterial);

                });
            }
        }

        private void DrawPoint(DisplayPipeline p)
        {
            if (PointPreview != null)
            {

                var pt = RhinoPoint3dConverter.ToPoint3d(PointPreview.Location);

                if (PointPreview is DotControlPoint)
                {
                    p.DrawPoint(pt, PointStyle.ControlPoint, PointColor, Color.White, ControlPointSize, 3,3,0 ,true,true);
                }
                else if (PointPreview is DotPastille pastille) //Safety Region
                {
                    p.DrawSphere(ScrewUtilities.CreateScrewSphere(pastille, StaticValues.SafetyRegionRadius*2), PointColor);
                }

                DrawRoITriggerWarning(p, pt);
            }
        }

        private void DrawRoITriggerWarning(DisplayPipeline p, Point3d pt)
        {
            if (_hasImplantSupport)
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
                                PastilleDiameter);
                        if (dist < triggerDist)
                        {
                            var tmpVecUnitize = closestPt - pt;
                            tmpVecUnitize.Unitize();
                            var loc = closestPt + (tmpVecUnitize * 3);

                            p.DrawDot(loc, "Too near!\nRoI will be\nregenerated", Color.Red, Color.White);
                            p.DrawLineArrow(new Line(pt, closestPt), Color.Yellow, 4, 0.3);
                            p.DrawLineArrow(new Line(pt, closestPt), Color.Black, 7, 0.3);
                        }
                    }
                });
            }
        }

        private void DrawDots(DisplayPipeline p)
        {
            if (DotPreview.Any())
            {
                var controlPoints = DotPreview.Where(dot => dot is DotControlPoint);
                p.DrawPoints(controlPoints.Select(dot => RhinoPoint3dConverter.ToPoint3d(dot.Location)), PointStyle.ControlPoint, ControlPointSize, PointColor);

                _dotPreviewPastilleBreps.ForEach(x =>
                {
                    p.DrawBrepShaded(x, _connectionDisplayMaterial);
                });

                _dotPreviewPastilleSphereBreps.ForEach(x =>
                {
                    p.DrawBrepShaded(x, _sphereDisplayMaterial);
                });

            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connectionDisplayMaterial.Dispose();
                _sphereDisplayMaterial.Dispose();
            }
        }

        private void DrawControlPoint(DisplayPipeline p)
        {
            if (!DotPreview.Any())
            {
                return;
            }
            
            foreach (var point in DotPreview)
            {
                var pointColor = (point is DotControlPoint) ? PointColor : Color.Yellow;
                var pointStyle = (point is DotControlPoint) ? PointStyle.ControlPoint : PointStyle.Simple;

                if (pointStyle == PointStyle.ControlPoint)
                {
                    p.DrawPoint(RhinoPoint3dConverter.ToPoint3d(point.Location), pointStyle, pointColor, Color.White, ControlPointSize, 3, 3, 0, true, true);
                }
                else
                {
                    p.DrawPoint(RhinoPoint3dConverter.ToPoint3d(point.Location), pointStyle, ControlPointSize, pointColor);
                }
            }
        }

        public Brep GeneratePlanningBrep()
        {
            var res = new Brep();
            _connectionBreps.ForEach(x => res.Append(x));
            _dotPreviewPastilleBreps.ForEach(x => { res.Append(x); });

            var allSpheres = new Brep();
            _dotPreviewPastilleSphereBreps.ForEach(x => allSpheres.Append(x));

            var subtracted = Brep.CreateBooleanDifference(res, allSpheres, 0.001);
            if (subtracted != null)
            {
                var unioned = Brep.CreateBooleanUnion(subtracted, 0.001);
                return BrepUtilities.Append(unioned);
            }

            return null;
        }

    }
}