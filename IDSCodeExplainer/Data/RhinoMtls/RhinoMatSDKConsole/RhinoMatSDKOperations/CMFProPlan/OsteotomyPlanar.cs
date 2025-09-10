using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Materialise.SDK.MDCK.Model.Objects;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public class OsteotomyPlanar : Osteotomy
    {
        public double Width { get; set; }
        public double Height { get; set; }

        public OsteotomyPlanar()
        {
            ControlPoints = new List<Tuple<Point3D, Vector3D>>();
        }

        //-----------------------------------------------------------------------------
        public void _GetMidWidthHeight(out Point3D o_mid, out Vector3D o_width_vector, out Vector3D o_height_vector)
          {
            var pt1 = ControlPoints[0].Item1;
            var pt2 = ControlPoints[1].Item1;
            var pt3 = ControlPoints[2].Item1;

            var add = new Point3D(pt1.X + pt2.X+ pt3.X, pt1.Y + pt2.Y + pt3.Y, pt1.Z + pt2.Z + pt3.Z);
            o_mid = new Point3D(add.X/3.0, add.Y/3.0, add.Z/3.0);
            o_width_vector = Point3D.Subtract(pt2, pt1);
            o_width_vector.Normalize();
            var width_vector = o_width_vector;
            var minusP3P1 = Point3D.Subtract(pt3, pt1);
            var dotVect = Vector3D.DotProduct(minusP3P1, width_vector);
            var multiplyVec = Vector3D.Multiply(width_vector, dotVect);
            o_height_vector = Vector3D.Subtract(minusP3P1, multiplyVec);
            o_height_vector.Normalize();
        }


        public override Model ReConstruct(TransformationInfo transformationInfo)
        {     
            Point3D midp;
            Vector3D width_vector, height_vector;
            _GetMidWidthHeight(out midp, out width_vector, out height_vector);
            width_vector.Normalize();
            height_vector.Normalize();
            width_vector *= Width / 2.0;
            height_vector *= Height / 2.0;

            List<Point3D> cornerPts = new List<Point3D>();
            cornerPts.Add(midp + width_vector + height_vector);
            cornerPts.Add(midp + width_vector - height_vector);
            cornerPts.Add(midp - width_vector - height_vector);
            cornerPts.Add(midp - width_vector + height_vector);

            var planeDir = Vector3D.CrossProduct(ControlPoints[1].Item1 - ControlPoints[0].Item1, ControlPoints[2].Item1 - ControlPoints[0].Item1);
            planeDir.Normalize();

            var thickness_vector = new Vector3D((planeDir.X * Thickness) / 2, (planeDir.Y * Thickness) / 2, (planeDir.Z * Thickness) / 2);

            List<Vertex> vertices = new List<Vertex>();
            var model = new Model();
            for (int i = 0; i < cornerPts.Count; ++i)
            {
                vertices.Add(ProplanUtilities.AddVertexPoint(cornerPts[i] + thickness_vector, model));
            }

            for (int j = 0; j < cornerPts.Count; ++j)
            {
                vertices.Add(ProplanUtilities.AddVertexPoint(cornerPts[j] - thickness_vector, model));
            }

            ProplanUtilities.AddTriangle(vertices[0], vertices[2], vertices[1], model);
            ProplanUtilities.AddTriangle(vertices[0], vertices[3], vertices[2], model);
            ProplanUtilities.AddTriangle(vertices[0], vertices[5], vertices[4], model);
            ProplanUtilities.AddTriangle(vertices[0], vertices[1], vertices[5], model);
            ProplanUtilities.AddTriangle(vertices[1], vertices[6], vertices[5], model);
            ProplanUtilities.AddTriangle(vertices[1], vertices[2], vertices[6], model);
            ProplanUtilities.AddTriangle(vertices[2], vertices[3], vertices[7], model);
            ProplanUtilities.AddTriangle(vertices[2], vertices[7], vertices[6], model);
            ProplanUtilities.AddTriangle(vertices[3], vertices[0], vertices[4], model);
            ProplanUtilities.AddTriangle(vertices[3], vertices[4], vertices[7], model);
            ProplanUtilities.AddTriangle(vertices[4], vertices[5], vertices[6], model);
            ProplanUtilities.AddTriangle(vertices[4], vertices[6], vertices[7], model);

            ProplanUtilities.AutoAdjustNormal(model);
// 
//             var coordSys = transformationInfo.CoordinateSystems;
//             var transfromMatrix = coordSys.Find(item => item.id == CoordinateSysId).TransformMatrix;
//             transfromMatrix.Invert();
//             //             var trans = new MDCK.Math.CoordinateSystems.Transformation();
//             //             trans.Matrix = transfromMatrix;
//             //             trans.Invert();

            //             var modelTrans = new MDCK.Operators.ModelTransform();
            //             modelTrans.Transformation = trans;
            //             modelTrans.AddModel(model);
            //             modelTrans.Operate();
            //             modelTrans.Dispose();

            return model;
        }
    }
}
