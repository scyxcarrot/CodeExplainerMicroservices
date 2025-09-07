using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMF.TestLib.Components
{
    public class AllImplantSupportComponent
    {
        public List<ImplantSupportComponent> ImplantSupports = new List<ImplantSupportComponent>();

        public void ParseToDirector(CMFImplantDirector director, string workDir)
        {
            var objectManager = new CMFObjectManager(director);
            var implantCaseComponent = new ImplantCaseComponent();

            foreach (var implantSupportComponent in ImplantSupports)
            {
                var casePreferenceDataModel = director.CasePrefManager.GetCase(implantSupportComponent.CaseGuid);
                var implantSupportBb = implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreferenceDataModel);

                var implantSupportMesh = implantSupportComponent.GetImplantSupportMesh(workDir);
                var guid = objectManager.AddNewBuildingBlock(implantSupportBb, implantSupportMesh);
                if (guid == Guid.Empty)
                {
                    throw new IDSUnexpectedState($"ImplantSupport_I{casePreferenceDataModel.NCase} failed to add into director");
                }
            }
        }

        public void FillToComponent(CMFImplantDirector director, string workDir)
        {
            var objectManager = new CMFObjectManager(director);
            var implantCaseComponent = new ImplantCaseComponent();
            ImplantSupports.Clear();

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var implantSupportBb = implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreferenceDataModel);
                if (objectManager.HasBuildingBlock(implantSupportBb))
                {
                    var implantSupport = objectManager.GetBuildingBlock(implantSupportBb).Geometry;
                    var implantSupportName = $"ImplantSupport_I{casePreferenceDataModel.NCase}";
                    
                    var implantSupportComponent = new ImplantSupportComponent();
                    implantSupportComponent.FillToComponent(implantSupportName, workDir,
                        casePreferenceDataModel.CaseGuid, (Mesh)implantSupport);
                    ImplantSupports.Add(implantSupportComponent);
                }
            }
        }
    }
}
