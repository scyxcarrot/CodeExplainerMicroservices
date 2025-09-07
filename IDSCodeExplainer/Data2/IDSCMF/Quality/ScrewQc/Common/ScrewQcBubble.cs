using IDS.CMF.V2.ScrewQc;
using IDS.Core.PluginHelper;
using IDS.RhinoInterfaces.Converter;
using Rhino.Display;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Text;

namespace IDS.CMF.ScrewQc
{
    public class ScrewQcBubble : DisplayConduit
    {
        public static readonly Color PassColor = Color.Green;
        public static readonly Color FailColor = Color.Red;
        public static readonly Color TextColor = Color.White;

        public ImmutableList<string> FailedMessage { get; }

        public string RemarkMessage { get; }

        public bool Failed => FailedMessage.Any();

        public Color BubbleColor => Failed ? FailColor : PassColor;

        public string Label { get; }

        public Point3d Location { get; }

        public ScrewQcBubble(ImmutableList<ScrewInfoRecord> screwInfoRecords, ImmutableList<string> failedMessage, string remarkMessage = null)
        {
            if (screwInfoRecords.Count == 1)
            {
                Label = $"{screwInfoRecords[0].Index}";
            }
            else if(screwInfoRecords.Count > 1)
            {
                Label = string.Join(",", ScrewQcUtilitiesV2.SortScrewInfoRecords(
                    screwInfoRecords).Select(r => r.GetScrewNumberForScrewQcBubble()));
            }
            else
            {
                throw new IDSException("Screw info");
            } 
            
            Location = RhinoPoint3dConverter.ToPoint3d(screwInfoRecords[0].HeadPoint);
            FailedMessage = failedMessage;
            RemarkMessage = remarkMessage;
        }

        public ScrewQcBubble(ScrewInfoRecord screwInfoRecord, ImmutableList<string> failedMessage, string remarkMessage = null) : 
            this(new List<ScrewInfoRecord>(){ screwInfoRecord }.ToImmutableList(), failedMessage, remarkMessage)
        {
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            base.DrawForeground(e);

            var infoMessage = new StringBuilder(Label);
            if (!string.IsNullOrEmpty(RemarkMessage))
            {
                infoMessage.Append($"\n{RemarkMessage}");
            }

            if (Failed)
            {
                infoMessage.Append($"\n{string.Join("\n", FailedMessage)}");
            }

            e.Display.DrawDot(Location, infoMessage.ToString(), BubbleColor, TextColor);
        }
    }
}
