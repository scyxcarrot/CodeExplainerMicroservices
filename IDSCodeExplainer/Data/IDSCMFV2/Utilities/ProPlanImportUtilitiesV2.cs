using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Logics;

namespace IDS.CMF.V2.Utilities
{
    public static class ProPlanImportUtilitiesV2
    {
        public static ProplanBoneType[] GetBoneTypes(string[] parts)
        {
            var boneType = new ProplanBoneType[parts.Length];
            for (int index = 0; index < parts.Length; index++)
            {
                if (ProPlanPartsUtilitiesV2.IsPreopPart(parts[index]))
                {
                    boneType[index] = ProplanBoneType.Preop;
                }
                else
                {
                    boneType[index] = ProPlanPartsUtilitiesV2.IsOriginalPart(parts[index]) ? ProplanBoneType.Original : ProplanBoneType.Planned;
                }
            }

            return boneType;
        }
    }
}
