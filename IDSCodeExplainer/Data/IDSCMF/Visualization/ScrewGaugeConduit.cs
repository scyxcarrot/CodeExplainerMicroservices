using Rhino.Display;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static IDS.CMF.Utilities.ScrewGaugeUtilities;

namespace IDS.CMF.Visualization
{
    public class ScrewGaugeConduit : DisplayConduit
    {

        public List<ScrewGaugeData> GaugesData { get; private set; }
        private readonly HashSet<Brep> _screws;

        public bool ShowScrew { get; set; }
        public bool ShowScrewOutline { get; set; }
        public bool ShowGaugeOutline { get; set; }

        public ScrewGaugeConduit(List<ScrewGaugeData> gaugesData)
        {
            GaugesData = gaugesData;
            ShowScrew = false;
            ShowScrewOutline = false;
            ShowGaugeOutline = true;

            _screws = new HashSet<Brep>();
            GaugesData.ForEach(x => { _screws.Add((Brep)x.Screw.Geometry); });
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            if (ShowScrew)
            {
                _screws.ToList().ForEach(x =>
                {
                    if (x != null && !x.Disposed)
                    {
                        e.Display.DrawBrepShaded(x, new DisplayMaterial(Colors.ScrewTemporary, 0.0));
                    }
                });
            }

            GaugesData.ForEach(x =>
            {
                if (x.Screw == null || x.Screw.Disposed)
                {
                    return;
                }

                var gaugeMesh = x.Gauge;
                if (gaugeMesh != null && !gaugeMesh.Disposed)
                {
                    e.Display.DrawMeshShaded(gaugeMesh, x.GaugeMaterial);
                }
            });
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            if (ShowGaugeOutline)
            {
                var gaugeMeshCombined = new Mesh();
                GaugesData.ForEach(x =>
                {
                    if (x.Screw == null || x.Screw.Disposed)
                    {
                        return;
                    }

                    gaugeMeshCombined.Append(x.Gauge);
                });

                var silhouettes = Silhouette.Compute(gaugeMeshCombined, SilhouetteType.Boundary, e.Viewport.CameraLocation, 0.1, 0.1).ToList();
                silhouettes.ForEach(x =>
                {
                    e.Display.DrawCurve(x.Curve, Color.LightGray, 1);
                });
            }

            if (ShowScrewOutline)
            {
                _screws.ToList().ForEach(x =>
                {
                    var ScrewSilhouettes = Silhouette.Compute(x, SilhouetteType.Boundary, e.Viewport.CameraLocation, 0.1, 0.1).ToList();
                    ScrewSilhouettes.ForEach(y =>
                    {
                        if (y.Curve != null)
                        {
                            e.Display.DrawCurve(y.Curve, Color.YellowGreen, 3);
                        }
                    });
                });
            }
        }
    }
}
