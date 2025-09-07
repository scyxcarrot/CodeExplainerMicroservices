using IDS.CMF.CasePreferences;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class OutdatedImplantSupportHelper
    {
        public static void SetImplantSupportOutdated(CMFImplantDirector director, CasePreferenceDataModel casePreferenceDataModel)
        {
            var objectManager = new CMFObjectManager(director);

            var implantSupportManager = new ImplantSupportManager(objectManager);
            var implantSupportRhObject = implantSupportManager.GetImplantSupportRhObj(casePreferenceDataModel);
            if (implantSupportRhObject == null || IsImplantSupportOutdated(implantSupportRhObject))
            {
                return;
            }

            RhinoObjectUtilities.SetRhObjectMeshVerticesColors(director, implantSupportRhObject,
                Colors.ImplantSupportInvalidated, false);
            RhinoObjectUtilities.ResetRhObjTransparency(director, implantSupportRhObject);

            casePreferenceDataModel.Graph.InvalidateGraph();
            casePreferenceDataModel.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.PlanningImplant });

            IDSPluginHelper.WriteLine(LogCategory.Warning, $"Implant support for case {casePreferenceDataModel.CaseName} was outdated!");
        }

        public static void SetMultipleImplantSupportsOutdated(CMFImplantDirector director, IEnumerable<CasePreferenceDataModel> casePreferenceDataModels)
        {
            foreach (var casePreferenceDataModel in casePreferenceDataModels)
            {
                SetImplantSupportOutdated(director, casePreferenceDataModel);
            }
        }

        public static void SetMultipleImplantSupportsOutdated(CMFImplantDirector director, IEnumerable<RhinoObject> implantSupportRhinoObjects)
        {
            var objectManager = new CMFObjectManager(director);
            var implantSupportManager = new ImplantSupportManager(objectManager);
            var casePreferenceDataModels = implantSupportManager.GetCasePreferenceDataModel(implantSupportRhinoObjects);
            SetMultipleImplantSupportsOutdated(director, casePreferenceDataModels);
        }

        public static void SetAllImplantSupportsOutdated(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var implantSupportManager = new ImplantSupportManager(objectManager);

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                if (implantSupportManager.HaveImplantSupport(casePreferenceDataModel))
                {
                    SetImplantSupportOutdated(director, casePreferenceDataModel);
                }
            }
        }

        public static List<RhinoObject> GetOutdatedImplantSupports(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            return objectManager.GetAllBuildingBlocks(IBB.ImplantSupport).Where(IsImplantSupportOutdated).ToList();
        }

        public static List<RhinoObject> GetValidImplantSupports(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            return objectManager.GetAllBuildingBlocks(IBB.ImplantSupport).Where(i =>!IsImplantSupportOutdated(i)).ToList();
        }

        public static bool HasAnyOutdatedImplantSupports(CMFImplantDirector director)
        {
            return GetOutdatedImplantSupports(director).Count > 0;
        }

        public static bool IsImplantSupportOutdated(Mesh supportMesh)
        {
            return supportMesh.VertexColors.Any();
        }

        public static bool IsImplantSupportOutdated(RhinoObject supportMeshRhinoObject)
        {
            return IsImplantSupportOutdated((Mesh)supportMeshRhinoObject.Geometry);
        }
    }
}
