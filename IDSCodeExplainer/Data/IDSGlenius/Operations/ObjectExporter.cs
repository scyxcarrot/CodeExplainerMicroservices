using IDS.Core.Utilities;
using IDS.Glenius.FileSystem;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace IDS.Glenius.Operations
{
    public class ObjectExporter
    {
        public List<string> FailedExports { get; } = new List<string>();

        public string ExportDirectory { get; set; }

        private readonly RhinoDoc document;

        public ObjectExporter(RhinoDoc document)
        {
            //Default to where the document is at.
            ExportDirectory = DirectoryStructure.GetWorkingDir(document);
            this.document = document;
        }

        private string AddExtension(string fileName, string extension)
        {
            var builder = new StringBuilder(fileName);
            builder.AppendFormat(".{0}", extension);

            return builder.ToString();
        }

        private string BuildFullPath(string fileName, string extension)
        {
            var fileNameWithExtension = AddExtension(fileName, extension);
            return Path.Combine(ExportDirectory, fileNameWithExtension);
        }

        public bool ExportStl(Mesh mesh, string fileName)
        {
            return ExportStlWithColor(mesh, fileName, Colors.GeneralGrey);
        }

        public bool ExportStlWithColor(Mesh mesh, string fileName, Color color)
        {
            if (mesh == null)
            {
                FailedExports.Add(fileName);
                return false;
            }

            var fullPath = BuildFullPath(fileName, "stl");

            try
            {
                var meshColor = new int[3] { color.R, color.G, color.B };
                StlUtilities.RhinoMesh2StlBinary(mesh, fullPath, meshColor);
            }
            catch (ArgumentNullException ex)
            {
                FailedExports.Add(fileName);
                return false;
            }

            return true;
        }

        public bool ExportStpAsStl(Brep brep, string fileName)
        {
            var meshes = Mesh.CreateFromBrep(brep);
            var combined = MeshUtilities.AppendMeshes(meshes);

            return ExportStl(combined, fileName);
        }

        public bool ExportStp(Brep brep, string fileName)
        {
            if(brep == null)
            {
                FailedExports.Add(fileName);
                return false;
            }

            var fullPath = BuildFullPath(fileName, "stp");

            var brepId = document.Objects.Add(brep);

            if (brepId != Guid.Empty && document.Objects.Select(brepId))
            {
                string exportScriptCommand = "-_Export ";
                StringBuilder builder = new StringBuilder(exportScriptCommand);
                builder.AppendFormat("\"{0}\" _Enter", fullPath);

                if (!RhinoApp.RunScript(builder.ToString(), false))
                {
                    document.Objects.Delete(brepId, true);
                    FailedExports.Add(fileName);
                    return false;
                }

                document.Objects.Delete(brepId, true);
                return true;
            }

            FailedExports.Add(fileName);
            return false;
        }
    }
}
