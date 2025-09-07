using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.IO;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Reduce
{
    public class MDCKReduce
    {
        /**
         * Perform the reduce operation on a list of rhino meshes.
         * @return       The merged, reduced meshes
         */

        public static bool ReduceOperationStl(Mesh[] meshes, MDCKReduceParameters opparams, out Mesh reduced)
        {
            // Append all the meshes into a single mesh
            Mesh targetmesh = meshes[0];
            for (int i = 1; i < meshes.Length; i++)
                targetmesh.Append(meshes[i]);

            // Convert mesh to MDCK via STL
            string meshpath = MDCKConversion.WriteStlTempFile(targetmesh);

            // Perform operation
            return ReduceOperationStl(meshpath, opparams, out reduced);
        }

        /**
         * Perform the reduce operation by importing an stl from a path.
         * @return       The reduced mesh
         */

        public static bool ReduceOperationStl(string StlFilePath, MDCKReduceParameters opparams, out Mesh reduced)
        {
            // Import the STL file
            MDCK.Model.Objects.Model mdck_in = new MDCK.Model.Objects.Model();
            using (var importer = new MDCK.Operators.ModelImportFromStl())
            {
                // Set operator parameters
                importer.FileName = StlFilePath;
                importer.ForceLoad = true; // STL file format check is done before reading
                importer.OutputModel = mdck_in;
                importer.MmPerUnit = 1.0; // Conversion factor: STL units to mm
                try
                {
                    importer.Operate(); // Import the STL
                    File.Delete(StlFilePath);
                }
                catch (MDCK.Operators.ModelImportFromStl.Exception)
                {
                    mdck_in.Dispose();
                    mdck_in = null;
                    reduced = null;
                    File.Delete(StlFilePath);
                    return false;
                }
            }

            // Make the reduce operator
            using (var op = new MDCK.Operators.Reduce())
            {
                op.GeometricalError = opparams.GeometricalError;
                op.TresholdAngleFlip = opparams.ThresholdAngleFlip;
                op.ReduceIterations = opparams.ReduceIterations;
                op.AccumulateError = opparams.AccumulateError;
                op.AddModel(mdck_in);
                try
                {
                    op.Operate();
                }
                catch (MDCK.Operators.Reduce.Exception)
                {
                    mdck_in.Dispose();
                    mdck_in = null;
                    reduced = null;
                    return false;
                }
            }

            // Convert to Rhino mesh via STL file
            bool ok = MDCKConversion.MDCK2RhinoMeshStl(mdck_in, out reduced);
            mdck_in.Dispose();
            mdck_in = null;
            return ok;
        }
    }
}