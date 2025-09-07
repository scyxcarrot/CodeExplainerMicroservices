using Materialise.SDK.MDCK.Model.Objects;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public interface IOsteotomy
    {
        int CoordinateSysId { get; set; }
        string Label { get; set; }
        double Depth { get; set; }
        double Thickness { get; set; }
        double ExtensionFront { get; set; }
        double ExtensionBack { get; set; }
        bool IsClosed { get; set; }
        Vector3D Direction { get; set; }
       
        bool IsDefined { get; set; }
        List<Tuple<Point3D, Vector3D>> ControlPoints { get; set; }

        Model ReConstruct(TransformationInfo transformationInfo);

    }

    public abstract class Osteotomy : IOsteotomy
    {
        public int CoordinateSysId { get; set; }
        public string Label { get; set; }
        public double Depth { get; set; }
        public double Thickness { get; set; }
        public double ExtensionFront { get; set; }
        public double ExtensionBack { get; set; }
        public bool IsClosed { get; set; }
        public Vector3D Direction { get; set; }

        public bool IsDefined { get; set; }
        public virtual List<Tuple<Point3D, Vector3D>> ControlPoints { get; set; }

        public abstract Model ReConstruct(TransformationInfo transformationInfo);
    }
}
