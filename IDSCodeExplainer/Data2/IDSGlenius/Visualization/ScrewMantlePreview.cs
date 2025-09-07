using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.Glenius.Visualization
{
    public class ScrewMantlePreview : IDisposable
    {
        private readonly ScrewMantleBrepFactory _factory;
        private readonly DisplayMaterial _material;
        private readonly Point3d _startExtension;
        private readonly Vector3d _extensionDirection;
        private Brep _screwMantleBrep;

        private double _extensionLength;
        public double ExtensionLength
        {
            get { return _extensionLength; }
            set
            {
                _extensionLength = value;
                UpdateScrewMantleBrep();
            }
        }

        public ScrewMantlePreview(ScrewMantle screwMantle)
        {
            _startExtension = screwMantle.StartExtension;
            var direction = new Vector3d(screwMantle.ExtensionDirection);
            if (!direction.IsUnitVector)
            {
                direction.Unitize();
            }
            _extensionDirection = direction;
            _extensionLength = screwMantle.ExtensionLength;
            _material = new DisplayMaterial(Colors.MobelifeRed, 0.75);

            _factory = new ScrewMantleBrepFactory(screwMantle.ScrewType);
            UpdateScrewMantleBrep();
        }

        public void DrawScrewMantle(DisplayPipeline display)
        {
            display.DrawBrepShaded(_screwMantleBrep, _material);
            display.DrawDot(100, 50, $"{ExtensionLength:F0}mm", Color.Black, Color.White);
        }

        private void UpdateScrewMantleBrep()
        {
            _screwMantleBrep = _factory.CreateScrewMantleBrep(_startExtension, _extensionDirection, ExtensionLength);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _material.Dispose();
            }
        }
    }
} 