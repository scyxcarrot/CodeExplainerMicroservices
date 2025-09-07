using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Display;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Visualization
{
    public class ImplantNumberBubbleConduit : NumberBubbleConduit
    {
        public ImplantDataModelBase ImplantData { get; set; }

        public ImplantNumberBubbleConduit(ImplantDataModelBase implantData, int number, Color textColor, Color bubbleColor) :
            base(number, textColor, bubbleColor)
        {
            ImplantData = implantData;
            Location = GetBubbleLocation();
        }

        private List<IDot> GetAllConnectionDots()
        {
            var res = new List<IDot>();
            res.AddRange(ImplantData.ConnectionList.Select(x => x.A));
            res.AddRange(ImplantData.ConnectionList.Select(x => x.B));
            return res;
        }

        private IDot GetRightSideOfCameraDot()
        {
            var dots = GetAllConnectionDots();
            if (!dots.Any())
            {
                return null;
            }

            return ImplantCreationUtilities.FindFurthestMostDot(dots, VectorUtilities.GetCameraRightSideVector());
        }

        private Point3d GetBubbleLocation()
        {
            var dot = GetRightSideOfCameraDot();
            if (dot == null)
            {
                return Point3d.Unset;
            }

            var vec = VectorUtilities.GetCameraRightSideVector();
            vec.Unitize();
            var tmpLoc = RhinoPoint3dConverter.ToPoint3d(dot.Location);
            return tmpLoc + (vec * 5);
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            if (Location == Point3d.Unset)
            {
                return;
            }

            Location = GetBubbleLocation();
            base.DrawForeground(e);
        }

    }
}
