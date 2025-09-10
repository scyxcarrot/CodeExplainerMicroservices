using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class ImplantTemplateBitmapDrawer
    {
        private readonly double _scaleX;
        private readonly double _scaleY;
        private readonly double _rotationAngle;
        private readonly Bitmap _drawingBitmap;
        private readonly Graphics _graphics;
        private readonly int _centreX;
        private readonly int _centreY;

        public Bitmap LatestBitmap => new Bitmap(_drawingBitmap);

        public ImplantTemplateBitmapDrawer(double scaleX = 1, double scaleY = 1, double rotationAngle = 0)
        {
            _scaleX = scaleX;
            _scaleY = scaleY;
            _rotationAngle = rotationAngle;

            var halfWidth = ImplantTemplateAppearance.ImplantTemplateDefaultWidth / 2;
            var halfHeight = ImplantTemplateAppearance.ImplantTemplateDefaultHeight / 2;

            var cornerPoints = new List<Point>()
            {
                new Point(-halfWidth, halfHeight),
                new Point(halfWidth, halfHeight),
                new Point(-halfWidth, -halfHeight),
                new Point(halfWidth, -halfHeight)
            };

            var transformedCornerPoints = cornerPoints.Select(p =>
                PointUtilities.ScaleThenRotate2dPoint(p, _scaleX, _scaleY, rotationAngle));

            PointUtilities.Get2dBoundingBox(transformedCornerPoints, out var minPoint, out var maxPoint);
            var newWidth = maxPoint.X - minPoint.X;
            var newHeight = maxPoint.Y - minPoint.Y;
            
            _drawingBitmap = new Bitmap(newWidth, newHeight);
            _graphics = Graphics.FromImage(_drawingBitmap);
            _graphics.SmoothingMode = SmoothingMode.HighQuality;
            _centreX = _drawingBitmap.Width / 2;
            _centreY = _drawingBitmap.Height / 2;
        }

        private Point TransformPoint(Point pointOriginal)
        {
            var transformedPoint = PointUtilities.ScaleThenRotate2dPoint(pointOriginal, _scaleX, _scaleY, _rotationAngle);
            transformedPoint.Offset(new Point(_centreX, _centreY));
            return transformedPoint;
        }

        private void DrawScrewPoint(int x, int y)
        {
            var diameter = ImplantTemplateAppearance.ScrewPointDiameter;

            var transformedPoint = TransformPoint(new Point(x, y));

            _graphics.FillEllipse(ImplantTemplateAppearance.ScrewPointFillDrawingBrush,
                transformedPoint.X - diameter / 2, transformedPoint.Y - diameter / 2, diameter, diameter);
        }

        private void DrawBaseConnection(ImplantTemplateScrew a, ImplantTemplateScrew b,
            Color strokeColor, float strokeThickness)
        {
            var stokePen = new Pen(strokeColor, strokeThickness)
            {
                Alignment = PenAlignment.Center
            };

            var transformedPointA = TransformPoint(new Point(a.X, a.Y));
            var transformedPointB = TransformPoint(new Point(b.X, b.Y));

            _graphics.DrawLine(stokePen, transformedPointA.X, transformedPointA.Y, transformedPointB.X, transformedPointB.Y);
        }

        private void DrawPlateConnection(ImplantTemplateScrew a, ImplantTemplateScrew b)
        {
            DrawBaseConnection(a, b, ImplantTemplateAppearance.PlateConnectionStrokeColor,
                ImplantTemplateAppearance.PlateConnectionStrokeThickness);
        }

        private void DrawLinkConnection(ImplantTemplateScrew a, ImplantTemplateScrew b)
        {
            DrawBaseConnection(a, b, ImplantTemplateAppearance.LinkConnectionStrokeColor,
                ImplantTemplateAppearance.LinkConnectionStrokeThickness);
        }

        private void DrawConnection(ImplantTemplateScrew a, ImplantTemplateScrew b, ImplantTemplateConnectionType type)
        {
            switch (type)
            {
                case ImplantTemplateConnectionType.Plate:
                    DrawPlateConnection(a, b);
                    break;
                case ImplantTemplateConnectionType.Link:
                    DrawLinkConnection(a, b);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Out of the option in enum", "type");
            }
        }

        private void DrawSegmentedBone(int x, int y, int width, int height)
        {
            var color = ImplantTemplateAppearance.SegmentedBoneFillColor;
            var brush = new SolidBrush(color);
            _graphics.FillRectangle(brush, x, y, width, height);
        }

        private void DrawAllSegmentedBone(List<ImplantTemplateSegmentedBone> segmentedBones)
        {
            foreach (var segmentedBone in segmentedBones)
            {
                DrawSegmentedBone(segmentedBone.X, segmentedBone.Y, segmentedBone.Width, segmentedBone.Height);
            }
        }

        public void DrawFromImplantTemplateDataModel(ImplantTemplateDataModel sourceImplantTemplateDataModel, bool drawSegmentedBone)
        {
            _graphics.TranslateTransform(_centreX, _centreY);
            _graphics.RotateTransform(Convert.ToSingle(_rotationAngle));
            _graphics.ScaleTransform(Convert.ToSingle(_scaleX), Convert.ToSingle(_scaleY));

            if (drawSegmentedBone)
            {
                DrawAllSegmentedBone(sourceImplantTemplateDataModel.SegmentedBones);
            }
            
            _graphics.ScaleTransform(Convert.ToSingle(1 / _scaleX), Convert.ToSingle(1 / _scaleY));
            _graphics.RotateTransform(-Convert.ToSingle(_rotationAngle));
            _graphics.TranslateTransform(-_centreX, -_centreY);

            foreach (var connection in sourceImplantTemplateDataModel.Connections)
            {
                DrawConnection(sourceImplantTemplateDataModel.Screws[connection.A],
                    sourceImplantTemplateDataModel.Screws[connection.B], connection.Type);
            }

            foreach (var screw in sourceImplantTemplateDataModel.Screws)
            {
                DrawScrewPoint(screw.X, screw.Y);
            }
        }
    }
}
