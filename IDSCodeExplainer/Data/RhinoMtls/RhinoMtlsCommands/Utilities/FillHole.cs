using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using RhinoMtlsCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhinoMtlsCommands.Utilities
{
    public class FillHole
    {
        public static void GetHoleFillSegments(RhinoDoc doc, int edgesCount, out Guid referenceMeshId, out Mesh targetMesh, out long[,] borderSegments)
        {
            // Indicate face near edge
            List<Mesh> meshes;
            List<Guid> meshIds;
            var closestEdgePointIndices = SelectionUtilities.IndicateNakedMeshEdgePoints(doc, edgesCount, out meshes, out meshIds);

            // Check if all were indicated on the same mesh
            var refId = meshIds[0];
            referenceMeshId = refId;
            if (meshIds.Any(selectedMeshId => selectedMeshId != refId))
            {
                RhinoApp.WriteLine("Indicated boundaries must lie on the same mesh.");
                targetMesh = null;
                borderSegments = null;
                return;
            }

            // Get corresponding edge segments and merge them
            var segments = new FlexibleArray<long>(0, 2);
            for (var i = 0; i < edgesCount; i++)
            {
                long[,] segment;
                var foundSegment = HoleFill.FindBorderVertexHoleSegments(meshes[i], closestEdgePointIndices[i], out segment);
                if (foundSegment)
                {
                    segments.AddRows(segment);
                }
            }

            // All meshes should be the same, so get the first one
            targetMesh = meshes[0];

            // Perform hole fill if an edge was indicated
            if (segments.Rows == 0)
            {
                borderSegments = null;
                return;
            }

            borderSegments = segments.Data;
        }

        public static void GetHoleFillParameters(out int edgesCount)
        {
            var getHolefillOptions = new GetOption();
            getHolefillOptions.AcceptNothing(true);
            getHolefillOptions.SetCommandPrompt("Hole Fill Option");
            var edgesCountOption = new OptionInteger(1, true, 1);
            getHolefillOptions.AddOptionInteger("EdgesCount", ref edgesCountOption);

            // Ask user to select object
            while (true)
            {
                var getResult = getHolefillOptions.Get(); // prompts the user for input
                if (getResult == GetResult.Nothing)
                {
                    break;
                }
            }

            edgesCount = edgesCountOption.CurrentValue;
        }

        public static bool GetHoleFillFreeformParameters(out double gridSize, out bool tangent, out bool treatAsOneHole)
        {
            // More options if fill hole freeform
            var getFreeformOptions = new GetOption();
            getFreeformOptions.AcceptNothing(true);

            gridSize = 0.2;
            var gridSizeOption = new OptionDouble(gridSize, true, 0.0);
            getFreeformOptions.AddOptionDouble("GridSize", ref gridSizeOption);

            tangent = false;
            var tangentOption = new OptionToggle(false, "No", "Yes");
            getFreeformOptions.AddOptionToggle("Tangent", ref tangentOption);

            treatAsOneHole = true;
            var treatAsOneHoleOption = new OptionToggle(true, "No", "Yes");
            getFreeformOptions.AddOptionToggle("TreatAsOneHole", ref treatAsOneHoleOption);

            // Ask user to set parameters
            getFreeformOptions.SetCommandPrompt("Set Fill Hole Freeform parameters");
            while (true)
            {
                var getResult = getFreeformOptions.Get(); // prompts the user for input
                if (getResult == GetResult.Nothing)
                {
                    break;
                }
                if (getResult == GetResult.Cancel)
                {
                    return false;
                }
            }

            gridSize = gridSizeOption.CurrentValue;
            tangent = tangentOption.CurrentValue;
            treatAsOneHole = treatAsOneHoleOption.CurrentValue;

            return true;
        }
    }
}