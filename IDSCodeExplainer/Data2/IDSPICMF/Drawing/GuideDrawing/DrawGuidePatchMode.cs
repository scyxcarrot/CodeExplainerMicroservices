using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace IDS.PICMF.Drawing
{
    public class DrawGuidePatchMode : IDrawGuideState
    {
        private readonly DrawGuidePositivePatchMode _positivePatchModeBuffer;
        private readonly DrawGuideNegativePatchMode _negativePatchModeBuffer;
        private IDrawGuideState _currentDrawGuideMode;
        private readonly bool _allowNegativeDrawing;
        private readonly bool _onlyPatchMode;

        private readonly DrawGuideDataContext _dataContext;

        public DrawGuidePatchMode(ref DrawGuideDataContext dataContext, bool allowNegativeDrawing, bool onlyPatchMode, bool drawSolidSurface)
        {
            _onlyPatchMode = onlyPatchMode;
            _allowNegativeDrawing = allowNegativeDrawing;
            _positivePatchModeBuffer = new DrawGuidePositivePatchMode(ref dataContext, drawSolidSurface);
            _negativePatchModeBuffer = new DrawGuideNegativePatchMode(ref dataContext);
            _currentDrawGuideMode = _positivePatchModeBuffer;
            _dataContext = dataContext;
        }

        public void OnKeyboard(int key, DrawGuide drawGuide)
        {
            bool dummy;
            switch (key)
            {
                case (76): // L
                    if (_allowNegativeDrawing)
                    {
                        //switch between positive and negative
                        if (OnFinalize(drawGuide.LowLoDConstraintMesh, out dummy))
                        {
                            if (_currentDrawGuideMode == _positivePatchModeBuffer)
                            {
                                IDSPluginHelper.WriteLine(LogCategory.Default, "Switching to Negative Patch Drawing Mode");
                                _currentDrawGuideMode = _negativePatchModeBuffer;
                            }
                            else
                            {
                                IDSPluginHelper.WriteLine(LogCategory.Default, "Switching to Positive Patch Drawing Mode");
                                _currentDrawGuideMode = _positivePatchModeBuffer;
                            }
                        }
                        else
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Warning, "Please close the patch first.");
                        }
                    }
                    break;
                case (80): // P
                    if (!_onlyPatchMode)
                    {
                        if (OnFinalize(drawGuide.LowLoDConstraintMesh, out dummy))
                        {
                            _currentDrawGuideMode = _positivePatchModeBuffer;
                            drawGuide.SetToSkeletonDrawing();
                        }
                        else
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Warning, "Please close the patch first.");
                        }
                    }
                    break;
                case (75): //K

                    break;
                default:
                    _currentDrawGuideMode.OnKeyboard(key, drawGuide);
                    return; // nothing to do
            }
        }

        public bool OnGetPoint(Point3d point, Mesh constraintMesh, GetCurvePoints drawCurvePointsDerivation)
        {
            return _currentDrawGuideMode.OnGetPoint(point, constraintMesh, drawCurvePointsDerivation);
        }

        public void OnDynamicDraw(GetPointDrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            _currentDrawGuideMode.OnDynamicDraw(e, drawCurvePointsDerivation);            
        }

        public void OnPostDrawObjects(DrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            _currentDrawGuideMode.OnPostDrawObjects(e, drawCurvePointsDerivation);
        }

        public void OnMouseMove(GetPointMouseEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            _currentDrawGuideMode.OnMouseMove(e, drawCurvePointsDerivation);
        }

        public void OnMouseLeave(RhinoView view)
        {
            _currentDrawGuideMode.OnMouseLeave(view);
        }

        public void OnMouseEnter(RhinoView view)
        {
            _currentDrawGuideMode.OnMouseEnter(view);
        }

        public bool OnFinalize(Mesh constraintMesh, out bool isContinueLooping)
        {
            return _currentDrawGuideMode.OnFinalize(constraintMesh, out isContinueLooping);
        }

        public void OnUndo(Mesh constraintMesh)
        {
            _currentDrawGuideMode.OnUndo(constraintMesh);
        }
    }
}
