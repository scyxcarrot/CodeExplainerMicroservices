using Materialise.SDK.MDCK.Math.Objects;
using Materialise.SDK.MDCK.Model.Objects;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public class OsteotomyCuttingPath : Osteotomy
    {
        public OsteotomyCuttingPath()
        {
            ControlPoints = new List<Tuple<Point3D, Vector3D>>();
        }

        private Vector3D ThicknessVectorTwoPoint(Point3D firstPoint, Point3D secondPoint, Vector3D firstAxis, double bladeThickness)
        {
            Vector3D segmentDir = secondPoint - firstPoint;
            segmentDir.Normalize();
            Vector3D thicknessVec = Vector3D.CrossProduct(firstAxis, segmentDir);
            thicknessVec.Normalize();
            var cross_product = Vector3D.CrossProduct(segmentDir, thicknessVec);
            if (Vector3D.DotProduct(firstAxis, cross_product) > 0)
            {
                thicknessVec *= bladeThickness;
            }
            else
            {
                thicknessVec *= -bladeThickness;
            }
            return thicknessVec;
        }

        private Vector3D ThicknessVector(Point3D firstPoint, Point3D secondPoint, Point3D thirdPoint, Vector3D firstAxis, double bladeThickness)
        {
            Vector3D thicknessVector;
            if (firstAxis.Length > 0)
            {
//                 var projDir = firstAxis;
//                 projDir.Normalize();
//                 //TODO to add thickness
//                 var distPrevPoint = (firstPoint - secondPoint).Length;
//                 var PrevPoint_proj = firstPoint - distPrevPoint * projDir;
//                 var distPoint = (secondPoint - secondPoint).Length;
//                 var Point_proj = secondPoint - distPoint * projDir;
//                 var distNextPoint = (thirdPoint - secondPoint).Length;
//                 var NextPoint_proj = thirdPoint - distNextPoint * projDir;
//                 firstPoint = PrevPoint_proj;
//                 secondPoint = Point_proj;
//                 thirdPoint = NextPoint_proj;

                Vector3D to_prev = new Vector3D(firstPoint.X - secondPoint.X, firstPoint.Y - secondPoint.Y, firstPoint.Z - secondPoint.Z);
                to_prev.Normalize();
                Vector3D to_next = new Vector3D(thirdPoint.X - secondPoint.X, thirdPoint.Y - secondPoint.Y, thirdPoint.Z - secondPoint.Z);
                to_next.Normalize();
                Vector3D plane_vec = to_prev - to_next; // An additional vector to which the height vector should be orthogonal.
                thicknessVector = Vector3D.CrossProduct(plane_vec, firstAxis);
                thicknessVector.Normalize();
                double cos_alpha = Vector3D.DotProduct(to_prev, to_next);
                thicknessVector *= bladeThickness / Math.Sqrt((1 - cos_alpha) / 2.0);
            }
            else
            {
                Vector3D segment = new Vector3D(-firstPoint.X + secondPoint.X,
                            -firstPoint.Y + secondPoint.Y,
                            -firstPoint.Z + secondPoint.Z);
                thicknessVector = new Vector3D(secondPoint.X - thirdPoint.X,
                            secondPoint.Y - thirdPoint.Y,
                            secondPoint.Z - thirdPoint.Z);
                segment.Normalize();
                thicknessVector.Normalize();
                Vector3D cross_product = Vector3D.CrossProduct(segment, thicknessVector);
                thicknessVector += segment;
                thicknessVector.Normalize();

                firstAxis.Normalize();
                if (Vector3D.DotProduct(firstAxis, cross_product) > 0)
                {
                    thicknessVector *= bladeThickness;
                }
                else
                {
                    thicknessVector *= -bladeThickness;
                }
            }
            return thicknessVector;
        }


        public override Model ReConstruct(TransformationInfo transformationInfo)
        {
            if (ControlPoints.Count< 3)
                return null;

            int last_index = ControlPoints.Count - 1;
            int last_index_half = ControlPoints.Count / 2 - 1;

            List<Vertex> back_point = new List<Vertex>();
            List<Vertex> back_point_top = new List<Vertex>();
            Vertex [] p_new_points = new Vertex[8];
            Vertex [] p_first_new_point = new Vertex[4];

            Vector3D heightVec = new Vector3D();

            Point3D prev_point, point, next_point;
            Point3D prev_point_back, point_back, next_point_back;

            prev_point = ControlPoints[0].Item1;
            point = ControlPoints[1].Item1;
            next_point = ControlPoints[2].Item1;
            prev_point_back = ControlPoints[last_index].Item1;
            point_back = ControlPoints[last_index - 1].Item1;
            next_point_back = ControlPoints[last_index - 2].Item1;

            //compute height vector
            Vector3D dir = point - point_back;
            if (!IsClosed && ControlPoints.Count != 4)
                heightVec = ThicknessVector(prev_point, point, next_point, dir, Thickness);
            else if (ControlPoints.Count == 4)
                heightVec = ThicknessVectorTwoPoint(prev_point, point, dir, Thickness);
            else
            {// use last point of half row to compute height
                Point3D prev_prev_point = ControlPoints[last_index_half].Item1;
                heightVec = ThicknessVector(prev_prev_point, prev_point, point, dir, Thickness);
            }

            if (!IsClosed)
            {
                Vector3D extension_front = new Vector3D(prev_point.X - point.X, prev_point.Y- point.Y, prev_point.Z - point.Z);
                extension_front.Normalize();
                extension_front *= ExtensionFront;
                prev_point += extension_front;
                prev_point_back += extension_front;
                if (ControlPoints.Count == 4)
                {
                    Vector3D extension_end = new Vector3D(point.X - prev_point.X, point.Y - prev_point.Y, point.Z - prev_point.Z);
                    extension_end.Normalize();
                    extension_end *= ExtensionBack;
                    point += extension_end;
                    point_back += extension_end;
                }
            }

            var model = new Model();
            // add points to stl
            p_new_points[0] = ProplanUtilities.AddVertexPoint(prev_point_back, model);
            back_point.Add(p_new_points[0]);

            var multiplyFirst = new Point3D(prev_point.X*2.0, prev_point.Y * 2.0, prev_point.Z * 2.0);
            var minusFirst = new Point3D(multiplyFirst.X - prev_point_back.X, multiplyFirst.Y - prev_point_back.Y, multiplyFirst.Z - prev_point_back.Z);
            p_new_points[1] = ProplanUtilities.AddVertexPoint(minusFirst, model);
            p_new_points[2] = ProplanUtilities.AddVertexPoint(minusFirst+heightVec, model);
            p_new_points[3] = ProplanUtilities.AddVertexPoint(prev_point_back + heightVec, model);

            if (!IsClosed)
            {
                // side face
                ProplanUtilities.AddTriangle(p_new_points[1], p_new_points[0], p_new_points[3], model);
                ProplanUtilities.AddTriangle(p_new_points[2], p_new_points[1], p_new_points[3], model);
            }
            else
            {
                p_first_new_point[0] = p_new_points[0];
                p_first_new_point[1] = p_new_points[1];
                p_first_new_point[2] = p_new_points[2];
                p_first_new_point[3] = p_new_points[3];
            }

            for (int i = 1; i <= last_index_half; i++)
            {
                // find normal to polyline
                if ((i == last_index_half) && (!IsClosed))
                {
                    // use same height as for previous point
                }
                else
                {
                    dir = point - point_back;
                    heightVec = ThicknessVector(prev_point, point, next_point, dir, Thickness);
                }

                var multiplySecond = new Point3D(point.X * 2.0, point.Y * 2.0, point.Z * 2.0);
                var minusSecond = new Point3D(multiplySecond.X - point_back.X, multiplySecond.Y - point_back.Y, multiplySecond.Z - point_back.Z);
                p_new_points[5] = ProplanUtilities.AddVertexPoint(minusSecond, model);
                p_new_points[4] = ProplanUtilities.AddVertexPoint(point_back, model);
                p_new_points[6] = ProplanUtilities.AddVertexPoint(minusSecond+heightVec, model);
                p_new_points[7] = ProplanUtilities.AddVertexPoint(point_back + heightVec, model);

                ProplanUtilities.AddTriangle(p_new_points[2], p_new_points[3], p_new_points[7], model);
                ProplanUtilities.AddTriangle(p_new_points[2], p_new_points[7], p_new_points[6], model);
                ProplanUtilities.AddTriangle(p_new_points[0], p_new_points[1], p_new_points[4], model);
                ProplanUtilities.AddTriangle(p_new_points[1], p_new_points[5], p_new_points[4], model);
                ProplanUtilities.AddTriangle(p_new_points[7], p_new_points[3], p_new_points[0], model);
                ProplanUtilities.AddTriangle(p_new_points[7], p_new_points[0], p_new_points[4], model);
                ProplanUtilities.AddTriangle(p_new_points[2], p_new_points[6], p_new_points[1], model);
                ProplanUtilities.AddTriangle(p_new_points[1], p_new_points[6], p_new_points[5], model);
                              

                // shift
                p_new_points[0] = p_new_points[4];
                p_new_points[1] = p_new_points[5];
                p_new_points[2] = p_new_points[6];
                p_new_points[3] = p_new_points[7];
                prev_point = point;
                point = next_point;
                prev_point_back = point_back;
                point_back = next_point_back;
                if (i < ControlPoints.Count / 2 - 2)
                {
                    next_point = ControlPoints[i + 2].Item1;
                    next_point_back = ControlPoints[last_index-i-2].Item1;
                    // next_point isn't recalculated anymore the moment the current point is 2 points away from the end
                    //const MMTransform &point_object = _GetTransformPointToObjectCoordinateSystem(i + 2);
                    //point_object.Transform(next_point, GetPoint(i + 2));
                    //const MMTransform &point_object2 = _GetTransformPointToObjectCoordinateSystem(last_index - i - 2);
                    //point_object2.Transform(next_point_back, GetPoint(last_index - i - 2));
                }
                if ((i == ControlPoints.Count / 2 - 2) && (IsClosed))
                {
                    next_point = ControlPoints[0].Item1;
                    next_point_back = ControlPoints[last_index].Item1;
                    //                     const MMTransform &point_object = _GetTransformPointToObjectCoordinateSystem(0);
                    //                     point_object.Transform(next_point, GetPoint(0));
                    //                     const MMTransform &point_object2 = _GetTransformPointToObjectCoordinateSystem(last_index);
                    //                     point_object2.Transform(next_point_back, GetPoint(last_index));
                }
                if ((i == ControlPoints.Count / 2 - 3) && (!IsClosed))
                {
                    // the current point is 3 positions away from the end of the CP
                    Vector3D extension_end = next_point - point;
                    extension_end.Normalize();
                    extension_end *= ExtensionBack;
                    next_point += extension_end;
                    next_point_back += extension_end;
                }
                if ((ControlPoints.Count == 6) && i == 1 && (!IsClosed))
                {
                    // making sure the end extension is also edittable with a CP of 3 cutting points
                    Vector3D extension_end = point - prev_point;
                    extension_end.Normalize();
                    extension_end *= ExtensionBack;
                    point += extension_end;
                    point_back += extension_end;
                }
                if ((i == ControlPoints.Count / 2 - 1) && (IsClosed))
                {
                    next_point = ControlPoints[1].Item1;
                    next_point_back = ControlPoints[last_index - i ].Item1;
                    // the current point is 1 position away from the end of the CP (last point of the loop)
                    //                     const MMTransform &point_object = _GetTransformPointToObjectCoordinateSystem(1);
                    //                     point_object.Transform(next_point, GetPoint(1));
                    //                     const MMTransform &point_object2 = _GetTransformPointToObjectCoordinateSystem(last_index - 1);
                    //                     point_object2.Transform(next_point_back, GetPoint(last_index - 1));
                }
            }

            if (IsClosed)
            {
                dir = point - point_back;
                heightVec = ThicknessVector(prev_point, point, next_point, dir, Thickness);

                p_new_points[4] = p_first_new_point[0];
                p_new_points[5] = p_first_new_point[1];
                p_new_points[6] = p_first_new_point[2];
                p_new_points[7] = p_first_new_point[3];

                ProplanUtilities.AddTriangle(p_new_points[2], p_new_points[3], p_new_points[7], model);
                ProplanUtilities.AddTriangle(p_new_points[2], p_new_points[7], p_new_points[6], model);
                ProplanUtilities.AddTriangle(p_new_points[0], p_new_points[1], p_new_points[4], model);
                ProplanUtilities.AddTriangle(p_new_points[1], p_new_points[5], p_new_points[4], model);
                ProplanUtilities.AddTriangle(p_new_points[7], p_new_points[3], p_new_points[0], model);
                ProplanUtilities.AddTriangle(p_new_points[7], p_new_points[0], p_new_points[4], model);
                ProplanUtilities.AddTriangle(p_new_points[2], p_new_points[6], p_new_points[1], model);
                ProplanUtilities.AddTriangle(p_new_points[1], p_new_points[6], p_new_points[5], model);
            }
            else
            {
                ProplanUtilities.AddTriangle(p_new_points[4], p_new_points[5], p_new_points[6], model);
                ProplanUtilities.AddTriangle(p_new_points[4], p_new_points[6], p_new_points[7], model);               
            }

            ProplanUtilities.AutoAdjustNormal(model);
            return model;
        }
    }
}
