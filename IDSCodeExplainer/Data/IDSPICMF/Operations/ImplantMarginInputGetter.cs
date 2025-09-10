using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.Utilities;
using IDS.PICMF.Helper;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Operations
{
    public class ImplantMarginInputGetter
    {
        private const double PointConduitDiameter = 1.0;
        private const double PointConduitTransparency = 0.0;

        private readonly CMFImplantDirector _director;
        private readonly List<ImplantMarginInputGetterDataModel> _implantMarginInputGetterDataModelList;
        private readonly List<ImplantMarginGetterDataModel> _addMarginGetterDataModels;
        private readonly List<ImplantMarginGetterDataModel> _existingMarginGetterDataModels;

        public Mesh OsteotomyParts { get; }
        public double MarginThickness { get; private set; }

        public ImplantMarginInputGetter(CMFImplantDirector director)
        {
            _director = director;

            var objectManager = new CMFObjectManager(director);
            var osteotomyParts = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(_director.Document);
            OsteotomyParts = MeshUtilities.AppendMeshes(osteotomyParts.Select(mesh => mesh.DuplicateMesh()));
            if (OsteotomyParts == null)
            {
                throw new Exception("Osteotomy part invalid.");
            }

            var implantSupportOutlineObjects = objectManager.GetAllBuildingBlocks(IBB.ImplantSupportGuidingOutline);
            if (!implantSupportOutlineObjects.Any())
            {
                throw new Exception("Implant Support Guiding Outline invalid.");
            }

            _implantMarginInputGetterDataModelList = GetImplantMarginInputGetterDataModelList(implantSupportOutlineObjects);
            _addMarginGetterDataModels = new List<ImplantMarginGetterDataModel>();
            _existingMarginGetterDataModels = new List<ImplantMarginGetterDataModel>();
            SetupExistingMarginConduits();
        }

        private List<ImplantMarginInputGetterDataModel> GetImplantMarginInputGetterDataModelList(IEnumerable<RhinoObject> outlines)
        {
            var implantMarginInputGetterDataModelList = new List<ImplantMarginInputGetterDataModel>();
            
            foreach (var outline in outlines)
            {
                if (!ImplantSupportGuidingOutlineHelper.ExtractTouchingOriginalPartId(outline,
                    out var touchingOriginalPartId))
                {
                    continue;
                }

                var implantMarginInputGetterDataModel = implantMarginInputGetterDataModelList
                    .FirstOrDefault(i => i.OriginalPartRhObject.Id == touchingOriginalPartId);

                if (implantMarginInputGetterDataModel == null) 
                {
                    var touchingOriginalPartRhObject = _director.Document.Objects.Find(touchingOriginalPartId);
                    var touchingPlannedPartRhObject = ProPlanImportUtilities.GetPlannedObjectByOriginalObject(
                        _director.Document, touchingOriginalPartRhObject);

                    implantMarginInputGetterDataModel = new ImplantMarginInputGetterDataModel()
                    {
                        PlannedPartRhObject = touchingPlannedPartRhObject?? touchingOriginalPartRhObject,
                        OriginalPartRhObject = touchingOriginalPartRhObject,
                        OutlinesRhObject = new List<RhinoObject>()
                    };
                    implantMarginInputGetterDataModelList.Add(implantMarginInputGetterDataModel);
                }

                implantMarginInputGetterDataModel.OutlinesRhObject.Add(outline);
            }

            return implantMarginInputGetterDataModelList;
        }

        private void UnlockAndShowPlannedParts()
        {
            var helper = new ImplantMarginInputGetterHelper(_director);
            var affectedPlannedPartsRhObjects = _implantMarginInputGetterDataModelList
                .Select(i => i.PlannedPartRhObject).ToList();

            helper.SetVisibleForAffectedParts(affectedPlannedPartsRhObjects);
            
            foreach (var affectedPlannedObject in affectedPlannedPartsRhObjects)
            {
                _director.Document.Objects.Unlock(affectedPlannedObject, true);
            }
        }

        public Result GetInputs(out IEnumerable<ImplantMarginAttribute> implantMarginAttributes)
        {
            MarginThickness = ImplantMarginParameters.DefaultThickness;
            implantMarginAttributes = null;
            
            UnlockAndShowPlannedParts();

            var selectPlaceablePlannedPart = new GetObject();
            selectPlaceablePlannedPart.SetCommandPrompt("<ENTER> to finalize or select the planned bone to create margin");
            selectPlaceablePlannedPart.EnablePreSelect(false, false);
            selectPlaceablePlannedPart.EnablePostSelect(true);
            selectPlaceablePlannedPart.AcceptNothing(true);
            selectPlaceablePlannedPart.EnableTransparentCommands(false);
            selectPlaceablePlannedPart.EnableHighlight(false);
            selectPlaceablePlannedPart.AcceptUndo(true);

            _existingMarginGetterDataModels.ForEach(d =>
            {
                d.PointAConduit.Enabled = true;
                d.PointBConduit.Enabled = true;
                d.TrimmedCurveConduit.Enabled = true;
            });

            while (true)
            {
                selectPlaceablePlannedPart.ClearCommandOptions();
                var optionThickness = new OptionToggle(MarginThickness == ImplantMarginParameters.MaxThickness, "0.5", "1");
                var thicknessOptionIndex = selectPlaceablePlannedPart.AddOptionToggle("MarginThickness", ref optionThickness);
                _director.Document.Views.Redraw();

                var res = selectPlaceablePlannedPart.Get();

                if (res == GetResult.Cancel)
                {
                    ResetGetterDataModels();
                    return Result.Cancel;
                }

                if (res == GetResult.Nothing)
                {
                    if (!_addMarginGetterDataModels.Any())
                    {
                        ResetGetterDataModels();
                        return Result.Cancel;
                    }

                    implantMarginAttributes = _addMarginGetterDataModels.Select(a => a.MarginAttribute).ToList();
                    ResetGetterDataModels();
                    return Result.Success;
                }

                if (res == GetResult.Option)
                {
                    if (selectPlaceablePlannedPart.OptionIndex() == thicknessOptionIndex)
                    {
                        MarginThickness = optionThickness.CurrentValue
                            ? ImplantMarginParameters.MaxThickness
                            : ImplantMarginParameters.MinThickness;
                    }
                    continue;
                }

                if (res == GetResult.Undo)
                {
                    if (_addMarginGetterDataModels.Any())
                    {
                        var lastAddMarginGetterDataModel = _addMarginGetterDataModels.Last();
                        lastAddMarginGetterDataModel.PointAConduit.Enabled = false;
                        lastAddMarginGetterDataModel.PointBConduit.Enabled = false;
                        lastAddMarginGetterDataModel.TrimmedCurveConduit.Enabled = false;
                        _addMarginGetterDataModels.Remove(lastAddMarginGetterDataModel);
                    }
                    continue;
                }

                if (res != GetResult.Object)
                {
                    continue;
                }

                var selectedRhinoObject = selectPlaceablePlannedPart.Object(0);
                var implantMarginInputGetterDataModel = _implantMarginInputGetterDataModelList
                    .FirstOrDefault(i => i.PlannedPartRhObject.Id == selectedRhinoObject.ObjectId);

                var addMarginGetterDataModelsBatch = GetCurve(
                    implantMarginInputGetterDataModel.OutlinesRhObject, implantMarginInputGetterDataModel);

                _addMarginGetterDataModels.AddRange(addMarginGetterDataModelsBatch);
            }
        }

        private IEnumerable<ImplantMarginGetterDataModel> GetCurve(IEnumerable<RhinoObject> outlineObjects, ImplantMarginInputGetterDataModel inputGetterDataModel)
        {
            var cancelled = false;
            var getPoints = new GetPoint();
            getPoints.AcceptNothing(true);
            getPoints.SetCommandPrompt("Select multiple 2 points on the implant support guiding outline and <ENTER> to finalize");
            getPoints.AcceptUndo(true);

            var constraintMesh = (Mesh)inputGetterDataModel.PlannedPartRhObject.Geometry.Duplicate();

            // Include osteotomies plane so that user can point at upward or downward view
            var helper = new ImplantMarginInputGetterHelper(_director);
            var transform = helper.GetMarginTransform(inputGetterDataModel.OriginalPartRhObject);
            var osteotomyPartsCopy = OsteotomyParts.DuplicateMesh();
            if (osteotomyPartsCopy.Transform(transform))
            {
                constraintMesh.Append(osteotomyPartsCopy);
            }
            getPoints.Constrain(constraintMesh, false);
            
            var addMarginGetterDataModels = new List<ImplantMarginGetterDataModel>();
            ImplantMarginGetterDataModel curAddMarginGetterDataModel = null;

            var outlineObjectsMap = outlineObjects.ToDictionary(
                m => (Curve) m.Geometry, m => m);
            var constraintCurves = outlineObjectsMap.Keys.ToList();
            var constraintCurveConduits = constraintCurves.Select(curve =>
                new CurveConduit
                {
                    CurveColor = IDS.CMF.Visualization.Colors.ImplantMarginGuidingOutline, 
                    CurveThickness = 2, 
                    CurvePreview = curve, 
                    Enabled = true
                }).ToList();

            while (true)
            {
                try
                {
                    getPoints.ClearCommandOptions();
                    var optionThickness = new OptionToggle(MarginThickness == ImplantMarginParameters.MaxThickness,
                        "0.5", "1");
                    var thicknessOptionIndex = getPoints.AddOptionToggle("MarginThickness", ref optionThickness);
                    _director.Document.Views.Redraw();

                    var getResult = getPoints.Get();

                    if (getResult == GetResult.Option)
                    {
                        if (getPoints.OptionIndex() == thicknessOptionIndex)
                        {
                            MarginThickness = optionThickness.CurrentValue
                                ? ImplantMarginParameters.MaxThickness
                                : ImplantMarginParameters.MinThickness;
                        }

                        continue;
                    }

                    if (getResult == GetResult.Undo)
                    {
                        if (curAddMarginGetterDataModel == null) // Point B
                        {
                            if (addMarginGetterDataModels.Any())
                            {
                                curAddMarginGetterDataModel = addMarginGetterDataModels.Last();
                                addMarginGetterDataModels.Remove(curAddMarginGetterDataModel);

                                curAddMarginGetterDataModel.PointBConduit.Enabled = false;
                                curAddMarginGetterDataModel.PointBConduit = null;

                                curAddMarginGetterDataModel.TrimmedCurveConduit.Enabled = false;
                                curAddMarginGetterDataModel.TrimmedCurveConduit = null;

                                curAddMarginGetterDataModel.FullOutlineConduit.Enabled = true;

                                constraintCurveConduits.ForEach(c => c.Enabled = false);
                            }
                        }
                        else // Point A
                        {
                            curAddMarginGetterDataModel.PointAConduit.Enabled = false;
                            curAddMarginGetterDataModel.PointAConduit = null;

                            curAddMarginGetterDataModel.FullOutlineConduit.Enabled = false;
                            curAddMarginGetterDataModel.FullOutlineConduit = null;

                            curAddMarginGetterDataModel = null;

                            constraintCurveConduits.ForEach(c => c.Enabled = true);
                        }

                        continue;
                    }

                    if (getResult == GetResult.Point)
                    {
                        if (curAddMarginGetterDataModel == null) // Point A
                        {
                            var pickedPointOnCurve = PickUtilities.GetPickedPoint3dFromCurves(getPoints.Point2d(),
                                getPoints.View().ActiveViewport, getPoints.Point(),
                                constraintCurves, double.MaxValue, out var pickedCurve);

                            curAddMarginGetterDataModel = new ImplantMarginGetterDataModel()
                            {
                                MarginAttribute = new ImplantMarginAttribute()
                                {
                                    MarginCurve = outlineObjectsMap[pickedCurve],
                                    PointA = pickedPointOnCurve,
                                    OriginalPart = inputGetterDataModel.OriginalPartRhObject
                                },
                                PointAConduit = new FullSphereConduit(pickedPointOnCurve, PointConduitDiameter,
                                    PointConduitTransparency,
                                    IDS.CMF.Visualization.Colors.ImplantMargin)
                                {
                                    Enabled = true
                                },
                                FullOutlineConduit = new CurveConduit
                                {
                                    CurveColor = IDS.CMF.Visualization.Colors.ImplantMarginGuidingOutline,
                                    CurveThickness = 2,
                                    CurvePreview = pickedCurve,
                                    Enabled = true
                                }
                            };

                            constraintCurveConduits.ForEach(c => c.Enabled = false);
                        }
                        else // Point B
                        {
                            var pickedPointOnCurve = PickUtilities.GetPickedPoint3dFromCurves(getPoints.Point2d(),
                                getPoints.View().ActiveViewport, getPoints.Point(),
                                new List<Curve>() { curAddMarginGetterDataModel.FullOutlineConduit.CurvePreview },
                                double.MaxValue, out var pickedCurve);

                            var trimmedCurve = ImplantMarginInputGetterHelper.TrimCurve(
                                curAddMarginGetterDataModel.MarginAttribute.PointA,
                                pickedPointOnCurve,
                                (Curve)curAddMarginGetterDataModel.MarginAttribute.MarginCurve.Geometry);


                            if (trimmedCurve == null ||
                                trimmedCurve.GetLength() <= ImplantMarginConstants.MinTrimmedCurveLength)
                            {
                                Dialogs.ShowMessage(
                                    $"The curve is lesser than or equals to {ImplantMarginConstants.MinTrimmedCurveLength}mm, please choose another point.",
                                    "Warning", ShowMessageButton.OK, ShowMessageIcon.Exclamation);
                                continue;
                            }

                            curAddMarginGetterDataModel.MarginAttribute.PointB = pickedPointOnCurve;
                            curAddMarginGetterDataModel.PointBConduit = new FullSphereConduit(pickedPointOnCurve,
                                PointConduitDiameter, PointConduitTransparency,
                                IDS.CMF.Visualization.Colors.ImplantMargin)
                            {
                                Enabled = true
                            };

                            curAddMarginGetterDataModel.TrimmedCurveConduit = new CurveConduit
                            {
                                CurveColor = IDS.CMF.Visualization.Colors.ImplantMargin,
                                CurveThickness = 2,
                                DrawOnTop = true,
                                CurvePreview = trimmedCurve,
                                Enabled = true
                            };
                            curAddMarginGetterDataModel.MarginAttribute.MarginTrimmedCurve = trimmedCurve;
                            curAddMarginGetterDataModel.FullOutlineConduit.Enabled = false;

                            addMarginGetterDataModels.Add(curAddMarginGetterDataModel);
                            curAddMarginGetterDataModel = null;
                            constraintCurveConduits.ForEach(c => c.Enabled = true);
                        }

                        continue;
                    }

                    if (getResult == GetResult.Cancel)
                    {
                        IDSPICMFPlugIn.WriteLine(LogCategory.Default, "Implant Margin Input canceled.");
                        cancelled = true;
                        break;
                    }

                    if (getResult == GetResult.Nothing)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, $"Handled Exception: {ex.Message}\n{ex.StackTrace}\n");
                }
            }


            if (curAddMarginGetterDataModel != null)
            {
                curAddMarginGetterDataModel.PointAConduit.Enabled = false;
                curAddMarginGetterDataModel.FullOutlineConduit.Enabled = false;
            }

            foreach (var implantMarginGetterDataModel in addMarginGetterDataModels)
            {
                if (cancelled)
                {
                    implantMarginGetterDataModel.PointAConduit.Enabled = false;
                    implantMarginGetterDataModel.PointBConduit.Enabled = false;
                    implantMarginGetterDataModel.TrimmedCurveConduit.Enabled = false;
                }
                implantMarginGetterDataModel.FullOutlineConduit.Enabled = false;
            }
            constraintCurveConduits.ForEach(c => c.Enabled = false);

            return addMarginGetterDataModels;
        }

        private void SetupExistingMarginConduits()
        {
            var marginHelper = new ImplantMarginHelper(_director);
            var margins = marginHelper.GetAllMargins();
            foreach (var margin in margins)
            {
                var trimmedMarginCurve = marginHelper.GetTrimmedMarginCurve(margin);

                var existingImplantMarginDataModel = new ImplantMarginGetterDataModel();

                existingImplantMarginDataModel.TrimmedCurveConduit = new CurveConduit
                {
                    CurveColor = IDS.CMF.Visualization.Colors.ImplantMargin,
                    CurveThickness = 2,
                    CurvePreview = trimmedMarginCurve,
                    DrawOnTop = true
                };

                var transparency = 0.5;
                existingImplantMarginDataModel.PointAConduit = new FullSphereConduit(trimmedMarginCurve.PointAtStart,
                    PointConduitDiameter, transparency, IDS.CMF.Visualization.Colors.ImplantMargin);

                existingImplantMarginDataModel.PointBConduit = new FullSphereConduit(trimmedMarginCurve.PointAtEnd,
                    PointConduitDiameter, transparency, IDS.CMF.Visualization.Colors.ImplantMargin);

                _existingMarginGetterDataModels.Add(existingImplantMarginDataModel);
            }
        }

        private void ResetGetterDataModels()
        {
            _addMarginGetterDataModels.ForEach(d =>
            {
                d.PointAConduit.Enabled = false;
                d.PointBConduit.Enabled = false;
                d.TrimmedCurveConduit.Enabled = false;
            });
            _addMarginGetterDataModels.Clear();

            _existingMarginGetterDataModels.ForEach(d =>
            {
                d.PointAConduit.Enabled = false;
                d.PointBConduit.Enabled = false;
                d.TrimmedCurveConduit.Enabled = false;
            });
            _existingMarginGetterDataModels.Clear();
        }
    }
}
