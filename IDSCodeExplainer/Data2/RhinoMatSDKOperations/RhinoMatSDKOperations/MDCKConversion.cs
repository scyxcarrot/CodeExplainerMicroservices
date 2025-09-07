using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MDCK = Materialise.SDK.MDCK;
using MDCKTriangle = Materialise.SDK.MDCK.Model.Objects.Triangle;
using MDCKVertex = Materialise.SDK.MDCK.Model.Objects.Vertex;
using WPoint3d = System.Windows.Media.Media3D.Point3D;

namespace RhinoMatSDKOperations.IO
{
    public class MDCKConversion
    {
        /**
         * Determines if the STL file mapped by the input stream is text or
         * binary based.
         */

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

        /**
         * Read a point specified as 3 single precision
         * floating point values from a binary stream.
         *
         * @return      The point on success, or Point3f.Unset
         *              on failure.
         */

        public static bool ReadPointSingle(BinaryReader reader, out Point3f point)
        {
            point = Point3f.Unset;
            if (reader == null)
                return false;

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
         * Read a binary STL file and convert it to a Rhino mesh.
         *
         * @param filepath                  path to STL file
         * @param[out] rhmesh               Rhino mesh read from STL
         * @throws ArgumentNullException    String is null or empty
         * @throws ArgumentException        String does not point to
         *                                  a valid file
         * @return      true on success, false on failure
         */

        public static bool StlBinary2RhinoMesh(string filepath, out Mesh rhmesh)
        {
            if (null == filepath || filepath == "")
                throw new ArgumentNullException("filepath");
            else if (!System.IO.File.Exists(filepath))
                throw new ArgumentException("Invalid file");

            // Mesh to contain vertices and faces
            rhmesh = new Mesh();

            // === Binary STL format ===
            // UINT8[80] – Header
            // UINT32 – Number of triangles
            //
            // foreach triangle
            // REAL32[3] – Normal vector
            // REAL32[3] – Vertex 1
            // REAL32[3] – Vertex 2
            // REAL32[3] – Vertex 3
            // UINT16 – Attribute byte count
            // end

            // Read byte stream
            using (Stream stream = File.OpenRead(filepath))
            {
                if (IsTextEncoded(stream))
                {
                    return false;
                    //throw new ArgumentException("Only binary STL format is supported");
                    //using (StreamReader reader = new StreamReader(stream, Encoding.ASCII, true, DefaultBufferSize, true))
                }
                else
                {
                    using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
                    {
                        if (reader == null)
                            return false;

                        // Read (and ignore) the header and number of triangles.
                        byte[] buffer = new byte[80];
                        buffer = reader.ReadBytes(80); // Header
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
                            ushort AttributeByteCount = reader.ReadUInt16();

                            // Check data
                            if (!rc)
                                break;

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
            rhmesh.Vertices.CombineIdentical(true, true);
            rhmesh.Weld(2 * Math.PI);
            rhmesh.Compact();
            rhmesh.Normals.ComputeNormals();
            return true;
        }

        /**
         * Write the given Rhino Mesh to a temporary STL file
         * with a unique name in the Windows Temp folder.
         */

        public static string WriteStlTempFile(Mesh rhmesh)
        {
            // Create temp file with guaranteed unique name
            string filename = System.IO.Path.GetTempPath() + "IDS_" + Guid.NewGuid().ToString() + ".stl";
            RhinoMesh2StlBinary(rhmesh, filename);
            return filename;
        }

        /**
         * Write a Rhino Mesh to binary STL file format.
         * @throws ArgumentNullException    The filepath is null or empty.
         * @throws ArgumentExeption         Invalid file path
         * @throws System.IO.PathTooLongException
         * @throws SystemUnauthorizedAccessException
         */

        public static void RhinoMesh2StlBinary(Mesh rhmesh, string filepath)
        {
            int[] theColor = new int[0];
            RhinoMesh2StlBinary(rhmesh, filepath, theColor: theColor);
        }

        public static void RhinoMesh2StlBinary(Mesh rhmesh, string filepath, int[] theColor)
        {
            if (null == filepath || filepath == "")
                throw new ArgumentNullException("filepath");

            // default color
            if (theColor.Length != 3)
            {
                theColor = new int[3] { 120, 100, 100 }; // dark red-gray
            }

            // Make sure mesh is triangulated
            if (rhmesh.Faces.QuadCount > 0)
                rhmesh.Faces.ConvertQuadsToTriangles();
            rhmesh.FaceNormals.ComputeFaceNormals(); // Face normals need to be computed for STL

            // Write the file
            // NOTE: The using directive opens the underlying stream with a hint
            //       to Windows that you'll be accessing it sequentially. In addition,
            //       it tells the runtime to do all the cleanup works on the Windows
            //       file handles.
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

        ///////////////////////////////////////////////////////////////////////
        /// Safe methods using STL files as interface                       ///
        ///////////////////////////////////////////////////////////////////////

        /**
         * Read an STL file and return the MDCK model.
         * @param filepath          Path to the STL file.
         * @param[out] outmodel     The MDCK model that will contain the
         *                          mesh described by the STL file.
         * @param units             Rhino.UnitSystem of the document where
         *                          the STL originates.
         */

        public static bool Stl2MDCKMesh(string filepath, out MDCK.Model.Objects.Model outmodel, UnitSystem units = UnitSystem.Millimeters)
        {
            outmodel = new MDCK.Model.Objects.Model();
            using (var importer = new MDCK.Operators.ModelImportFromStl())
            {
                // Set operator parameters
                importer.FileName = filepath;
                importer.ForceLoad = true; // STL file format check is done before reading
                importer.OutputModel = outmodel;
                double unitfactor = 1.0; // Conversion factor: STL units to mm
                if (units == UnitSystem.Nanometers)
                    unitfactor = 1e-6;
                else if (units == UnitSystem.Microns)
                    unitfactor = 1e-3;
                else if (units == UnitSystem.Millimeters)
                    unitfactor = 1.0;
                else if (units == UnitSystem.Centimeters)
                    unitfactor = 10.0;
                else if (units == UnitSystem.Decimeters)
                    unitfactor = 100.0;
                else if (units == UnitSystem.Meters)
                    unitfactor = 1000.0;
                else if (units == UnitSystem.Dekameters)
                    unitfactor = 10000.0;
                else if (units == UnitSystem.Hectometers)
                    unitfactor = 100000.0;
                else if (units == UnitSystem.Kilometers)
                    unitfactor = 1000000.0;
                importer.MmPerUnit = unitfactor;

                // Call operator
                try
                {
                    importer.Operate(); // Import the STL
                }
                catch (MDCK.Operators.ModelImportFromStl.Exception)
                {
                    return false;
                }
                return true;
            }
        }

        /**
         * Convert a Mesh object into a MDCK model object.
         */

        public static bool Rhino2MDCKMeshStl(Mesh inmesh, out MDCK.Model.Objects.Model outmodel)
        {
            // Convert mesh to MDCK via STL
            string meshpath = WriteStlTempFile(inmesh);
            bool success = Stl2MDCKMesh(meshpath, out outmodel, UnitSystem.Millimeters);
            File.Delete(meshpath);
            return success;
        }

        /**
         * Convert Rhino Meshes to MDCK model with separate surfaces for each disjoint mesh.
         *
         * @pram rhmesh             RhinoMesh consisting of only triangular faces
         *                          with the faces of each subsurface stored
         *                          sequentially in the face matrix.
         * @param surfStartIdx      The starting indices in the mesh face list where
         *                          each new surface starts.
         * @param surfNames         Name for each surface.
         */

        public static bool Rhino2MDCKSurfacesStl(MDCK.Model.Objects.Model outmodel, params Mesh[] rhmeshes)
        {
            // Write each rhino mesh as separate STL
            List<string> surf_paths = new List<string>(rhmeshes.Length);
            for (int i = 0; i < rhmeshes.Length; i++)
            {
                if (rhmeshes[i].Faces.QuadCount > 0)
                    throw new ArgumentException("All mesh faces must be triangulated before writing STL file.");
                string filepath = WriteStlTempFile(rhmeshes[i]);
                surf_paths.Add(filepath);
            }

            // Import each sub-mesh as a surface
            for (int i = 0; i < surf_paths.Count; i++)
            {
                // Import stl to temp model
                using (var surf_model = new MDCK.Model.Objects.Model())
                {
                    // Read the STL
                    using (var importer = new MDCK.Operators.ModelImportFromStl())
                    {
                        // Set operator parameters
                        importer.FileName = surf_paths[i];
                        importer.ForceLoad = true; // STL file format check is done before reading
                        importer.OutputModel = surf_model;
                        importer.MmPerUnit = 1.0; // Conversion factor: STL units to mm
                        try
                        {
                            importer.Operate(); // Import the STL
                        }
                        catch (MDCK.Operators.ModelImportFromStl.Exception)
                        {
                            // Remove temporary STLs
                            foreach (string path in surf_paths)
                                File.Delete(path);
                            return false;
                        }
                    }

                    // Copy its borders to explicit curves so they can be used after merge
                    using (var borderdup = new MDCK.Operators.BorderCopyToCurveSet())
                    {
                        borderdup.SourceBorder = surf_model.Border;
                        // Destination created automatically
                        try
                        {
                            borderdup.Operate();
                        }
                        catch (MDCK.Operators.BorderCopyToCurveSet.Exception)
                        {
                            // Remove temporary STLs
                            foreach (string path in surf_paths)
                                File.Delete(path);
                            return false;
                        }
                    }

                    // Copy model to surface on output model
                    // TODO: cannot use the output model directly
                    // TODO: experiment with Model/Feature/Surface-MoveTo-Mode/Feature/Surface
                    using (var copyer = new MDCK.Operators.ModelMoveToModel())
                    {
                        copyer.SourceModel = surf_model;
                        copyer.DestinationModel = outmodel;
                        try
                        {
                            copyer.Operate();
                        }
                        catch (MDCK.Operators.ModelMoveToModel.Exception)
                        {
                            // Remove temporary STLs
                            foreach (string path in surf_paths)
                                File.Delete(path);
                            return false;
                        }
                    }
                }
            }

            // Remove temporary STLs
            foreach (string path in surf_paths)
                File.Delete(path);
            return true;
        }

        /**
         * Convert MDCK model to Rhino mesh via a temporary STL file.
         */

        public static bool MDCK2RhinoMeshStl(MDCK.Model.Objects.Model inmodel, out Mesh outmesh)
        {
            // Write temporary STL
            string filepath = System.IO.Path.GetTempPath() + "IDS_" + Guid.NewGuid().ToString() + ".stl";
            using (var writer = new MDCK.Operators.ModelExportToStl())
            {
                writer.Model = inmodel;
                writer.MmPerUnit = 1.0;
                writer.ExportAsAscii = false;
                writer.ExportAsMultipleSurfaces = false;
                writer.ExportIncludeColor = false;
                writer.FileName = filepath;
                try
                {
                    writer.Operate();
                }
                catch (MDCK.Operators.ModelExportToStl.Exception)
                {
                    outmesh = null;
                    return false;
                }
            }

            // Read STL
            bool ok = StlBinary2RhinoMesh(filepath, out outmesh);
            File.Delete(filepath);
            return ok;
        }

        /////////////////////////////////////////////////////////////////////////
        ///// Unsafe methods (may yield "pure virtual function call" error    ///
        /////////////////////////////////////////////////////////////////////////

        /**
        * Convert a Rhino.Geometry.Mesh object into a MDCK model object, and an associated
        * mesh border to a MDCK.Model.Objects.Curve
        * @throws ArgumentException     One of the borders contains an integer higher
        *                               than the number of vertices
        */

        public static bool Rhino2MDCKMeshUnsafe(Mesh inmesh, IEnumerable<int[]> inborders, out MDCK.Model.Objects.Model outmodel, out List<MDCK.Model.Objects.Curve> outborders, out MDCK.Model.Objects.CurveSet curveset)
        {
            // Get the Model object with vertex list
            outmodel = null;
            outborders = null;
            curveset = null;
            List<MDCKVertex> vertexlist;
            List<MDCKTriangle> facelist;
            bool res = Rhino2MDCKMeshUnsafe(inmesh, out outmodel, out vertexlist, out facelist);
            if (!res)
            {
                vertexlist = null;
                facelist = null;
                return false;
            }

            // Add a curveset to contain the curves
            MDCK.Model.Objects.CurveSet cset;
            using (var setadd = new MDCK.Operators.ModelAddCurveSet())
            {
                try
                {
                    setadd.Model = outmodel;
                    setadd.Operate();
                }
                catch (MDCK.Operators.ModelAddCurveSet.Exception)
                {
                    setadd.Model = null;
                    vertexlist = null;
                    facelist = null;
                    return false;
                }
                cset = setadd.NewCurveSet;
            }

            // Build the curves
            outborders = new List<MDCK.Model.Objects.Curve>();
            foreach (int[] border in inborders)
            {
                // Add curve to the curveset
                MDCK.Model.Objects.Curve hcurve;
                using (var cadd = new MDCK.Operators.CurveSetAddCurve())
                {
                    cadd.CurveSet = cset;
                    bool isclosed = border[0] == border[border.Length - 1];
                    cadd.IsClosed = isclosed;
                    try
                    {
                        cadd.Operate();
                    }
                    catch (MDCK.Operators.CurveSetAddCurve.Exception)
                    {
                        vertexlist = null;
                        facelist = null;
                        return false;
                    }
                    hcurve = cadd.NewCurve;
                }

                // Then add vertices to the curve
                foreach (int i in border)
                {
                    if (i >= vertexlist.Count)
                        throw new ArgumentException("Vertex index in border exceeds number of vertices added to new mesh");
                    // Extend curve using vertex
                    using (var vop = new MDCK.Operators.CurveAddVertexBack())
                    {
                        vop.Curve = hcurve;
                        vop.ModelVertex = vertexlist[i];
                        try
                        {
                            vop.Operate();
                        }
                        catch (MDCK.Operators.CurveAddVertexBack.Exception)
                        {
                            vop.Curve = null;
                            vop.ModelVertex = null;
                            vertexlist = null;
                            facelist = null;
                            return false;
                        }
                    }
                }

                // Add curve to list of borders
                outborders.Add(hcurve);
            }
            curveset = cset;
            return true;
        }

        /**
         * Convert a Mesh object into a MDCK model object.
         */

        public static bool Rhino2MDCKMeshUnsafe(Mesh inmesh, out MDCK.Model.Objects.Model outmodel)
        {
            List<MDCKVertex> vertexlist;
            List<MDCKTriangle> facelist;
            bool res = Rhino2MDCKMeshUnsafe(inmesh, out outmodel, out vertexlist, out facelist);

            // Aid the garbage collector
            vertexlist = null;
            facelist = null;
            return res;
        }

        /**
         * Convert a Mesh object into a MDCK model object, and obtain a mapping
         * between Rhino mesh vertex/face indices and MDCK vertex/triangle objects.
         */

        public static bool Rhino2MDCKMeshUnsafe(Mesh inmesh, out MDCK.Model.Objects.Model outmodel, out List<MDCKVertex> vertexlist, out List<MDCKTriangle> facelist)
        {
            if (inmesh.Faces.QuadCount > 0)
            {
                inmesh.Faces.ConvertQuadsToTriangles();
            }
            vertexlist = null;
            facelist = null;

            // Add a new surface that contains the mesh triangles
            outmodel = new MDCK.Model.Objects.Model();
            MDCK.Model.Objects.Surface matsurf;
            using (var sop = new MDCK.Operators.ModelAddSurface())
            {
                sop.Model = outmodel;
                try
                {
                    sop.Operate();
                }
                catch (MDCK.Operators.ModelAddSurface.Exception)
                {
                    sop.Model = null;
                    outmodel.Dispose();
                    return false;
                }

                // Check surface
                matsurf = sop.NewSurface;
                if (null == matsurf)
                {
                    throw new Exception("Could not add surface to the model!");
                }
            }

            // Copy all the vertices into the MDCK model
            List<MDCKVertex> vlist = new List<MDCKVertex>(inmesh.Vertices.Count); // Indexable reference to all added vertices
            foreach (Rhino.Geometry.Point3f vertex in inmesh.Vertices)
            {
                using (var addvert = new MDCK.Operators.ModelAddVertex())
                {
                    addvert.Model = outmodel;
                    addvert.InputPoint = new WPoint3d(vertex.X, vertex.Y, vertex.Z);
                    try
                    {
                        addvert.Operate();
                    }
                    catch (MDCK.Operators.ModelAddVertex.Exception)
                    {
                        addvert.Model = null;
                        vlist = null;
                        matsurf = null;
                        outmodel.Dispose();
                        return false;
                    }
                    MDCKVertex va = addvert.NewModelVertex;
                    vlist.Add(va);
                    va = null;
                }
            }

            // Add all the mesh triangles to the new surface in the MDCK model
            List<MDCKTriangle> flist = new List<MDCKTriangle>(inmesh.Faces.Count);
            foreach (Rhino.Geometry.MeshFace face in inmesh.Faces)
            {
                using (var addtri = new MDCK.Operators.SurfaceAddTriangle())
                {
                    addtri.Surface = matsurf;
                    try
                    {
                        // Same counter-clockwise convention in Rhino and MDCK!
                        addtri.VertexFirst = vlist[face[0]];
                        addtri.VertexSecond = vlist[face[1]];
                        addtri.VertexThird = vlist[face[2]];
                        addtri.Operate();
                    }
                    catch (MDCK.Operators.SurfaceAddTriangle.Exception)
                    {
                        addtri.Surface = null;
                        vlist = null;
                        flist = null;
                        matsurf = null;
                        outmodel.Dispose();
                        return false;
                    }
                    MDCKTriangle fa = addtri.NewTriangle;
                    flist.Add(fa);
                    fa = null;
                }
            }

            vertexlist = vlist;
            facelist = flist;
            vlist = null;
            flist = null;
            matsurf = null;
            return true;
        }

        /**
         * Convert Rhino Meshes to MDCK model with separate surfaces for each disjoint mesh.
         */

        public static bool Rhino2MDCKSurfaces(out MDCK.Model.Objects.Model outmodel, params Mesh[] inmeshes)
        {
            outmodel = new MDCK.Model.Objects.Model();

            foreach (Mesh rhmesh in inmeshes)
            {
                // Remove quad faces
                if (rhmesh.Faces.QuadCount > 0)
                {
                    rhmesh.Faces.ConvertQuadsToTriangles();
                }

                // Make surface to hold the mesh
                MDCK.Model.Objects.Surface surf;
                try
                {
                    var sop = new MDCK.Operators.ModelAddSurface();
                    sop.Model = outmodel;
                    sop.Operate();
                    surf = sop.NewSurface;
                }
                catch (MDCK.Operators.ModelAddSurface.Exception)
                {
                    return false;
                }

                // Copy face and vertex data to the surface
                List<MDCKVertex> vlist = new List<MDCKVertex>(rhmesh.Vertices.Count);
                foreach (Rhino.Geometry.Point3d vertex in rhmesh.Vertices)
                {
                    try
                    {
                        var vop = new MDCK.Operators.ModelAddVertex();
                        vop.Model = outmodel;
                        vop.InputPoint = new WPoint3d(vertex.X, vertex.Y, vertex.Z);
                        vop.Operate();
                        MDCKVertex va = vop.NewModelVertex;
                        vlist.Add(va);
                    }
                    catch (MDCK.Operators.ModelAddVertex.Exception)
                    {
                        return false;
                    }
                }

                // Add all the mesh triangles to the new surface in the MDCK model
                foreach (Rhino.Geometry.MeshFace face in rhmesh.Faces)
                {
                    try
                    {
                        var top = new MDCK.Operators.SurfaceAddTriangle();
                        top.Surface = surf;
                        // Counter-clockwise convention in Rhino and MDCK (normal according to right hand rule)
                        top.VertexFirst = vlist[face[0]];
                        top.VertexSecond = vlist[face[1]];
                        top.VertexThird = vlist[face[2]];
                        top.Operate();
                    }
                    catch (MDCK.Operators.SurfaceAddTriangle.Exception)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}