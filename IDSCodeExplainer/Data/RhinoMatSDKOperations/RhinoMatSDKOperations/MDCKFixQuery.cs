using Rhino;
using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System;
using System.Collections.Generic;
using System.IO;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Fix
{
    public class MDCKFixQuery
    {
        /**
         * Perform the autofix operation
         * @return       The fixed mesh
         */

        public static bool MeshFixQueryStl(Mesh mesh, out Dictionary<string, ulong> fixQueryDict, bool showInCommandLine = true)
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
                    fixQueryDict = null;
                    File.Delete(StlFilePath);
                    return false;
                }
            }

            // Query the mesh
            bool res = MeshFixQuery(mdck_in, out fixQueryDict, showInCommandLine);
            if (res == false)
                RhinoApp.WriteLine("[MDCK] Autofix Report Failed");

            return res;
        }

        /*
         * - NumberOfBadContours
         * - NumberOfBadEdges
         * - NumberOfIntersectingTriangles
         * - NumberOfInvertedNormals
         * - NumberOfNearBadEdges
         * - NumberOfNoiseShells
         * - NumberOfPlanarHoles
         * - NumberOfShells
         * - NumberOfDoubleTriangles
        */

        public static bool MeshFixQuery(MDCK.Model.Objects.Model mdck_model, out Dictionary<string, ulong> fixQueryDict, bool showInCommandLine = true)
        {
            // init dictionary
            fixQueryDict = new Dictionary<string, ulong>();

            // NumberOfBadContours & NumberOfBadEdges
            fixQueryDict.Add("NumberOfBadContours", mdck_model.NumberOfBadContours);
            fixQueryDict.Add("NumberOfBadEdges", mdck_model.NumberOfBadEdges);

            try
            {
                // Query IntersectingTriangles
                using (var query = new MDCK.Queries.ModelIntersectingTrianglesNumber())
                {
                    query.Model = mdck_model;
                    query.Query();
                    fixQueryDict.Add("NumberOfIntersectingTriangles", query.NumberOfIntersectingTriangles);
                }

                // Query NumberOfInvertedNormals
                using (var query = new MDCK.Queries.ModelInvertedNormalsNumber())
                {
                    query.Model = mdck_model;
                    query.Query();
                    fixQueryDict.Add("NumberOfInvertedNormals", query.NumberOfInvertedNormals);
                }

                // Query NumberOfNearBadEdges
                using (var query = new MDCK.Queries.ModelNearBadEdgesNumber())
                {
                    query.Model = mdck_model;
                    query.Query();
                    fixQueryDict.Add("NumberOfNearBadEdges", query.NumberOfNearBadEdges);
                }

                // Query NumberOfNoiseShells
                using (var query = new MDCK.Queries.ModelNoiseShells())
                {
                    query.Model = mdck_model;
                    query.Query();
                    fixQueryDict.Add("NumberOfNoiseShells", query.NumberOfNoiseShells);
                }

                // Query NumberOfPlanarHoles
                using (var query = new MDCK.Queries.ModelPlanarHolesNumber())
                {
                    query.Model = mdck_model;
                    query.Query();
                    fixQueryDict.Add("NumberOfPlanarHoles", query.NumberOfPlanarHoles);
                }

                // Query NumberOfShells
                using (var query = new MDCK.Queries.ModelShellsNumber())
                {
                    query.Model = mdck_model;
                    query.Query();
                    fixQueryDict.Add("NumberOfShells", query.NumberOfShells);
                }

                // Query NumberOfDoubleTriangles
                using (var query = new MDCK.Queries.ModelDoubleTrianglesNumber())
                {
                    query.Model = mdck_model;
                    query.Query();
                    fixQueryDict.Add("NumberOfDoubleTriangles", query.NumberOfDoubleTriangles);
                }
            }
            catch (Exception ex)
            {
                if (ex is MDCK.Queries.ModelDoubleTrianglesNumber.Exception ||
                    ex is MDCK.Queries.ModelShellsNumber.Exception ||
                    ex is MDCK.Queries.ModelPlanarHolesNumber.Exception ||
                    ex is MDCK.Queries.ModelNoiseShells.Exception ||
                    ex is MDCK.Queries.ModelNearBadEdgesNumber.Exception ||
                    ex is MDCK.Queries.ModelInvertedNormalsNumber.Exception ||
                    ex is MDCK.Queries.ModelIntersectingTrianglesNumber.Exception)
                {
                    return false;
                }
                throw;
            }

            // Postprocessing for summary

            // all fixed except for the NumberOfShells
            ulong allFix = fixQueryDict["NumberOfDoubleTriangles"] + fixQueryDict["NumberOfPlanarHoles"] +
                           fixQueryDict["NumberOfNoiseShells"] + fixQueryDict["NumberOfNearBadEdges"] +
                           fixQueryDict["NumberOfInvertedNormals"] + fixQueryDict["NumberOfIntersectingTriangles"] +
                           fixQueryDict["NumberOfBadEdges"] + fixQueryDict["NumberOfBadContours"];

            fixQueryDict.Add("TotalFix", allFix);

            // show in command line
            if (showInCommandLine)
            {
                RhinoApp.WriteLine("______________________");
                RhinoApp.WriteLine("[MDCK] Autofix Report");
                RhinoApp.WriteLine("| BadContours: {0}", fixQueryDict["NumberOfBadContours"]);
                RhinoApp.WriteLine("| BadEdges: {0}", fixQueryDict["NumberOfBadEdges"]);
                RhinoApp.WriteLine("| NearBadEdges: {0}", fixQueryDict["NumberOfNearBadEdges"]);
                RhinoApp.WriteLine("| InvertedNormals: {0}", fixQueryDict["NumberOfInvertedNormals"]);
                RhinoApp.WriteLine("| PlanarHoles: {0}", fixQueryDict["NumberOfPlanarHoles"]);
                RhinoApp.WriteLine("| IntersectingTriangles: {0}", fixQueryDict["NumberOfIntersectingTriangles"]);
                RhinoApp.WriteLine("| DoubleTriangles: {0}", fixQueryDict["NumberOfDoubleTriangles"]);
                RhinoApp.WriteLine("| NoiseShells: {0}", fixQueryDict["NumberOfNoiseShells"]);
                RhinoApp.WriteLine("| Shells: {0}", fixQueryDict["NumberOfShells"]);
                RhinoApp.WriteLine("--------------------------");
            }

            return true;
        }
    }
}