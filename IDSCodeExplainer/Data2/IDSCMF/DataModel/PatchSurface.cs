using Rhino.Collections;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IDS.CMF.DataModel
{
    public class PatchSurface : IGuideSurface
    {
        public List<Point3d> ControlPoints { get; set; }

        public double Diameter { get; set; }

        public bool IsNegative { get; set; }

        public static string SerializationLabelConst => "PatchSurface";
        public string SerializationLabel => SerializationLabelConst;
        private readonly string KeyControlPoints = "ControlPoints";
        private readonly string KeyDiameter = "Diameter";
        private readonly string KeyIsNegative = "IsNegative";

        public object Clone()
        {
            return new PatchSurface()
            {
                ControlPoints = new List<Point3d>(ControlPoints),
                Diameter = Diameter,
                IsNegative = IsNegative
            };
        }

        public bool DeSerialize(ArchivableDictionary serializer)
        {
            ControlPoints = new List<Point3d>();

            foreach (var d in serializer)
            {
                if (Regex.IsMatch(d.Key, KeyControlPoints + "_\\d+"))
                {
                    var ctrlPt = (Point3d)d.Value;
                    ControlPoints.Add(ctrlPt);
                }
            }

            Diameter = serializer.GetDouble(KeyDiameter);
            IsNegative = serializer.GetBool(KeyIsNegative);

            return true;
        }

        public bool Serialize(ArchivableDictionary serializer)
        {
            serializer.Set(Constants.Serialization.KeySerializationLabel, SerializationLabel);
            var controlPointCounter = 0;
            foreach (var controlPoint in ControlPoints)
            {
                serializer.Set(KeyControlPoints + $"_{controlPointCounter}", controlPoint);
                controlPointCounter++;
            }

            serializer.Set(KeyDiameter, Diameter);
            serializer.Set(KeyIsNegative, IsNegative);
            return true;
        }
    }
}