using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class LoadFromStl
    {
        [HandleProcessCorruptedStateExceptions]
        public Mesh LoadFromStlFile(string path)
        {
            var loader = new MtlsIds34.ImportExport.LoadFromStl()
            {
                FilePath = path
            };

            try
            {
                using (var context = MtlsIds34Globals.CreateContext())
                {
                    var result = loader.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    var mesh = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                    mesh.Vertices.UseDoublePrecisionVertices = false;
                    return mesh;
                }
            }
            catch (Exception e)
            {
                throw new MtlsException("LoadFromStl", e.Message);
            }
        }
    }
}
