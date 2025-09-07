using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Drawing;

namespace IDS.CMF.Visualization
{
    public class NumberBubbleConduit : DisplayConduit
    {
        private bool _invalidateBubble = false;

        public int Number { get; set; }
        public Color TextColor { get; set; }

        private Color _bubbleColor;
        public Color BubbleColor
        {
            get => _bubbleColor;
            set
            {
                _bubbleColor = value;
                _invalidateBubble = true;
            }
        }

        private Point3d _location;
        public Point3d Location
        {
            get => _location;
            set
            {
                _location = value;
                _invalidateBubble = true;
            }
        }

        private double _bubbleRadius { get; set; }
        public double BubbleRadius
        {
            get => _bubbleRadius;
            set
            {
                _bubbleRadius = value;
                _invalidateBubble = true;
            }
        }

        private class NumberBubbleData
        {
            public Brep BubbleBrep;
            public Circle BubbleBorder;
            public Vector3d Normal;
            public Point3d Origin;
        }

        private NumberBubbleData _numberBubbleBuffer;

        public Color BorderColor { get; set; }
        public int BubbleBorderThickness { get; set; }
        public double DisplaySize { get; set; }

        protected NumberBubbleConduit(int number, Color textColor, Color bubbleColor) : this(Point3d.Origin, number, textColor, bubbleColor)
        {
            BorderColor = Color.Black;
            BubbleRadius = 1.0;
            DisplaySize = 1.0;
            BubbleBorderThickness = 2;
        }

        public NumberBubbleConduit(Point3d location, int number, Color textColor, Color bubbleColor)
        {
            BubbleColor = bubbleColor;
            Number = number;
            Location = location;
            TextColor = textColor;
            BorderColor = Color.Black;
            BubbleRadius = 1.0;
            DisplaySize = 1.0;
            BubbleBorderThickness = 2;
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            var txtEntity = new TextEntity();
            txtEntity.PlainText = Number.ToString();
            txtEntity.TextHorizontalAlignment = TextHorizontalAlignment.Center;
            txtEntity.TextVerticalAlignment = TextVerticalAlignment.Middle;
            txtEntity.TextHeight = DisplaySize;
            txtEntity.SetBold(true);

            var txtEntity2 = new TextEntity();
            txtEntity2.PlainText = Number.ToString();
            txtEntity2.TextHorizontalAlignment = TextHorizontalAlignment.Center;
            txtEntity2.TextVerticalAlignment = TextVerticalAlignment.Middle;
            txtEntity2.TextHeight = DisplaySize + 0.1;
            txtEntity2.SetBold(true);

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.GetCameraFrame(out var camFrame);
            var xformTranslateNumber = Transform.Translation(Location - camFrame.Origin);
            var xformCamFrame = Transform.PlaneToPlane(Plane.WorldXY, camFrame);
            var xformFinalNumber = Transform.Multiply(xformTranslateNumber, xformCamFrame);

            var vecBgTranslate = Location - camFrame.Origin;
            var vecBgTranslateLength = vecBgTranslate.Length;
            vecBgTranslate.Unitize();
            var ptBg = camFrame.Origin + vecBgTranslate * (vecBgTranslateLength + 0.1);

            var xformTranslateBg = Transform.Translation(ptBg - camFrame.Origin);
            var xformFinalBg = Transform.Multiply(xformTranslateBg, xformCamFrame);

            if (_invalidateBubble)
            {
                var cBg = new Circle(Point3d.Origin, BubbleRadius);
                cBg.Transform(xformFinalBg);
                var zAxis = Vector3d.ZAxis;
                zAxis.Transform(xformFinalBg);
                zAxis.Unitize();
                var ptOrigin = Point3d.Origin;
                ptOrigin.Transform(xformFinalBg);

                var tmpPl = Plane.WorldXY;
                tmpPl.Transform(xformFinalBg);

                var cBdr = new Circle(Point3d.Origin, BubbleRadius);
                cBdr.Transform(xformFinalBg);

                _numberBubbleBuffer = new NumberBubbleData() { Normal = zAxis, Origin = ptOrigin, BubbleBrep = Brep.CreateFromCylinder(new Cylinder(cBg, 0.1), true, true), BubbleBorder = cBdr };
                _invalidateBubble = false;
            }
            else
            {
                var sourceNormal = _numberBubbleBuffer.Normal;
                var targetNormal = -RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection;

                if (!sourceNormal.EpsilonEquals(targetNormal, 0.001))
                {
                    var xform = Transform.Rotation(sourceNormal, targetNormal, _numberBubbleBuffer.Origin);
                    _numberBubbleBuffer.BubbleBrep.Transform(xform);
                    sourceNormal.Transform(xform);
                    _numberBubbleBuffer.Normal = sourceNormal;

                    _numberBubbleBuffer.BubbleBorder.Transform(xform);
                }
            }

            e.Display.DrawBrepShaded(_numberBubbleBuffer.BubbleBrep, new DisplayMaterial(Number < 0 ? Color.Red : BubbleColor));

            e.Display.DrawCircle(_numberBubbleBuffer.BubbleBorder, BorderColor, BubbleBorderThickness);

            e.Display.DrawText(txtEntity2, Color.Black, xformFinalNumber);
            e.Display.DrawText(txtEntity, Color.White, xformFinalNumber);
        }

    }
}
