using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class ScrewLengthBackwardCompatibility
    {
        private readonly Dictionary<string, List<double>> _screwTypeLengthsDictionary;

        private readonly CMFImplantDirector _director;

        public ScrewLengthBackwardCompatibility(CMFImplantDirector director)
        {
            _director = director;
            _screwTypeLengthsDictionary = new Dictionary<string, List<double>>();

            var screwLengthsData = CasePreferencesHelper.LoadScrewLengthData();
            foreach (var screwLength in screwLengthsData.ScrewLengths)
            {
                _screwTypeLengthsDictionary.Add(screwLength.ScrewType, Queries.GetDefaultScrewStyleAvailableScrewLengths(screwLength.ScrewType));
            }
        }

        // Can rename in future if we need different way to readjust the screw length
        private Screw ReAdjustScrewLengthToLowerClosest(Screw screw)
        {
            if (!_screwTypeLengthsDictionary.ContainsKey(screw.ScrewType))
            {
                return null;
            }
            
            var screwsLengths = _screwTypeLengthsDictionary[screw.ScrewType];
            if (screwsLengths.Any(screwsLength => Math.Abs(screwsLength - screw.Length) < 0.01))
            {
                return null;
            }

            var length = screwsLengths.Where(l => l <= screw.Length).OrderBy(l => l).Last();
            var tipPoint = screw.HeadPoint + length * screw.Direction;

            return ScrewUtilities.AdjustScrewLength(screw, tipPoint);
        }

        public void PerformScrewLengthCorrection()
        {
            var screwManager = new ScrewManager(_director);
            var objectManager = new CMFObjectManager(_director);

            var screws = screwManager.GetAllScrews(false);
            foreach (var screw in screws)
            {
                var newScrew = ReAdjustScrewLengthToLowerClosest(screw);
                if (newScrew == null)
                {
                    continue;
                }
                
                var casePreferenceData = objectManager.GetCasePreference(screw);
                var screwRef = screw;
                screwManager.ReplaceExistingImplantScrewWithoutAnyInvalidation(newScrew, ref screwRef, casePreferenceData);

                IDSPluginHelper.WriteLine(LogCategory.Warning, $"\"{casePreferenceData.CaseName}\", screw({screw.Index})'s length changed from {screw.Length}mm to {newScrew.Length}mm");
            }

        }
    }
}
