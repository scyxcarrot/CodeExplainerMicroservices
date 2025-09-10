using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.Query;
using IDS.Core.Enumerators;
using IDS.Core.Visualization;
using IDS.PICMF;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Operations
{
    public class CurveOnBoneGetter : ICurveGetter
    {
        private readonly CMFImplantDirector _director;

        private TransitionCurveGetterHelper _helper;

        private List<string> _placeableBoneLayers;
        private List<RhinoObject> _placeableBoneObjects;
        private Mesh _lowLodConstraintMesh;
        private RhinoObject _selectedRhinoObject;

        public CurveOnBoneGetter(CMFImplantDirector director)
        {
            _director = director;

            var objectManager = new CMFObjectManager(_director);
            var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
            _placeableBoneObjects = constraintMeshQuery.GetConstraintRhinoObjectForImplant().ToList();

            var doc = _director.Document;
            _placeableBoneLayers = new List<string>();

            foreach (var bone in _placeableBoneObjects)
            {
                var l = bone.Attributes.LayerIndex;
                var layer = doc.Layers[l];
                _placeableBoneLayers.Add(layer.FullPath);
            }

            _helper = new TransitionCurveGetterHelper();
        }

        public void OnPreGetting(ref GetPoint getPoints, Color conduitColor)
        {
            _helper.SetPointConduits(conduitColor);
            _helper.SetTrimmedCurveConduit(conduitColor);

            //show all implant placable bones
            var doc = _director.Document;
            Visibility.SetVisible(doc, _placeableBoneLayers, true, false, true);
        }

        public void OnCancel()
        {
            _helper.DisableAllConduits();
        }

        public void OnPointPicked(ref GetPoint getPoints)
        {
            var pickedPoint = getPoints.Point();

            if (_helper.Point1 == Point3d.Unset)
            {
                getPoints.ClearConstraints();
                _selectedRhinoObject = GetConstraintMesh(getPoints, out pickedPoint);
                if (_selectedRhinoObject == null)
                {
                    return;
                }

                Mesh lowLoDConstraintMesh;
                var objectManager = new CMFObjectManager(_director);
                objectManager.GetBuildingBlockLoDLow(_selectedRhinoObject.Id, out lowLoDConstraintMesh);

                _lowLodConstraintMesh = lowLoDConstraintMesh;
                if (_lowLodConstraintMesh == null)
                {
                    return;
                }

                getPoints.Constrain(_lowLodConstraintMesh, false);

                _helper.Point1 = pickedPoint;
                _helper.Point1Conduit.Center = _helper.Point1;
                _helper.Point1Conduit.Enabled = true;
            }
            else if (_helper.Point2 == Point3d.Unset)
            {
                if (_helper.Point1 == pickedPoint)
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Picked Point is identical!");
                    return;
                }

                _helper.Point2 = pickedPoint;
                _helper.Point2Conduit.Center = _helper.Point2;
                _helper.Point2Conduit.Enabled = true;

                var curve = new PolylineCurve(new[] { _helper.Point1, _helper.Point2 });
                _helper.TrimmedCurveConduit.CurvePreview = curve;
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
            }
            else if (_helper.Point1 != Point3d.Unset)
            {
                //undo point1 selection
                _helper.Point1 = Point3d.Unset;
                _helper.Point1Conduit.Center = _helper.Point1;
                _helper.Point1Conduit.Enabled = false;

                getPoints.ClearConstraints();
                _lowLodConstraintMesh = null;
            }

            _helper.TrimmedCurveConduit.CurvePreview = null;
            _helper.TrimmedCurveConduit.Enabled = false;
        }

        public bool OnFinalized(out ImplantTransitionInputCurveDataModel outputDataModel)
        {
            outputDataModel = null;

            if (_helper.Point1 == Point3d.Unset || _helper.Point2 == Point3d.Unset)
            {
                _helper.Point1Conduit.Enabled = false;
                _helper.Point2Conduit.Enabled = false;
                _helper.TrimmedCurveConduit.Enabled = false;
                return false;
            }

            outputDataModel = new ImplantTransitionInputCurveDataModel();

            //full and trimmed curve is same curve
            var curve = new PolylineCurve(new[] { _helper.Point1, _helper.Point2 });
            var constraintMesh = (Mesh) _selectedRhinoObject.Geometry;
            var pulledCurve = curve.PullToMesh(constraintMesh, 0.1);
            outputDataModel.FullCurve = pulledCurve;
            outputDataModel.TrimmedCurve = pulledCurve;

            if (outputDataModel.TrimmedCurve == null)
            {
                _helper.Point1Conduit.Enabled = false;
                _helper.Point2Conduit.Enabled = false;
                _helper.TrimmedCurveConduit.Enabled = false;
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Points picked could not generate valid curve! Please check that the points are on the same guiding outline!");
                return false;
            }

            outputDataModel.DerivedObjectGuid = _selectedRhinoObject.Id;

            _helper.Point1Conduit.Enabled = false;
            _helper.Point2Conduit.Enabled = false;
            _helper.TrimmedCurveConduit.Enabled = false;

            _helper.Point1 = Point3d.Unset;
            _helper.Point2 = Point3d.Unset;

            return true;
        }

        public void OnMouseMove(object sender, GetPointMouseEventArgs e)
        {
            if (!e.LeftButtonDown && _helper.Point1 != Point3d.Unset && _helper.Point2 == Point3d.Unset)
            {
                //draw line that follows cursor
                var cursorPoint = e.Point;
                var curve = new PolylineCurve(new[] { _helper.Point1, cursorPoint });
                _helper.TrimmedCurveConduit.CurvePreview = curve;
                _helper.TrimmedCurveConduit.Enabled = true;
            }
        }

        private RhinoObject GetConstraintMesh(GetPoint getPoint, out Point3d point)
        {
            point = Point3d.Unset;

            var pickedPoint2d = getPoint.Point2d();
            var viewport = getPoint.View().ActiveViewport;
            var picker = new PickContext();
            picker.View = viewport.ParentView;
            picker.PickStyle = PickStyle.PointPick;
            var xform = viewport.GetPickTransform(pickedPoint2d);
            picker.SetPickTransform(xform);

            RhinoObject selectedRhinoObj = null;
            var refDepth = double.MinValue;

            foreach (var boneObject in _placeableBoneObjects)
            {
                if (!boneObject.Visible)
                {
                    continue;
                }

                double distance;
                Point3d hitPoint;
                PickContext.MeshHitFlag hitFlag;
                int hitIndex;
                double depth;

                var mesh = (Mesh) boneObject.Geometry;
                if (!picker.PickFrustumTest(mesh, PickContext.MeshPickStyle.ShadedModePicking, out hitPoint,
                        out depth, out distance, out hitFlag, out hitIndex) ||
                    !(Math.Abs(distance) < double.Epsilon) ||
                    selectedRhinoObj != null && !(refDepth < depth))
                {
                    continue;
                }
                
                selectedRhinoObj = boneObject;
                refDepth = depth;
                point = hitPoint;
            }

            return selectedRhinoObj;
        }
    }
}
