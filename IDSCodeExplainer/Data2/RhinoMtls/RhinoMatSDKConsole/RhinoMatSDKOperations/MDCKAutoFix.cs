using System.Collections.Generic;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Fix
{
    public class MDCKAutoFix
    {
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
                    MDCKFixQuery.MeshFixQuery(model, out fixQueryDict);

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