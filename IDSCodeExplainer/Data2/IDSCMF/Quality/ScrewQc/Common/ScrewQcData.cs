using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.V2.ScrewQc;
using IDS.Interface.Geometry;
using IDS.RhinoInterfaces.Converter;
using System;

namespace IDS.CMF.ScrewQc
{
    public class ScrewQcData : IScrewQcData
    {
        public Guid Id { get; set; }

        public int Index { get; set; }

        public string ScrewType { get; set; }

        public IPoint3D HeadPoint { get; set; }

        public IPoint3D TipPoint { get; set; }

        public IPoint3D BodyOrigin { get; set; }

        public Guid CaseGuid { get; set; }

        public string CaseName { get; set; }

        public int NCase { get; set; }

        public double CylinderDiameter { get; set; }

        private static IScrewQcData Create(Screw screw, ICaseData caseData)
        {
            var screwQcData = Create(screw) as ScrewQcData;
            screwQcData.CaseGuid = caseData.CaseGuid;
            screwQcData.CaseName = caseData.CaseName;
            screwQcData.NCase = caseData.NCase;
            return screwQcData;
        }

        public static IScrewQcData Create(Screw screw)
        {
            var cylinderDiameter = Queries.GetScrewQCCylinderDiameter(screw.ScrewType);

            return new ScrewQcData
            {
                Id = screw.Id,
                Index = screw.Index,
                ScrewType = screw.ScrewType,
                HeadPoint = RhinoPoint3dConverter.ToIPoint3D(screw.HeadPoint),
                TipPoint = RhinoPoint3dConverter.ToIPoint3D(screw.TipPoint),
                BodyOrigin = RhinoPoint3dConverter.ToIPoint3D(screw.BodyOrigin),
                CylinderDiameter = cylinderDiameter
            };
        }

        public static IScrewQcData CreateImplantScrewQcData(Screw screw)
        {
            var screwManager = new ScrewManager(screw.Director);
            var implantCasePref = screwManager.GetImplantPreferenceTheScrewBelongsTo(screw);

            return Create(screw, implantCasePref);
        }

        public static IScrewQcData CreateImplantScrewQcData(Screw screw, Screw originalPositionedScrew)
        {
            var screwManager = new ScrewManager(screw.Director);
            var implantCasePref = screwManager.GetImplantPreferenceTheScrewBelongsTo(screw);

            return Create(originalPositionedScrew, implantCasePref);
        }
    }
}
