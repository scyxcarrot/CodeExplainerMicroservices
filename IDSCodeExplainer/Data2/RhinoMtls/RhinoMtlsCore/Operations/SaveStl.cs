using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class SaveStl
    {
        [HandleProcessCorruptedStateExceptions]
        public static bool SaveToStlFile(Mesh mesh, string filePath)
        {
            using (var context = MtlsIds34Globals.CreateContext())
            {
                var saveToStl = new MtlsIds34.ImportExport.SaveToStl() { FilePath = filePath };
                saveToStl.Triangles = mesh.Faces.ToArray2D(context);
                saveToStl.Vertices = mesh.Vertices.ToArray2D(context);

                try
                {
                    saveToStl.Operate(context);
                }
                catch (Exception e)
                {
                    throw new MtlsException("SaveToStl", e.Message);
                }
            }

            return true;
        }
    }
}