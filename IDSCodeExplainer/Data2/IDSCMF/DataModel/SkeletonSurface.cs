using Rhino.Collections;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IDS.CMF.DataModel
{
    public class SkeletonSurface : IGuideSurface
    {
        public List<List<Point3d>> ControlPoints { get; set; }

        public double Diameter { get; set; }

        public bool IsNegative { get; set; }

        public static string SerializationLabelConst => "SkeletonSurface";
        public string SerializationLabel => SerializationLabelConst;
        private const string KeyControlPoints = "ControlPoints";
        private const string KeyDiameter = "Diameter";
        private const string KeyIsNegative = "IsNegative";

        public object Clone()
        {
            var controlPoints = new List<List<Point3d>>();
            ControlPoints.ForEach(cp => controlPoints.Add(new List<Point3d>(cp)));
            return new SkeletonSurface()
            {
                ControlPoints = controlPoints,
                Diameter = Diameter,
                IsNegative = IsNegative
            };
        }

        public bool DeSerialize(ArchivableDictionary serializer)
        {
            ControlPoints = new List<List<Point3d>>();

            var controlPointsDict = new Dictionary<string, List<Point3d>>();
            foreach (var d in serializer)
            {
                
                if (Regex.IsMatch(d.Key, KeyControlPoints + "_\\d+_\\d+"))
                {
                    var re = new Regex(@"\d+");
                    var m1 = re.Match(d.Key);
                    var setNumber = m1.Value;

                    if (!controlPointsDict.ContainsKey(setNumber))
                    {
                        controlPointsDict.Add(setNumber, new List<Point3d>());
                        controlPointsDict[setNumber].Add((Point3d)d.Value);
                    }
                    else
                    {
                        controlPointsDict[setNumber].Add((Point3d)d.Value);
                    }
                }
            }

            foreach (var keyValuePair in controlPointsDict)
            {
                ControlPoints.Add(keyValuePair.Value);
            }

            Diameter = serializer.GetDouble(KeyDiameter);
            IsNegative = serializer.GetBool(KeyIsNegative);

            return true;
        }

        public bool Serialize(ArchivableDictionary serializer)
        {
            serializer.Set(Constants.Serialization.KeySerializationLabel, SerializationLabel);
            var controlPointSetCounter = 0;

            foreach (var controlPointSet in ControlPoints)
            {
                var controlPointCounter = 0;
                foreach (var controlPoint in controlPointSet)
                {
                    serializer.Set(KeyControlPoints + $"_{controlPointSetCounter}_{controlPointCounter}", controlPoint);
                    controlPointCounter++;
                }
                controlPointSetCounter++;
            }

            serializer.Set(KeyDiameter, Diameter);
            serializer.Set(KeyIsNegative, IsNegative);
            return true;
        }
    }
}