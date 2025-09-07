using IDS.Amace.ImplantBuildingBlocks;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;


namespace IDS.Amace.Visualization
{
    public class CupOrientationConduit : DisplayConduit
    {
        /// <summary>
        /// The arrow size
        /// </summary>
        private const double ArrowSize = 50.0;

        /// <summary>
        /// The director
        /// </summary>
        private Cup cup;

        /// <summary>
        /// Show the cup orientation vector
        /// </summary>
        public bool ShowCupVector { get; set; }

        /// <summary>
        /// Show vectors for measuring AV angle
        /// </summary>
        public bool ShowAnteversion { get; set; }

        /// <summary>
        /// Show vectors for measuring INCL angle
        /// </summary>
        public bool ShowInclination { get; set; }

        /// <summary>
        /// Gets or sets the size of the font.
        /// </summary>
        /// <value>
        /// The size of the font.
        /// </value>
        public int FontSize { get; set; }

        public CupOrientationConduit(Cup cup)
        {
            this.cup = cup;

            ShowCupVector = false;
            ShowAnteversion = false;
            ShowInclination = false;

            FontSize = 20;
        }

        /// <summary>
        /// Updates the conduit.
        /// </summary>
        /// <param name="cup">The cup.</param>
        public void UpdateConduit(Cup cup)
        {
            this.cup = cup;
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
            if (cup != null)
            {
                base.DrawForeground(e);

                // Show cup orientation vector
                if (ShowCupVector)
                {
                    HandleShowCupVector(e);
                }

                // Show lateral direction
                if (ShowAnteversion || ShowInclination)
                {
                    HandleShowLateralDirection(e);
                }


                // Show AV vectors in the axial plane
                if (ShowAnteversion)
                {
                    HandleShowAnteversion(e);
                }

                // Show INCL vectors in the frontal plane
                if (ShowInclination)
                {
                    HandleShowInclination(e);
                }
            }
        }

        private Vector3d lateralDirection
        {
            get
            {
                return cup.defectIsLeft ? cup.coordinateSystem.YAxis : -cup.coordinateSystem.YAxis;
            }
        }

        private void HandleShowInclination(DrawEventArgs e)
        {
            HandleShowAngle(  e, 
                        cup.centerOfRotation, 
                        "INCL", 
                        cup.defectIsLeft ? cup.coordinateSystem.XAxis : -cup.coordinateSystem.XAxis, 
                        new Plane(cup.centerOfRotation, -cup.coordinateSystem.ZAxis, lateralDirection), 
                        cup.inclination, 
                        -cup.coordinateSystem.ZAxis);
        }

        private void HandleShowAnteversion(DrawEventArgs e)
        {
            // Values and vector
            HandleShowAngle(  e, 
                        cup.centerOfRotation, 
                        "AV", 
                        cup.defectIsLeft ? -cup.coordinateSystem.ZAxis : cup.coordinateSystem.ZAxis, 
                        new Plane(cup.centerOfRotation, lateralDirection, cup.coordinateSystem.XAxis), 
                        cup.anteversion, 
                        cup.coordinateSystem.XAxis);
        }

        private void HandleShowAngle(DrawEventArgs e, Point3d centerOfRotation, string label, Vector3d rotationAxis, Plane arcPlane, double angleDegrees, Vector3d referenceAxis)
        {
            double angleRadians = RhinoMath.ToRadians(angleDegrees);
            Vector3d angleVector = arcPlane.XAxis;
            angleVector.Rotate(angleRadians, rotationAxis);

            // Plot arrows
            Line arrowAngleReference = new Line(centerOfRotation, centerOfRotation + (ArrowSize * referenceAxis));
            e.Display.DrawArrow(arrowAngleReference, Color.Purple);
            Line arrowAngleDirection = new Line(centerOfRotation, centerOfRotation + (ArrowSize * angleVector));
            e.Display.DrawArrow(arrowAngleDirection, Color.Red);

            // Draw arc
            Arc arcAngle = new Arc(arcPlane, ArrowSize, angleRadians);
            e.Display.DrawArc(arcAngle, Color.Black);
            // Draw text
            double textDistance = 10.0;
            Point3d textCoordinates = arrowAngleDirection.To + (textDistance * angleVector);
            e.Display.DrawDot(textCoordinates, string.Format("{1}: {0,-5:F1}", angleDegrees, label), Color.Black, Color.White);
        }

        private void HandleShowLateralDirection(DrawEventArgs e)
        {
            Line arrowLateral = new Line(cup.centerOfRotation, cup.centerOfRotation + (ArrowSize * lateralDirection));
            e.Display.DrawArrow(arrowLateral, Color.ForestGreen);
        }

        private void HandleShowCupVector(DrawEventArgs e)
        {
            Line orientLine = new Line(cup.centerOfRotation, cup.centerOfRotation + (ArrowSize * cup.orientation));
            e.Display.DrawArrow(orientLine, Color.Gold);
        }
    }
}