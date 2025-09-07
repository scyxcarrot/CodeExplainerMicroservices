using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Operations;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Operations
{
    public abstract class AdjustScrewLengthBase : AdjustGenericScrewLength
    {
        protected readonly List<double> availableScrewLengths;
        protected readonly CMFImplantDirector director;
        protected Screw referenceScrew;
        protected ScrewGaugeConduit _gaugeConduit;

        protected AdjustScrewLengthBase(Screw screw, List<double> availableLengths)
            : base(screw.HeadPoint, screw.TipPoint, new ScrewPreview(screw))
        {
            director = screw.Director;
            referenceScrew = screw;
            availableScrewLengths = availableLengths;

            var gauges = ScrewGaugeUtilities.CreateScrewGauges(screw, screw.ScrewType);
            _gaugeConduit = new ScrewGaugeConduit(gauges);
        }

        public override Result AdjustLength()
        {
            _gaugeConduit.Enabled = true;
            ConduitUtilities.RefeshConduit();
            var res = base.AdjustLength();
            _gaugeConduit.Enabled = false;

            return res;
        }

        protected override void AdjustMovingPoint(Point3d toPoint)
        {
            if (toPoint != Point3d.Unset)
            {
                var screw = ScrewUtilities.AdjustScrewLength(referenceScrew, toPoint);
                UpdateScrew(screw);
            }

            director.Document.Views.Redraw();
        }

        protected override double GetNearestAvailableScrewLength(double currentLength)
        {
            return Queries.GetNearestAvailableScrewLength(availableScrewLengths, currentLength);
        }

        protected abstract void UpdateScrew(Screw updatedScrew);

        protected override void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            base.DynamicDraw(sender, e);

            var silhouettes = Silhouette.Compute(genericScrewPreview.screwPreview, SilhouetteType.Projecting, e.Viewport.CameraLocation, 0.1, 0.1).ToList();
            silhouettes.ForEach(x =>
            {
                if (x.Curve != null)
                {
                    e.Display.DrawCurve(x.Curve, Color.GreenYellow, 3);
                }
            });
        }
    }
}