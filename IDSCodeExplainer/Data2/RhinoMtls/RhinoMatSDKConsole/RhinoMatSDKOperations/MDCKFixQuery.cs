using System;
using System.Collections.Generic;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Fix
{
    public class MDCKFixQuery
    {
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

        public static bool MeshFixQuery(MDCK.Model.Objects.Model mdck_model, out Dictionary<string, ulong> fixQueryDict)
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

            return true;
        }
    }
}