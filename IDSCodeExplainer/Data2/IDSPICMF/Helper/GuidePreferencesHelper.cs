using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;

namespace IDS.PICMF.Helper
{
    public static class GuidePreferencesHelper
    {
        public static Guid PromptForPreferenceId()
        {
            var gm = new GetOption();
            gm.AcceptNothing(false);
            var modeCasePreference = gm.AddOption("PreferenceId");

            var prefId = Guid.Empty;

            while (true)
            {
                var gres = gm.Get();
                if (gres == GetResult.Cancel)
                {
                    break;
                }

                if (gres != GetResult.Option)
                {
                    continue;
                }

                if (gm.OptionIndex() != modeCasePreference)
                {
                    continue;
                }

                prefId = GetCasePreferenceId();
                if (prefId == Guid.Empty)
                {
                    RhinoApp.WriteLine($"Invalid case preference id: {prefId}");
                }

                break;
            }

            return prefId;
        }

        private static Guid GetCasePreferenceId()
        {
            var casePreferenceId = Guid.Empty;
            var casePreferenceIdStr = string.Empty;
            var result = RhinoGet.GetString("PreferenceId", false, ref casePreferenceIdStr);
            if (result != Result.Success)
            {
                return casePreferenceId;
            }
            if (!Guid.TryParse(casePreferenceIdStr, out casePreferenceId))
            {
                casePreferenceId = Guid.Empty;
            }
            return casePreferenceId;
        }

        public static bool PromptForGuideCaseNumber(
            CMFImplantDirector director,
            out GuidePreferenceDataModel casePreferenceDataModel)
        {
            var getOption = new GetOption();
            getOption.AcceptNothing(false);
            var modeCaseNumber = getOption.AddOption("GuideCaseNumber");

            casePreferenceDataModel = null;
            var success = false;

            while (true)
            {
                var getResult = getOption.Get();
                if (getResult == GetResult.Cancel)
                {
                    break;
                }

                if (getResult != GetResult.Option)
                {
                    continue;
                }

                if (getOption.OptionIndex() != modeCaseNumber)
                {
                    continue;
                }

                success = GetCasePreferenceDataModel(director, out casePreferenceDataModel);
                if (!success)
                {
                    return success;
                }

                break;
            }

            return success;
        }

        private static bool GetCasePreferenceDataModel(CMFImplantDirector director, out GuidePreferenceDataModel guidePreferenceDataModel)
        {
            guidePreferenceDataModel = null;
            var caseNumberString = string.Empty;
            var result = RhinoGet.GetString("GuideCaseNumber", false, ref caseNumberString);
            if (result != Result.Success)
            {
                return false;
            }
            if (!int.TryParse(caseNumberString, out var caseNumber))
            {
                return false;
            }

            guidePreferenceDataModel = director.CasePrefManager.GuidePreferences
                .FirstOrDefault(x => x.NCase == caseNumber);

            if (guidePreferenceDataModel == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Guide Preference Number = {caseNumber} not found");
                return false;
            }

            return true;
        }
    }
}
