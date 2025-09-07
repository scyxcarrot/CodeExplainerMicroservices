using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.IO;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Wrap
{
    public class MDCKShrinkWrap
    {
        /**
        * Perform the shrinkwrap operation on an arbitrary number of meshes
        * @return       The wrapped mesh
        */

        public static bool ShrinkWrapOperation(Mesh[] meshes, MDCKShrinkWrapParameters opparams, out Mesh wrapped)
        {
            // Create new MDCK model for mesh
            MDCK.Model.Objects.Model matmesh;
            bool res = MDCKConversion.Rhino2MDCKSurfaces(out matmesh, meshes);
            if (!res)
            {
                wrapped = null;
                return false;
            }

            // Make the operator
            var matout = new MDCK.Model.Objects.Model();
            var sop = new MDCK.Operators.ShrinkWrap();
            sop.Resolution = opparams.resolution;
            sop.GapSize = opparams.gapSize;
            sop.ResultingOffset = opparams.resultingOffset;
            sop.ProtectThinWalls = opparams.protectThinWalls;
            sop.ReduceTriangles = opparams.reduceTriangles;
            sop.PreserveSharpFeatures = opparams.preserveSharpFeatures;
            sop.PreserveSurfaces = opparams.preserveSurfaces;
            sop.Model = matmesh;
            sop.DestinationModel = matout;
            try
            {
                sop.Operate();
            }
            catch (MDCK.Operators.ShrinkWrap.Exception)
            {
                wrapped = null;
                return false;
            }

            // Convert the output back to a Rhino mesh
            Mesh rmesh;
            bool result = MDCKConversion.MDCK2RhinoMeshStl(matout, out rmesh);
            if (!result)
            {
                wrapped = null;
                return false;
            }
            wrapped = rmesh;
            return true;
        }

        /**
         * Perform the shrinkwrap operation by importing the Rhino
         * meshes as STLs in the MDCK model. This incurs the overhead
         * of writing the mesh to a temporary STL file and then
         * importing it.
         * @return       The wrapped mesh
         */

        public static bool ShrinkWrapOperationStl(Mesh[] meshes, MDCKShrinkWrapParameters opparams, out Mesh wrapped)
        {
            // Append all the meshes into a single mesh
            Mesh targetmesh = meshes[0];
            for (int i = 1; i < meshes.Length; i++)
                targetmesh.Append(meshes[i]);

            // Convert mesh to MDCK via STL
            string meshpath = MDCKConversion.WriteStlTempFile(targetmesh);

            // Perform operation
            return ShrinkWrapOperationStl(meshpath, opparams, out wrapped);
        }

        /**
         * Perform the shrinkwrap operation by importing the Rhino
         * meshes as STLs in the MDCK model. This incurs the overhead
         * of writing the mesh to a temporary STL file and then
         * importing it.
         * @return       The wrapped mesh
         */

        private static bool ShrinkWrapOperationStl(string StlFilePath, MDCKShrinkWrapParameters opparams, out Mesh wrapped)
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
                    wrapped = null;
                    File.Delete(StlFilePath);
                    return false;
                }
            }

            // Make the operator
            var mdck_out = new MDCK.Model.Objects.Model();
            using (var sop = new MDCK.Operators.ShrinkWrap())
            {
                sop.Resolution = opparams.resolution;
                sop.GapSize = opparams.gapSize;
                sop.ResultingOffset = opparams.resultingOffset;
                sop.ProtectThinWalls = opparams.protectThinWalls;
                sop.ReduceTriangles = opparams.reduceTriangles;
                sop.PreserveSharpFeatures = opparams.preserveSharpFeatures;
                sop.PreserveSurfaces = opparams.preserveSurfaces;
                sop.Model = mdck_in;
                sop.DestinationModel = mdck_out;
                try
                {
                    sop.Operate();
                }
                catch (MDCK.Operators.ShrinkWrap.Exception)
                {
                    mdck_in.Dispose();
                    mdck_in = null;
                    mdck_out.Dispose();
                    mdck_out = null;
                    wrapped = null;
                    return false;
                }
            }

            // Convert to Rhino mesh via STL file
            bool ok = MDCKConversion.MDCK2RhinoMeshStl(mdck_out, out wrapped);
            mdck_out.Dispose();
            mdck_in.Dispose();
            mdck_out = null;
            mdck_in = null;
            return ok;
        }
    }
}