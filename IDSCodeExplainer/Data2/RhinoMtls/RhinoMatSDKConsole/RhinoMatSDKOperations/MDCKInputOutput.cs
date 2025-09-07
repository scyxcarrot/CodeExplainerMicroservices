using Materialise.SDK.MDCK.Model.Objects;
using System;
using System.Runtime.ExceptionServices;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.IO
{
    public class MDCKInputOutput
    {
        [HandleProcessCorruptedStateExceptions]
        // Function that imports an stl and returns a model from an stl filepath
        public static bool ModelFromStlPath(string filePath, out Model inModel)
        {
            inModel = null;
            using (var importer = new MDCK.Operators.ModelImportFromStl())
            {
                // Set operator parameters
                importer.FileName = filePath;
                importer.ForceLoad = true; // STL file format check is done before reading
                importer.OutputModel = new Model();
                importer.MmPerUnit = 1.0; // Conversion factor: STL units to mm
                try
                {
                    importer.Operate(); // Import the STL
                    inModel = importer.OutputModel;
                }
                catch (Exception)
                {
                    //
                }
                finally
                {
                    if (inModel == null)
                    {
                        importer.Operate(); // Import the STL
                        inModel = importer.OutputModel;
                    }
                }
            }
            return inModel != null;
        }

    }
}