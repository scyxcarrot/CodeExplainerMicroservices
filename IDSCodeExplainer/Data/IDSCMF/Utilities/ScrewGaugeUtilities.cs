using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.Utilities
{
    public class ScrewGaugeUtilities
    {
        public const string GaugeFileNamePattern = "Gauge_*.stl";

        public static List<string> OrderGaugePathsByScrewLength(IEnumerable<string> paths)
        {
            return paths.OrderBy(p => GetScrewLength(p)).ToList();
        }

        public static int[] GetGaugeColorArray(int index)
        {
            var color = GetGaugeColor(index);
            var colorArray = new int[] { color.R, color.G, color.B };
            return colorArray;
        }

        private static double GetScrewLength(string path)
        {
            double length;
            var startIndex = path.LastIndexOf('_') + 1;
            var lengthToExtract = path.LastIndexOf('.') - startIndex;
            var lengthInStr = path.Substring(startIndex, lengthToExtract);
            
            if (!lengthInStr.TryParseToInvariantCulture(out length))
            {
                return -1;
            }

            return length;
        }

        public static Color GetGaugeColor(int index)
        {
            var key = index % 8;
            var dict = new Dictionary<int, Color>
            {
                { 1, Color.FromArgb(35, 74, 113) },
                { 2, Color.FromArgb(117, 157, 157) },
                { 3, Color.FromArgb(155, 160, 116) },
                { 4, Color.FromArgb(186, 149, 97) },
                { 5, Color.FromArgb(121, 95, 135) },
                { 6, Color.FromArgb(91, 47, 48) },
                { 7, Color.FromArgb(128, 128, 0) },
                { 0, Color.FromArgb(135, 140, 203) }
            };
            var color = dict[key];
            return color;
        }

        public struct ScrewGaugeData
        {
            public Screw Screw { get; private set; }
            public int GaugeIndex { get; private set; }
            public Mesh Gauge { get; private set; }
            public int[] Color { get; private set; }
            public DisplayMaterial GaugeMaterial { get; private set; }

            public ScrewGaugeData(Screw screw, int gaugeIndex, Mesh gauge, int[] color, DisplayMaterial gaugeMaterial)
            {
                Screw = screw;
                GaugeIndex = gaugeIndex;
                Gauge = gauge;
                Color = color;
                GaugeMaterial = gaugeMaterial;
            }
        }

        public static List<ScrewGaugeData> CreateScrewGauges(Screw screw, string screwType)
        {
            var orderedGaugesFilePath = GetOrderedGaugesFilePath(screwType);

            var screwGaugeData = new List<ScrewGaugeData>();
            for (var i = 0; i < orderedGaugesFilePath.Count; i++)
            {
                var gaugeFilePath = orderedGaugesFilePath[i];
                var screwGauge = CreateAlignedScrewGaugeMeshFromStl(gaugeFilePath, screw);
                if (screwGauge == null)
                {
                    continue;
                }

                var gaugeIndex = i + 1;
                var screwGaugeMeshColor = GetGaugeColorArray(gaugeIndex);

                var color = GetGaugeColor(gaugeIndex);
                var material = new DisplayMaterial(color, Constants.Transparency.Opaque);

                screwGaugeData.Add(new ScrewGaugeData(screw, gaugeIndex, screwGauge, screwGaugeMeshColor, material));
            }

            return screwGaugeData;
        }

        public static Mesh MergeAllLengthScrewGaugeMeshes(Screw screw)
        {
            var allScrewGauges = CreateScrewGaugeMeshesFromStl(screw);
            return MeshUtilities.UnifyMeshParts(allScrewGauges.ToArray());
        }

        private static List<Mesh> CreateScrewGaugeMeshesFromStl(Screw screw)
        {
            var screwType = screw.ScrewType;
            var orderedGaugesFilePath = GetOrderedGaugesFilePath(screwType);
            var screwGaugeMeshes = new List<Mesh>();

            foreach (var gaugeFilePath in orderedGaugesFilePath)
            {
                var screwGaugeMesh = CreateAlignedScrewGaugeMeshFromStl(gaugeFilePath, screw);
                if (screwGaugeMesh != null)
                {
                    screwGaugeMeshes.Add(screwGaugeMesh);
                }
            }

            return screwGaugeMeshes;
        }

        private static Mesh CreateAlignedScrewGaugeMeshFromStl(string gaugeFilePath, Screw screw)
        {
            if (!StlUtilities.StlBinary2RhinoMesh(gaugeFilePath, out var screwGauge))
            {
                return null;
            }

            var alignedScrewGaugeMesh = screwGauge.DuplicateMesh();
            alignedScrewGaugeMesh.Transform(screw.AlignmentTransform);

            return alignedScrewGaugeMesh;
        }

        private static List<string> GetOrderedGaugesFilePath(string screwType)
        {
            var resources = new ScrewResources();
            var gaugesFilePath = resources.GetGaugesFilePath(screwType);
            return OrderGaugePathsByScrewLength(gaugesFilePath);
        }
    }
}