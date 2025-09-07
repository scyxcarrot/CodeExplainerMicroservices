using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.IO
{
    public class MDCKInputOutput
    {
        // Function that imports an stl and returns a model from an stl filepath
        public static bool ModelFromStlPath(string filePath, out MDCK.Model.Objects.Model inModel)
        {
            inModel = new MDCK.Model.Objects.Model();
            using (var importer = new MDCK.Operators.ModelImportFromStl())
            {
                // Set operator parameters
                importer.FileName = filePath;
                importer.ForceLoad = true; // STL file format check is done before reading
                importer.OutputModel = inModel;
                importer.MmPerUnit = 1.0; // Conversion factor: STL units to mm
                try
                {
                    importer.Operate(); // Import the STL
                }
                catch (MDCK.Operators.ModelImportFromStl.Exception)
                {
                    inModel.Dispose();
                    inModel = null;
                    return false;
                }
            }
            return true;
        }

        /**
         * Write Rhino meshes to MXP file.
         */

        public static bool OperatorWriteMXP(string filepath, List<Tuple<Mesh, string, Color>> models)
        {
            // Project save exporter
            MDCK.Operators.ProjectSave exporter = new MDCK.Operators.ProjectSave();

            // Add all the meshes
            List<MDCK.Model.Objects.Model> allmodels = new List<MDCK.Model.Objects.Model>();
            foreach (var modelinfo in models)
            {
                Mesh outmesh = modelinfo.Item1;
                string name = modelinfo.Item2;
                Color outcolor = modelinfo.Item3;

                if (outmesh.Vertices.Count <= 0)
                    continue;

                MDCK.Model.Objects.Model importModel;
                bool res = MDCKConversion.Rhino2MDCKMeshStl(outmesh, out importModel);
                importModel.Name = name;
                allmodels.Add(importModel);

                // Add model to exporter with given color
                importModel.Color = System.Windows.Media.Color.FromRgb(outcolor.R, outcolor.G, outcolor.B);
                exporter.AddModel(importModel);
            }

            // Export
            exporter.CompressionMethod = MDCK.Operators.ProjectSave.ECompressionMethod.STANDARD;
            exporter.FileName = filepath;
            try
            {
                exporter.Operate();
            }
            catch (MDCK.Operators.ProjectSave.Exception)
            {
                return false;
            }
            return true;
        }
    }
}