using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using System;
using System.Linq;

namespace IDS.CMF.TestLib.Components
{
    public class ImplantScrewComponent
    {
        public Guid Id { get; set; } = Guid.Empty;

        public int Index { get; set; } = -1;

        public IDSPoint3D HeadPoint { get; set; } = IDSPoint3D.Unset;

        public IDSPoint3D TipPoint { get; set; } = IDSPoint3D.Unset;

        public int GroupIndex { get; set; } = -1;

        public string BarrelType { get; set; } = string.Empty;

        public void SetScrew(Screw screw, ScrewManager.ScrewGroupManager screwGroupManager)
        {
            Id = screw.Id;
            Index = screw.Index;
            HeadPoint = (IDSPoint3D)RhinoPoint3dConverter.ToIPoint3D(screw.HeadPoint);
            TipPoint = (IDSPoint3D)RhinoPoint3dConverter.ToIPoint3D(screw.TipPoint);
            GroupIndex = screwGroupManager.GetScrewGroupIndex(screw);
            BarrelType = screw.BarrelType;
        }

        public Screw GetScrew(CMFImplantDirector director, CasePreferenceDataModel casePreferenceData, out int groupIndex)
        {
            var screwAideDDict = casePreferenceData.ScrewAideData.GenerateScrewAideDictionary();
            var screw = new Screw(director,
                RhinoPoint3dConverter.ToPoint3d(HeadPoint),
                RhinoPoint3dConverter.ToPoint3d(TipPoint),
                screwAideDDict,
                Index,
                casePreferenceData.CasePrefData.ScrewTypeValue,
                casePreferenceData.CasePrefData.BarrelTypeValue
            )
            {
                Id = Id
            };

            var screwManager = new ScrewManager(screw.Director);
            var implantPreferenceModel = screwManager.GetImplantPreferenceTheScrewBelongsTo(screw);
            var availableBarrelType = implantPreferenceModel.BarrelTypes.ToList();

            if (availableBarrelType.Contains(BarrelType))
            {
                screw.BarrelType = BarrelType;
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Failed to set barrel type for screw({screw.Id})");
            }

            groupIndex = GroupIndex;
            return screw;
        }
    }
}
