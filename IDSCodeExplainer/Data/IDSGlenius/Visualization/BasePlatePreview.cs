using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Display;
using Rhino.Geometry;
using System;

namespace IDS.Glenius.Visualization
{
    public class BasePlatePreview : IDisposable
    {
        private readonly BasePlateMaker _maker;
        private Mesh _basePlateMesh;
        private readonly DisplayMaterial _material;

        private Curve _topCurve;
        public Curve TopCurve
        {
            get { return _topCurve; }
            set
            {
                _topCurve = value;
                UpdateBasePlateMesh();
            }
        }

        private Curve _bottomCurve;
        public Curve BottomCurve
        {
            get { return _bottomCurve; }
            set
            {
                _bottomCurve = value;
                UpdateBasePlateMesh();
            }
        }

        private bool _showSideWallOnly;
        public bool ShowSideWallOnly
        {
            get { return _showSideWallOnly; }
            set
            {
                _showSideWallOnly = value;
                UpdateBasePlateMesh();
            }
        }

        public BasePlatePreview()
        {
            _material = new DisplayMaterial(BuildingBlocks.Blocks[IBB.PlateBasePlate].Color, 0.0);
            _maker = new BasePlateMaker();
            UpdateBasePlateMesh();
        }

        public void DrawPreview(DisplayPipeline display)
        {
            if (_basePlateMesh != null)
            {
                display.DrawMeshShaded(_basePlateMesh, _material);
            }
        }

        private void UpdateBasePlateMesh()
        {
            if (_topCurve == null || _bottomCurve == null)
            {
                return;
            }

            if (_maker.CreateBasePlate(_topCurve, _bottomCurve, _showSideWallOnly))
            {
                _basePlateMesh = _maker.BasePlate;
            }
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