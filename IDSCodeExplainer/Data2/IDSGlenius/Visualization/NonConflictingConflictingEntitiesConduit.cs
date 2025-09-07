using System;
using IDS.Common;
using IDS.Glenius.Visualization;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.UI;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Glenius.Operations
{
    public class NonConflictingConflictingEntitiesConduit : DisplayConduit, IDisposable
    {
        private readonly List<EntityObject> _entities;
        private readonly DisplayMaterial _nonConflictingMaterial;
        private readonly DisplayMaterial _conflictingMaterial;
        private readonly BoundingBox _boundingBox;
        private readonly IDSMouseCallback _mouseCallback;
        private uint _cameraChangeCounter;

        public new bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                HookUnhookMouseEvent(value);
                base.Enabled = value;
            }
        }

        public NonConflictingConflictingEntitiesConduit(List<EntityObject> list)
        {
            _nonConflictingMaterial = new DisplayMaterial(Colors.NonConflictingEntities);
            _conflictingMaterial = new DisplayMaterial(Colors.ConflictingEntities);
            _entities = list;
            _boundingBox = BoundingBox.Empty;
            var meshes = list.Select(entity => entity.Mesh);
            foreach (var mesh in meshes)
            {
                _boundingBox.Union(mesh.GetBoundingBox(true));
            }
            _mouseCallback = new IDSMouseCallback();
            _cameraChangeCounter = 0;
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            e.IncludeBoundingBox(_boundingBox);
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            foreach (var entity in _entities)
            {
                if (!entity.IsVisible)
                {
                    continue;
                }

                DisplayMaterial material;

                if (!entity.IsConflicting.HasValue)
                {
                    material = new DisplayMaterial(entity.OriginalColor);
                }
                else if (entity.IsConflicting.Value)
                {
                    material = _conflictingMaterial;
                }
                else //if (!entity.IsConflicting.Value)
                {
                    material = _nonConflictingMaterial;
                }

                e.Display.DrawMeshShaded(entity.Mesh, material);
            }
        }

        private void HookUnhookMouseEvent(bool hook)
        {
            if (hook)
            {
                _cameraChangeCounter = 0;
                _mouseCallback.Enabled = true;
                _mouseCallback.MouseDown += OnMouseDown;
                _mouseCallback.MouseUp += OnMouseUp;
            }
            else
            {
                _mouseCallback.Enabled = false;
                _mouseCallback.MouseDown -= OnMouseDown;
                _mouseCallback.MouseUp -= OnMouseUp;
            }
        }

        private void OnMouseDown(MouseCallbackEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                _cameraChangeCounter = e.View.ActiveViewport.ChangeCounter;
            }
        }

        private void OnMouseUp(MouseCallbackEventArgs e)
        {
            if ((e.Button == MouseButtons.Left || e.Button == MouseButtons.Right) && _cameraChangeCounter == e.View.ActiveViewport.ChangeCounter)
            {
                var picker = new PickContext();
                picker.View = e.View.ActiveViewport.ParentView;
                picker.PickStyle = PickStyle.PointPick;
                var xform = e.View.ActiveViewport.GetPickTransform(e.ViewportPoint);
                picker.SetPickTransform(xform);

                double depth = 0;
                EntityObject selectedMesh = null;
                var refDepth = depth;

                foreach (var mesh in _entities)
                {
                    if (!mesh.IsVisible)
                    {
                        continue;
                    }

                    double distance;
                    Point3d hitPoint;
                    PickContext.MeshHitFlag hitFlag;
                    int hitIndex;
                    if (!picker.PickFrustumTest(mesh.Mesh, PickContext.MeshPickStyle.ShadedModePicking, out hitPoint,
                            out depth, out distance, out hitFlag, out hitIndex) ||
                        !(Math.Abs(distance) < double.Epsilon) ||
                        selectedMesh != null && !(refDepth < depth))
                    {
                        continue;
                    }
                    //depth returned here for point picks LARGER values are NEARER to the camera. SMALLER values are FARTHER from the camera.
                    selectedMesh = mesh;
                    refDepth = depth;
                }

                if (selectedMesh != null)
                {
                    if (Control.ModifierKeys == Keys.Control)
                    {
                        selectedMesh.IsConflicting = null;
                    }
                    else if (e.Button == MouseButtons.Left)
                    {
                        selectedMesh.IsConflicting = false;
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        selectedMesh.IsConflicting = true;
                    }

                    selectedMesh.Update();
                }

                //flag event as handled so that RhinoGet.GetString does not return
                e.Cancel = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _nonConflictingMaterial.Dispose();
            _conflictingMaterial.Dispose();
        }
    }
}