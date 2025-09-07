using Rhino.Geometry;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace IDS.Core.Utilities
{
    public static class StlUtilities
    {
        /**
         * Write the given Rhino Mesh to a temporary STL file
         * with a unique name in the Windows Temp folder.
         */
        [Obsolete("Obsolete, please use StlUtilitiesV2.WriteStlTempFile")]
        public static string WriteStlTempFile(Mesh rhmesh)
        {
            // Create temp file with guaranteed unique name
            string filename = System.IO.Path.GetTempPath() + "IDS_" + Guid.NewGuid().ToString() + ".stl";
            RhinoMesh2StlBinary(rhmesh, filename);
            return filename;
        }

        /**
         * Read a binary STL file and convert it to a Rhino mesh.
         *
         * @param filepath                  path to STL file
         * @param[out] rhmesh               Rhino mesh read from STL
         * @throws ArgumentNullException    String is null or empty
         * @throws ArgumentException        String does not point to
         *                                  a valid file
         * @return      true on success, false on failure
         */
        [Obsolete("Obsolete, please use StlUtilitiesV2.StlBinaryToIDSMesh")]
        public static bool StlBinary2RhinoMesh(string filepath, out Mesh rhmesh)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                throw new ArgumentNullException("filepath");
            }

            if (!System.IO.File.Exists(filepath))
            {
                throw new ArgumentException("Invalid file");
            }

            // Mesh to contain vertices and faces
            rhmesh = new Mesh();

            // === Binary STL format === UINT8[80] – Header UINT32 – Number of triangles
            //
            // foreach triangle REAL32[3] – Normal vector REAL32[3] – Vertex 1 REAL32[3] – Vertex 2
            // REAL32[3] – Vertex 3 UINT16 – Attribute byte count end

            // Read byte stream
            using (Stream stream = File.OpenRead(filepath))
            {
                if (IsTextEncoded(stream))
                {
                    return false;
                }
                else
                {
                    using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
                    {
                        // Read (and ignore) the header and number of triangles.
                        var buffer = reader.ReadBytes(80); // Header
                        reader.ReadBytes(4); // Number of triangles

                        // Read mesh faces
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            // Read the normal
                            Point3f n, a, b, c;
                            bool rc = ReadPointSingle(reader, out n);

                            // Read the vertices
                            rc &= ReadPointSingle(reader, out a);
                            rc &= ReadPointSingle(reader, out b);
                            rc &= ReadPointSingle(reader, out c);
                            reader.ReadUInt16();

                            // Check data
                            if (!rc)
                            {
                                break;
                            }

                            // Add to Rhino mesh
                            int aid = rhmesh.Vertices.Add(a);
                            int bid = rhmesh.Vertices.Add(b);
                            int cid = rhmesh.Vertices.Add(c);
                            rhmesh.Faces.AddFace(aid, bid, cid);
                        }
                    }
                }
            }

            // Clean up the mesh
            rhmesh.Vertices.UseDoublePrecisionVertices = false;
            rhmesh.Weld(2 * Math.PI);
            rhmesh.Compact();
            rhmesh.Normals.ComputeNormals();
            rhmesh.Faces.CullDegenerateFaces();
            return true;
        }

        /**
         * Read a point specified as 3 single precision
         * floating point values from a binary stream.
         *
         * @return      The point on success, or Point3f.Unset
         *              on failure.
         */
        [Obsolete("Obsolete, please use StlUtilitiesV2.ReadPointSingle")]
        public static bool ReadPointSingle(BinaryReader reader, out Point3f point)
        {
            point = Point3f.Unset;
            if (reader == null)
            {
                return false;
            }

            const int floatSize = sizeof(float);
            const int vertexSize = (floatSize * 3);

            //Read 3 floats.
            byte[] data = new byte[vertexSize];
            int bytesRead = reader.Read(data, 0, data.Length);

            //If no bytes are read then we're at the end of the stream.
            if (bytesRead == 0)
                return false;
            else if (bytesRead != data.Length)
                throw new FormatException(string.Format("Could not convert the binary data to a vertex. Expected {0} bytes but found {1}.", vertexSize, bytesRead));

            // Read normal
            float x = BitConverter.ToSingle(data, 0);
            float y = BitConverter.ToSingle(data, floatSize);
            float z = BitConverter.ToSingle(data, (floatSize * 2));
            point = new Point3f(x, y, z);
            return true;
        }

        /**
         * Determines if the STL file mapped by the input stream is text or
         * binary based.
         */
        [Obsolete("Obsolete, please use StlUtilitiesV2.IsTextEncoded")]
        public static bool IsTextEncoded(Stream stream)
        {
            const string solid = "solid";

            byte[] buffer = new byte[5];
            string header = null;

            //Reset the stream to tbe beginning and read the first few bytes, then reset the stream to the beginning again.
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(buffer, 0, buffer.Length);
            stream.Seek(0, SeekOrigin.Begin);

            //Read the header as ASCII and compare it to keyword
            header = Encoding.ASCII.GetString(buffer);
            bool istext = string.Equals(solid, header, StringComparison.InvariantCultureIgnoreCase);
            return istext;
        }

        /// <summary>
        /// Imports the ASCII STL.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        public static Mesh ImportAsciiStl(string filePath)
        {
            Mesh importedMesh = new Mesh();
            double[][] vertices = new double[3][];
            int v = 0;

            using (StreamReader sr = File.OpenText(filePath))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split(' ');
                    if(parts.Length > 0)
                    {
                        if (parts[0].ToLower() == "facet" && parts[1].ToLower() == "normal")
                        {
                            // do nothing
                        }
                        else if (parts[0].ToLower() == "outer" && parts[1].ToLower() == "loop")
                        {
                            // Initialize new vertex array
                            vertices = new double[3][];
                            vertices[0] = new double[3];
                            vertices[1] = new double[3];
                            vertices[2] = new double[3];
                            v = 0;
                        }
                        else if (parts[0].ToLower() == "vertex")
                        {
                            // store vertex
                            vertices[v][0] = double.Parse(parts[1]);
                            vertices[v][1] = double.Parse(parts[2]);
                            vertices[v][2] = double.Parse(parts[3]);
                            // increment vertex index
                            v++;
                        }
                        else if (parts[0].ToLower() == "endloop")
                        {
                            // add face and vertices
                            importedMesh.Vertices.Add(vertices[0][0], vertices[0][1], vertices[0][2]);
                            importedMesh.Vertices.Add(vertices[1][0], vertices[1][1], vertices[1][2]);
                            importedMesh.Vertices.Add(vertices[2][0], vertices[2][1], vertices[2][2]);
                            importedMesh.Faces.AddFace(importedMesh.Vertices.Count - 3, importedMesh.Vertices.Count - 2, importedMesh.Vertices.Count - 1);
                        }
                        else if (parts[0].ToLower() == "endfacet")
                        {
                            // do nothing
                        }
                    }
                }
            }

            // Simplify mesh
            importedMesh.Compact();
            importedMesh.Faces.CullDegenerateFaces();
            return importedMesh;
        }

        /// <summary>
        /// Write Rhino Mesh as ASCII STL
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="filepath">The filepath.</param>
        public static void RhinoMesh2StlAscii(Mesh mesh, string filepath)
        {
            // Make sure mesh is triangulated
            if (mesh.Faces.QuadCount > 0)
                mesh.Faces.ConvertQuadsToTriangles();
            mesh.FaceNormals.ComputeFaceNormals(); // Face normals need to be computed for STL

            // Open a writer
            System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);

            // Loop over screws and write to stl file
            file.WriteLine("solid IDS_ASCII_Export");

            Point3f v1;
            Point3f v2;
            Point3f v3;
            Point3f v4;
            Vector3d n;
            
            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                mesh.Faces.GetFaceVertices(i, out v1, out v2, out v3, out v4);
                n = mesh.FaceNormals[i];


                file.WriteLine(string.Format(CultureInfo.InvariantCulture, "facet normal {0,0:F8} {1,0:F8} {2,0:F8}", n.X, n.Y, n.Z));
                file.WriteLine("outer loop");
                file.WriteLine(string.Format(CultureInfo.InvariantCulture, "vertex {0,0:F8} {1,0:F8} {2,0:F8}", v1.X, v1.Y, v1.Z));
                file.WriteLine(string.Format(CultureInfo.InvariantCulture, "vertex {0,0:F8} {1,0:F8} {2,0:F8}", v2.X, v2.Y, v2.Z));
                file.WriteLine(string.Format(CultureInfo.InvariantCulture, "vertex {0,0:F8} {1,0:F8} {2,0:F8}", v3.X, v3.Y, v3.Z));
                file.WriteLine("endloop");
                file.WriteLine("endfacet");
            }

            file.WriteLine("endsolid IDS_ASCII_Export");

            // Close file
            file.Close();
        }

        /// <summary>
        /// Exports a Rhino Mesh to an STL file
        /// </summary>
        /// <param name="rhmesh">The rhmesh.</param>
        /// <param name="filepath">The filepath.</param>
        [Obsolete("Obsolete, please use StlUtilitiesV2.IDSMeshToStlBinary")]
        public static void RhinoMesh2StlBinary(Mesh rhmesh, string filepath)
        {
            int[] theColor = new int[0];
            RhinoMesh2StlBinary(rhmesh, filepath, theColor);
        }

        /// <summary>
        /// Exports a Rhino Mesh to an STL file
        /// </summary>
        /// <param name="rhmesh">The rhmesh.</param>
        /// <param name="filepath">The filepath.</param>
        /// <param name="theColor">The color.</param>
        /// <exception cref="ArgumentNullException">filepath</exception>
        [Obsolete("Obsolete, please use StlUtilitiesV2.IDSMeshToStlBinary")]
        public static void RhinoMesh2StlBinary(Mesh rhmesh, string filepath, int[] inputTheColor)
        {
            var theColor = inputTheColor;

            if (string.IsNullOrEmpty(filepath))
            {
                throw new ArgumentNullException("filepath");
            }

            // default color
            if (theColor.Length != 3)
            {
                theColor = new int[3] { 120, 100, 100 }; // dark red-gray
            }

            // Make sure mesh is triangulated
            if (rhmesh.Faces.QuadCount > 0)
            {
                rhmesh.Faces.ConvertQuadsToTriangles();
            }
            rhmesh.FaceNormals.ComputeFaceNormals(); // Face normals need to be computed for STL

            // Write the file
            // NOTE: The using directive opens the underlying stream with a hint to Windows that
            //       you'll be accessing it sequentially. In addition, it tells the runtime to do all
            //       the cleanup works on the Windows file handles.
            // NOTE: you can chain using directives without nesting blocks.
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            using (Stream stream = File.Create(filepath))
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII))
            {
                // header is 80 bytes in total
                byte[] headerFull = new byte[80];

                // First 70 bytes are the header text
                byte[] headerText = Encoding.ASCII.GetBytes("Binary STL - Mobelife IDS");
                byte[] headerTextFull = new byte[70];
                Buffer.BlockCopy(headerText, 0, headerTextFull, 0, Math.Min(headerText.Length, headerTextFull.Length));

                Buffer.BlockCopy(headerTextFull, 0, headerFull, 0, Math.Min(headerTextFull.Length, headerFull.Length));

                // Next 6 bytes are the text COLOR=
                byte[] colorText = Encoding.ASCII.GetBytes("COLOR=");
                Buffer.BlockCopy(colorText, 0, headerFull, 70, colorText.Length);

                // Next 6 bytes are the text COLOR=
                byte[] colorValue = new byte[4];
                colorValue[0] = (byte)theColor[0];
                colorValue[1] = (byte)theColor[1];
                colorValue[2] = (byte)theColor[2];
                colorValue[3] = 1;
                Buffer.BlockCopy(colorValue, 0, headerFull, 76, colorValue.Length);

                // Write the header and face count
                writer.Write(headerFull);
                writer.Write((UInt32)rhmesh.Faces.Count);

                // Write each face sequentially
                for (int i = 0; i < rhmesh.Faces.Count; i++)
                {
                    // First write the normal belonging to the face
                    Vector3f normal = rhmesh.FaceNormals[i];
                    writer.Write(normal.X);
                    writer.Write(normal.Y);
                    writer.Write(normal.Z);

                    // Write each vertex
                    Point3f vert_a = rhmesh.Vertices[rhmesh.Faces[i][0]];
                    writer.Write(vert_a.X);
                    writer.Write(vert_a.Y);
                    writer.Write(vert_a.Z);
                    Point3f vert_b = rhmesh.Vertices[rhmesh.Faces[i][1]];
                    writer.Write(vert_b.X);
                    writer.Write(vert_b.Y);
                    writer.Write(vert_b.Z);
                    Point3f vert_c = rhmesh.Vertices[rhmesh.Faces[i][2]];
                    writer.Write(vert_c.X);
                    writer.Write(vert_c.Y);
                    writer.Write(vert_c.Z);

                    // Define the colors per face
                    string binZero = "0";
                    string binR = Convert.ToString((int)(((double)theColor[0]) / 255 * 31), 2);
                    string binG = Convert.ToString((int)(((double)theColor[1]) / 255 * 31), 2);
                    string binB = Convert.ToString((int)(((double)theColor[2]) / 255 * 31), 2);

                    string colorTextValue = binZero + binB.PadLeft(5, '0') + binG.PadLeft(5, '0') + binR.PadLeft(5, '0');
                    UInt16 colorIntValue = Convert.ToUInt16(colorTextValue, 2);

                    writer.Write(colorIntValue);
                }
            }
        }

        // Mimicking export Mesh to STL and reimport back
        public static Mesh RebuildMesh(Mesh rhmesh)
        {
            // Make sure mesh is triangulated
            if (rhmesh.Faces.QuadCount > 0)
            {
                rhmesh.Faces.ConvertQuadsToTriangles();
            }
            rhmesh.FaceNormals.ComputeFaceNormals(); // Face normals need to be computed for STL

            var rebuiltMesh = new Mesh();
            //technically, we can just Duplicate rhmesh here and clean up

            for (int i = 0; i < rhmesh.Faces.Count; i++)
            {
                var vert_a = rhmesh.Vertices[rhmesh.Faces[i][0]];
                var vert_b = rhmesh.Vertices[rhmesh.Faces[i][1]];
                var vert_c = rhmesh.Vertices[rhmesh.Faces[i][2]];

                // Add to Rhino mesh
                int aid = rebuiltMesh.Vertices.Add(vert_a);
                int bid = rebuiltMesh.Vertices.Add(vert_b);
                int cid = rebuiltMesh.Vertices.Add(vert_c);
                rebuiltMesh.Faces.AddFace(aid, bid, cid);
            }

            // Clean up the mesh
            rebuiltMesh.Vertices.UseDoublePrecisionVertices = false;
            rebuiltMesh.Weld(2 * Math.PI);
            rebuiltMesh.Compact();
            rebuiltMesh.Normals.ComputeNormals();
            return rebuiltMesh;
        }
    }
}