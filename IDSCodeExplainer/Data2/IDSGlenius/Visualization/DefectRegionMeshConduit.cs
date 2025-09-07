using System.Collections.Generic;
using Rhino.Display;
using Rhino.Geometry;
using IDS.Glenius.Visualization;

namespace IDSCore.Glenius.Drawing
{
    public class DefectRegionMeshConduit : DisplayConduit
    {
        private List<Mesh> _defectRegions;
        private List<Mesh> _nonDefectRegions;
        private BoundingBox _boundingBox = BoundingBox.Empty;

        public List<Curve> DrawnDefectCurves { get; set; }

        public new bool Enabled
        {
            get { return base.Enabled; }
            set
            {
                if(value)
                    InvalidateBoundingBox();

                base.Enabled = value;
            }
        }

        public List<Mesh> DefectRegions
        {
            get { return _defectRegions; }
            set
            {
                _defectRegions = value;
                InvalidateBoundingBox();
            }
        }

        public List<Mesh> NonDefectRegions
        {
            get { return _nonDefectRegions; }
            set
            {
                _nonDefectRegions = value;
                InvalidateBoundingBox();
            }
        }

        private void InvalidateBoundingBox()
        {
            var meshOfAll = new Mesh();
            _defectRegions?.ForEach(x => meshOfAll.Append(x));
            _nonDefectRegions?.ForEach(x => meshOfAll.Append(x));
            _boundingBox = meshOfAll.GetBoundingBox(true);
        }

        private DisplayMaterial CreateMaterial(int r, int g, int b)
        {
            var color = System.Drawing.Color.FromArgb(r, g, b);

            var mat = new DisplayMaterial();
            mat.Ambient = color;
            mat.Diffuse = color;

            return mat;
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            foreach (var nD in NonDefectRegions)
            {
                var color = Colors.GoodScapula;
                e.Display.DrawMeshShaded(nD, CreateMaterial(color.R, color.G, color.B));
            }

            foreach (var dfMesh in DefectRegions)
            {
                var color = Colors.DefectRegionMesh;
                e.Display.DrawMeshShaded(dfMesh, CreateMaterial(color.R, color.G, color.B));
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            foreach(var c in DrawnDefectCurves)
            {
                e.Display.DrawCurve(c, Colors.DefectRegionCurve, 3);
            }
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            if (_boundingBox.IsValid)
            {
                e.IncludeBoundingBox(_boundingBox);
            }
        }
    }
}
