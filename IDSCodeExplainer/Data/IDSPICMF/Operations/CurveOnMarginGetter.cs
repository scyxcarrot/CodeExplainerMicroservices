using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Cursors = System.Windows.Input;
using Locking = IDS.CMF.Operations.Locking;

namespace IDS.PICMF.Operations
{
    public class CurveOnMarginGetter : ICurveGetter
    {
        private readonly CMFImplantDirector _director;

        private Dictionary<RhinoObject, Mesh> _implantMarginDictionary;
        private List<Mesh> _implantMargins;
        private MeshConduit _highlightMargin;
        private Curve _constraintCurve;
        private Guid _selectedMarginGuid;

        private TransitionCurveGetterHelper _helper;

        public CurveOnMarginGetter(CMFImplantDirector director)
        {
            _director = director;

            var objectManager = new CMFObjectManager(_director);
            var implantMarginBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantMargin).ToList();
            _implantMarginDictionary = new Dictionary<RhinoObject, Mesh>();
            _implantMargins = new List<Mesh>();
            foreach (var implantMarginBlock in implantMarginBlocks)
            {
                var mesh = (Mesh)implantMarginBlock.Geometry;
                _implantMarginDictionary.Add(implantMarginBlock, mesh);
                _implantMargins.Add(mesh);
            }

            _helper = new TransitionCurveGetterHelper();

            _selectedMarginGuid = Guid.Empty;
        }

        public void OnPreGetting(ref GetPoint getPoints, Color conduitColor)
        {
            var doc = _director.Document;

            Locking.UnlockImplantMargin(doc);

            if (_highlightMargin == null)
            {
                _highlightMargin = new MeshConduit(true);
            }

            _helper.SetPointConduits(conduitColor);
            _helper.SetTrimmedCurveConduit(conduitColor);

            _highlightMargin.Enabled = true;

            _helper.SetConstraintCurveConduits(new List<Curve> {null});

            _selectedMarginGuid = Guid.Empty;
        }

        public void OnCancel()
        {
            _helper.DisableAllConduits();
            _highlightMargin.Enabled = false;
            _selectedMarginGuid = Guid.Empty;
        }

        public void OnPointPicked(ref GetPoint getPoints)
        {
            if (_selectedMarginGuid == Guid.Empty)
            {
                var selectedMesh = PickMargin(getPoints.View().ActiveViewport, getPoints.Point2d());

                if (selectedMesh != null)
                {
                    var rhinoObj = _implantMarginDictionary.FirstOrDefault(i => i.Value == selectedMesh).Key;
                    if (rhinoObj != null)
                    {
                        var curve = GetMarginCurve(rhinoObj);
                        if (curve == null)
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Error, "Error while getting margin curve!");
                            return;
                        }

                        _selectedMarginGuid = rhinoObj.Id;

                        _helper.Point1 = curve.PointAtStart;
                        _helper.Point1Conduit.Center = _helper.Point1;
                        _helper.Point1Conduit.Enabled = true;

                        //filter constrain to only the selected curve
                        getPoints.ClearConstraints();
                        getPoints.Constrain(curve, false);

                        _helper.ConstraintCurveConduits[0].CurvePreview = curve;
                        _helper.ConstraintCurveConduits[0].Enabled = true;

                        _helper.Point2 = curve.PointAtEnd;
                        _helper.Point2Conduit.Center = _helper.Point2;
                        _helper.Point2Conduit.Enabled = true;

                        _helper.TrimmedCurveConduit.CurvePreview = curve;
                        _helper.TrimmedCurveConduit.Enabled = true;

                        _constraintCurve = curve;
                    }
                }
            }
            else if (_helper.Point1 == Point3d.Unset)
            {
                var pickedPoint = getPoints.Point();

                _helper.Point1 = pickedPoint;
                _helper.Point1Conduit.Center = _helper.Point1;
                _helper.Point1Conduit.Enabled = true;
            }
            else if (_helper.Point2 == Point3d.Unset)
            {
                var pickedPoint = getPoints.Point();
                
                if (_helper.Point1 == pickedPoint)
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Picked Point is identical!");
                    return;
                }

                var trimmedCurve = _helper.TrimCurve(_helper.Point1, pickedPoint, _constraintCurve);
                if (trimmedCurve == null)
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Unable to trim curve!");
                    return;
                }

                _helper.Point2 = pickedPoint;
                _helper.Point2Conduit.Center = _helper.Point2;
                _helper.Point2Conduit.Enabled = true;

                _helper.TrimmedCurveConduit.CurvePreview = trimmedCurve;
                _helper.TrimmedCurveConduit.Enabled = true;
            }
        }

        public void OnUndo(ref GetPoint getPoints)
        {
            if (_helper.Point2 != Point3d.Unset)
            {
                //undo point2 selection
                _helper.Point2 = Point3d.Unset;
                _helper.Point2Conduit.Center = _helper.Point2;
                _helper.Point2Conduit.Enabled = false;

                _helper.TrimmedCurveConduit.CurvePreview = null;
                _helper.TrimmedCurveConduit.Enabled = false;
            }
            else if (_helper.Point1 != Point3d.Unset)
            {
                //undo point1 selection
                _helper.Point1 = Point3d.Unset;
                _helper.Point1Conduit.Center = _helper.Point1;
                _helper.Point1Conduit.Enabled = false;
            }
            else
            {
                //clear constrains
                getPoints.ClearConstraints();
                _helper.ConstraintCurveConduits[0].Enabled = false;
                _constraintCurve = null;
                _selectedMarginGuid = Guid.Empty;
            }
        }

        public bool OnFinalized(out ImplantTransitionInputCurveDataModel outputDataModel)
        {
            outputDataModel = null;

            if (_helper.Point1 == Point3d.Unset || _helper.Point2 == Point3d.Unset)
            {
                _helper.Point1Conduit.Enabled = false;
                _helper.Point2Conduit.Enabled = false;
                _helper.TrimmedCurveConduit.Enabled = false;
                _helper.ConstraintCurveConduits[0].Enabled = false;
                _highlightMargin.Enabled = false;
                _selectedMarginGuid = Guid.Empty;
                return false;
            }

            outputDataModel = new ImplantTransitionInputCurveDataModel();
            outputDataModel.FullCurve = _constraintCurve;
            outputDataModel.TrimmedCurve = _helper.TrimCurve(_helper.Point1, _helper.Point2, outputDataModel.FullCurve);

            if (outputDataModel.TrimmedCurve == null)
            {
                _helper.Point1Conduit.Enabled = false;
                _helper.Point2Conduit.Enabled = false;
                _helper.TrimmedCurveConduit.Enabled = false;
                _helper.ConstraintCurveConduits[0].Enabled = false;
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Points picked could not generate valid curve! Please check that the points are on the same guiding outline!");
                _highlightMargin.Enabled = false;
                return false;
            }

            outputDataModel.DerivedObjectGuid = _selectedMarginGuid;

            _helper.Point1Conduit.Enabled = false;
            _helper.Point2Conduit.Enabled = false;
            _helper.TrimmedCurveConduit.Enabled = false;
            _helper.ConstraintCurveConduits[0].Enabled = false;
            _highlightMargin.Enabled = false;
            _selectedMarginGuid = Guid.Empty;

            return true;
        }

        public void OnMouseMove(object sender, GetPointMouseEventArgs e)
        {
            if (!e.LeftButtonDown && _selectedMarginGuid == Guid.Empty)
            {
                Cursors.Mouse.SetCursor(Cursors.Cursors.Pen);

                var selectedMesh = PickMargin(e.Source.View().ActiveViewport, e.WindowPoint);

                //highlight margin
                if (selectedMesh != null)
                {
                    _highlightMargin.SetMesh(selectedMesh, Color.Yellow, 0.0);
                }
                else
                {
                    _highlightMargin.ResetMesh();
                }

            }
            else
            {
                _highlightMargin.ResetMesh();
            }

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }

        private Mesh PickMargin(RhinoViewport viewPort, System.Drawing.Point point)
        {
            var picker = new PickContext();
            picker.View = viewPort.ParentView;
            picker.PickStyle = PickStyle.PointPick;
            var xform = viewPort.GetPickTransform(point);
            picker.SetPickTransform(xform);

            double depth = 0;
            Mesh selectedMesh = null;
            var refDepth = depth;

            foreach (var mesh in _implantMargins)
            {
                double distance;
                Point3d hitPoint;
                PickContext.MeshHitFlag hitFlag;
                int hitIndex;
                if (!picker.PickFrustumTest(mesh, PickContext.MeshPickStyle.ShadedModePicking, out hitPoint,
                        out depth, out distance, out hitFlag, out hitIndex) ||
                    !(Math.Abs(distance) < double.Epsilon) ||
                    selectedMesh != null && !(refDepth < depth))
                {
                    continue;
                }
                //depth returned here for point picks LARGER values are NEARER to the camera. SMALLER values are FARTHER from the camera.
                selectedMesh = mesh;
                refDepth = depth;
            }

            return selectedMesh;
        }

        private Curve GetMarginCurve(RhinoObject implantMarginObject)
        {
            try
            {
                var marginHelper = new ImplantMarginHelper(_director);
                var margin = implantMarginObject;
                return marginHelper.GetOffsettedMarginCurve(margin); ;
            }
            catch
            {
                return null;
            }
        }
    }
}
