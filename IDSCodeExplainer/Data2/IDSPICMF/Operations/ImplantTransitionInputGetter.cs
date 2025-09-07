using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.Operations;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using Colors = IDS.CMF.Visualization.Colors;

namespace IDS.PICMF.Operations
{
    public class ImplantTransitionInputGetter
    {
        private enum CurveDrawingMode
        {
            Cut,
            Margin,
            Bone
        }

        private readonly IImplantTransitionVisualization _visualizer;
        private readonly CurveOnOsteotomyCutGetter _curveOnOsteotomyCutGetter;
        private readonly CurveOnMarginGetter _curveOnMarginGetter;
        private readonly CurveOnBoneGetter _curveOnBoneGetter;
        private readonly ImplantTransitionCreator _transitionCreator;

        private ICurveGetter _currentCurveGetter;

        private CurveDrawingMode _drawingMode;
        private CurveDrawingMode DrawingMode
        {
            get { return _drawingMode; }
            set
            {
                _drawingMode = value;
                switch (_drawingMode)
                {
                    case CurveDrawingMode.Cut:
                        _currentCurveGetter = _curveOnOsteotomyCutGetter;
                        _visualizer.OnCutModeSelected();
                        break;
                    case CurveDrawingMode.Margin:
                        _currentCurveGetter = _curveOnMarginGetter;
                        _visualizer.OnMarginModeSelected();
                        break;
                    case CurveDrawingMode.Bone:
                        _currentCurveGetter = _curveOnBoneGetter;
                        _visualizer.OnBoneModeSelected();
                        break;
                }
            }
        }

        public ImplantTransitionInputGetter(CMFImplantDirector director, IImplantTransitionVisualization visualizer)
        {
            _visualizer = visualizer;

            _curveOnOsteotomyCutGetter = new CurveOnOsteotomyCutGetter(director);
            _curveOnMarginGetter = new CurveOnMarginGetter(director);
            _curveOnBoneGetter = new CurveOnBoneGetter(director);

            _transitionCreator = new ImplantTransitionCreator(director);

            DrawingMode = CurveDrawingMode.Margin;
        }

        public Result GetInputs(out Dictionary<ImplantTransitionDataModel, Mesh> implantTransitionDictionary)
        {
            implantTransitionDictionary = new Dictionary<ImplantTransitionDataModel, Mesh>();

            DrawingMode = CurveDrawingMode.Margin;

            var conduits = new List<CurveConduit>();
            var transitionPreviews = new List<MeshConduit>();
            var result = Result.Cancel;

            while (true)
            {
                var enterPressed = false;

                ImplantTransitionInputCurveDataModel dataModel1;
                var curve1Color = Color.Red;
                CurveConduit curve1Conduit;
                var getCurve1Result = GetInput("CURVE1", curve1Color, out dataModel1, out curve1Conduit, out enterPressed);
                if (!getCurve1Result)
                {
                    if (!enterPressed)
                    {
                        result = Result.Failure;
                    }
                    else
                    {
                        result = Result.Success;
                    }
                    break;
                }

                conduits.Add(curve1Conduit);

                ImplantTransitionInputCurveDataModel dataModel2;
                var curve2Color = Color.Brown;
                CurveConduit curve2Conduit;
                var getCurve2Result = GetInput("CURVE2", curve2Color, out dataModel2, out curve2Conduit, out enterPressed);
                if (!getCurve2Result)
                {
                    result = Result.Failure;
                    break;
                }

                conduits.Add(curve2Conduit);

                Mesh transitionMesh;
                if (!_transitionCreator.GenerateImplantTransition(dataModel1.TrimmedCurve, dataModel2.TrimmedCurve, out transitionMesh))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Error while generating transition!");
                    result = Result.Failure;
                    break;
                }

                var preview = new MeshConduit();
                preview.SetMesh(transitionMesh, Colors.ImplantTransition, 0.25);
                transitionPreviews.Add(preview);
                preview.Enabled = true;

                implantTransitionDictionary.Add(new ImplantTransitionDataModel
                {
                    CurveA = dataModel1,
                    CurveB = dataModel2
                }, transitionMesh);
            }

            foreach (var conduit in conduits)
            {
                conduit.Enabled = false;
            }

            foreach (var preview in transitionPreviews)
            {
                preview.Enabled = false;
            }

            return result;
        }

        private bool GetInput(string prependMessage, Color conduitColor, out ImplantTransitionInputCurveDataModel outputDataModel, out CurveConduit curveConduit, out bool enterPressed)
        {
            outputDataModel = null;
            enterPressed = false;

            var getPoints = new GetPoint();
            getPoints.AcceptNothing(true);
            getPoints.SetCommandPrompt($"{prependMessage}: Select 2 points and <ENTER> to finalize");
            getPoints.AcceptUndo(true);
            getPoints.EnableTransparentCommands(false);

            var modes = new[]
            {
                CurveDrawingMode.Cut,
                CurveDrawingMode.Margin,
                CurveDrawingMode.Bone
            };

            getPoints.AddOptionEnumList("Mode", DrawingMode, modes);
            getPoints.MouseMove += GetPoints_MouseMove;

            _currentCurveGetter.OnPreGetting(ref getPoints, conduitColor);

            var success = true;

            while (true)
            {
                var getResult = getPoints.Get(true);
                if (getResult == GetResult.Option)
                {
                    _currentCurveGetter.OnCancel();
                    DrawingMode = modes[getPoints.Option().CurrentListOptionIndex];
                    _currentCurveGetter.OnPreGetting(ref getPoints, conduitColor);
                }
                else if (getResult == GetResult.Cancel)
                {
                    _currentCurveGetter.OnCancel();
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Implant Support Transition Input canceled.");
                    success = false;
                    break;
                }
                else if (getResult == GetResult.Point)
                {
                    _currentCurveGetter.OnPointPicked(ref getPoints);
                }
                else if (getResult == GetResult.Undo)
                {
                    _currentCurveGetter.OnUndo(ref getPoints);
                }
                else if (getResult == GetResult.Nothing)
                {
                    success = _currentCurveGetter.OnFinalized(out outputDataModel);
                    enterPressed = true;
                    break;
                }
            }

            getPoints.MouseMove -= GetPoints_MouseMove;

            curveConduit = new CurveConduit();
            if (success)
            {
                curveConduit.CurveColor = conduitColor;
                curveConduit.CurveThickness = 2;
                curveConduit.DrawOnTop = true;
                curveConduit.CurvePreview = outputDataModel.TrimmedCurve;
                curveConduit.Enabled = true;
            }

            return success;
        }

        private void GetPoints_MouseMove(object sender, GetPointMouseEventArgs e)
        {
            _currentCurveGetter.OnMouseMove(sender, e);

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }
    }
}
