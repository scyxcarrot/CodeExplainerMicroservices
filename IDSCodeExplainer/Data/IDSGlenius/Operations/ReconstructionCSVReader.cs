using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.Glenius.Operations
{
    public class ReconstructionCSVReader
    {
        public Point3d angleInf;
        public Point3d trig;
        public Point3d glenPlaneOrigin;
        public Vector3d glenPlaneNormal;

        public bool Read(string csvFile)
        {
            string[] lines = File.ReadAllLines(csvFile);

            foreach (var l in lines)
            {
                var values = l.Split(',');

                if (values.Count() == 4)
                {
                    if (values[0] == "AngInf")
                    {
                        angleInf = new Point3d(Double.Parse(values[1], CultureInfo.InvariantCulture),
                            Double.Parse(values[2], CultureInfo.InvariantCulture), Double.Parse(values[3], CultureInfo.InvariantCulture));
                    }
                    else if (values[0] == "Trig")
                    {
                        trig = new Point3d(Double.Parse(values[1], CultureInfo.InvariantCulture),
                            Double.Parse(values[2], CultureInfo.InvariantCulture), Double.Parse(values[3], CultureInfo.InvariantCulture));
                    }
                    else if (values[0] == "GlenPlaneOrigin")
                    {
                        glenPlaneOrigin = new Point3d(Double.Parse(values[1], CultureInfo.InvariantCulture),
                            Double.Parse(values[2], CultureInfo.InvariantCulture), Double.Parse(values[3], CultureInfo.InvariantCulture));
                    }
                    else if (values[0] == "GlenPlaneNormal")
                    {
                        glenPlaneNormal = new Vector3d(Double.Parse(values[1], CultureInfo.InvariantCulture),
                            Double.Parse(values[2], CultureInfo.InvariantCulture), Double.Parse(values[3], CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        return false; //Should not reach here
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }


    }
}
