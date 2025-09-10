using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.ImportExport;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class ImportExport
    {
        [HandleProcessCorruptedStateExceptions]
        public static IMesh LoadFromStlFile(IConsole console, string path)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var loader = new LoadFromStl()
                    {
                        FilePath = path
                    };

                    var result = loader.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;
                    var mesh = new IDSMesh(vertexArray, triangleArray);
                    return mesh;
                }
                catch (Exception e)
                {
                    throw new MtlsException("LoadFromStl", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static bool ExportToStlFile(IConsole console, string path, IMesh mesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var export = new SaveToStl()
                    {
                        FilePath = path,
                        Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                        Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                    };

                    export.Operate(context);
                    return true;
                }
                catch (Exception e)
                {
                    throw new MtlsException("SaveToStl", e.Message);
                }
            }
        }
    }
}
