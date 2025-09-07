using Materialise.SDK.MatSAX;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public class ControlPointsParser : ISAXReadHandler
    {
        private List<Tuple<Point3D, Vector3D>> ControlPoints;
        private Point3D CurrentPoint;
        private Point3D CurrentVector;
        public ControlPointsParser(List<Tuple<Point3D, Vector3D>> ControlPointList)
        {
            ControlPoints = ControlPointList;
        }
        public bool HandleTag(string tag, MSAXReaderWrapper reader)
        {
            if (tag == "Point")
            {
                reader.ReadValue(out CurrentPoint);
                return true;
            }

            if (tag == "Normal")
            {
                reader.ReadValue(out CurrentVector);
                return true;
            }

            return false;
        }
        public void HandleEndTag(string tag)
        {
            if (tag == "Normal")
            {
                var controlPoint = new Tuple<Point3D, Vector3D>(CurrentPoint, new Vector3D(CurrentVector.X, CurrentVector.Y, CurrentVector.Z));
                ControlPoints.Add(controlPoint);
            }
        }

        public void InitAfterLoading()
        {
        }
    }


}
