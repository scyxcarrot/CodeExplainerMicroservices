using IDS.Core.Utilities;
using IDS.Core.V2.Utilities;
using IDS.Core.Visualization;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.Core.Operations
{
    public class AnalysisMeshMaker
    {
        public static Mesh CreateMeshDifference(Mesh source, Mesh target)
        {
            return CreateDistanceMesh(source, target, 0.01, 1.0, ColorMap.MeshDifference);
        }

        public static Mesh CreateDesignMeshDifference(Mesh designPelvis, Mesh defectPelvis)
        {
            return CreateDistanceMesh(designPelvis, defectPelvis, 0.01, 1.0, ColorMap.MeshDifference);
        }
        
        public static Mesh CreateImplantClearanceMesh(Mesh solidPlateBottom, Mesh reamedPelvis)
        {
            return CreateDistanceMesh(solidPlateBottom, reamedPelvis, 0.1, 1.5, ColorMap.Clearance);
        }

        public static Mesh CreateImplantWithTransitionClearanceMesh(Mesh plateWithTransition, Mesh reamedPelvis)
        {
            // Distances
            var distances = MeshUtilities.Mesh2MeshDistance(plateWithTransition, reamedPelvis, true);
            // Whenever the Plate with transitions is inside the bone, it needs to be highlighted in red (as if the distance was zero).
            for (var i = 0; i < distances.Count; i++)
            {
                if (distances[i] < 0)
                {
                    distances[i] = 0;
                }
            }

            return CreateDistanceMesh(plateWithTransition, 0.1, 1.5, ColorMap.Clearance, distances);
        }

        public static Mesh CreateDistanceMesh(Mesh from, Mesh to, double colorScaleMinimum, double colorScaleMaximum, ColorMap colorMap)
        {
            // Distances
            var distances = MeshUtilities.Mesh2MeshDistance(from, to);

            // Colors
            return CreateDistanceMesh(from, colorScaleMinimum, colorScaleMaximum, colorMap, distances);
        }

        public static Mesh CreateDistanceMesh(Mesh from, double colorScaleMinimum, double colorScaleMaximum, ColorMap colorMap, List<double> distances)
        {
            // Init variables
            var distanceMap = from;

            // Colors
            var colors = DrawUtilitiesV2.GetColors(distances, colorScaleMinimum, 
                colorScaleMaximum, DrawUtilities.GetColorScale(colorMap));
            distanceMap.VertexColors.SetColors(colors.ToArray());
            
            return distanceMap;
        }
    }
}