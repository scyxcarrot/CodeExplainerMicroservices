using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.MTLS.Operation;
using IDS.RhinoInterface.Converter;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    //TODO: Do not add anything into document!
    public static class GuideCreatorHelper 
    {
        public class CreateGuideParameters : IDisposable
        {
            private bool _barrelsAndItsEntitiesInitialized;

            public CreateGuideParameters()
            {
                _barrelsAndItsEntitiesInitialized = false;
            }

            private Mesh _guideSupport;
            public Mesh GuideSupport {
                get
                {
                    if (_guideSupport == null)
                    {
                        _guideSupport = (Mesh)_objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry.Duplicate();
                    }

                    return _guideSupport;
                }
            }

            private Mesh _guideSurfaceWrap;
            public Mesh GuideSurfaceWrap
            {
                get
                {
                    if (_guideSurfaceWrap == null)
                    {
                        _guideSurfaceWrap = (Mesh)_objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap).Geometry.Duplicate();
                    }

                    return _guideSurfaceWrap;
                }
            }

            private Mesh _guidePreviewMesh;
            public Mesh GuidePreviewMesh
            {
                get
                {
                    if (_guidePreviewMesh != null)
                    {
                        return _guidePreviewMesh;
                    }

                    _guidePreviewMesh = TryGetExtendedIbbMesh(IBB.GuidePreviewSmoothen);
                    return _guidePreviewMesh;
                }
            }

            private List<Mesh> _guideSurfaces = new List<Mesh>();
            public List<Mesh> GuideSurfaces
            {
                get
                {
                    if (_guideSurfaces.Any())
                    {
                        return _guideSurfaces;
                    }

                    _guideSurfaces = TryGetExtendedIbbMeshes(IBB.GuideSurface);
                    return _guideSurfaces;
                }
            }

            private List<Mesh> _guideSurfacesSmoothed = new List<Mesh>();
            public List<Mesh> GuideSurfacesSmoothed
            {
                get
                {
                    if (_guideSurfacesSmoothed == null)
                    {
                        _guideSurfacesSmoothed = new List<Mesh>();
                    }

                    if (_guideSurfacesSmoothed.Any())
                    {
                        return _guideSurfacesSmoothed;
                    }

                    _guideSurfacesSmoothed = GenerateSmoothGuideSurfaces(_director, _dataModel);
                    return _guideSurfacesSmoothed;
                }
            }

            private List<Screw> _guideScrews = new List<Screw>();
            public List<Screw> GuideScrews
            {
                get
                {
                    if (_guideScrews.Any())
                    {
                        return _guideScrews;
                    }

                    var guideComponent = new GuideCaseComponent();
                    var guideScrewEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, _dataModel);
                    _guideScrews = _objectManager.GetAllBuildingBlocks(guideScrewEibb).Select(x => (Screw)x).ToList();

                    return _guideScrews;
                }
            }

            private List<Mesh> _linkSurfaces = new List<Mesh>();
            public List<Mesh> LinkSurfaces
            {
                get
                {
                    if (_linkSurfaces.Any())
                    {
                        return _linkSurfaces;
                    }

                    _linkSurfaces = TryGetExtendedIbbMeshes(IBB.GuideLinkSurface);
                    return _linkSurfaces;
                }
            }

            private List<Mesh> _solidSurfaces = new List<Mesh>();
            public List<Mesh> SolidSurfaces
            {
                get
                {
                    if (_solidSurfaces.Any())
                    {
                        return _solidSurfaces;
                    }

                    _solidSurfaces = TryGetExtendedIbbMeshes(IBB.GuideSolidSurface);
                    return _solidSurfaces;
                }
            }

            private Mesh _osteotomyMesh;
            public Mesh OsteotomyMesh
            {
                get
                {
                    if (_osteotomyMesh != null)
                    {
                        return _osteotomyMesh;
                    }

                    var osteotomies = ProPlanImportUtilities.
                        GetAllOriginalOsteotomyPartsRhinoObjects(_director.Document).
                        Select(o => (Mesh)o.Geometry).ToList();

                    Mesh osteotomyMesh;
                    if (osteotomies.Any() && Booleans.PerformBooleanUnion(out osteotomyMesh, osteotomies.ToArray()))
                    {
                        _osteotomyMesh = osteotomyMesh;
                    }

                    return _osteotomyMesh;
                }
            }

            private List<KeyValuePair<Brep, Plane>> _bridges = new List<KeyValuePair<Brep, Plane>>();
            public List<KeyValuePair<Brep, Plane>> Bridges
            {
                get
                {
                    if (_bridges.Any())
                    {
                        return _bridges;
                    }

                    var guideComponent = new GuideCaseComponent();
                    var guideBridgeEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideBridge, _dataModel);
                    var guideBridgeRhObjs = _objectManager.GetAllBuildingBlocks(guideBridgeEibb).ToList();

                    _bridges = new List<KeyValuePair<Brep, Plane>>();
                    guideBridgeRhObjs.ForEach(x =>
                    {
                        Plane cs;
                        _objectManager.GetBuildingBlockCoordinateSystem(x.Id, out cs);
                        var val = new KeyValuePair<Brep, Plane>((Brep)x.Geometry, cs);
                        _bridges.Add(val);
                    });

                    return _bridges;
                }
            }

            private List<Mesh> _teethBlocks = new List<Mesh>();
            public List<Mesh> TeethBlocks
            {
                get
                {
                    if (_teethBlocks.Any())
                    {
                        return _teethBlocks;
                    }

                    var guideCaseComponent = new GuideCaseComponent();
                    var teethBuildingBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.TeethBlock, (_dataModel));
                    var teethBlockObjects = _objectManager.GetAllBuildingBlocks(teethBuildingBlock);
                    _teethBlocks = teethBlockObjects.Select(
                        teethBlockObject => (Mesh)teethBlockObject.Geometry).ToList();

                    return _teethBlocks;
                }
            }

            private List<Mesh> _flanges = new List<Mesh>();
            public List<Mesh> Flanges
            {
                get
                {
                    if (_flanges.Any())
                    {
                        return _flanges;
                    }

                    _flanges = new List<Mesh>();

                    var guideComponent = new GuideCaseComponent();
                    var guideFlangesEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideFlange, _dataModel);
                    _flanges = _objectManager.GetAllBuildingBlocks(guideFlangesEibb).Select(o => (Mesh)o.Geometry).ToList();
                    return _flanges;
                }
            }

            private readonly List<Mesh> _barrelsShape = new List<Mesh>();
            public List<Mesh> BarrelsShape
            {
                get
                {
                    if (_barrelsAndItsEntitiesInitialized)
                    {
                        return _barrelsShape;
                    }
                    InitializeBarrelsAndItsEntities();
                    return _barrelsShape;
                }
            }

            private readonly List<Mesh> _barrelsSubtractors = new List<Mesh>();
            public List<Mesh> BarrelsSubtractors
            {
                get
                {
                    if (_barrelsAndItsEntitiesInitialized)
                    {
                        return _barrelsSubtractors;
                    }
                    InitializeBarrelsAndItsEntities();
                    return _barrelsSubtractors;
                }
            }

            private readonly List<Brep> _barrels = new List<Brep>();
            public List<Brep> Barrels
            {
                get
                {
                    if (_barrelsAndItsEntitiesInitialized)
                    {
                        return _barrels;
                    }
                    InitializeBarrelsAndItsEntities();
                    return _barrels;
                }
            }

            private readonly Dictionary<Guid, Mesh> _unprocessedBarrelsShape = new Dictionary<Guid, Mesh>();
            public Dictionary<Guid, Mesh> UnprocessedBarrelsShape
            {
                get
                {
                    if (_barrelsAndItsEntitiesInitialized)
                    {
                        return _unprocessedBarrelsShape;
                    }
                    InitializeBarrelsAndItsEntities();
                    return _unprocessedBarrelsShape;
                }
            }

            private readonly Dictionary<Guid, Mesh> _unprocessedBarrelsSubtractors = new Dictionary<Guid, Mesh>();
            public Dictionary<Guid, Mesh> UnprocessedBarrelsSubtractors
            {
                get
                {
                    if (_barrelsAndItsEntitiesInitialized)
                    {
                        return _unprocessedBarrelsSubtractors;
                    }
                    InitializeBarrelsAndItsEntities();
                    return _unprocessedBarrelsSubtractors;
                }
            }

            private Mesh _guideSurfaceWrapRoI;
            public Mesh GuideSurfaceWrapRoI
            {
                get
                {
                    if (_guideSurfaceWrapRoI != null)
                    {
                        return _guideSurfaceWrapRoI;
                    }

                    var appendedGuideSurfaces = MeshUtilities.AppendMeshes(GuideSurfaces);
                    _guideSurfaceWrapRoI = GuideDrawingUtilities.CreateRoiMesh(GuideSurfaceWrap, appendedGuideSurfaces);
                    return _guideSurfaceWrapRoI;
                }
            }

            private Mesh _guideSupportRoI;
            public Mesh GuideSupportRoI
            {
                get
                {
                    if (_guideSupportRoI != null)
                    {
                        return _guideSupportRoI;
                    }

                    var appendedGuideSurfaces = MeshUtilities.AppendMeshes(GuideSurfaces);
                    _guideSupportRoI = GuideDrawingUtilities.CreateRoiMesh(GuideSupport, appendedGuideSurfaces, 1.0);
                    return _guideSupportRoI;
                }
            }

            public Mesh GuideBase { get; set; }

            public bool GenerateGuideBase { get; set; } = true;

            public int NCase => _dataModel.NCase;

            private readonly CMFImplantDirector _director;
            private readonly CMFObjectManager _objectManager;
            private readonly GuidePreferenceDataModel _dataModel;

            public CreateGuideParameters(CMFImplantDirector director, GuidePreferenceDataModel dataModel)
            {
                _director = director;
                _objectManager = new CMFObjectManager(director);
                _dataModel = dataModel;
            }

            public void InitializeSmoothGuideSurface()
            {
                _guideSurfacesSmoothed = GenerateSmoothGuideSurfaces(_director, _dataModel);
            }

            public void Dispose()
            {
                _guideSupport?.Dispose();
                _guideSurfaceWrap?.Dispose();
                _guidePreviewMesh?.Dispose();
                _guideSurfaces?.ForEach(x => x?.Dispose());
                _guideSurfacesSmoothed?.ForEach(x => x?.Dispose());
                _guideScrews?.ForEach(x => x?.Dispose());
                _linkSurfaces?.ForEach(x => x?.Dispose());
                _solidSurfaces?.ForEach(x => x?.Dispose());
                _osteotomyMesh?.Dispose();
                _bridges?.ForEach(x => x.Key?.Dispose());
                _teethBlocks?.ForEach(x => x.Dispose());
                _flanges?.ForEach(x => x?.Dispose());
                _barrels?.ForEach(x => x?.Dispose());
                _barrelsShape?.ForEach(x => x?.Dispose());
                _barrelsSubtractors?.ForEach(x => x?.Dispose());
                _unprocessedBarrelsShape?.Values.ToList().ForEach(x => x?.Dispose());
                _unprocessedBarrelsSubtractors?.Values.ToList().ForEach(x => x?.Dispose());
                _guideSurfaceWrapRoI?.Dispose();
                _guideSupportRoI?.Dispose();
                GuideBase?.Dispose();

                _barrelsAndItsEntitiesInitialized = false;
            }

            private Mesh TryGetExtendedIbbMesh(IBB eibb)
            {
                var guideComponent = new GuideCaseComponent();
                var guidePreviewEibb = guideComponent.GetGuideBuildingBlock(eibb, _dataModel);
                if (_objectManager.HasBuildingBlock(guidePreviewEibb))
                {
                    return (Mesh)_objectManager.GetBuildingBlock(eibb).Geometry.Duplicate();
                }

                return null;
            }

            private List<Mesh> TryGetExtendedIbbMeshes(IBB eibb)
            {
                var res = new List<Mesh>();
                var guideComponent = new GuideCaseComponent();
                var guideEibb = guideComponent.GetGuideBuildingBlock(eibb, _dataModel);
                if (_objectManager.HasBuildingBlock(guideEibb))
                {
                    res = _objectManager.
                        GetAllBuildingBlocks(guideEibb).
                        Select(s => ((Mesh)s.Geometry).DuplicateMesh()).ToList();
                }

                return res;
            }

            private void InitializeBarrelsAndItsEntities()
            {
                var barrelAidesDictionary = new Dictionary<string, BarrelAideDataModel>();

                var linkedImplantScrews = _dataModel.LinkedImplantScrews.ToList();

                var implantScrews = _objectManager.GetAllBuildingBlocks(IBB.Screw);

                var implantBarrels = _objectManager.GetAllBuildingBlocks(IBB.RegisteredBarrel);

                foreach (var linkedImplantScrew in linkedImplantScrews)
                {
                    var implantScrew = (Screw)implantScrews.FirstOrDefault(s => s.Id == linkedImplantScrew);
                    if (implantScrew != null)
                    {
                        if (!implantScrew.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel))
                        {
                            continue;
                        }

                        var implantBarrel = implantBarrels.FirstOrDefault(b => b.Id == implantScrew.ScrewGuideAidesInDocument[IBB.RegisteredBarrel]);
                        if (implantBarrel != null)
                        {
                            BarrelAideDataModel barrelAides;
                            if (barrelAidesDictionary.ContainsKey(implantScrew.ScrewTypeAndBarrelType))
                            {
                                barrelAides = barrelAidesDictionary[implantScrew.ScrewTypeAndBarrelType];
                            }
                            else
                            {
                                barrelAides = new BarrelAideDataModel(
                                    implantScrew.ScrewType, implantScrew.BarrelType);
                                barrelAidesDictionary.Add(implantScrew.ScrewTypeAndBarrelType, barrelAides);
                            }

                            var subtractor = GetRegisteredBarrelComponent(barrelAides.ScrewBarrelSubtractor, implantBarrel);
                            var barrelShape = GetRegisteredBarrelComponent(barrelAides.ScrewBarrelShape, implantBarrel);

                            if (implantScrew.BarrelType.ToLower().Contains(Constants.BarrelTypeName.Marking.ToLower()))
                            {
                                var guideSurfaceWrapMesh = (Mesh)_objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap).Geometry.Duplicate();

                                var constraintMesh = RhinoMeshConverter.ToIDSMesh(guideSurfaceWrapMesh);
                                var subtractorMesh = RhinoMeshConverter.ToIDSMesh(subtractor);
                                var barrelShapeMesh = RhinoMeshConverter.ToIDSMesh(barrelShape);
                                var console = new IDSRhinoConsole();

                                var intersection = BooleansV2.PerformBooleanIntersection(console, constraintMesh, barrelShapeMesh);
                                if (intersection != null && intersection.Faces.Any())
                                {
                                    WrapV2.PerformWrap(console, new[] { intersection }, 0.1, 0.0, 0.65, false, false, false, false, out var wrappedMesh);
                                    _barrels.Add((Brep)implantBarrel.DuplicateGeometry());
                                    _unprocessedBarrelsSubtractors.Add(implantBarrel.Id, subtractor);
                                    _unprocessedBarrelsShape.Add(implantBarrel.Id, RhinoMeshConverter.ToRhinoMesh(wrappedMesh));
                                }
                            }
                            else
                            {
                                _barrels.Add((Brep)implantBarrel.DuplicateGeometry());
                                _barrelsSubtractors.Add(subtractor);
                                _barrelsShape.Add(barrelShape);
                            }
                        }
                    }
                }

                _barrelsAndItsEntitiesInitialized = true;
            }

            private List<Mesh> GenerateSmoothGuideSurfaces(CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel)
            {
                var guideComponent = new GuideCaseComponent();
                var positiveGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.PositiveGuideDrawings, guidePrefModel);
                var negativeGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.NegativeGuideDrawing, guidePrefModel);
                var linkSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideLinkSurface, guidePrefModel);
                var solidSurfacesEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideSolidSurface, guidePrefModel);

                var objectManager = new CMFObjectManager(director);
                var existingPositiveSurfaces = objectManager.GetAllBuildingBlocks(positiveGuideDrawingEibb).Select(s => (Mesh)s.Geometry).ToList();
                var existingNegativeSurfaces = objectManager.GetAllBuildingBlocks(negativeGuideDrawingEibb).Select(s => (Mesh)s.Geometry).ToList();
                var existingLinkSurfaces = objectManager.GetAllBuildingBlocks(linkSurfaceEibb).Select(s => (Mesh)s.Geometry).ToList();
                var existingSolidSurfaces = objectManager.GetAllBuildingBlocks(solidSurfacesEibb).Select(s => (Mesh)s.Geometry).ToList();
                var constraintMesh = objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap);

                var osteotomies = ProPlanImportUtilities.GetAllOriginalOsteotomyPartsRhinoObjects(director.Document).Select(o => (Mesh)o.Geometry).ToList();
                var guideSurfaces = GuideSurfaceUtilities.CreateGuideSurfaces(existingPositiveSurfaces, existingNegativeSurfaces, existingLinkSurfaces, existingSolidSurfaces,
                                                                                        osteotomies, (Mesh)constraintMesh.Geometry.Duplicate(), guidePrefModel.CaseName);
                if (guideSurfaces == null || !guideSurfaces.Any())
                {
                    return null;
                }

                return guideSurfaces;
            }

            public Mesh GenerateGuideBaseSurface(Mesh guideSurface, GuideParams parameter)
            {
                var creator = new GuideBaseCreator();
                if (!creator.CreateGuideBaseSurface(guideSurface, parameter))
                {
                    return null;
                }

                return creator.IntGuideBaseSurface;
            }
        }

        public static Mesh CreateGuideBaseLightWeight(RhinoDoc doc, CMFImplantDirector director,
            GuidePreferenceDataModel dataModel)
        {
            var guideCreationParams = new CreateGuideParameters(director, dataModel);
            var guideSurfacesSmoothed = guideCreationParams.GuideSurfacesSmoothed;

            if (guideSurfacesSmoothed == null || !guideSurfacesSmoothed.Any())
            {
                guideCreationParams.Dispose();
                return null;
            }

            var guideSurfaceWrapRoIed = guideCreationParams.GuideSurfaceWrapRoI;
            var guideSupportRoIed = guideCreationParams.GuideSupportRoI;
            var guideLinkSurfaces = guideCreationParams.LinkSurfaces;
            var osteotomyMesh = guideCreationParams.OsteotomyMesh;
            var parameter = CMFPreferences.GetActualGuideParameters();

            var guideBase = guideCreationParams.GenerateGuideBaseSurface(MeshUtilities.AppendMeshes(guideSurfacesSmoothed), parameter);

            var guide = CreateGuideBaseLightWeight(director, guideSurfaceWrapRoIed, guideSupportRoIed, guideBase, guideLinkSurfaces, osteotomyMesh, dataModel, parameter);
            guideCreationParams.Dispose();
            return guide;
        }

        public static Mesh CreateGuideBaseLightWeight(CMFImplantDirector director, Mesh guideSurfaceWrap, Mesh guideSupport, List<Mesh> guideSurfaces, List<Mesh> guideLinkSurfaces, List<Mesh> osteotomies, GuidePreferenceDataModel dataModel)
        {
            Mesh osteotomyMesh = null;
            if (osteotomies.Any())
            {
                Booleans.PerformBooleanUnion(out osteotomyMesh, osteotomies.ToArray());
            }

            var parameter = CMFPreferences.GetActualGuideParameters();

            var creator = new GuideBaseCreator();
            if (!creator.CreateGuideBaseSurface(MeshUtilities.AppendMeshes(guideSurfaces), parameter))
            {
                return null;
            }

            var guideBase = creator.IntGuideBaseSurface;

            return CreateGuideBaseLightWeight(director, guideSurfaceWrap, guideSupport, guideBase, guideLinkSurfaces, osteotomyMesh, dataModel, parameter);
        }

        private static Mesh CreateGuideBaseLightWeight(CMFImplantDirector director, Mesh guideSurfaceWrap, Mesh guideSupport, Mesh guideBase, List<Mesh> guideLinkSurfaces, Mesh osteotomyMesh,
            GuidePreferenceDataModel dataModel, GuideParams parameter)
        {
            var guideCreatorOps = new GuideCreatorV2(guideBase, guideSurfaceWrap,
                guideSupport, parameter, new List<Screw>(), dataModel,
                guideLinkSurfaces, new List<Mesh>(), osteotomyMesh, new List<KeyValuePair<Brep, Plane>>(), new List<Mesh>(), new List<Brep>(), new List<Mesh>(), new List<Mesh>(),
                new Dictionary<Guid, Mesh>(), new Dictionary<Guid, Mesh>(), new List<Mesh>());

            guideCreatorOps.FilterDisjointPieces = false;
            guideCreatorOps.DoStlFixing = true;

            SetKeyIsGuideCreationErrorToFalse(director, dataModel);
            if (!guideCreatorOps.CreateGuide(out var failedBarrels))
            {
                return null;
            }
            UpdateFailedBarrels(director, failedBarrels);

            return guideCreatorOps.ResGuideCreated;
        }

        public static Mesh CreateGuide(RhinoDoc doc, CMFImplantDirector director, GuidePreferenceDataModel
            dataModel, bool isCreatePreview, out bool isNeedManualQprt)
        {
            GuideCreatorV2.InputMeshesInfo info;
            return CreateGuide(doc, director, dataModel, isCreatePreview, out info, out isNeedManualQprt);
        }

        public static Mesh CreateGuide(RhinoDoc doc, CMFImplantDirector director, GuidePreferenceDataModel
            dataModel, bool isCreatePreview, out GuideCreatorV2.InputMeshesInfo inputMeshInfo, out bool isNeedManualQprt)
        {
            var guideCreationParams = new CreateGuideParameters(director, dataModel);
            var res = CreateGuide(doc, director, dataModel, isCreatePreview, guideCreationParams,
                out inputMeshInfo, out isNeedManualQprt);
            guideCreationParams.Dispose();
            return res;
        }

        public static Mesh CreateGuide(RhinoDoc doc, CMFImplantDirector director, GuidePreferenceDataModel
                dataModel, bool isCreatePreview, CreateGuideParameters guideCreationParams,
            out GuideCreatorV2.InputMeshesInfo inputMeshInfo, out bool isNeedManualQprt)
        {
            isNeedManualQprt = false;
            inputMeshInfo = new GuideCreatorV2.InputMeshesInfo();

            var guideCreatorOps = GenerateGuideCreatorOps(dataModel, guideCreationParams);

            if (guideCreatorOps == null)
            {
                return null;
            }

            if (isCreatePreview)
            {
                guideCreatorOps.DoStlFixing = false;
            }
            else
            {
                guideCreatorOps.DoStlFixing = true;
            }

            SetKeyIsGuideCreationErrorToFalse(director, dataModel);
            if (!guideCreatorOps.CreateGuide(out var failedBarrels))
            {
                return null;
            }
            UpdateFailedBarrels(director, failedBarrels);

            if (guideCreatorOps.DoStlFixing && guideCreatorOps.IsNeedToDoManualQprt)
            {
                isNeedManualQprt = true;
            }

            var res = guideCreatorOps.ResGuideCreated;
            inputMeshInfo = guideCreatorOps.GetInputForOperationMeshInfo();

            return res;
        }

        private static GuideCreatorV2 GenerateGuideCreatorOps(GuidePreferenceDataModel dataModel, CreateGuideParameters guideCreationParams)
        {
            var guideName = dataModel.CaseName;

            var guideSurfaces = guideCreationParams.GuideSurfaces;
            if (!guideSurfaces.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"{guideName} Preview creation skipped because there is no guide surface.");
                return null;
            }

            guideSurfaces = guideCreationParams.GuideSurfacesSmoothed;
            if (guideSurfaces == null || !guideSurfaces.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"{guideName} Preview creation skipped because there is no guide smoothen surface.");
                return null;
            }

            var guideLinkSurfaces = guideCreationParams.LinkSurfaces;
            var guideSolidSurfaces = guideCreationParams.SolidSurfaces;
            var osteotomyMesh = guideCreationParams.OsteotomyMesh;
            var guideBridges = guideCreationParams.Bridges;
            var guideFlanges = guideCreationParams.Flanges;
            var barrels = guideCreationParams.Barrels;
            var barrelsSubtractors = guideCreationParams.BarrelsSubtractors;
            var barrelsShape = guideCreationParams.BarrelsShape;
            var unprocessedBarrelsSubtractors = guideCreationParams.UnprocessedBarrelsSubtractors;
            var unprocessedBarrelsShape = guideCreationParams.UnprocessedBarrelsShape;
            var guideSurfaceWrapRoIed = guideCreationParams.GuideSurfaceWrapRoI;
            var guideSupportRoIed = guideCreationParams.GuideSupportRoI;
            var teethBlocks = guideCreationParams.TeethBlocks;
            var parameter = CMFPreferences.GetActualGuideParameters();

            if (guideCreationParams.GenerateGuideBase)
            {
                guideCreationParams.GuideBase = guideCreationParams.GenerateGuideBaseSurface(MeshUtilities.AppendMeshes(guideSurfaces), parameter);
            }

            var guideBase = guideCreationParams.GuideBase;
            if (guideBase == null)
            {
                return null;
            }
            var guideScrews = guideCreationParams.GuideScrews;

            return new GuideCreatorV2(guideBase, guideSurfaceWrapRoIed,
                guideSupportRoIed, parameter, guideScrews, dataModel,
                guideLinkSurfaces, guideSolidSurfaces, osteotomyMesh, guideBridges, guideFlanges, barrels, barrelsShape, barrelsSubtractors,
                unprocessedBarrelsShape, unprocessedBarrelsSubtractors, teethBlocks);
        }

        public static Mesh CreateGuideWithTransitions(RhinoDoc doc, CMFImplantDirector director, GuidePreferenceDataModel
            dataModel, bool isCreatePreview, CreateGuideParameters guideCreationParams, 
            double transitionRadius, double transitionGapClosingDistacne, out GuideCreatorV2.InputMeshesInfo inputMeshInfo)
        {
            inputMeshInfo = new GuideCreatorV2.InputMeshesInfo();

            var guideCreatorOps = GenerateGuideCreatorOps(dataModel, guideCreationParams);

            if (guideCreatorOps == null)
            {
                return null;
            }

            if (isCreatePreview)
            {
                guideCreatorOps.DoStlFixing = false;
            }
            else
            {
                guideCreatorOps.DoStlFixing = true;
            }

            SetKeyIsGuideCreationErrorToFalse(director, dataModel);
            if (!guideCreatorOps.CreateGuideWithTransitions(transitionRadius,transitionGapClosingDistacne, out var failedBarrels))
            {
                return null;
            }
            UpdateFailedBarrels(director, failedBarrels);

            var res = guideCreatorOps.ResGuideCreated;
            inputMeshInfo = guideCreatorOps.GetInputForOperationMeshInfo();

            return res;
        }

        public static Mesh CreateActualGuide(RhinoDoc doc, CMFImplantDirector director, 
            GuidePreferenceDataModel dataModel, bool allowToCreateFromScratch, out GuideCreatorV2.InputMeshesInfo inputMeshInfo, out bool isNeedManualQprt)
        {
            isNeedManualQprt = false;
            var guideComponent = new GuideCaseComponent();
            var objectManager = new CMFObjectManager(director);
            
            inputMeshInfo = new GuideCreatorV2.InputMeshesInfo()
            {
                GuideName = dataModel.CaseName
            };

            var guidePreviewSmoothenEibb = guideComponent.GetGuideBuildingBlock(IBB.GuidePreviewSmoothen, dataModel);
            if (objectManager.HasBuildingBlock(guidePreviewSmoothenEibb))
            {
                var guidePreviewSmoothen = ((Mesh)objectManager.GetBuildingBlock(guidePreviewSmoothenEibb).Geometry).DuplicateMesh();
                var guide = GuideCreatorV2.DoGuideStlFixing(guidePreviewSmoothen, true, dataModel.CaseName, out isNeedManualQprt);
                guidePreviewSmoothen.Dispose();
                return guide;
            }
            else if (allowToCreateFromScratch)
            {
                var guide = CreateGuide(doc, director, dataModel, false, out inputMeshInfo, out isNeedManualQprt);
                return guide;
            }
            else
            {
                return null;
            }
        }

        private static Mesh GetRegisteredBarrelComponent(Brep screwComponentAtOrigin, RhinoObject barrel)
        {
            var screwComponent = new Brep();
            screwComponent.Append(screwComponentAtOrigin);
            var alignTransform = (Transform)barrel.Attributes.UserDictionary["transformation_matrix"];
            screwComponent.Transform(alignTransform);

            var component = MeshUtilities.ConvertBrepToMesh(screwComponent, true,
                MeshParameters.IDS(Constants.GuideCreationParameters.MeshingParameterMinEdgeLength,
                Constants.GuideCreationParameters.MeshingParameterMaxEdgeLength));
            screwComponent.Dispose();

            return component;
        }

        public static int GetNumberOfMissingActualGuide(CMFImplantDirector director)
        {
            var count = 0;

            foreach (var guidePreferenceData in director.CasePrefManager.GuidePreferences)
            {
                if (IsActualGuideMissing(director, guidePreferenceData))
                {
                    count++;
                }
            }

            return count;
        }

        public static int GetNumberOfMissingSmoothGuideBaseSurface(CMFImplantDirector director)
        {
            var count = 0;

            foreach (var guidePreferenceData in director.CasePrefManager.GuidePreferences)
            {
                if (IsSmoothGuideBaseSurfaceMissing(director, guidePreferenceData))
                {
                    count++;
                }
            }

            return count;
        }

        public static bool IsActualGuideMissing(CMFImplantDirector director, GuidePreferenceDataModel guidePreferenceData)
        {
            var guideComponent = new GuideCaseComponent();
            var objectManager = new CMFObjectManager(director);

            var actualGuideEibb = guideComponent.GetGuideBuildingBlock(IBB.ActualGuide, guidePreferenceData);
            var guideSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideSurface, guidePreferenceData);

            if (!objectManager.HasBuildingBlock(actualGuideEibb) && objectManager.HasBuildingBlock(guideSurfaceEibb))
            {
                return true;
            }

            return false;
        }

        public static bool IsSmoothGuideBaseSurfaceMissing(CMFImplantDirector director, GuidePreferenceDataModel guidePreferenceData)
        {
            var guideComponent = new GuideCaseComponent();
            var objectManager = new CMFObjectManager(director);

            var guidePreviewSmoothenEibb = guideComponent.GetGuideBuildingBlock(IBB.GuidePreviewSmoothen, guidePreferenceData);
            var smoothGuideBaseSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.SmoothGuideBaseSurface, guidePreferenceData);

            if (!objectManager.HasBuildingBlock(smoothGuideBaseSurfaceEibb) && objectManager.HasBuildingBlock(guidePreviewSmoothenEibb))
            {
                return true;
            }

            return false;
        }

        public static bool HasSmoothenGuidePreview(CMFImplantDirector director, GuidePreferenceDataModel guidePreferenceData)
        {
            return HasGuideBuildingBlock(director, guidePreferenceData, IBB.GuidePreviewSmoothen);
        }

        private static bool HasGuideBuildingBlock(CMFImplantDirector director, GuidePreferenceDataModel guidePreferenceData, IBB block)
        {
            var objectManager = new CMFObjectManager(director);
            var guideComponent = new GuideCaseComponent();
            var buildingBlock = guideComponent.GetGuideBuildingBlock(block, guidePreferenceData);
            return objectManager.HasBuildingBlock(buildingBlock.Block);
        }

        private static void UpdateFailedBarrels(CMFImplantDirector director, List<Guid> failedBarrelGuids)
        {
            foreach (var failedBarrelGuid in failedBarrelGuids)
            {
                var failedBarrel = director.Document.Objects.Find(failedBarrelGuid);
                UserDictionaryUtilities.ModifyUserDictionary(failedBarrel,
                    Constants.BarrelAttributeKeys.KeyIsGuideCreationError,
                    true);
            }

            var objectManager = new CMFObjectManager(director);
            var failedScrews = failedBarrelGuids.Select(
                failedBarrelGuid =>
                    (Screw)objectManager.GetAllBuildingBlocks(IBB.Screw)
                        .FirstOrDefault(screw => ((Screw)screw).RegisteredBarrelId == failedBarrelGuid));
            var screwQcCheckManager =
                new ScrewQcCheckerManager(director, new[] { new BarrelTypeChecker() });
            director.ImplantScrewQcLiveUpdateHandler?.RecheckCertainResult(screwQcCheckManager, failedScrews);
        }

        private static void SetKeyIsGuideCreationErrorToFalse(CMFImplantDirector director, GuidePreferenceDataModel guidePreferenceDataModel)
        {
            var linkedImplantScrewGuids = guidePreferenceDataModel.LinkedImplantScrews;
            foreach (var linkedImplantScrewGuid in linkedImplantScrewGuids)
            {
                var implantScrew = director.Document.Objects.Find(linkedImplantScrewGuid) as Screw;
                if (implantScrew == null || implantScrew.RegisteredBarrelId == Guid.Empty)
                {
                    continue;
                }

                var selectedBarrelId = implantScrew.RegisteredBarrelId;
                var registeredBarrel = director.Document.Objects.Find(selectedBarrelId);
                UserDictionaryUtilities.ModifyUserDictionary(registeredBarrel, Constants.BarrelAttributeKeys.KeyIsGuideCreationError, false);
            }
        }
    }
}
