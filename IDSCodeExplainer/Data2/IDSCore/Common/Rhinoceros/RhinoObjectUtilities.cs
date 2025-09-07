using IDS.Core.ImplantDirector;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.Core.Utilities
{
    public static class RhinoObjectUtilities
    {
        public static void SetRhObjectMeshVerticesColors(IImplantDirector director, RhinoObject rhObj, Color color, bool overwriteExisting)
        {
            SetRhObjectsMeshVerticesColors(director, new List<RhinoObject>() {rhObj}, color, overwriteExisting);
        }

        public static void SetRhObjectsMeshVerticesColors(IImplantDirector director, IEnumerable<RhinoObject> rhObjs, Color color, bool overwriteExisting)
        {
            foreach (var rhinoObject in rhObjs)
            {
                if (rhinoObject.ObjectType != ObjectType.Mesh)
                {
                    return;
                }

                var doc = director.Document;
                var id = rhinoObject.Id;

                doc.Objects.Unlock(id, true);

                var mesh = (Mesh)rhinoObject.Geometry;
                if (MeshUtilities.FillVerticesColor(mesh, color, out var coloredMesh, overwriteExisting))
                {
                    doc.Objects.Replace(id, coloredMesh);
                }

                doc.Objects.Lock(id, true);
            }
        }

        public static void SetRhObjTransparencies(IImplantDirector director, IEnumerable<RhinoObject> rhObjs, double transparencyValue)
        {
            var doc = director.Document;
            SetRhObjTransparency(doc, rhObjs, transparencyValue);
        }

        public static void SetRhObjTransparency(RhinoDoc doc, RhinoObject rhObj, double transparencyValue)
        {
            SetRhObjTransparency(doc, new List<RhinoObject>() { rhObj }, transparencyValue);
        }

        public static void SetRhObjTransparency(RhinoDoc doc, IEnumerable<RhinoObject> rhObjs, double transparencyValue)
        {
            foreach (var rhinoObject in rhObjs)
            {
                var matIdx = rhinoObject.Attributes.MaterialIndex;
                var mat = doc.Materials[matIdx];
                mat.Transparency = transparencyValue;
                mat.CommitChanges();
            }
        }

        public static void SetRhObjVisibility(RhinoDoc doc, RhinoObject rhObj, bool isVisible)
        {
            SetRhObjVisibility(doc, new List<RhinoObject>() { rhObj }, isVisible);
        }

        public static void SetRhObjVisibility(RhinoDoc doc, IEnumerable<RhinoObject> rhObjs, bool isVisible)
        {
            foreach (var rhinoObject in rhObjs)
            {
                rhinoObject.Attributes.Visible = isVisible;
                rhinoObject.CommitChanges();
            }
        }

        public static void ResetRhObjTransparency(RhinoDoc doc, RhinoObject rhObj)
        {
            ResetRhObjTransparency(doc, new List<RhinoObject>() { rhObj });
        }

        public static void ResetRhObjTransparency(RhinoDoc doc, IEnumerable<RhinoObject> rhObjs)
        {
            SetRhObjTransparency(doc, rhObjs, 0.0);
        }

        public static void ResetRhObjTransparency(IImplantDirector director, RhinoObject rhObj)
        {
            ResetRhObjTransparencies(director, new List<RhinoObject>() { rhObj });
        }

        public static void ResetRhObjTransparencies(IImplantDirector director, IEnumerable<RhinoObject> rhObjs)
        {
            SetRhObjTransparencies(director, rhObjs, 0.0);
        }

        public static void ResetRhObjectMeshVerticesColors(IImplantDirector director, RhinoObject rhObj)
        {
            ResetRhObjectsMeshVerticesColors(director, new List<RhinoObject>() {rhObj});
        }

        public static void ResetRhObjectsMeshVerticesColors(IImplantDirector director, IEnumerable<RhinoObject> rhObjs)
        {
            foreach (var rhinoObject in rhObjs)
            {
                if (rhinoObject.ObjectType != ObjectType.Mesh)
                {
                    return;
                }

                var doc = director.Document;
                var id = rhinoObject.Id;

                doc.Objects.Unlock(id, true);

                var mesh = (Mesh)rhinoObject.Geometry;

                if (MeshUtilities.ResetVerticesColor(mesh, out var colorlessMesh))
                {
                    doc.Objects.Replace(id, colorlessMesh);
                }

                doc.Objects.Lock(id, true);
            }
        }

        public static LinearDimension CreateDimension(Point3d pt1, Point3d pt2, RhinoDoc doc)
        {
            var camera = doc.Views.ActiveView.ActiveViewport;
            var planeNormal = -camera.CameraDirection;
            planeNormal.Unitize();

            var planeAxisX = (pt2 - pt1) / 2;
            planeAxisX.Unitize();

            var planeAxisY = Vector3d.CrossProduct(planeNormal, planeAxisX);
            planeAxisY.Unitize();

            var measurementPlane = new Plane((pt2 + pt1) / 2, planeAxisX, planeAxisY);

            var dim = LinearDimension.Create(AnnotationType.Aligned, doc.DimStyles.Current, measurementPlane,
                planeAxisX, pt1, pt2,
                (pt2 + pt1) / 2, 0.0);

            dim.ArrowheadType1 = DimensionStyle.ArrowType.SolidTriangle;
            dim.ArrowheadType2 = DimensionStyle.ArrowType.SolidTriangle;

            return dim;
        }

        public static void ToggleShowHideRhObj(RhinoDoc doc, RhinoObject rhObj, bool ignoreLayerMode = true)
        {
            if (rhObj.IsHidden)
            {
                doc.Objects.Show(rhObj.Id, ignoreLayerMode);
            }
            else
            {
                doc.Objects.Hide(rhObj.Id, ignoreLayerMode);
            }
        }
    }
}
