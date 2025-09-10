using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Operations
{
    public class EditImplantMarginInputGetter
    {
        private const double PointConduitDiameter = 1.0;
        private const double PointConduitTransparency = 0.0;

        private readonly CMFImplantDirector _director;
        private FullSphereConduit _currentPickedPointConduit;
        private ImplantMarginGetterDataModel _pickedEditMarginGetterDataModel;
        private List<ImplantMarginGetterDataModel> _editMarginGetterDataModels;
        private Mesh _constraintMesh;
        public EditImplantMarginInputGetter(CMFImplantDirector director)
        {
            _director = director;
        }

        private IEnumerable<RhinoObject> GetAllAffectedPlannedPartsRhObjects()
        {
            var objectManager = new CMFObjectManager(_director);

            var implantSupportOutlineObjects = objectManager.GetAllBuildingBlocks(IBB.ImplantSupportGuidingOutline);
            if (!implantSupportOutlineObjects.Any())
            {
                throw new Exception("Implant Support Guiding Outline invalid.");
            }

            var originalPlannedPartsMap = new Dictionary<Guid, RhinoObject>();
            foreach (var implantSupportOutlineObject in implantSupportOutlineObjects)
            {
                if (!ImplantSupportGuidingOutlineHelper.ExtractTouchingOriginalPartId(implantSupportOutlineObject,
                    out var touchingOriginalPartId))
                {
                    continue;
                }

                if (!originalPlannedPartsMap.ContainsKey(touchingOriginalPartId))
                {
                    var touchingOriginalPartRhObject = _director.Document.Objects.Find(touchingOriginalPartId);
                    var touchingPlannedPartRhObject = ProPlanImportUtilities.GetPlannedObjectByOriginalObject(
                        _director.Document, touchingOriginalPartRhObject) ?? touchingOriginalPartRhObject;
                    originalPlannedPartsMap.Add(touchingOriginalPartId, touchingPlannedPartRhObject);
                }
            }

            return originalPlannedPartsMap.Select(kv => kv.Value);
        }

        public Result GetInputs(out List<ImplantMarginAttribute> implantMarginAttributeList)
        {
            var helper = new ImplantMarginInputGetterHelper(_director);
            var afftectedPlannedPartsRhObjects = GetAllAffectedPlannedPartsRhObjects();
            
            helper.SetVisibleForAffectedParts(afftectedPlannedPartsRhObjects);

            var afftectedPlannedPartsMeshes = afftectedPlannedPartsRhObjects
                .Where(p => p.Geometry is Mesh).Select(p => (Mesh) p.Geometry);
            
            _constraintMesh = MeshUtilities.AppendMeshes(afftectedPlannedPartsMeshes);

            var res = EditCurves(out implantMarginAttributeList);
            return res;
        }

        private IEnumerable<ImplantMarginAttribute> GetAllEditImplantMarginAttribute()
        {
            var marginHelper = new ImplantMarginHelper(_director);

            var margins = marginHelper.GetAllMargins();
            var implantMarginAttributes = new List<ImplantMarginAttribute>();

            foreach (var margin in margins)
            {
                var implantMarginAttribute = new ImplantMarginAttribute();

                implantMarginAttribute.MarginGuid = margin.Id;
                var marginGuid = marginHelper.GetMarginCurve(margin);
                implantMarginAttribute.MarginCurve = _director.Document.Objects.Find(marginGuid);
                implantMarginAttribute.MarginTrimmedCurve = marginHelper.GetTrimmedMarginCurve(margin);

                implantMarginAttribute.PointA = implantMarginAttribute.MarginTrimmedCurve.PointAtStart;
                implantMarginAttribute.PointB = implantMarginAttribute.MarginTrimmedCurve.PointAtEnd;

                implantMarginAttribute.MarginThickness = marginHelper.GetMarginThickness(margin);
                var originalPartGuid = marginHelper.GetOriginalPartBelongTo(margin);
                implantMarginAttribute.OriginalPart = _director.Document.Objects.Find(originalPartGuid);

                implantMarginAttributes.Add(implantMarginAttribute);
            }

            return implantMarginAttributes;
        }

        private Result EditCurves(out List<ImplantMarginAttribute> implantMarginAttributeList)
        {
            implantMarginAttributeList = null;
            var marginAttributes = GetAllEditImplantMarginAttribute();
            _currentPickedPointConduit = null;
            _pickedEditMarginGetterDataModel = null;
            
            _editMarginGetterDataModels = new List<ImplantMarginGetterDataModel>();
            foreach (var marginAttribute in marginAttributes)
            {
                var attributeAndConduit = new ImplantMarginGetterDataModel();
                attributeAndConduit.MarginAttribute = marginAttribute;

                attributeAndConduit.FullOutlineConduit = new CurveConduit
                {
                    CurveColor = IDS.CMF.Visualization.Colors.ImplantMarginGuidingOutline,
                    CurveThickness = 2,
                    CurvePreview = (Curve)marginAttribute.MarginCurve.Geometry,
                    Enabled = true
                };

                attributeAndConduit.TrimmedCurveConduit = new CurveConduit
                {
                    CurveColor = IDS.CMF.Visualization.Colors.ImplantMargin,
                    CurveThickness = 2,
                    CurvePreview = marginAttribute.MarginTrimmedCurve,
                    Enabled = true,
                    DrawOnTop = true
                };

                attributeAndConduit.PointAConduit = new FullSphereConduit(marginAttribute.PointA,
                    PointConduitDiameter, PointConduitTransparency, IDS.CMF.Visualization.Colors.ImplantMargin)
                {
                    Enabled = true
                };

                attributeAndConduit.PointBConduit = new FullSphereConduit(marginAttribute.PointB,
                    PointConduitDiameter, PointConduitTransparency, IDS.CMF.Visualization.Colors.ImplantMargin)
                {
                    Enabled = true
                };

                _editMarginGetterDataModels.Add(attributeAndConduit);
            }
            _director.Document.Views.Redraw();

            Result result;
            var editPoint = new GetPoint();
            editPoint.AcceptNothing(true);
            editPoint.SetCommandPrompt("Select 2 points of implant margin to Edit, Enter to accept, or Esc to cancel changes");
            editPoint.AcceptUndo(false);
            editPoint.EnableTransparentCommands(false);
            if (_constraintMesh != null)
            {
                editPoint.Constrain(_constraintMesh, false);
            }
            editPoint.MouseDown += EditPoints_MouseDown;
            editPoint.MouseMove += EditPoints_MouseMove;

            while (true)
            {
                editPoint.ClearCommandOptions();

                var getResult = editPoint.Get(true);
                if (getResult == GetResult.Cancel)
                {
                    result = Result.Cancel;
                    break;
                }
                
                if (getResult == GetResult.Nothing)
                {
                    result = Result.Success;
                    break;
                }
                
                if (getResult == GetResult.Point)
                {
                    _currentPickedPointConduit = null;
                    _pickedEditMarginGetterDataModel = null;
                    continue;
                }

                IDSPICMFPlugIn.WriteLine(LogCategory.Warning, $"unhandled get point result: {getResult}");
            }

            editPoint.MouseDown -= EditPoints_MouseDown;
            editPoint.MouseMove -= EditPoints_MouseMove;

            if (result == Result.Success)
            {
                implantMarginAttributeList = new List<ImplantMarginAttribute>();
                foreach (var editMarginGetterDataModel in _editMarginGetterDataModels)
                {
                    if (editMarginGetterDataModel.MarginAttribute.PointA.DistanceTo(editMarginGetterDataModel.PointAConduit.Center) < DistanceParameters.Epsilon2Decimal &&
                        editMarginGetterDataModel.MarginAttribute.PointB.DistanceTo(editMarginGetterDataModel.PointBConduit.Center) < DistanceParameters.Epsilon2Decimal)
                    {
                        continue;
                    }

                    var implantMarginAttribute = new ImplantMarginAttribute
                    {
                        MarginGuid = editMarginGetterDataModel.MarginAttribute.MarginGuid,
                        MarginCurve = editMarginGetterDataModel.MarginAttribute.MarginCurve,
                        MarginTrimmedCurve = editMarginGetterDataModel.TrimmedCurveConduit.CurvePreview,
                        PointA = editMarginGetterDataModel.PointAConduit.Center,
                        PointB = editMarginGetterDataModel.PointBConduit.Center,
                        MarginThickness = editMarginGetterDataModel.MarginAttribute.MarginThickness,
                        OriginalPart = editMarginGetterDataModel.MarginAttribute.OriginalPart
                    };

                    implantMarginAttributeList.Add(implantMarginAttribute);
                }
            }

            foreach (var editMarginGetterDataModel in _editMarginGetterDataModels)
            {
                editMarginGetterDataModel.FullOutlineConduit.Enabled = false;
                editMarginGetterDataModel.TrimmedCurveConduit.Enabled = false;
                editMarginGetterDataModel.PointAConduit.Enabled = false;
                editMarginGetterDataModel.PointBConduit.Enabled = false;
            }

            _director.Document.Views.Redraw();

            return result;
        }
        private Dictionary<Point3d, ImplantMarginGetterDataModel> GetPointsPair()
        {
            var pointsPair = new Dictionary<Point3d, ImplantMarginGetterDataModel>();

            foreach (var editMarginGetterDataModel in _editMarginGetterDataModels)
            {
                pointsPair.Add(editMarginGetterDataModel.PointAConduit.Center, editMarginGetterDataModel);
                pointsPair.Add(editMarginGetterDataModel.PointBConduit.Center, editMarginGetterDataModel);
            }

            return pointsPair;
        }

        private void EditPoints_MouseDown(object sender, GetPointMouseEventArgs e)
        {
            var point2d = e.WindowPoint;
            var viewport = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            var point3d = e.Point;
            var editPointsPair = GetPointsPair();
            var pointList = editPointsPair.Keys.ToList();
            var outlines = editPointsPair.Values.Select(v => v.FullOutlineConduit.CurvePreview);
            var pickedPoint3d = PickUtilities.GetPickedPoint3dFromCurves(point2d, viewport, point3d, outlines, PointConduitDiameter, out _);

            var pointIndex = PickUtilities.GetClosestPickedPointIndex(pickedPoint3d, pointList, PointConduitDiameter);
            if (pointIndex < 0)
            {
                return;
            }

            _pickedEditMarginGetterDataModel = editPointsPair[pointList[pointIndex]];
            _currentPickedPointConduit = (pointIndex % 2 == 0) ?
                _pickedEditMarginGetterDataModel.PointAConduit
                : _pickedEditMarginGetterDataModel.PointBConduit;
        }

        private void EditPoints_MouseMove(object sender, GetPointMouseEventArgs e)
        {
            if (e.LeftButtonDown && _currentPickedPointConduit != null && _pickedEditMarginGetterDataModel != null)
            {
                var constraintsCurve = _pickedEditMarginGetterDataModel.FullOutlineConduit.CurvePreview;
                var point2d = e.WindowPoint;
                var viewport = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
                var point3d = e.Point;

                var pickedPoint3d = PickUtilities.GetPickedPoint3dFromCurves(point2d, viewport, point3d, 
                    new List<Curve>(){ constraintsCurve }, 0, out _);

                var isPickingPointA = _pickedEditMarginGetterDataModel.PointAConduit == _currentPickedPointConduit;
                var pointA = isPickingPointA ? pickedPoint3d : _pickedEditMarginGetterDataModel.PointAConduit.Center;
                var pointB = !isPickingPointA ? pickedPoint3d : _pickedEditMarginGetterDataModel.PointBConduit.Center;
                var constraintCurve = _pickedEditMarginGetterDataModel.FullOutlineConduit.CurvePreview;

                var newTrimmedCurve = ImplantMarginInputGetterHelper.TrimCurve(pointA,
                    pointB, constraintCurve);
                if (newTrimmedCurve != null &&
                    newTrimmedCurve.GetLength() > ImplantMarginConstants.MinTrimmedCurveLength)
                {
                    _pickedEditMarginGetterDataModel.TrimmedCurveConduit.CurvePreview = newTrimmedCurve;
                    _currentPickedPointConduit.Center = pickedPoint3d;
                }
            }

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }
    }
}
