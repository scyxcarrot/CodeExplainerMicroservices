using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Core.Drawing
{
    public class PlaneConduit : DisplayConduit
    {
        private readonly List<Point3d> _points;

        private Point3d _origin;
        private Vector3d _normal;
        private BoundingBox _boundingBox;

        private double _transparency;
        private System.Drawing.Color _color;

        private bool _renderBack;

        private Func<DrawEventArgs, DisplayMaterial, bool> _quickPlaneRender; //set to null if points are added/appended

        public bool UsePostRendering { get; set; }

        public PlaneConduit()
        {
            _points = new List<Point3d>();
            _color = System.Drawing.Color.LightGray;
            _transparency = 0.0;
            _renderBack = false;
            UsePostRendering = true;
            _boundingBox = BoundingBox.Empty;
        }

        public void AddPoint(Point3d pt)
        {
            if (_points.Count < 3)
            {
                _quickPlaneRender = null;
                _points.Add(pt);
            }
            else
                throw new Exception("The PlaneConduit can only contain three points");
        }

        public Point3d GetPoint(int index)
        {
            if (_points.Count > index)
            {
                return _points[index];
            }

            return Point3d.Unset;
        }

        //If this is set, it will override any other settings
        public void SetPlane(Plane plane, int size)
        {
            this._origin = plane.Origin;
            this._normal = plane.Normal;

            Interval span = new Interval(-size / 2, size / 2);
            Surface planesurface = new PlaneSurface(plane, span, span);
            Brep planeBrep = planesurface.ToBrep();
            if (planeBrep != null)
            {
                _boundingBox = planeBrep.GetBoundingBox(true);
            }

            _quickPlaneRender = (e , displayMaterial) =>
            {
                if (planeBrep != null)
                {
                    e.Display.DrawBrepShaded(planeBrep, displayMaterial);
                }

                return true;
            };
        }
        public void SetPlane(Point3d origin, Vector3d normal, int size)
        {
            SetPlane(new Plane(origin, normal),  size);
        }

        public void SetPlane(Point3d origin, Vector3d vec1, Vector3d vec2, int size)
        {
            var plane = new Plane(origin, vec1, vec2);
            this._normal = plane.Normal;
            this._origin = origin;

            Interval span = new Interval(-size / 2, size / 2);
            Surface planesurface = new PlaneSurface(plane, span, span);
            Brep planeBrep = planesurface.ToBrep();
            if (planeBrep != null)
            {
                _boundingBox = planeBrep.GetBoundingBox(true);
            }

            _quickPlaneRender = (e, displayMaterial) =>
            {
                if (planeBrep != null)
                {
                    e.Display.DrawBrepShaded(planeBrep, displayMaterial);
                }

                return true;
            };
        }

        public void SetPoint(int index, Point3d pt)
        {
            if (_points.Count > index)
            {
                _points[index] = pt;
            }
            else if (_points.Count == index)
            {
                AddPoint(pt);
            }
        }

        public Point3d point0 => GetPoint(0);

        public Point3d point1 => GetPoint(1);

        public Point3d point2 => GetPoint(2);

        public void SetColor(System.Drawing.Color color)
        {
            this._color = color;
        }

        public void SetColor(int r, int g, int b)
        {
            this._color = System.Drawing.Color.FromArgb(r,g,b);
        }

        public void SetRenderBack(bool renderBack)
        {
            this._renderBack = renderBack;
        }

        public void SetTransparency(double transparency)
        {
            this._transparency = transparency;
        }

        public Point3d GetOrigin()
        {
            return _origin;
        }

        public Vector3d GetNormal()
        {
            return _normal;
        }

        private void DrawObjects(DrawEventArgs e)
        {
            DisplayMaterial materialCutPlane = new DisplayMaterial()
            {
                Transparency = _transparency,
                Shine = 0.0,
                IsTwoSided = false,
                Diffuse = _color,
            };

            if(_renderBack)
            {
                materialCutPlane.BackDiffuse = _color;
                materialCutPlane.BackTransparency = _transparency;
            }

            if (_quickPlaneRender != null)
            {
                _quickPlaneRender(e, materialCutPlane);
            }
            else
            {
                // no unset points
                if (_points.Count == 3)
                {
                    Plane pl = new Plane(point0, point1, point2);
                    Vector3d xvec = new Vector3d(point1 - point0);
                    Vector3d yvec = new Vector3d(point2 - point0);
                    Interval xspan = new Interval(0, xvec.Length);
                    Interval yspan = new Interval(0, yvec.Length);

                    Surface planesurface = new PlaneSurface(pl, xspan, yspan);
                    Brep planeBrep = planesurface.ToBrep();
                    if (planeBrep != null)
                    {
                        e.Display.DrawBrepShaded(planeBrep, materialCutPlane);
                    }

                    _normal = pl.Normal;
                    _origin = pl.Origin;
                }
                // 2 point not unset
                else if (_points.Count == 2)
                {
                    e.Display.DrawLine(point0, point1, _color, 3);
                }
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            if(UsePostRendering)
            {
                DrawObjects(e);
            }
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            if (!UsePostRendering)
            {
                DrawObjects(e);
            }
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            if (!_boundingBox.IsValid && _points.Count > 0)
            {
                _boundingBox = new BoundingBox(_points);
            }

            if (_boundingBox.IsValid)
            {
                e.IncludeBoundingBox(_boundingBox);
            }
        }
    }
}