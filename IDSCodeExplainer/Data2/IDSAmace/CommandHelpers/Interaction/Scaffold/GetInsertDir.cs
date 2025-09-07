using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Visualization;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;

using SysColor = System.Drawing.Color;

namespace IDS.Amace.Operations
{
    /// <summary>
    /// Custom getter for indicating the insertion direction
    /// </summary>
    /// <seealso cref="Rhino.Input.Custom.GetPoint" />
    public class GetInsertDir : Rhino.Input.Custom.GetPoint
    {
        /// <summary>
        /// The director
        /// </summary>
        private ImplantDirector _director;

        /// <summary>
        /// The material entity
        /// </summary>
        private DisplayMaterial _materialEntity = new DisplayMaterial()
        {
            Transparency = 0.5,
            Shine = 0.0,
            IsTwoSided = false,
            Ambient = SysColor.ForestGreen,
            Diffuse = SysColor.ForestGreen,
            Specular = SysColor.ForestGreen,
            Emission = SysColor.ForestGreen,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="GetInsertDir"/> class.
        /// </summary>
        /// <param name="director">The director.</param>
        public GetInsertDir(ImplantDirector director)
        {
            _director = director;
            this.AcceptNothing(true);
        }

        /// <summary>
        /// Create new getter for indicating the insertion direction.
        /// </summary>
        /// <returns></returns>
        public Vector3d GetDirection()
        {
            Vector3d dir = Vector3d.Unset;
            while (true)
            {
                Rhino.Input.GetResult res = this.Get();
                if (res == Rhino.Input.GetResult.Nothing) // Pressed Enter
                {
                    dir = _director.Document.Views.ActiveView.ActiveViewport.CameraDirection;
                    dir.Unitize();
                    return dir;
                }

                if (res == Rhino.Input.GetResult.Cancel)
                {
                    return dir;
                }
            }
        }

        /// <summary>
        /// Default calls the DynamicDraw event.
        /// Draw the visual aides every frame.
        /// </summary>
        /// <param name="e">Current argument for the event.</param>
        /// <example>
        ///   <code source="examples\vbnet\ex_getpointdynamicdraw.vb" lang="vbnet" />
        ///   <code source="examples\cs\ex_getpointdynamicdraw.cs" lang="cs" />
        ///   <code source="examples\py\ex_getpointdynamicdraw.py" lang="py" />
        /// </example>
        protected override void OnDynamicDraw(Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            Cup cup = _director.cup;
            Vector3d insertionDirection = -_director.Document.Views.ActiveView.ActiveViewport.CameraDirection;
            Plane PCS = _director.Inspector.AxialPlane;

            // Draw cup vector
            Line cupLine = new Line(cup.centerOfRotation, cup.centerOfRotation + (cup.orientation * 100));
            e.Display.DrawLine(cupLine, Colors.MetalCup, 5);

            // Compute angles
            Vector3d lateralDir = _director.defectIsLeft ? PCS.YAxis : -PCS.YAxis;
            double ang_AV_rad = Math.Atan2(insertionDirection * PCS.XAxis, insertionDirection * lateralDir);
            double ang_AV_deg = RhinoMath.ToDegrees(ang_AV_rad);
            double ang_INCL_rad = Math.Atan2(insertionDirection * lateralDir, insertionDirection * -PCS.ZAxis);
            double ang_INCL_deg = RhinoMath.ToDegrees(ang_INCL_rad);

            // Display Angles
            string ang_string = string.Format("AV: {0,-5:F1}{2}INCL: {1,-5:F1}", ang_AV_deg, ang_INCL_deg, Environment.NewLine.ToString());
            e.Display.DrawDot(50, 50, ang_string, SysColor.Black, SysColor.White);
        }
    }
}