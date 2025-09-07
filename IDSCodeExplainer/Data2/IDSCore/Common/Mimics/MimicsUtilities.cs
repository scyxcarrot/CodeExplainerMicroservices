using Rhino.Geometry;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IDS.Core.Utilities
{
    public static class MimicsUtilities
    {
        /**
         * Read a plane from a Mimics text file.
         */

        public static bool ReadMimicsPlane(string filepath, out Plane plane)
        {
            // Check inputs
            plane = Plane.Unset;
            if (!System.IO.File.Exists(filepath))
                return false;

            // Read the text file as UTF8
            using (StreamReader reader = new StreamReader(filepath, Encoding.UTF8))
            {
                // Read the contents
                char[] buffer = new char[600]; // max length of Mimics plane file: prevent overflow
                reader.Read(buffer, 0, buffer.Length);
                string contents = new string(buffer);
                string[] contents_lines = contents.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Find the line that contains our plane definition Regex matching 8 consecutive
                // floats followed by whitespace
                var re = new Regex(@"((-?\d+\.\d+)\s+){8}", RegexOptions.IgnoreCase);
                string plane_params = contents_lines.FirstOrDefault(s => re.IsMatch(s));
                if (null == plane_params)
                    return false;

                // Extract the parameters
                var re_float = new Regex(@"(-?\d+\.\d+)"); // matches a single float
                MatchCollection m = re_float.Matches(plane_params);
                if (m.Count != 8)
                    return false;
                try
                {
                    // Find the parameters (floats) using regex
                    var num_style = System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowExponent;
                    var cult = System.Globalization.CultureInfo.InvariantCulture;
                    
                    double px = double.Parse(m[0].Groups[1].Value, num_style, cult);
                    double py = double.Parse(m[1].Groups[1].Value, num_style, cult);
                    double pz = double.Parse(m[2].Groups[1].Value, num_style, cult);
                    double nx = double.Parse(m[3].Groups[1].Value, num_style, cult);
                    double ny = double.Parse(m[4].Groups[1].Value, num_style, cult);
                    double nz = double.Parse(m[5].Groups[1].Value, num_style, cult);
                    //double width = double.Parse(m[6].Groups[1].Value, num_style, cult);
                    //double height = double.Parse(m[7].Groups[1].Value, num_style, cult);
                    //var format = System.Globalization.NumberFormatInfo.InvariantInfo;

                    // Construct the plane
                    var origin = new Point3d(px, py, pz);
                    var normal = new Vector3d(nx, ny, nz);
                    plane = new Plane(origin, normal);
                }
                catch (FormatException)
                {
                    return false;
                }
            }
            return true;
        }
    }
}