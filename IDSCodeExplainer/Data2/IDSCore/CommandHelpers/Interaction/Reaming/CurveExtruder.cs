using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;

namespace IDS.Core.Operations
{
    public class CurveExtruder : IDisposable
    {
        private Point3d _cursorReference;
        private Vector3d _patchNormal;
        private Brep _patch;
        private Brep _patchExtruded = null;
        private Guid _existingCurveGuid = Guid.Empty;

        private readonly DisplayMaterial _materialEntity = new DisplayMaterial()
        {
            Transparency = 0.5,
            Shine = 0.0,
            IsTwoSided = false,
            Ambient = System.Drawing.Color.ForestGreen,
            Diffuse = System.Drawing.Color.ForestGreen,
            Specular = System.Drawing.Color.ForestGreen,
            Emission = System.Drawing.Color.ForestGreen,
        };

        public void SetExistingCurveId(Guid existingCurveId)
        {
            _existingCurveGuid = existingCurveId;
        }

        public bool ExtrudeCurve(RhinoDoc doc, Surface constrainSurf, Transform planeTransform, out Brep patchExtruded)
        {
            // init
            Curve pieceContour = null;
            Brep patch = null;
            patchExtruded = null;
            int bottomFaceIndex = 1; // index of the face of a reaming entity

            // Draw a new curve
            DrawCurve getCurve = new DrawCurve(doc);
            getCurve.ConstraintSurface = constrainSurf;
            getCurve.SetCommandPrompt("Delineate the piece to ream on the surface");
            getCurve.AcceptNothing(true); // Pressing ENTER is allowed to close the curve
            getCurve.AcceptUndo(true); // Enables ctrl-z
            getCurve.PermitObjectSnap(false);

            // If an originalEntityId is given, extract the old curve and let user edit that
            if (_existingCurveGuid != Guid.Empty)
            {
                var originalEntity = (Brep)doc.Objects.Find(_existingCurveGuid).Geometry;
                var originalEntityTransf = (Brep)originalEntity.Duplicate();
                originalEntityTransf.Transform(planeTransform);
                var originalCurve = originalEntityTransf.Loops[bottomFaceIndex].To3dCurve();
                getCurve.SetExistingCurve(originalCurve, true, false);
            }

            // Check until extrusion can be don
            bool badExtrusion = true;
            while (badExtrusion)
            {
                if (pieceContour != null) // if the user has to redraw because the curve drawing failed
                {
                    getCurve.SetExistingCurve(pieceContour, true, false);
                }

                pieceContour = getCurve.Draw();
                if (pieceContour == null) // user canceled
                {
                    return false;
                }

                // Cut out surface patch
                bool ok = BrepUtilities.CutPatchFromSurface(constrainSurf, pieceContour, out patch);
                if (!ok)
                {
                    return false;
                }

                // Test if patch can be extruded properly, if not let user adjust the curve he created
                Brep extrusionTest = Brep.CreateFromOffsetFace(patch.Faces[0], 1, 0.0, false, true);
                badExtrusion = extrusionTest.Faces.Count == 2; // no side surface
                if (badExtrusion)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Could not create a proper extrusion. Please adjust the curve and press ENTER to try again.");
                }
            }

            // Interactively extrude the patch
            _patch = patch;
            _cursorReference = pieceContour.PointAtStart;
            double u, v;
            bool found = patch.Faces[0].ClosestPoint(_cursorReference, out u, out v);
            if (!found)
            {
                return false;
            }
            _patchNormal = patch.Faces[0].NormalAt(u, v);
            Line cursorLine = new Line(_cursorReference, _patchNormal);

            // Interactive extrude
            GetPoint getExtrusionPoint = new Rhino.Input.Custom.GetPoint();
            getExtrusionPoint.SetCommandPrompt("Extrude cutting patch");
            getExtrusionPoint.DynamicDraw += this.DrawExtrusion;
            getExtrusionPoint.DrawLineFromPoint(_cursorReference, false);
            getExtrusionPoint.Constrain(cursorLine);
            getExtrusionPoint.PermitObjectSnap(false);
            getExtrusionPoint.AcceptNothing(true); // accept ENTER to confirm
            getExtrusionPoint.EnableTransparentCommands(false);
            while (true)
            {
                GetResult getResult = getExtrusionPoint.Get(); // function only returns after clicking
                if (getResult == GetResult.Cancel)
                {
                    return false;
                }

                if (getResult == GetResult.Point)
                {
                    break;
                }

                if (getResult == GetResult.Nothing)
                {
                    break;
                }
            }
            if (null == _patchExtruded)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid extrusion.");
                return false;
            }

            // output patchExtruded
            patchExtruded = _patchExtruded;

            // Success
            return true;
        }

        /**
         * Event handler for dynamic draw event of extrusion.
         */

        public void DrawExtrusion(Object sender, Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            // Dynamically draw the solid pie
            Vector3d cursor_vec = e.CurrentPoint - _cursorReference;
            double offset = cursor_vec * _patchNormal;
            _patchExtruded = Brep.CreateFromOffsetFace(_patch.Faces[0], offset, 0.0, false, true);

            // Draw pie shaded with wires
            e.Display.DrawBrepWires(_patchExtruded, System.Drawing.Color.Black, 3);
            e.Display.DrawBrepShaded(_patchExtruded, _materialEntity);
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
                _materialEntity.Dispose();
            }
        }
    }
}