using IDS.CMF.DataModel;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Drawing
{
    //The class that holds all the variables and data for drawing.
    public class DrawGuideDataContext
    {
        public double DrawStepSize { get; set; } = 0.5;
        public double SkeletonTubeDiameter { get; set; } = 4.5;
        public List<PatchData> SkeletonSurfaces { get; private set; } = new List<PatchData>();

        public double PatchTubeDiameter = 4.0;
        public double NegativePatchTubeDiameter = 4.0;
        public List<PatchData> NegativePatchSurfaces { get; private set; } = new List<PatchData>();
        public List<PatchData> PatchSurfaces { get; private set; } = new List<PatchData>();

        public List<KeyValuePair<List<Curve>, SkeletonSurface>> SkeletonCurves { get; private set; } =
            new List<KeyValuePair<List<Curve>, SkeletonSurface>>();

        public List<KeyValuePair<Brep, SkeletonSurface>> SkeletonTubes { get; private set; } =
            new List<KeyValuePair<Brep, SkeletonSurface>>();

        public List<KeyValuePair<Mesh, PatchSurface>> PositivePatchTubes { get; private set; } =
            new List<KeyValuePair<Mesh, PatchSurface>>();

        public List<KeyValuePair<Mesh, PatchSurface>> NegativePatchTubes { get; private set; } =
            new List<KeyValuePair<Mesh, PatchSurface>>();

        public List<KeyValuePair<Brep, PatchSurface>> PositivePatchSurface { get; private set; } =
            new List<KeyValuePair<Brep, PatchSurface>>();

        public List<KeyValuePair<Brep, PatchSurface>> NegativePatchSurface { get; private set; } =
            new List<KeyValuePair<Brep, PatchSurface>>();

        public Mesh RoIMeshDefiner { get; set; }

        public bool ContainsDrawing()
        {
            var hasDrawing = false;
            hasDrawing |= SkeletonSurfaces.Any();
            hasDrawing |= NegativePatchSurfaces.Any();
            hasDrawing |= PatchSurfaces.Any();
            hasDrawing |= SkeletonCurves.Any();
            hasDrawing |= SkeletonTubes.Any();
            hasDrawing |= PositivePatchTubes.Any();
            hasDrawing |= NegativePatchTubes.Any();
            hasDrawing |= PositivePatchSurface.Any();
            hasDrawing |= NegativePatchSurface.Any();

            return hasDrawing;
        }
    }
}
