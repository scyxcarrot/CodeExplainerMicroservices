using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.IO;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Fix
{
    public class MDCKUnify
    {
        /**
         * Perform the unify operation on a list of rhino meshes.
         * @return       The merged, unified meshes
         */

        public static bool UnifyOperationStl(Mesh[] meshes, out Mesh unified)
        {
            // Append all the meshes into a single mesh
            Mesh targetmesh = meshes[0];
            for (int i = 1; i < meshes.Length; i++)
                targetmesh.Append(meshes[i]);

            // Convert mesh to MDCK via STL
            string meshpath = MDCKConversion.WriteStlTempFile(targetmesh);

            // Perform operation
            return UnifyOperationStl(meshpath, out unified);
        }

        /**
         * Perform the unify operation by importing an stl from a path.
         * @return       The unified mesh
         */

        public static bool UnifyOperationStl(string StlFilePath, out Mesh unified)
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
                    unified = null;
                    File.Delete(StlFilePath);
                    return false;
                }
            }

            // Make the unify operator
            using (var op = new MDCK.Operators.BooleanUnify())
            {
                op.Model = mdck_in;
                try
                {
                    op.Operate();
                }
                catch (MDCK.Operators.BooleanUnify.Exception)
                {
                    mdck_in.Dispose();
                    mdck_in = null;
                    unified = null;
                    return false;
                }
            }

            // Convert to Rhino mesh via STL file
            bool ok = MDCKConversion.MDCK2RhinoMeshStl(mdck_in, out unified);
            mdck_in.Dispose();
            mdck_in = null;
            return ok;
        }
    }
}