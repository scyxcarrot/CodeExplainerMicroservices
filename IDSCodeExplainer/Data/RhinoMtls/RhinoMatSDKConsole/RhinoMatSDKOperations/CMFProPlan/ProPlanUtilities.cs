using Materialise.SDK.MDCK.Model.Objects;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public static class ProplanUtilities
    {
        public static Vertex AddVertexPoint(Point3D point, Model model)
        {
            var addModelVertex = new MDCK.Operators.ModelAddVertex();
            addModelVertex.Model = model;
            addModelVertex.InputPoint = point;
            addModelVertex.Operate();
            var vertex = addModelVertex.NewModelVertex;
            addModelVertex.Dispose();
            return vertex;
        }

        public static void AddTriangle(Vertex vertex1, Vertex vertex2, Vertex vertex3, Model model)
        {
            var addModelTriangle = new MDCK.Operators.ModelAddTriangle
            {
                VertexFirst = vertex1,
                VertexSecond = vertex2,
                VertexThird = vertex3,
                Model = model
            };
            try
            {
                addModelTriangle.Operate();
            }
            catch (MDCK.Operators.ModelAddTriangle.Exception ex)
            {
                throw new Exception("Could not add triangle: " + ex.Message);
            }
            finally
            {
                addModelTriangle.Dispose();
            }
        }

        public static void AutoAdjustNormal(Model model)
        {
            var autoAdjustNormal = new MDCK.Operators.ModelAutoAdjustNormals
            {                
                Model = model
            };
            try
            {
                autoAdjustNormal.Operate();
            }
            catch (MDCK.Operators.ModelAutoAdjustNormals.Exception ex)
            {
                throw new Exception("Could not adjust normal: " + ex.Message);
            }
            finally
            {
                autoAdjustNormal.Dispose();
            }
        }

        public static List<Tuple<Point3D, Vector3D>> TransformPoints(List<Tuple<Point3D, Vector3D>> points, Matrix3D transformMatrix)
        {
            List<Tuple<Point3D, Vector3D>> transformedPts = new List<Tuple<Point3D, Vector3D>>();
            foreach (var pt in points)
            {
                var transf = transformMatrix;
                transf.Invert();
                var firstPt = transf.Transform(pt.Item1);
                var secondPt = transf.Transform(pt.Item2);
                transformedPts.Add(new Tuple<Point3D, Vector3D>(firstPt, secondPt));
            }
            return transformedPts;
        }
    }
}
