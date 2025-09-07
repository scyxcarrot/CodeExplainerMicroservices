using IDS.CMF.DataModel;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Drawing
{
    public class DrawSurfaceDataContext
    {
        public double DrawStepSize { get; set; } = 0.5;
        public double PatchTubeDiameter = 4.0;
        public double ExtensionLength { get; set; } = 10;
        public double SkeletonTubeDiameter { get; set; } = 4.0;
        public List<PatchData> SkeletonSurfaces { get; set; } = new List<PatchData>();
        public List<PatchData> PatchSurfaces { get; set; } = new List<PatchData>();
        public List<KeyValuePair<List<Curve>, SkeletonSurface>> SkeletonCurves { get; set; }
            = new List<KeyValuePair<List<Curve>, SkeletonSurface>>();
        public List<KeyValuePair<Brep, SkeletonSurface>> SkeletonTubes { get; set; }
            = new List<KeyValuePair<Brep, SkeletonSurface>>();
        public List<KeyValuePair<Mesh, PatchSurface>> PositivePatchTubes { get; set; }
            = new List<KeyValuePair<Mesh, PatchSurface>>();
        public List<KeyValuePair<Brep, PatchSurface>> PositivePatchSurface { get; set; }
            = new List<KeyValuePair<Brep, PatchSurface>>();
        public bool ContainsDrawing()
        {
            var hasDrawing = false;
            hasDrawing |= SkeletonSurfaces.Any();
            hasDrawing |= PatchSurfaces.Any();
            hasDrawing |= SkeletonCurves.Any();
            hasDrawing |= SkeletonTubes.Any();
            hasDrawing |= PositivePatchTubes.Any();
            hasDrawing |= PositivePatchSurface.Any();

            return hasDrawing;
        }
    }
}