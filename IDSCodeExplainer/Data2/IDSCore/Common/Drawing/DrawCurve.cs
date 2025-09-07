using IDS.Core.Utilities;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IDS.Core.Drawing
{
    public class DrawCurve : GetCurvePoints
    {
        //delegates
        public delegate void OnDynamicDrawDelegate(Point3d currentPoint);
        public delegate void OnDynamicDrawPreRebuildCurveDelegate(Point3d currentPoint, int movingPointIndex);
        public delegate bool OnNewCurveAddPointDelegate(Point3d newPoint); //If return false, it will stop picking point
        public delegate void OnPointListChangedDelegate(List<Point3d> changedPointList);

        public OnDynamicDrawDelegate OnDynamicDrawing { get; set; }
        public OnDynamicDrawPreRebuildCurveDelegate OnDynamicDrawPreRebuildCurve { get; set; }
        public OnNewCurveAddPointDelegate OnNewCurveAddPoint { get; set; }
        public OnPointListChangedDelegate OnPointListChanged { get; set; }

        //public 

        //Constraints
        private Mesh _constraintMesh;

        public Mesh ConstraintMesh
        {
            get
            {
                return _constraintMesh;
            }
            set
            {
                if (_constraintMesh == null)
                {
                    _numberOfConstraintTypes++;
                }
                _constraintMesh = value;
            }
        }

        protected Surface _constraintSurface;

        public Surface ConstraintSurface
        {
            get
            {
                return _constraintSurface;
            }
            set
            {
                if (_constraintSurface == null)
                {
                    _numberOfConstraintTypes++;
                }
                _constraintSurface = value;
            }
        }

        protected Plane _constraintPlane;

        public Plane ConstraintPlane => _constraintPlane;

        private List<Curve> _constraintCurves;

        public List<Curve> ConstraintCurves
        {
            get
            {
                return _constraintCurves;
            }
            set
            {
                if (_constraintCurves.Count == 0)
                {
                    _numberOfConstraintTypes++;
                }
                _constraintCurves = value;
            }
        }

        public List<Curve> SnapCurves { get; set; }

        // Existing curve
        private Curve _existingCurve;

        public Curve existingCurve => _existingCurve;

        // Starting Point
        public Point3d StartingPoint
        {
            get
            {
                if (PointList.Any())
                {
                    return PointList[0];
                }

                return Point3d.Unset;
            }
            set
            {
                if (PointList.Any())
                {
                    PointList[0] = value;
                }
                else
                {
                    PointList.Add(value);
                }
                OnPointListChanged?.Invoke(new List<Point3d>(PointList));
            }
        }

        public Point3d EndPoint
        {
            get
            {
                if (PointList.Any())
                {
                    return PointList[PointList.Count - 1];
                }

                return Point3d.Unset;
            }
            set
            {
                PointList[PointList.Count - 1] = value;
                OnPointListChanged?.Invoke(new List<Point3d>(PointList));
            }
        }

        public int NumberOfControlPoints => PointList.Count;

        // Always show the curve on top of all other geometries
        private bool? _alwaysOnTop;

        public bool? AlwaysOnTop
        {
            get
            {
                return _alwaysOnTop;
            }
            set
            {
                _alwaysOnTop = value;
                _conduit.DrawOnTop = value ?? false;
            }
        }

        // Other private variables
        private bool _closed;
        private int _degree;

        private bool _lockedEnds;
        private readonly Color _curveColor = System.Drawing.Color.Aqua;
        private DrawMode _drawMode;
        private readonly int _controlPointSize;
        private readonly CurveConduit _conduit;
        private readonly CurveConduit _curveSnapHighlightConduit;
        private readonly List<CurveConduit> _curveSnapConduits;
        private Guid _surfaceId = Guid.Empty;
        protected int _movingPointIndex;
        private int _nearestInd1;
        private int _nearestInd2;
        private int _numberOfConstraintTypes;
        private Interval _span;
        private readonly List<int> _lockedPoints = new List<int>();
        private readonly RhinoDoc _document;

        protected List<Point3d> PointList;

        // Other public variables
        public bool CurveUpdated { get; protected set; }

        public bool ShowConstraintCurves { get; set; }
        public bool ShowConstraintSurface { get; set; }
        public bool UniqueCurves { get; set; }

        // Constructor
        public DrawCurve(RhinoDoc doc) : this()
        {
            this._document = doc;
        }

        // Default constructor
        public DrawCurve()
        {
            _constraintMesh = null;
            _constraintSurface = null;
            _constraintPlane = Plane.Unset;
            _constraintCurves = new List<Curve>();
            SnapCurves = new List<Curve>();

            _alwaysOnTop = null;
            _closed = true;
            _degree = 3;
            _controlPointSize = 5;
            _existingCurve = null;
            _lockedEnds = false;
            _movingPointIndex = -1;
            _nearestInd1 = 0;
            _nearestInd2 = 0;
            _numberOfConstraintTypes = 0;
            PointList = new List<Point3d>();

            _conduit = new CurveConduit();
            _conduit.ControlPointSize = _controlPointSize;
            _conduit.CurveColor = _curveColor;
            _conduit.PointColor = System.Drawing.Color.Crimson;

            _curveSnapHighlightConduit = new CurveConduit();
            _curveSnapHighlightConduit.CurveColor = System.Drawing.Color.Magenta;
            _curveSnapHighlightConduit.CurveThickness = 3;

            _curveSnapConduits = new List<CurveConduit>();

            ShowConstraintCurves = true;
            ShowConstraintSurface = false;
            UniqueCurves = false;
        }

        public void SetCurveColor(Color color)
        {
            _conduit.CurveColor = color;
        }

        public void SetIsClosedCurve(bool isClosed)
        {
            _closed = isClosed;
        }

        public void SetCurveDegree(int degree)
        {
            _degree = degree;
        }

        // Get the key state
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetKeyState(int keyCode);

        // Detect if a keyboard key is down
        protected static bool IsKeyDown(int key)
        {
            short retVal = GetKeyState(key);

            //If the high-order bit is 1, the key is down
            //otherwise, it is up.
            if ((retVal & 0x8000) == 0x8000)
            {
                return true;
            }
            //If the low-order bit is 1, the key is toggled.
            return false;
        }

        // Respond to keyboard control
        protected virtual void OnKeyboard(int key)
        {
            // Only execute if key is down (avoid triggering on key release)
            if (!IsKeyDown(key))
                return;

            if (_constraintPlane != Plane.Unset)
            {
                // Init
                double angle = 0;

                switch (key)
                {
                    case (82): // R
                        angle = -5;
                        break;

                    case (84): // T
                        angle = -1;
                        break;

                    case (89): // Y
                        angle = 1;
                        break;

                    case (85): // U
                        angle = 5;
                        break;

                    default:
                        return; // nothing to do
                }

                // Transform
                Transform rotation = Transform.Rotation(RhinoMath.ToRadians(angle), _constraintPlane.XAxis, _constraintPlane.Origin);

                // Rotate the surface
                _document.Objects.Unlock(_surfaceId, true);
                Surface surf = _constraintSurface;
                surf.Transform(rotation);
                _document.Objects.Replace(_surfaceId, surf);
                _document.Objects.Lock(_surfaceId, true);
                // Rotate the plane
                _constraintPlane.Transform(rotation);
                // Rotate the points
                for (int i = 0; i < PointList.Count; i++)
                {
                    Point3d p = PointList[i];
                    p.Transform(rotation);
                    PointList[i] = p;
                }
                OnPointListChanged?.Invoke(new List<Point3d>(PointList));

                // Set constraint to transformed surface
                Constrain(surf, false);

                // Refresh
                _document.Views.Redraw();
            }

        }

        // Callback when the user presses a mouse button
        protected override void OnMouseDown(GetPointMouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Shift)
            {
                // if only a curve was used to constrain the curve points, make sure the selected
                // point is on a constraining curve
                bool okToAdd = true;
                if (ConstraintCurves.Count != 0 && ConstraintMesh == null)
                {
                    double t = 0.0;
                    Curve targetCurve = base.PointOnCurve(out t);
                    okToAdd = targetCurve != null;
                }

                // Always OK to add unless there are only curve constraints and the user did not
                // click on a curve.
                if (okToAdd)
                {
                    // Add point
                    if (_closed && _nearestInd1 > _nearestInd2)
                    { PointList.Insert(_nearestInd1, base.Point()); }

                    else if (_closed && _nearestInd1 == 0)
                    {
                        PointList.Insert(_nearestInd1, base.Point());
                        PointList[PointList.Count - 1] = base.Point(); // replace
                    }
                    else if (!_closed && _nearestInd1 > _nearestInd2)
                    {
                        PointList.Insert(_nearestInd1, base.Point());
                    }
                    else if (!_closed && _nearestInd1 == 0)
                    {
                        PointList.Insert(1, base.Point());
                    }
                    else // _nearestInd2 > _nearestInd1
                    {
                        PointList.Insert(_nearestInd2, base.Point());
                    }
                    OnPointListChanged?.Invoke(new List<Point3d>(PointList));

                    // Update locked point indices
                    if (_lockedEnds)
                    {
                        _lockedPoints[_lockedPoints.IndexOf(PointList.Count - 2)] = PointList.Count - 1;
                    }

                    base.AddSnapPoint(base.Point());

                    CurveUpdated = true;
                }
            }
            else if (Control.ModifierKeys == Keys.Alt)
            {
                // Remove point
                if (!_lockedPoints.Contains(_nearestInd1))
                {
                    PointList.RemoveAt(_nearestInd1);
                    OnPointListChanged?.Invoke(new List<Point3d>(PointList));

                    // Update locked point indices
                    if (_lockedEnds)
                    {
                        _lockedPoints[_lockedPoints.IndexOf(PointList.Count)] = PointList.Count - 1;
                    }

                    CurveUpdated = true;
                }
            }
            else if (_movingPointIndex == -1 && !_lockedPoints.Contains(_nearestInd1)) // get new point
            {
                _movingPointIndex = _nearestInd1;
                _conduit.DrawOnTop = true;
                Rhino.ApplicationSettings.ModelAidSettings.OsnapModes = Rhino.ApplicationSettings.OsnapModes.None;
            }
        }


        private Point3d GetPointOnSnapCurve(Point3d point, double maxDistance, out Curve closestCurve)
        {
            var pointOnCurve = Point3d.Unset;
            closestCurve = null;

            foreach (var scurve in SnapCurves)
            {
                double t;
                if (scurve.ClosestPoint(point, out t, maxDistance))
                {
                    if (pointOnCurve == Point3d.Unset || scurve.PointAt(t).DistanceTo(point) < pointOnCurve.DistanceTo(point))
                    {
                        pointOnCurve = scurve.PointAt(t);
                        closestCurve = scurve;
                    }
                }
            }

            return pointOnCurve;
        }

        // Callback when the mouse is moved around
        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
                
            // Update nearest indices
            ClosestCurvePoints(e.WindowPoint);

            //When Editing
            if (!e.LeftButtonDown && _movingPointIndex >= 0)
            {
                // if only a curve was used to constrain the curve points, make sure the selected
                // point is on a constraining curve
                bool okToAdd = true;

                if (ConstraintCurves.Count != 0 && ConstraintMesh == null)
                {
                    okToAdd = IsOnConstraintCurve(base.Point());
                }


                if (okToAdd)
                {
                    Point3d movedPoint = base.Point();

                    //If has constraint curves, snap on curve is no longer relevant, also we do not want to snap it if control key is pressed down
                    if (SnapCurves.Any() && !ConstraintCurves.Any() && Control.ModifierKeys != Keys.Control)
                    {
                        Curve closestCurve;
                        var pointOnSnapCurve = GetPointOnSnapCurve(movedPoint, 0.4, out closestCurve);
                        if (pointOnSnapCurve != Point3d.Unset)
                        {
                            movedPoint = pointOnSnapCurve;
                        } 
                    }

                    // Change point in point list
                    PointList[_movingPointIndex] = movedPoint;
                    // Also change end point if first point on curve was selected
                    if (_movingPointIndex == 0 && _closed)
                    {
                        PointList[PointList.Count - 1] = movedPoint;
                    }
                    OnPointListChanged?.Invoke(new List<Point3d>(PointList));

                    // Back to picking points
                    _movingPointIndex = -1;
                    if (!(_alwaysOnTop.HasValue && _alwaysOnTop.Value))
                    {
                        _conduit.DrawOnTop = false;
                    }
                    Rhino.ApplicationSettings.ModelAidSettings.OsnapModes = Rhino.ApplicationSettings.OsnapModes.Point;

                    _curveSnapHighlightConduit.Enabled = false;
                    CurveUpdated = true;
                }
            }
            else if (SnapCurves.Any()) //When drawing
            {
                //If press control, dont snap it
                if (Control.ModifierKeys == Keys.Control)
                {
                    ClearSnapPoints();
                }
                else
                {
                    List<Point3d> snapPoints = new List<Point3d>();

                    foreach (var c in SnapCurves)
                    {
                        double dummy;
                        snapPoints.Add(CurveUtilities.GetClosestPoint(c, base.Point(), out dummy));
                    }

                    if (snapPoints.Any())
                    {
                        ClearSnapPoints();
                        AddSnapPoints(snapPoints.ToArray());
                    }
                }

            }
        }

        // Separate function to set constraint plane
        public void SetConstraintPlane(Plane plane, Interval span, bool show, bool isPlaneRotatable = true)
        {
            // Set variables
            _constraintPlane = plane;
            this._span = span;

            // Convert to surface and set constraint surface
            PlaneSurface surface = new PlaneSurface(plane, span, span);

            // Convert to surface and set constraint surface
            this.ConstraintSurface = surface;
            ShowConstraintSurface = show;

            // Increment number of constraint types
            if (_constraintPlane == Plane.Unset)
            {
                _numberOfConstraintTypes++;
            }

            if (isPlaneRotatable)
            {
                // Enable rotation using the keyboard
                this.AcceptString(true);
                Rhino.RhinoApp.KeyboardEvent += new Rhino.RhinoApp.KeyboardHookEvent(OnKeyboard);
            }
        }

        // Separate function to set constraint plane
        public bool SetConstraintPlane(Plane plane, Interval span, bool show, Curve[] boudingCurves)
        {
            // Set variables
            _constraintPlane = plane;
            this._span = span;

            // Convert to surface and set constraint surface
            Surface surface = new PlaneSurface(plane, span, span);

            Brep result = new Brep();

            foreach (var c in boudingCurves)
            {
                if (c.IsClosed)
                {
                    Brep tmpResult;
                    BrepUtilities.RemovePatchFromSurface(surface, c, 1.0, out tmpResult);
                    result.Append(tmpResult);
                }
                else
                {
                    return false;
                }
            }

            ConstraintMesh = MeshUtilities.AppendMeshes(Mesh.CreateFromBrep(result));
            return true;
        }

        public void AddPoint(Point3d point)
        {
            PointList.Add(point);
            OnPointListChanged?.Invoke(new List<Point3d>(PointList));
        }

        // Separate function to set existing curve
        public void SetExistingCurve(Curve existing, bool _closed, bool lockedEnds)
        {
            _existingCurve = existing;

            // if no points were given by the user, recover them from the curve
            if (PointList.Count == 0)
            {
                var points = new List<Point3d>();
                // Restore original points
                for (int i = _existingCurve.Degree - 1;
                    i < _existingCurve.ToNurbsCurve().Knots.Count - (_existingCurve.Degree - 1);
                    i++)
                {
                    points.Add(_existingCurve.PointAt(_existingCurve.ToNurbsCurve().Knots[i]));
                }
                if (_existingCurve.IsClosed)
                {
                    points[points.Count - 1] = points[0];
                }
                PointList = points;
                OnPointListChanged?.Invoke(new List<Point3d>(PointList));
                _drawMode = DrawMode.Edit;
            }

            this._closed = _closed;
            if (lockedEnds)
            {
                lockEnds();
            }
        }

        // Set constraints and start drawing
        public virtual Curve Draw(int maxPoints = 0)
        {
            List<Guid> constraintCurveIDs = new List<Guid>();
            _conduit.Enabled = true;
            CurveUpdated = true;

            //Prepare curveSnap Conduits if no constraintCurves is set
            _curveSnapConduits.Clear();
            if (!_constraintCurves.Any())
            {
                foreach (var sc in SnapCurves)
                {
                    var curveSnapConduit = new CurveConduit();
                    curveSnapConduit.CurvePreview = sc;
                    curveSnapConduit.CurveColor = System.Drawing.Color.Black;
                    curveSnapConduit.CurveThickness = 1;
                    curveSnapConduit.Enabled = true;

                    _curveSnapConduits.Add(curveSnapConduit);
                }
            }

            // Constrain to the geometries supplied by the user
            if (_constraintMesh != null)
            {
                Constrain(_constraintMesh, false);
            }
            if (_constraintSurface != null)
            {
                Constrain(_constraintSurface, false);
                // Visualise
                if (ShowConstraintSurface)
                {
                    ObjectAttributes oa = new ObjectAttributes();
                    oa.Visible = true;
                    _surfaceId = _document.Objects.AddSurface(_constraintSurface, oa);
                }
            }
            if (ConstraintCurves.Count != 0)
            {
                if (ShowConstraintCurves)
                {
                    foreach (Curve constraintCurve in ConstraintCurves)
                    {
                        constraintCurveIDs.Add(_document.Objects.AddCurve(constraintCurve));
                    }
                }
                
                base.EnableSnapToCurves(true);
                _document.Views.Redraw();
            }

            if (_drawMode == DrawMode.Edit && _existingCurve == null)
            {
                return null;
            }

            if (_drawMode == DrawMode.Indicate && !(_alwaysOnTop.HasValue && !_alwaysOnTop.Value))
            {
                _conduit.DrawOnTop = true;
            }

            var newCurve = _existingCurve == null ? Indicate(maxPoints: maxPoints) : Edit();

            // Delete constraint curve visual
            if (ShowConstraintCurves)
            {
                foreach (Guid id in constraintCurveIDs)
                {
                    if (_document.Objects.Find(id).IsLocked)
                    {
                        _document.Objects.Unlock(id, true);
                    }
                    _document.Objects.Delete(id, true);
                }
            }
            // Delete constraint surface visual
            if (ShowConstraintSurface)
            {
                _document.Objects.Unlock(_surfaceId, true);
                _document.Objects.Delete(_surfaceId, true);
                _document.Objects.Lock(_surfaceId, true);
            }

            Rhino.RhinoApp.KeyboardEvent -= OnKeyboard;

            foreach (var scc in _curveSnapConduits)
            {
                scc.Enabled = false;
            }

            _conduit.Enabled = false;
            return newCurve;
        }

        // Indicate a new curve
        private Rhino.Geometry.Curve Indicate(int maxPoints = 0)
        {
            while (PointList.Count < maxPoints || maxPoints == 0)
            {
                GetResult rc = base.Get();
                _curveSnapHighlightConduit.Enabled = false;

                if (rc == GetResult.Point)
                {
                    if (OnNewCurveAddPoint != null && !OnNewCurveAddPoint(base.Point()))
                    {
                        break;
                    }

                    // Check if curve was _closed
                    if (PointList.Count > 3 && base.Point().EpsilonEquals(PointList[0], 1))
                    {
                        PointList.Add(PointList[0]);
                        OnPointListChanged?.Invoke(new List<Point3d>(PointList));
                        break;
                    }

                    // Check curve constraints
                    bool okToAdd = true;
                    double t;
                    Curve clickedCurve = base.PointOnCurve(out t);
                    // If only constrained to curves and a curves is not clicked
                    if (ConstraintCurves.Count != 0 && _numberOfConstraintTypes == 1 && clickedCurve == null)
                    {
                        // user has not clicked a curve
                        okToAdd = false;
                    }
                    // if a curve was clicked, check if is part of the constraining curves
                    else if (ConstraintCurves.Count != 0 && clickedCurve != null)
                    {
                        Curve cCurve;
                        okToAdd = IsOnConstraintCurve(out cCurve);
                        if (okToAdd && UniqueCurves)
                        {
                            ConstraintCurves.Remove(cCurve);
                        }
                    }

                    if (okToAdd)
                    {
                        if (base.GetSnapPoints().Length == 0)
                        {
                            base.AddConstructionPoint(base.Point());
                        }
                        PointList.Add(base.Point());
                        OnPointListChanged?.Invoke(new List<Point3d>(PointList));
                        CurveUpdated = true;
                    }
                }
                else if (rc == GetResult.Undo)
                {
                    UndoLastPoint();
                }
                else if (rc == GetResult.Object)
                {
                    break;
                }
                else if (rc == GetResult.Nothing)
                {
                    break; // User pressed ENTER or _closed curve
                }
                else if (rc == GetResult.Cancel)
                {
                    return null;
                }
            }
            return BuildCurve(_closed);
        }

        // Edit an existing curve
        private Rhino.Geometry.Curve Edit()
        {
            GetResult rc;
            while (true)
            {
                base.EnableTransparentCommands(false);
                // Move it around until user clicks to set it
                rc = base.Get(true); // grab original point
                _curveSnapHighlightConduit.Enabled = false;

                if (rc == GetResult.Object)
                {
                    break;
                }
                if (rc == GetResult.Nothing)
                {
                    break; // User pressed ENTER or _closed curve
                }
                if (rc == GetResult.Cancel)
                {
                    return null;
                }
                if (rc == GetResult.String)
                {
                    break;
                }
            } 
            return BuildCurve(_closed);
        }

        // Remove the last point from the list
        private void UndoLastPoint()
        {
            if (PointList.Count > 1)
            {
                PointList.RemoveAt(PointList.Count - 1);
                OnPointListChanged?.Invoke(new List<Point3d>(PointList));
            }
        }

        // Lock curve ends in place
        private void lockEnds()
        {
            if (_existingCurve != null && _closed == false)
            {
                int lastInd = PointList.Count - 1;
                if (!_lockedPoints.Contains(0))
                {
                    _lockedPoints.Add(0);
                }
                if (!_lockedPoints.Contains(lastInd))
                {
                    _lockedPoints.Add(lastInd);
                }

            }
            _lockedEnds = true;
        }

        // Make Curve with the points that were indicated.
        protected Rhino.Geometry.Curve BuildCurve(bool _closed)
        {
            return CurveUtilities.BuildCurve(PointList, _degree, _closed);
        }

        private bool IsOnConstraintCurve(out Curve curve)
        {
            // Init
            bool onCCurve = false;
            curve = null;

            // Check every constraint curve
            foreach (Curve cCurve in ConstraintCurves)
            {
                double t2;
                onCCurve = cCurve.ClosestPoint(base.Point(), out t2, 1e-6);

                // Clicked on constraint curve
                if (onCCurve)
                {
                    // Remove the curve from the constraints to avoid that the user clicks it twice
                    curve = cCurve;
                    break;
                }
            }

            return onCCurve;
        }

        private bool IsOnConstraintCurve(Point3d point)
        {
            Curve curve = null;
            return IsOnConstraintCurve(out curve);
        }

        // Returns the closest point index (index1) and the second closest (index2)
        private void ClosestCurvePoints(System.Drawing.Point screenPoint)
        {
            // Convert srcScreenCoord to Rhino Point2d
            Point2d srcScreenCoord = new Point2d(screenPoint.X, screenPoint.Y);
            double minDist = double.MaxValue;

            if (PointList.Count > 0)
            {
                int lastPoint = -1;
                if (_closed)
                {
                    lastPoint = PointList.Count - 2; // do not scan last point (equals first)
                }
                else
                {
                    lastPoint = PointList.Count - 1; // scan all points
                }

                for (int i = 0; i <= lastPoint; i++)
                {
                    // Convert to screen point
                    double dist = (_document.Views.ActiveView.ActiveViewport.WorldToClient(PointList[i]) - srcScreenCoord).Length;
                    // Get closest
                    if (dist < minDist)
                    {
                        minDist = dist;
                        _nearestInd1 = i;
                    }
                }

                if (PointList.Count > 1)
                {
                    // Index of previous and next point
                    int indPrev = -1;
                    int indNext = -1;

                    // For open curve end points
                    if (!_closed && _nearestInd1 == 0)
                    {
                        _nearestInd2 = 1;
                    }
                    else if (!_closed && _nearestInd1 == PointList.Count - 1)
                    {
                        _nearestInd2 = PointList.Count - 2;
                    }
                    // _closed curves and open curve points other than endpoints
                    else
                    {
                        // Determine previous and next index
                        if (_nearestInd1 == 0) // for _closed curve
                        {
                            indPrev = PointList.Count - 2;
                        }
                        else
                        {
                            indPrev = _nearestInd1 - 1;
                        }
                        if (_nearestInd1 == PointList.Count - 1) // for _closed curve
                        {
                            indNext = 1;
                        }
                        else
                        {
                            indNext = _nearestInd1 + 1;
                        }
                        // Determine second nearest point
                        Vector2d vecNear = _document.Views.ActiveView.ActiveViewport.WorldToClient(PointList[_nearestInd1]) - srcScreenCoord;
                        Vector2d vecPrev = _document.Views.ActiveView.ActiveViewport.WorldToClient(PointList[indPrev]) - srcScreenCoord;
                        Vector2d vecNext = _document.Views.ActiveView.ActiveViewport.WorldToClient(PointList[indNext]) - srcScreenCoord;
                        if (MathUtilities.Angle(vecNear, vecNext) > MathUtilities.Angle(vecNear, vecPrev))
                        {
                            _nearestInd2 = indNext;
                        }
                        else
                        {
                            _nearestInd2 = indPrev;
                        }
                    }
                }
            }
        }

        public int GetNumberOfControlPoints()
        {
            return PointList.Count;
        }

        private bool IsCurrentPointOnSnapCurves(Point3d currentPoint)
        {
            if (!SnapCurves.Any())
            {
                return false;
            }

            Curve closestCurve;
            var ptSnapper = GetPointOnSnapCurve(currentPoint, 0.4, out closestCurve);
            return (ptSnapper != Point3d.Unset) && ((currentPoint - ptSnapper).Length < 0.01) && SnapCurves.Any();
        }

        private bool IsDragCurveNearSnapCurve()
        {
            if (_drawMode == DrawMode.Edit)
            {
                return SnapCurves.Any() && !ConstraintCurves.Any() && Control.ModifierKeys != Keys.Control && (Control.MouseButtons & MouseButtons.Left) != 0;
            }

            return false;
        }

        // Dynamically draw the curve using the current cursor position.
        protected override void OnDynamicDraw(Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e); // Invokes DynamicDraw event
            
            OnDynamicDrawing?.Invoke(e.CurrentPoint);

            Point3d _originalPoint = Point3d.Unset;
            bool is_closed = _closed;

            // Draw interpolation points
            if (PointList.Count > 0)
            {
                _conduit.PointPreview = PointList;
            }

            // Highlight nearest point
            try
            {
                e.Display.DrawPoint(PointList[_nearestInd1], Rhino.Display.PointStyle.ActivePoint, _controlPointSize + 2, System.Drawing.Color.Gold);
                if (Control.ModifierKeys == Keys.Shift) // Adding a point
                {
                    e.Display.DrawPoint(PointList[_nearestInd2], Rhino.Display.PointStyle.ActivePoint, _controlPointSize + 2, System.Drawing.Color.Silver);
                }
            }
            catch
            {
                // Do nothing
            }

            // Do custom checking
            bool validPoint = !ShouldValidatePoints || IsValidCurvePoint(e.CurrentPoint);
            if (validPoint)
            {
                if (_existingCurve == null) // creating new curve
                {
                    PointList.Add(e.CurrentPoint);
                    is_closed = false;
                }
                else if (_movingPointIndex != -1)// moving curve point
                {
                    _originalPoint = PointList[_movingPointIndex];
                    PointList[_movingPointIndex] = e.CurrentPoint;
                    if (_movingPointIndex == 0 && _closed)
                    {
                        // Also replace last point
                        PointList[PointList.Count - 1] = e.CurrentPoint;
                    }
                }
                OnPointListChanged?.Invoke(new List<Point3d>(PointList));
            }
            else
            {
                is_closed = false;
            }

            OnDynamicDrawPreRebuildCurve?.Invoke(e.CurrentPoint, _movingPointIndex);

            // Make the curve and draw it
            Curve dynamicCurve = BuildCurve(is_closed);
            if (null != dynamicCurve)
            {
                _conduit.CurvePreview = dynamicCurve;
            }
            if (validPoint)
            {
                if (_existingCurve == null) // creating new curve
                {
                    PointList.RemoveAt(PointList.Count - 1);
                }
                else if (_movingPointIndex != -1)// moving curve point
                {
                    PointList[_movingPointIndex] = _originalPoint;
                    if (_movingPointIndex == 0 && _closed)
                    {
                        // Also replace last point
                        PointList[PointList.Count - 1] = _originalPoint;
                    }
                }
                OnPointListChanged?.Invoke(new List<Point3d>(PointList));
            }

            //Only on Edit mode this makes sense
            //If has constraint curves, snap on curve is no longer relevant, also we do not want to snap it if control key is pressed down
            if (IsCurrentPointOnSnapCurves(e.CurrentPoint) || IsDragCurveNearSnapCurve())
            {
                Curve closestCurve;
                var pointOnSnapCurve = GetPointOnSnapCurve(e.CurrentPoint, 0.4, out closestCurve);
                if (pointOnSnapCurve != Point3d.Unset)
                {
                    _curveSnapHighlightConduit.CurvePreview = closestCurve;
                    _curveSnapHighlightConduit.Enabled = true;
                    _curveSnapHighlightConduit.DrawOnTop = true;
                }
                else
                {
                    _curveSnapHighlightConduit.Enabled = false;
                }
            }
            else
            {
                _curveSnapHighlightConduit.Enabled = false;
            }

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }
    }
}