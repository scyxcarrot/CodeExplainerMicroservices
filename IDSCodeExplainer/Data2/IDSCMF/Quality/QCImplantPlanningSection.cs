using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.V2.DataModel;
using IDS.Core.PluginHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace IDS.CMF.Quality
{
    public static class QCImplantPlanningSection
    {
        public static void ImplantPlanningInfo(ref Dictionary<string, string> valueDictionary, CasePreferenceDataModel casePref)
        {
            var timerComponent = new Stopwatch();
            timerComponent.Start();
            var timeRecorded = new Dictionary<string, string>();

            FillInQcCasePrefInfo(ref valueDictionary, casePref);

            timerComponent.Stop();
            timeRecorded.Add($"FillInQcCasePrefInfo", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
            timerComponent.Restart();

            timerComponent.Stop();
            timeRecorded.Add($"GeneratePlanningImplantOnBoneImageString", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");

            Msai.TrackDevEvent($"QCDoc Implant Info Section-ImplantPlanningInfo {casePref.CasePrefData.ImplantTypeValue}", "CMF", timeRecorded);
            Msai.PublishToAzure();
        }

        private static void FillInQcCasePrefInfo(ref Dictionary<string, string> valueDictionary, CasePreferenceDataModel casePref)
        {
            var tagPlateThickness = "VAL_PLANNING_PLATE_THICKNESS";
            var tagPlateWidth = "VAL_PLANNING_PLATE_WIDTH";
            var tagLinkWidth = "VAL_PLANNING_LINK_WIDTH";
            var tagScrewType = "VAL_PLANNING_SCREW_TYPE";
            var tagScrewStyle = "VAL_PLANNING_SCREW_STYLE";
            var tagBarrelType = "VAL_PLANNING_BARREL_TYPE";

            var casePrefData = casePref.CasePrefData;
            var implantDataModel = casePref.ImplantDataModel;

            valueDictionary.Add(tagPlateThickness, string.Format(CultureInfo.InvariantCulture, "{0:F2}", casePrefData.PlateThicknessMm));
            valueDictionary.Add(tagPlateWidth, string.Format(CultureInfo.InvariantCulture, "{0:F2}{1}", casePrefData.PlateWidthMm, GetConnectionWidthOverrideNote(implantDataModel, typeof(ConnectionPlate), casePrefData.PlateWidthMm)));
            valueDictionary.Add(tagLinkWidth, string.Format(CultureInfo.InvariantCulture, "{0:F2}{1}", casePrefData.LinkWidthMm, GetConnectionWidthOverrideNote(implantDataModel, typeof(ConnectionLink), casePrefData.LinkWidthMm)));
            valueDictionary.Add(tagScrewType, casePrefData.ScrewTypeValue);
            valueDictionary.Add(tagScrewStyle, casePrefData.ScrewStyle);
            valueDictionary.Add(tagBarrelType, casePrefData.BarrelTypeValue);
        }

        private static string GetConnectionWidthOverrideNote(ImplantDataModel implantDataModel, Type connectionType, double defaultWidth)
        {
            var connections = implantDataModel.ConnectionList;
            if (!connections.Any())
            {
                return string.Empty;
            }

            var overrideValues = new List<double>();

            foreach (var connection in connections)
            {
                if (connectionType.IsAssignableFrom(connection.GetType()))
                {
                    if (Math.Abs(connection.Width - defaultWidth) < 0.001)
                    {
                        continue;
                    }

                    overrideValues.Add(connection.Width);
                }
            }

            return !overrideValues.Any()
                ? string.Empty
                : $"<br/>*Override value(s): {string.Join(", ", overrideValues.OrderBy(w => w).Select(w => string.Format(CultureInfo.InvariantCulture, "{0:F2}", w)).Distinct())}";
        }
    }
}
