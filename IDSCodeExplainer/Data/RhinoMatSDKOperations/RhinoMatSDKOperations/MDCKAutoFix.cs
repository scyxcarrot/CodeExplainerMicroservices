using Rhino;
using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.Collections.Generic;
using System.IO;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Fix
{
    public class MDCKAutoFix
    {
        /**
         * Perform the autofix operation
         * @return       The fixed mesh
         */

        public static bool AutoFixOperationStl(Mesh mesh, MDCKAutoFixParameters opparams, out Mesh fixedMesh)
        {
            // Convert mesh to MDCK via STL
            string StlFilePath = MDCKConversion.WriteStlTempFile(mesh);

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
                    fixedMesh = null;
                    File.Delete(StlFilePath);
                    return false;
                }
            }

            var modelFixed = AutoFixOperation(mdck_in, opparams);
            
            // Convert to Rhino mesh via STL file
            if (modelFixed)
            {
                bool ok = MDCKConversion.MDCK2RhinoMeshStl(mdck_in, out fixedMesh);
                mdck_in.Dispose();
                mdck_in = null;
                RhinoApp.WriteLine("[MDCK] Completely fixed mesh");
                return ok;
            }
            else
            {
                bool ok = MDCKConversion.MDCK2RhinoMeshStl(mdck_in, out fixedMesh);
                mdck_in.Dispose();
                mdck_in = null;
                RhinoApp.WriteLine("[MDCK] Could not completely fix mesh");
                return false;
            }
        }

        public static bool AutoFixOperation(MDCK.Model.Objects.Model model, MDCKAutoFixParameters opparams)
        {
            // Make the autofix operator
            bool modelFixed = false;
            using (var op = new MDCK.Operators.AutoFix())
            {
                // Setting up the AutoFix operator
                op.FixAutomatic = opparams.FixAutomatic;
                op.AddModel(model);
                op.FilterNoiseShells = true;

                // For loop until fixed or max iterations
                Dictionary<string, ulong> fixQueryDict = new Dictionary<string, ulong>();
                Dictionary<string, ulong> prevDict = new Dictionary<string, ulong>();
                int dictChangeIter = 0;

                for (int iter = 0; iter < opparams.MaxAutoFixIterations; iter++)
                {
                    // set prevDict
                    if (iter > 0)
                        prevDict = fixQueryDict;

                    // Query the mdck_in model
                    MDCKFixQuery.MeshFixQuery(model, out fixQueryDict, opparams.ShowInCommand);

                    // Check if anything is changeing
                    if (iter > 0)
                    {
                        bool same = fixQueryDict["NumberOfBadContours"] == prevDict["NumberOfBadContours"] &&
                                    fixQueryDict["NumberOfBadEdges"] == prevDict["NumberOfBadEdges"] &&
                                    fixQueryDict["NumberOfNearBadEdges"] == prevDict["NumberOfNearBadEdges"] &&
                                    fixQueryDict["NumberOfInvertedNormals"] == prevDict["NumberOfInvertedNormals"] &&
                                    fixQueryDict["NumberOfPlanarHoles"] == prevDict["NumberOfPlanarHoles"] &&
                                    fixQueryDict["NumberOfIntersectingTriangles"] == prevDict["NumberOfIntersectingTriangles"] &&
                                    fixQueryDict["NumberOfDoubleTriangles"] == prevDict["NumberOfDoubleTriangles"] &&
                                    fixQueryDict["NumberOfNoiseShells"] == prevDict["NumberOfNoiseShells"] &&
                                    fixQueryDict["NumberOfShells"] == prevDict["NumberOfShells"];
                        if (same == false)
                        {
                            dictChangeIter = 0; // it changed so reset dictChangeIter
                        }
                        else if (dictChangeIter < opparams.MaxSameQueryIterations)
                        {
                            dictChangeIter = dictChangeIter + 1;
                        }
                        else
                        {
                            modelFixed = false;
                            break;
                        }
                    }

                    // Check fix
                    if (fixQueryDict["TotalFix"] == 0)
                    {
                        modelFixed = true;
                        break;
                    }
                    else
                    {
                        // It needs fixing
                        try
                        {
                            op.Operate();
                        }
                        catch (MDCK.Operators.AutoFix.Exception)
                        {
                            return false;
                        }
                    }
                }
            }
            
            return modelFixed;
        }
    }
}