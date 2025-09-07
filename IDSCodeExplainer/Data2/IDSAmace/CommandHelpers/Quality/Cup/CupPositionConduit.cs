using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;


namespace IDS.Amace.Visualization
{
    /**
     * Displayconduit to show visual aides for cup positioning.
     */

    public class CupPositionConduit : DisplayConduit
    {
        /// <summary>
        /// Gets or sets the cup.
        /// </summary>
        /// <value>
        /// The cup.
        /// </value>
        public Cup cup { get; set; }

        /// <summary>
        /// Gets the coordinate system.
        /// </summary>
        /// <value>
        /// The coordinate system.
        /// </value>
        private Plane CoordinateSystem => cup?.coordinateSystem ?? Plane.Unset;

        /// <summary>
        /// Gets the cup center of rotation.
        /// </summary>
        /// <value>
        /// The cup center of rotation.
        /// </value>
        public Point3d CupCenterOfRotation => cup?.centerOfRotation ?? Point3d.Unset;

        /// <summary>
        /// The contralateral center of rotation
        /// </summary>
        private readonly Point3d _contralateralCenterOfRotation = Point3d.Unset;
        /// <summary>
        /// The defect center of rotation
        /// </summary>
        private readonly Point3d _defectCenterOfRotation = Point3d.Unset;

        /// <summary>
        /// The color
        /// </summary>
        private readonly Color _lineColor = Color.Black;

        /// <summary>
        /// Gets or sets the line thickness.
        /// </summary>
        /// <value>
        /// The line thickness.
        /// </value>
        public int LineThickness { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [draw lines].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [draw lines]; otherwise, <c>false</c>.
        /// </value>
        public bool DrawLines { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [draw text].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [draw text]; otherwise, <c>false</c>.
        /// </value>
        public bool DrawText { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [draw centers of rotation].
        /// </summary>
        /// <value>
        /// <c>true</c> if [draw centers of rotation]; otherwise, <c>false</c>.
        /// </value>
        public bool DrawCentersOfRotation { get; set; }

        private readonly Mesh _defect;
        private readonly Mesh _contralateral;
        private readonly Mesh _sacrum;

        /**
         * Create a new display conduit for showing QC measurements.
         */

        public CupPositionConduit(Cup cup, Point3d centerOfRotationContralateralFemur, Point3d centerOfRotationDefectFemur, Color color, Mesh defectPelvis, Mesh contralateralPelvis, Mesh sacrum, bool drawLines, bool drawCentersOfRotation, bool drawText)
        {
            this.cup = cup;

            if (centerOfRotationContralateralFemur.IsValid)
            {
                _contralateralCenterOfRotation = centerOfRotationContralateralFemur;
            }

            if (centerOfRotationDefectFemur.IsValid)
            {
                _defectCenterOfRotation = centerOfRotationDefectFemur;
            }

            _lineColor = color;
            LineThickness = 2;

            DrawLines = drawLines;
            DrawCentersOfRotation = drawCentersOfRotation;
            DrawText = drawText;

            _sacrum = sacrum;
            _defect = defectPelvis ?? new Mesh();
            _contralateral = contralateralPelvis ?? new Mesh();
        }

        public BoundingBox Bounds
        {
            get
            {
                // Combine to get geometry around which the bounds should be created
                var full = new Mesh();
                full.Append(_defect);
                full.Append(_contralateral);
                full.Append(_sacrum);
                // Calculate bounding box and inflate slightly
                var bnds = full.GetBoundingBox(false);
                bnds.Inflate(15);

                // Dispose
                full.Dispose(); // full mesh is not disposed automatically!

                return bnds;
            }
        }

        /// <summary>
        /// Called after all non-highlighted objects have been drawn and PostDrawObjects has been called.
        /// Depth writing and testing are turned OFF. If you want to draw with depth writing/testing,
        /// see PostDrawObjects.
        /// <para>The default implementation does nothing.</para>
        /// </summary>
        /// <param name="e">The event argument contains the current viewport and display state.</param>
        protected override void DrawForeground(DrawEventArgs e)
        {
            // General coordinates
            var scrMinBnd = cup.Director.Document.Views.ActiveView.ActiveViewport.WorldToClient(Bounds.Min);
            var scrMaxBnd = cup.Director.Document.Views.ActiveView.ActiveViewport.WorldToClient(Bounds.Max);
            var scrPcs = cup.Director.Document.Views.ActiveView.ActiveViewport.WorldToClient(cup.Director.Pcs.Origin);

            // Cup screen coordinates
            var scrCupCor = cup.Director.Document.Views.ActiveView.ActiveViewport.WorldToClient(CupCenterOfRotation);
            var scrLatCupPcs = new Point2d(scrPcs.X + (scrCupCor.X - scrPcs.X) / 2, scrMinBnd.Y);

            // Cup values
            var latCupPcs = (CupCenterOfRotation - CoordinateSystem.Origin) * CoordinateSystem.YAxis;

            // Draw
            DrawCupVisuals(e, scrLatCupPcs, latCupPcs);
            DrawContralateralVisuals(e, scrLatCupPcs, scrPcs, scrMinBnd, scrMaxBnd, scrCupCor, latCupPcs);
            DrawDefectVisuals(e, scrCupCor, scrMinBnd, scrMaxBnd);
        }

        private void DrawCupVisuals(DrawEventArgs e, Point2d scrLatCupPcs, double latCupPcs)
        {
            // Draw lines
            if (DrawLines)
            {
                Drawing2D.DrawVerticalLine(e.Display, _lineColor, LineThickness, Bounds, CupCenterOfRotation, CoordinateSystem);
                Drawing2D.DrawVerticalLine(e.Display, _lineColor, LineThickness, Bounds, CoordinateSystem.Origin, CoordinateSystem);
                Drawing2D.DrawHorizontalLine(e.Display, _lineColor, LineThickness, Bounds, CupCenterOfRotation, CoordinateSystem);
            }

            if (!DrawCentersOfRotation)
            {
                return;
            }

            e.Display.DrawDot(CupCenterOfRotation, "", Color.Green, Color.White);
            if (_contralateralCenterOfRotation == Point3d.Unset)
            {
                e.Display.DrawDot((int)scrLatCupPcs.X, (int)scrLatCupPcs.Y, string.Format("{0:F0}mm", cup.Director.SignMedLatPcs * latCupPcs), _lineColor, Color.WhiteSmoke);
            }
        }

        private void DrawDefectVisuals(DrawEventArgs e, Point2d scrCupCor, Point2d scrMinBnd, Point2d scrMaxBnd)
        {
            if (_defectCenterOfRotation == Point3d.Unset)
            {
                return;
            }

            // Defect screen coordinates
            var scrDefectCOR = cup.Director.Document.Views.ActiveView.ActiveViewport.WorldToClient(_defectCenterOfRotation);
            var scrLatCupDefect = new Point2d(scrDefectCOR.X + (scrCupCor.X - scrDefectCOR.X) / 2, scrMaxBnd.Y);
            var scrInfCupDefect = new Point2d();

            scrInfCupDefect = cup.Director.defectIsLeft ? 
                new Point2d(scrMaxBnd.X, scrDefectCOR.Y + (scrCupCor.Y - scrDefectCOR.Y) / 2) :
                new Point2d(scrMinBnd.X, scrDefectCOR.Y + (scrCupCor.Y - scrDefectCOR.Y) / 2);

            // Defect values
            var infCupDefect = MathUtilities.GetOffset(CoordinateSystem.ZAxis, _defectCenterOfRotation, CupCenterOfRotation);
            var latCupDefect = MathUtilities.GetOffset(CoordinateSystem.YAxis, _defectCenterOfRotation, CupCenterOfRotation);

            // Draw lines
            if (DrawLines)
            {
                Drawing2D.DrawVerticalLine(e.Display, _lineColor, LineThickness, Bounds, _defectCenterOfRotation, CoordinateSystem, LineType.Top);
                Drawing2D.DrawHorizontalLine(e.Display, _lineColor, LineThickness, Bounds, _defectCenterOfRotation, CoordinateSystem, cup.Director.defectIsLeft ? LineType.Right : LineType.Left);
            }

            // Draw HJC
            if (DrawCentersOfRotation)
            {
                e.Display.DrawDot(_defectCenterOfRotation, "", Color.Red, Color.White);
            }

            // Measurement text
            if (!DrawText)
            {
                return;
            }

            e.Display.DrawDot((int)scrLatCupDefect.X, (int)scrLatCupDefect.Y, string.Format("{0:+#;-#;0}mm", cup.Director.SignMedLatDef * latCupDefect), _lineColor, Color.WhiteSmoke);
            e.Display.DrawDot((int)scrInfCupDefect.X, (int)scrInfCupDefect.Y, string.Format("{0:+#;-#;0}mm", cup.Director.SignInfSupDef * infCupDefect), _lineColor, Color.WhiteSmoke);
        }

        private void DrawContralateralVisuals(DrawEventArgs e, Point2d scrLatCupPCS, Point2d scrPCS, Point2d scrMinBnd, Point2d scrMaxBnd, Point2d scrCupCOR, double latCupPCS)
        {
            if (_contralateralCenterOfRotation == Point3d.Unset)
            {
                return;
            }

            // Contralateral screen coordinates
            var scrClatCOR = cup.Director.Document.Views.ActiveView.ActiveViewport.WorldToClient(_contralateralCenterOfRotation);
            var scrLatClatPCS = new Point2d(scrPCS.X + (scrClatCOR.X - scrPCS.X) / 2, scrMinBnd.Y);
            var scrInfCupClat = new Point2d();

            scrInfCupClat = cup.Director.defectIsLeft ?
                new Point2d(scrMinBnd.X, scrClatCOR.Y + (scrCupCOR.Y - scrClatCOR.Y) / 2) :
                new Point2d(scrMaxBnd.X, scrClatCOR.Y + (scrCupCOR.Y - scrClatCOR.Y) / 2);


            // Clat values
            var infCupClat = MathUtilities.GetOffset(CoordinateSystem.ZAxis, _contralateralCenterOfRotation, CupCenterOfRotation);
            var latClatPcs = MathUtilities.GetOffset(CoordinateSystem.YAxis, CoordinateSystem.Origin, _contralateralCenterOfRotation);

            // Draw lines
            if (DrawLines)
            {
                Drawing2D.DrawVerticalLine(e.Display, _lineColor, LineThickness, Bounds, _contralateralCenterOfRotation, CoordinateSystem);
                Drawing2D.DrawHorizontalLine(e.Display, _lineColor, LineThickness, Bounds, _contralateralCenterOfRotation, CoordinateSystem,
                    cup.Director.defectIsLeft ? LineType.Left : LineType.Right);
            }

            // Draw COR
            if (DrawCentersOfRotation)
            {
                e.Display.DrawDot(_contralateralCenterOfRotation, "", Color.Yellow, Color.White);
            }

            // Measurement text
            if (!DrawText)
            {
                return;
            }

            e.Display.DrawDot((int)scrInfCupClat.X, (int)scrInfCupClat.Y, string.Format("{0:+#;-#;0}mm", cup.Director.SignInfSupClat * infCupClat), _lineColor, Color.WhiteSmoke);
            e.Display.DrawDot((int)scrLatClatPCS.X, (int)scrLatClatPCS.Y, string.Format("{0:F0}mm", -cup.Director.SignMedLatPcs * latClatPcs), _lineColor, Color.WhiteSmoke);
            e.Display.DrawDot((int)scrLatCupPCS.X, (int)scrLatCupPCS.Y, string.Format("{0:F0}mm ({1:+#;-#;0}mm)",
                cup.Director.SignMedLatPcs * latCupPCS, cup.Director.SignMedLatPcs * latCupPCS + cup.Director.SignMedLatPcs * latClatPcs), _lineColor, Color.WhiteSmoke);
        }
    }
}