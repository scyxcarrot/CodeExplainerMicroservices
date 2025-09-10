using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Core.Importer
{
    public static class StlImporter
    {
        [Obsolete("Deprecated for CMF. Use IDS.Core.V2.Utilities.ImportUtilities instead.")]
        public static List<Mesh> ImportStl()
        {
            var fileNames = SelectStlFiles(true);
            if (fileNames == null)
            {
                return null;
            }

            var meshes = new List<Mesh>();
            foreach (var filename in fileNames)
            {
                var imported = ImportStl(filename);
                if (imported == null)
                {
                    return null;
                }

                meshes.Add(imported);
            }

            return meshes;
        }

        public static Mesh ImportStl(string path)
        {
            Mesh blockMesh;
            return !StlUtilities.StlBinary2RhinoMesh(path, out blockMesh) ? null : blockMesh;
        }

        public static Mesh ImportSingleStl()
        {
            var fileNames = SelectStlFiles(false);
            if (fileNames == null)
            {
                return null;
            }

            var imported = ImportStl(fileNames[0]);
            return imported;
        }

        public static string[] SelectStlFiles(bool multiselect, string title = "Please select an STL file")
        {
            var fd = new OpenFileDialog
            {
                Multiselect = multiselect,
                Title = title,
                Filter = "STL files (*.stl)|*.stl||",
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString()
            };
            var drc = fd.ShowDialog();
            if (drc != DialogResult.OK || !fd.FileNames.All(n => Path.GetExtension(n).ToLower() == ".stl"))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid or no file was chosen.");
                return null;
            }
            return fd.FileNames;
        }
    }
}