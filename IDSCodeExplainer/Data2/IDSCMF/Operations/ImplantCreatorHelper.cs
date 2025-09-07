using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Operations
{
    public static class ImplantCreatorHelper
    {
        public class ImplantCreatorParamsSupportData
        {
            public CasePreferenceDataModel CPrefDataModel { get; private set; }
            private readonly CMFObjectManager _objectManager;
            private RhinoObject _supportRhObj;

            private readonly bool _hasImplantSupport;
            private readonly bool _hasPatchSupport;
            private readonly SupportType _supportType;

            public SupportType SupportType => _supportType;

            public List<PatchSupportData> PatchSupportDataList { get; private set; }

            public ImplantCreatorParamsSupportData(CMFImplantDirector director, CasePreferenceDataModel casePreferenceDataModel)
            {
                _objectManager = new CMFObjectManager(director);
                CPrefDataModel = casePreferenceDataModel;

                // Cache support type flags for performance
                _hasImplantSupport = CheckHasImplantSupport();
                _hasPatchSupport = CheckHasPatchSupport();
                _supportType = DetermineSupportType();

                if (_supportType == SupportType.Both)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"An Implant Case has conflicting support types: Implant Support and Patch Support");
                }

                if (_supportType == SupportType.ImplantSupport)
                {
                    var implantSupportBb = GetImplantSupportBb(casePreferenceDataModel);
                    _supportRhObj = _objectManager.GetBuildingBlock(implantSupportBb);
                }
                else if (_supportType == SupportType.PatchSupport)
                {
                    var patchSupportBb = GetPatchSupportBb(casePreferenceDataModel);
                    _supportRhObj = _objectManager.GetBuildingBlock(patchSupportBb);

                    // Initialize patch support data
                    InitializePatchSupportData();
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"ImplantCreatorParamsSupportData: Missing Implant Support and Patch Support");
                }
            }

            private void InitializePatchSupportData()
            {
                PatchSupportDataList = new List<PatchSupportData>();

                if (_supportType != SupportType.PatchSupport)
                    return;

                var patchSupportBb = GetPatchSupportBb(CPrefDataModel);
                var patchSupportObjects = _objectManager.GetAllBuildingBlocks(patchSupportBb);

                foreach (var patchSupportRhinoObject in patchSupportObjects)
                {
                    try
                    {
                        var patchData = new PatchSupportData(patchSupportRhinoObject, CPrefDataModel);
                        PatchSupportDataList.Add(patchData);
                    }
                    catch (Exception ex)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to initialize patch support data: {ex.Message}");
                    }
                }
            }

            private bool CheckHasImplantSupport()
            {
                var implantSupportBb = GetImplantSupportBb(CPrefDataModel);
                return _objectManager.HasBuildingBlock(implantSupportBb.Block);
            }

            private bool CheckHasPatchSupport()
            {
                var patchSupportBb = GetPatchSupportBb(CPrefDataModel);
                return _objectManager.HasBuildingBlock(patchSupportBb.Block);
            }

            private SupportType DetermineSupportType()
            {
                if (_hasImplantSupport && _hasPatchSupport)
                    return SupportType.Both;
                else if (_hasImplantSupport)
                    return SupportType.ImplantSupport;
                else if (_hasPatchSupport)
                    return SupportType.PatchSupport;
                else
                    return SupportType.None;
            }

            public bool MissingImplantSupportRhObj()
            {
                return _supportRhObj == null;
            }

            public bool ContainOutdatedImplantSupport()
            {
                if (_supportRhObj == null) return false;
                return OutdatedImplantSupportHelper.IsImplantSupportOutdated((Mesh)_supportRhObj.Geometry);
            }

            public bool SupportMeshIsNull()
            {
                return _supportMesh == null;
            }

            private Mesh _supportMesh;
            public Mesh SupportMesh
            {
                get
                {
                    if (_supportMesh != null)
                    {
                        return _supportMesh;
                    }

                    GetAndSetImplantRoIs();

                    return _supportMesh;
                }
            }

            private Mesh _supportMeshFull;
            public Mesh SupportMeshFull
            {
                get
                {
                    if (_supportMeshFull != null)
                    {
                        return _supportMeshFull;
                    }

                    if (_hasPatchSupport)
                    {
                        GetAndSetImplantRoIs();
                    }
                    else
                    {
                        var support = (Mesh)_objectManager.GetBuildingBlock(GetImplantSupportBb(CPrefDataModel)).Geometry;
                        if (!support.FaceNormals.Any())
                        {
                            support.FaceNormals.ComputeFaceNormals();
                        }

                        _supportMeshFull = support.DuplicateMesh();
                    }

                    return _supportMeshFull;
                }
            }

            private Mesh _supportMeshBigger;
            public Mesh SupportMeshBigger
            {
                get
                {
                    if (_supportMeshBigger != null)
                    {
                        return _supportMeshBigger;
                    }

                    GetAndSetImplantRoIs();

                    return _supportMeshBigger;
                }
            }

            private void GetAndSetImplantRoIs()
            {
                if (_hasPatchSupport)
                {
                    var patchSupportBb = GetPatchSupportBb(CPrefDataModel);
                    var patchSupportObjects = _objectManager.GetAllBuildingBlocks(patchSupportBb);
                    if (patchSupportObjects.Any())
                    {
                        var biggerConstraintMeshes = new List<Mesh>();
                        var smallerRoIs = new List<Mesh>();

                        foreach (var patchSupport in patchSupportObjects)
                        {
                            var biggerConstraintMesh = (Mesh)patchSupport.DuplicateGeometry();
                            biggerConstraintMeshes.Add(biggerConstraintMesh);

                            if (!patchSupport.Attributes.UserDictionary.ContainsKey(PatchSupportKeys.SmallerRoIKey))
                            {
                                throw new IDSException($"Key {PatchSupportKeys.SmallerRoIKey} not found!");
                            }

                            var smallRoI = ((Mesh)patchSupport.Attributes.UserDictionary[PatchSupportKeys.SmallerRoIKey]).DuplicateMesh();
                            smallerRoIs.Add(smallRoI);
                        }

                        Booleans.PerformBooleanUnion(out var unionedSmallerRoIs, smallerRoIs.ToArray());
                        if (!unionedSmallerRoIs.FaceNormals.Any())
                        {
                            unionedSmallerRoIs.FaceNormals.ComputeFaceNormals();
                        }
                        _supportMeshFull = unionedSmallerRoIs;

                        Booleans.PerformBooleanUnion(out var unionedBiggerConstraints, biggerConstraintMeshes.ToArray());
                        if (!unionedBiggerConstraints.FaceNormals.Any())
                        {
                            unionedBiggerConstraints.FaceNormals.ComputeFaceNormals();
                        }
                        _supportMeshBigger = unionedBiggerConstraints;

                        _supportMesh = unionedSmallerRoIs.DuplicateMesh();
                    }
                }
                else
                {
                    // Use traditional ImplantSupport ROI generation
                    Mesh biggerRoI;
                    var smallRoI = ImplantCreationUtilities.GetImplantRoIForImplantCreation(_objectManager, CPrefDataModel, ref _supportRhObj, out biggerRoI);
                    if (!smallRoI.FaceNormals.Any())
                    {
                        smallRoI.FaceNormals.ComputeFaceNormals();
                    }

                    _supportMesh = smallRoI.DuplicateMesh();

                    if (!biggerRoI.FaceNormals.Any())
                    {
                        biggerRoI.FaceNormals.ComputeFaceNormals();
                    }

                    _supportMeshBigger = biggerRoI.DuplicateMesh();
                }
            }

            private ExtendedImplantBuildingBlock GetImplantSupportBb(CasePreferenceDataModel casePreferenceDataModel)
            {
                var implantCaseComponent = new ImplantCaseComponent();
                return implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreferenceDataModel);
            }

            private ExtendedImplantBuildingBlock GetPatchSupportBb(CasePreferenceDataModel casePreferenceDataModel)
            {
                var implantCaseComponent = new ImplantCaseComponent();
                return implantCaseComponent.GetImplantBuildingBlock(IBB.PatchSupport, casePreferenceDataModel);
            }
        }

        public class PastillePreviewIntermediateParamsData
        {
            public CasePreferenceDataModel CPrefDataModel { get; }
            public List<Mesh> pastillePreviewIntermediates { get; }
            public List<Mesh> pastillePreviewLandmarkIntermediates { get; }
            public List<Mesh> pastilleCylinder { get; }

            public PastillePreviewIntermediateParamsData(CMFImplantDirector director, CasePreferenceDataModel casePreferenceDataModel)
            {
                CPrefDataModel = casePreferenceDataModel;

                var pastillePreviewHelper = new PastillePreviewHelper(director);
                pastillePreviewIntermediates = pastillePreviewHelper.GetIntermediatePastillePreviews(casePreferenceDataModel);
                pastillePreviewLandmarkIntermediates = pastillePreviewHelper.GetIntermediatePastilleLandmarkPreviews(casePreferenceDataModel);
                pastilleCylinder = pastillePreviewHelper.GetPastilleCylinder(casePreferenceDataModel);
            }
        }

        public class ConnectionPreviewIntermediateParamsData
        {
            public CasePreferenceDataModel CPrefDataModel { get; }
            public List<Mesh> ConnectionPreviewIntermediates { get; }

            public ConnectionPreviewIntermediateParamsData(CMFImplantDirector director, CasePreferenceDataModel casePreferenceDataModel)
            {
                CPrefDataModel = casePreferenceDataModel;

                var connectionPreviewHelper = new ConnectionPreviewHelper(director);
                ConnectionPreviewIntermediates = connectionPreviewHelper.GetIntermediateConnectionPreviews(casePreferenceDataModel);
            }
        }

        public static ImplantCreatorParams CreateImplantCreatorParams(CMFImplantDirector director)
        {
            var supportMeshRoIs = new List<ImplantCreatorParamsSupportData>();
            var pastilleIntermediates = new List<PastillePreviewIntermediateParamsData>();
            var connectionIntermediates = new List<ConnectionPreviewIntermediateParamsData>();

            var screwManager = new ScrewManager(director);
            var allScrews = screwManager.GetAllScrews(false);

            director.CasePrefManager.CasePreferences.ForEach(cp =>
            {
                supportMeshRoIs.Add(new ImplantCreatorParamsSupportData(director, cp));
                pastilleIntermediates.Add(new PastillePreviewIntermediateParamsData(director, cp));
                connectionIntermediates.Add(new ConnectionPreviewIntermediateParamsData(director, cp));
            });

            return new ImplantCreatorParams(allScrews, supportMeshRoIs, director.CasePrefManager.CasePreferences, pastilleIntermediates, connectionIntermediates);
        }
    }

    public static class DisplayConduitProvider
    {
        private static readonly List<DisplayConduit> _conduits = new List<DisplayConduit>();

        public static void AddConduit(DisplayConduit conduit)
        {
            _conduits.Add(conduit);
        }

        public static void RemoveConduit(DisplayConduit conduit)
        {
            _conduits.Remove(conduit);
        }

        public static List<T> GetConduit<T>() where T : DisplayConduit
        {
            return _conduits.OfType<T>().ToList();
        }

        public static void Reset()
        {
            _conduits.ForEach(x => x.Enabled = false);
            _conduits.Clear();
        }
    }

    public class ImplantSurfaceRoIVisualizer : DisplayConduit, IDisposable
    {
        private readonly CasePreferenceDataModel _cp;
        public List<NurbsCurve> RoiSurfaceBorders { get; private set; } = new List<NurbsCurve>();

        public ImplantSurfaceRoIVisualizer(CasePreferenceDataModel cp, RhinoObject implantSupportRhObj)
        {
            _cp = cp;
            InvalidateBorder(implantSupportRhObj);
            DisplayConduitProvider.AddConduit(this);
        }

        public void InvalidateBorder(RhinoObject implantSupportRhObj)
        {
            var keyRoiSurfaceString = ImplantCreationUtilities.GenerateImplantRoISurfaceKey(_cp);
            var keyRoiVolumeString = ImplantCreationUtilities.GenerateImplantRoIVolumeKey(_cp);

            if (implantSupportRhObj.Attributes.UserDictionary.ContainsKey(keyRoiSurfaceString))
            {
                var roiVolume = (Mesh)implantSupportRhObj.Attributes.UserDictionary[keyRoiVolumeString];
                var _roiSurface = (Mesh)implantSupportRhObj.Attributes.UserDictionary[keyRoiSurfaceString];

                var nakedEdges = _roiSurface.GetNakedEdges();

                if (nakedEdges != null) //Only true when we can use roi as a surface
                {
                    var borders = nakedEdges.Select(x => x.ToNurbsCurve()).Where(x => x.IsClosed).ToList();
                    borders.ForEach(x =>
                    {
                        var pulled = x.PullToMesh(roiVolume, Constants.ImplantCreation.DotMeshDistancePullTolerance);
                        RoiSurfaceBorders.Add(pulled.ToNurbsCurve());
                    });

                }
            }
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            RoiSurfaceBorders.ForEach(x =>
            {
                e.Display.DrawCurve(x, Color.DarkRed, 2);
            });
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
                DisplayConduitProvider.RemoveConduit(this);
            }
        }
    }
}
