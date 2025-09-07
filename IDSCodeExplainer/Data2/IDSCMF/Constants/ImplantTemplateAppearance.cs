using System.Drawing;
using System.Windows.Media;
using Color = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaBrush = System.Windows.Media.Brush;
using DrawingBrush = System.Drawing.Brush;

namespace IDS.CMF.Constants
{
    public static class ImplantTemplateAppearance
    {
        //Legend Appearance
        public static readonly MediaBrush LegendForeBrush = MediaBrushes.White;
        //Canvas Appearance
        public const int ImplantTemplateDefaultWidth = 200;
        public const int ImplantTemplateDefaultHeight = 150;
        //Canvas Border Appearance
        public static readonly MediaBrush ImplantTemplateBorderInactiveBrush = MediaBrushes.Black;
        public static readonly MediaBrush ImplantTemplateBorderActiveBrush = MediaBrushes.GreenYellow;
        //Screw Point Appearance
        public const int ScrewPointDiameter = 8;
        public static readonly Color ScrewPointFillColor = Color.Yellow;
        public static readonly DrawingBrush ScrewPointFillDrawingBrush = new SolidBrush(ScrewPointFillColor);
        //Plate Connection Appearance
        public static readonly Color PlateConnectionStrokeColor = Color.Blue;
        public static readonly MediaBrush PlateConnectionStrokeColorBrush = new SolidColorBrush(PlateConnectionStrokeColor.ToMediaColor());
        public const float PlateConnectionStrokeThickness = 8;
        //Link Connection Appearance
        public static readonly Color LinkConnectionStrokeColor = Color.Green;
        public static readonly MediaBrush LinkConnectionStrokeColorBrush = new SolidColorBrush(LinkConnectionStrokeColor.ToMediaColor());
        public const float LinkConnectionStrokeThickness = 8;
        //Segmented Bone Appearance
        public static readonly Color SegmentedBoneFillColor = Color.DarkGray;

        private static MediaColor ToMediaColor(this Color color)
        {
            return MediaColor.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
