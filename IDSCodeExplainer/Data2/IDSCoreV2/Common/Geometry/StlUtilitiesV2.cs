using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IDS.Core.V2.Geometry
{
    public static class StlUtilitiesV2
    {
        public static string WriteStlTempFile(IMesh idsMesh)
        {
            // Create temp file with guaranteed unique name
            var filename = Path.GetTempPath() + "IDS_" + Guid.NewGuid().ToString() + ".stl";
            IDSMeshToStlBinary(idsMesh, filename);
            return filename;
        }

        public static bool StlBinaryToIDSMesh(string filePath, out IMesh idsMesh)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Invalid file");
            }

            // Mesh to contain vertices and faces
            idsMesh = new IDSMesh();
            var faces = new List<IFace>();
            var vertices = new List<IVertex>();

            // === Binary STL format === UINT8[80] – Header UINT32 – Number of triangles
            //
            // foreach triangle REAL32[3] – Normal vector REAL32[3] – Vertex 1 REAL32[3] – Vertex 2
            // REAL32[3] – Vertex 3 UINT16 – Attribute byte count end

            // Read byte stream
            using (Stream stream = File.OpenRead(filePath))
            {
                if (IsTextEncoded(stream))
                {
                    return false;
                }

                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
                {
                    // Read (and ignore) the header and number of triangles.
                    _ = reader.ReadBytes(80); // Header
                    reader.ReadBytes(4); // Number of triangles
                    var index = 0; // Index of vertex

                    // Read mesh faces
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        //Read the normal
                        bool rc = ReadPointSingle(reader, out _);

                        // Read the vertices
                        rc &= ReadPointSingle(reader, out IPoint3F a);
                        rc &= ReadPointSingle(reader, out IPoint3F b);
                        rc &= ReadPointSingle(reader, out IPoint3F c);
                        reader.ReadUInt16();

                        // Check data
                        if (!rc)
                        {
                            break;
                        }

                        // Assign Vertices and Triangles
                        vertices.Add(new IDSVertex(Convert.ToDouble(a.X), Convert.ToDouble(a.Y), Convert.ToDouble(a.Z)));
                        vertices.Add(new IDSVertex(Convert.ToDouble(b.X), Convert.ToDouble(b.Y), Convert.ToDouble(b.Z)));
                        vertices.Add(new IDSVertex(Convert.ToDouble(c.X), Convert.ToDouble(c.Y), Convert.ToDouble(c.Z)));

                        var face = new IDSFace(Convert.ToUInt64(index), Convert.ToUInt64(index + 1),
                            Convert.ToUInt64(index + 2));
                        faces.Add(face);
                        index += 3;
                    }

                    // Create IDSMesh
                    idsMesh = new IDSMesh(vertices.ToVerticesArray2D(),
                        faces.ToFacesArray2D());
                }
            }

            return true;
        }

        public static bool IsTextEncoded(Stream stream)
        {
            const string solid = "solid";
            var buffer = new byte[5];

            //Reset the stream to tbe beginning and read the first few bytes, then reset the stream to the beginning again.
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(buffer, 0, buffer.Length);
            stream.Seek(0, SeekOrigin.Begin);

            //Read the header as ASCII and compare it to keyword
            var header = Encoding.ASCII.GetString(buffer);
            var isText = string.Equals(solid, header, StringComparison.InvariantCultureIgnoreCase);
            return isText;
        }

        public static bool ReadPointSingle(BinaryReader reader, out IPoint3F point)
        {
            point = IDSPoint3F.Unset();
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
            
            if (bytesRead != data.Length)
                throw new FormatException($"Could not convert the binary data to a vertex. Expected {vertexSize} bytes but found {bytesRead}.");

            // Read normal
            float x = BitConverter.ToSingle(data, 0);
            float y = BitConverter.ToSingle(data, floatSize);
            float z = BitConverter.ToSingle(data, (floatSize * 2));
            point = new IDSPoint3F(x, y, z);
            return true;
        }

        public static void IDSMeshToStlBinary(IMesh mesh, string filepath)
        {
            int[] theColor = new int[0];
            IDSMeshToStlBinary(mesh, filepath, theColor);
        }

        public static void IDSMeshToStlBinary(IMesh mesh, string filepath, int[] inputTheColor)
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
                writer.Write((UInt32)mesh.Faces.Count);

                // Write each face sequentially
                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    // Original: First write the normal belonging to the face
                    // Writing arbitrary value because it is not used
                    writer.Write(0.0f);
                    writer.Write(0.0f);
                    writer.Write(0.0f);

                    var face = mesh.Faces[i];

                    // Write each vertex
                    var vert_a = mesh.Vertices[Convert.ToInt32(face.A)];
                    writer.Write((float)vert_a.X);
                    writer.Write((float)vert_a.Y);
                    writer.Write((float)vert_a.Z);
                    var vert_b = mesh.Vertices[Convert.ToInt32(face.B)];
                    writer.Write((float)vert_b.X);
                    writer.Write((float)vert_b.Y);
                    writer.Write((float)vert_b.Z);
                    var vert_c = mesh.Vertices[Convert.ToInt32(face.C)];
                    writer.Write((float)vert_c.X);
                    writer.Write((float)vert_c.Y);
                    writer.Write((float)vert_c.Z);

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
    }
}