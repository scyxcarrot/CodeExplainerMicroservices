using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Core.Utilities
{
    /**
        * Get mesh faces using a circular brush that is dynamically drawn
        * in the viewport.
        *
        * After calling Get(true), faces will be in the
        * selected state on the input MeshObject, and the indices of
        * selected faces are stored in the selectedFaces member variable.
        */

    public class MeshFaceBrushSelector : Rhino.Input.Custom.GetPoint
    {
        public HashSet<int> SelectedFaces { get; private set; }
        private readonly MeshObject _ballpark;
        private bool _isConstrained = false;
        private bool _isIndicating = false; // When mouse is down, faces are being indicated
        private bool _isDeselecting = false; // True when user is de-selecting triangles
        private double _brushRadius = 5.0; // Brush circle radius in mm
        public const double DefaultBrushRadius = 5.0;
        private readonly Rhino.Input.Custom.OptionDouble _optionRadius;

        /**
            * Create a new mesh GetMeshFaces object.
            */

        public MeshFaceBrushSelector(MeshObject ballpark)
        {
            ballpark.Select(false, true); // De-select the ballpark mesh
            this._ballpark = ballpark;
            this.Constrain(ballpark.MeshGeometry, false);
            SelectedFaces = new HashSet<int>();
            this._ballpark.MeshGeometry.FaceNormals.ComputeFaceNormals(); // Needed later

            // Set options for getter
            _optionRadius = new Rhino.Input.Custom.OptionDouble(DefaultBrushRadius, 2.0, 50.0);
            this.AddOptionDouble("Radius", ref _optionRadius);
            this.AcceptNothing(true); // press ENTER to stop selecting
        }

        /**
            * Constrain the picked point to lie on a mesh.
            * Method is overriden to set the isConstrained property.
            */

        public new bool Constrain(Mesh mesh, bool allowPickingPointOffObject)
        {
            this._isConstrained = base.Constrain(mesh, allowPickingPointOffObject);
            return this._isConstrained;
        }

        /**
            * Called by user to start getting/indicating points if
            * he wants to select the point on mouse button UP.
            *
            * NOTE: Do not use the alternative Get() method without arguments!
            */

        public new Rhino.Input.GetResult Get(bool onMouseUp)
        {
            this.SetCursor(CursorStyle.CrossHair); // Set cursor to brush
            Rhino.Input.GetResult gottype = base.Get(onMouseUp);
            if (gottype == Rhino.Input.GetResult.Option)
            {
                _brushRadius = _optionRadius.CurrentValue;
            }
            else
            {
                this.SetCursor(CursorStyle.Default); // Restore cursor
            }
            if (gottype == Rhino.Input.GetResult.Cancel || gottype == Rhino.Input.GetResult.Nothing)
            {
                // User stopped selecting, collext all selected face indices
                ComponentIndex[] selectedSubObj = _ballpark.GetSelectedSubObjects();
                if (selectedSubObj != null)
                {
                    foreach (ComponentIndex subobj in selectedSubObj)
                    {
                        if (subobj.ComponentIndexType != ComponentIndexType.MeshFace)
                        {
                            continue;
                        }
                        SelectedFaces.Add(subobj.Index);
                    }
                }
            }
            return gottype;
        }

        private void IndicateTriangles(Point3d target, Line frustumLine)
        {
            // line segment from intersecton point camera-target and near plane to target point
            Line targetLine = new Line(frustumLine.To, target);
            Plane brushPlane = new Plane(frustumLine.To, -frustumLine.Direction);
            var nearCircle = new Circle(brushPlane, frustumLine.To, this._brushRadius);
            var targetCircle = new Circle(brushPlane, targetLine.To, this._brushRadius);

            // Shoot rays from within circle
            Point3d[] launchPts = SampleCircleArea(nearCircle);
            Dictionary<int, bool> hitFaces = RayCastDict(launchPts, -frustumLine.Direction, _ballpark.MeshGeometry);
            if (hitFaces.Count == 0)
            {
                return;
            }

            // Region growing
            Dictionary<int, bool> vertBounded = new Dictionary<int, bool>();
            foreach (int i in hitFaces.Keys)
            {
                MeshFace f = _ballpark.MeshGeometry.Faces[i];
                vertBounded[f.A] = true;
                vertBounded[f.B] = true;
                vertBounded[f.C] = true;
            }
            RegionGrow(new HashSet<int>(hitFaces.Keys), ref hitFaces, ref vertBounded, targetCircle, _ballpark.MeshGeometry);

            // Select + highlight faces that were hit
            foreach (int faceIdx in hitFaces.Where(x => x.Value).Select(x => x.Key))
            {
                var compIdx = new ComponentIndex(ComponentIndexType.MeshFace, faceIdx);
                _ballpark.SelectSubObject(compIdx, !this._isDeselecting, true);
            }
            this.View().Redraw(); // Plot selected faces
        }

        /**
            * Event handler/callback for the GetPoint.MouseDown event
            *
            * Called during Get2dRectangle, Get2dLine, and GetPoint(..,true)
            * when the mouse down event for the initial point occurs. This
            * function is not called during ordinary point getting because
            * the mouse down event terminates an ordinary point get and returns
            * a GetResult.Point result.
            */

        protected override void OnMouseDown(Rhino.Input.Custom.GetPointMouseEventArgs e)
        {
            base.OnMouseDown(e);
            this._isDeselecting = (Control.ModifierKeys == Keys.Alt);

            Point3d target = e.Point;
            System.Drawing.Point screenPoint = e.WindowPoint;

            // Line starting on near clipping plane and ending on far clipping plane
            Line frustumLine;
            bool res = e.Viewport.GetFrustumLine(screenPoint.X, screenPoint.Y, out frustumLine);
            if (!res)
            {
                return;
            }

            IndicateTriangles(target, frustumLine);
        }

        /**
            * Event handler/callback for the GetPoint.MouseMove event.
            *
            * Called every time the mouse moves. MouseMove is called once
            * per mouse move and is called BEFORE any calls to OnDynamicDraw.
            * If you are doing anything that takes a long time, periodically
            * call InterruptMouseMove() to see if you should stop. If the view
            * is such that the 2d screen point can't be mapped to a 3d point,
            * the 'point' argument will be Unset.
            */

        protected override void OnMouseMove(Rhino.Input.Custom.GetPointMouseEventArgs e)
        {
            base.OnMouseMove(e);
            this._isIndicating = e.LeftButtonDown;
            this._isDeselecting = (Control.ModifierKeys == Keys.Alt);

            if (this._isIndicating)
            {
                Point3d target = e.Point;
                System.Drawing.Point screenPoint = e.WindowPoint;

                // Line starting on near clipping plane and ending on far clipping plane
                Line frustumLine;
                bool res = e.Viewport.GetFrustumLine(screenPoint.X, screenPoint.Y, out frustumLine);
                if (!res)
                {
                    return;
                }

                IndicateTriangles(target, frustumLine);
            }
        }

        /**
            * Event handler/callback for the GetPoint.DynamicDraw event.
            *
            *  Every time the mouse moves, DynamicDraw will be called once
            *  per viewport. The calls to DynamicDraw happen AFTER the call
            *  to MouseMove. If you are drawing anything that takes a long
            *  time, periodically call InterruptMouseMove() to see if you
            *  should stop.
            *
            * Effect: checks for faces under the cursor and adds them to
            * selection.
            */

        protected override void OnDynamicDraw(Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            // Get View Frustum & cursor info
            Point3d target = e.CurrentPoint;
            var screenPoint = target;
            Transform world2Screen = e.Viewport.GetTransform(CoordinateSystem.World, CoordinateSystem.Screen);
            screenPoint.Transform(world2Screen);
            // Line starting on near clipping plane and ending on far clipping plane
            Line frustumLine;
            bool res = e.Viewport.GetFrustumLine(screenPoint.X, screenPoint.Y, out frustumLine);
            if (!res)
            {
                return;
            }
            // line segment from intersecton point camera-target and near plane to target point
            Line targetLine = new Line(frustumLine.To, target);

            // Make the brush cursor: draw it at target location to get real size
            Plane brushPlane = new Plane(frustumLine.To, -frustumLine.Direction);
            var targetCircle = new Circle(brushPlane, targetLine.To, this._brushRadius);
            e.Display.DrawCircle(targetCircle, System.Drawing.Color.Magenta);
        }

        /**
            * Shoot rays towards the mesh in the direction of
            * the circle normal.
            *
            * @param[out] hitFaces     Indices of faces that were hit.
            * @parm[out] launchPoints  Launch points for rays that were cast.
            */

        public static Dictionary<int, bool> RayCastDict(IEnumerable<Point3d> launchPts, Vector3d shootDir, Mesh target)
        {
            var hitFaces = new Dictionary<int, bool>();
            foreach (Point3d origin in launchPts)
            {
                Ray3d ray = new Ray3d(origin, shootDir);
                int[] hitIdx;
                double hitDist = Intersection.MeshRay(target, ray, out hitIdx);
                if (hitDist >= 0)
                {
                    foreach (int faceid in hitIdx)
                    {
                        hitFaces[faceid] = true;
                    }
                }
            }
            return hitFaces;
        }

        /**
            * Given a set of selected faces, add neighboring faces to the
            * selection by region growing and checking if neighbors are
            * still within the given bounds.
            *
            * @param[out] faces    The original list of faces, with all immediately
            *                      neighboring faces added that lie withink the
            *                      brush circle.
            */

        public static void GetNeighborFacesBounded(ref HashSet<int> faces, Circle brushBounds, Mesh target)
        {
            // Mapping face->inside/outside
            Dictionary<int, bool> regionFaces = new Dictionary<int, bool>(faces.Count * 2);
            Dictionary<int, bool> regionVertices = new Dictionary<int, bool>(faces.Count * 2);
            foreach (int fid in faces)
            {
                regionVertices[target.Faces[fid].A] = true;
                regionVertices[target.Faces[fid].B] = true;
                regionVertices[target.Faces[fid].C] = true;

                int[] neighbors = target.Faces.AdjacentFaces(fid);
                foreach (int nid in neighbors)
                {
                    if (faces.Contains(nid) || regionFaces.ContainsKey(nid))
                    {
                        continue;
                    }

                    // Check if all faces fall within brush bounds
                    int[] neighbor_verts = { target.Faces[nid].A, target.Faces[nid].B, target.Faces[nid].C };
                    bool allInside = true;
                    foreach (int vid in neighbor_verts)
                    {
                        if (regionVertices.ContainsKey(vid))
                        {
                            if (regionVertices[vid])
                            {
                                continue;
                            }
                            else
                            {
                                allInside = false;
                                break;
                            }
                        }
                        Point3f vert = target.Vertices[vid];
                        bool inside = IsPointInsideCircle(brushBounds, vert);
                        if (!inside)
                        {
                            // Mark all faces connected to this vertex as outside
                            int[] outFaces = target.Vertices.GetVertexFaces(vid);
                            foreach (int j in outFaces)
                            {
                                regionFaces[j] = false;
                            }
                            allInside = false;
                            break;
                        }
                    }
                    if (allInside)
                    {
                        regionFaces[nid] = true;
                    }
                }
            }

            // Add the faces that were found
            faces.UnionWith(regionFaces.Where(x => x.Value).Select(x => x.Key));
        }

        /**
            * Perform region growing starting from a given set of faces,
            * and record the containment status for given bounds in the
            * faceBounded dictionary.
            *
            * @param faceFront             The current 'wavefront' of faces for
            *                              region growing.
            * @param[out] faceBounded      Faces that were already checked
            *                              and their bounded status. Should contain
            *                              all faces in faceFront;
            * @param[out] vertBounded      Vertices that were already checked
            *                              and their bounded status.
            * @param bounds                The bounds for checking containment. The normal
            *                              should correspond to the ray cast/shoot direction.
            * @param target                The target mesh.
            */

        public static void RegionGrow(HashSet<int> faceFront, ref Dictionary<int, bool> faceBounded, ref Dictionary<int, bool> vertBounded, Circle bounds, Mesh target)
        {
            // End recursion
            if (faceFront.Count == 0)
            {
                return;
            }

            // Gather new adjacent faces for all startFaces
            HashSet<int> newFaceFront = new HashSet<int>();
            foreach (int fid in faceFront)
            {
                // Get adjacent faces for current inside face
                int[] adj_fid = target.Faces.AdjacentFaces(fid);

                // Optimization: add own vertices to bounded vertices
                vertBounded[target.Faces[fid].A] = true;
                vertBounded[target.Faces[fid].B] = true;
                vertBounded[target.Faces[fid].C] = true;

                // Filter them based on bounds containment
                foreach (int aid in adj_fid)
                {
                    // If it is part of initial set or already handled, it is not part of the new front
                    if (faceBounded.ContainsKey(aid))
                    {
                        continue;
                    }

                    // Face normal must be opposite to shoot direction
                    if (target.FaceNormals[aid] * bounds.Normal >= 0)
                    {
                        continue;
                    }

                    // Check containment for all vertices
                    int[] avid = { target.Faces[aid].A, target.Faces[aid].B, target.Faces[aid].C };
                    bool allVertsBounded = true;
                    foreach (int vid in avid)
                    {
                        // Optimization: vertex may already be checked
                        bool inside;
                        bool rc = vertBounded.TryGetValue(vid, out inside);
                        if (rc && inside)
                        {
                            continue;
                        }
                        else if (rc && !inside)
                        {
                            allVertsBounded = false;
                            faceBounded[aid] = false;
                            break;
                        }

                        // Do the containment check
                        Point3f vert = target.Vertices[vid];
                        inside = IsPointInsideCircle(bounds, vert);
                        vertBounded[vid] = inside;
                        if (!inside)
                        {
                            allVertsBounded = false;
                            faceBounded[aid] = false;
                            break;
                        }
                    }

                    // If all verts within bounds and new face: add to new front
                    if (allVertsBounded)
                    {
                        newFaceFront.Add(aid);
                        faceBounded[aid] = true;
                    }
                }
            }

            // Recursive call to regionGrow()
            RegionGrow(newFaceFront, ref faceBounded, ref vertBounded, bounds, target);
        }

        /**
            * Test if point is withink a circle in the circle plane.
            */

        public static bool IsPointInsideCircle(Circle bound, Point3f test)
        {
            Vector3d centervec = bound.Center - test;
            Vector3d centervec_proj = centervec - ((centervec * bound.Normal) * bound.Normal);
            return centervec_proj.Length <= bound.Radius;
        }

        /**
            * Sample a circle equidistanctly in the radial
            * and circumferential direction
            */

        public static Point3d[] SampleCircleArea(Circle launchPad)
        {
            Queue<Point3d> launchPts = new Queue<Point3d>();
            Plane launchPlane = launchPad.Plane;

            // Loop over radial and circumferential directon
            launchPts.Enqueue(launchPad.Center);
            double steps_per_mm = 0.5;
            int rad_steps = (int)(launchPad.Radius * steps_per_mm + 1.0); // casting truncates
            for (int i = 0; i <= rad_steps; i++)
            {
                double cur_rad = i * launchPad.Radius / rad_steps;
                double circumf = 2 * cur_rad * Math.PI;
                int n_steps = (int)(circumf * steps_per_mm + 1.0);
                double d_step = circumf / (double)n_steps;
                double offset = (i % 2) * d_step / 2; // so points on circles are not alignes
                for (int j = 0; j < n_steps; j++)
                {
                    double cur_ang = j * d_step + offset;
                    Point3d launch = launchPlane.Origin +
                        (cur_rad * Math.Cos(cur_ang) * launchPlane.XAxis) +
                        (cur_rad * Math.Sin(cur_ang) * launchPlane.YAxis);
                    launchPts.Enqueue(launch);
                }
            }
            return launchPts.ToArray();
        }
    }
}