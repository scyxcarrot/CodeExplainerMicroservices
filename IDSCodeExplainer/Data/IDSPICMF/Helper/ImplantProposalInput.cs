using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Preferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;

namespace IDS.PICMF.Helper
{
    public class ImplantProposalInput
    {
        private readonly CMFImplantDirector _director;

        public ImplantProposalInput(CMFImplantDirector director)
        {
            _director = director;
        }

        private Guid GetImplantCasePreferenceId()
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

            if (!_director.CasePrefManager.IsCaseExist(casePreferenceId))
            {
                casePreferenceId = Guid.Empty;
            }

            return casePreferenceId;
        }

        public bool GetImplantPreferenceModel(out ImplantPreferenceModel implantPreferenceModel)
        {
            implantPreferenceModel = null;
            var casePreferenceId = GetImplantCasePreferenceId();
            if (casePreferenceId == Guid.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid Case Preference Id");
                return false;
            }
            var casePreferenceData = (ImplantPreferenceModel)_director.CasePrefManager.GetCase(casePreferenceId);

            // TODO: Extend this boolean to include other implant proposal implant types in the future
            if (casePreferenceData.SelectedImplantType != ImplantProposalOperations.Genio)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Command is only available for {ImplantProposalOperations.Genio} cases");
                return false;
            }

            if (casePreferenceData.ImplantDataModel.IsHasConstruction())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "There is an existing planning implant, please delete it before running this command");
                return false;
            }

            implantPreferenceModel = casePreferenceData;
            return true;
        }

        public bool GetUserInputs(ref ImplantProposalGenioModel implantProposalGenioModel)
        {
            string[] preset = new[] { "Narrow", "Wide", "Wider" };
            var includeMiddlePlateToggle = new OptionToggle(
                true, "No", "Yes");

            var getOption = new GetOption();
            getOption.SetCommandPrompt($"{ImplantProposalOperations.Genio} Auto Implant Options");
            getOption.AcceptNothing(true); // accept ENTER to confirm
            var mandiblePresetOptionIndex = getOption.AddOptionList("MandiblePreset", preset, 0);
            var genioPresetOptionIndex = getOption.AddOptionList("GenioPreset", preset, 1);
            var includeMiddlePlateOptionIndex = getOption.AddOptionToggle(
                "IncludeMiddlePlate", ref includeMiddlePlateToggle);
            var genioAutoImplantParams = CMFPreferences.GetGenioAutoImplantParams();

            var mandibleDistances = new double[]
            {
                genioAutoImplantParams.MandibleNarrowDistance,
                genioAutoImplantParams.MandibleWideDistance,
                genioAutoImplantParams.MandibleWiderDistance
            };
            var genioDistances = new double[]
            {
                genioAutoImplantParams.GenioNarrowDistance,
                genioAutoImplantParams.GenioWideDistance,
                genioAutoImplantParams.GenioWiderDistance
            };

            while (true)
            {
                var result = getOption.Get();
                if (result == GetResult.Option)
                {
                    var currentValue = getOption.Option().CurrentListOptionIndex;

                    if (getOption.OptionIndex() == mandiblePresetOptionIndex)
                    {
                        implantProposalGenioModel.MandibleInterScrewDistance = mandibleDistances[currentValue];
                    }
                    else if (getOption.OptionIndex() == genioPresetOptionIndex)
                    {
                        implantProposalGenioModel.GenioInterScrewDistance = genioDistances[currentValue];
                    }
                    else if (getOption.OptionIndex() == includeMiddlePlateOptionIndex)
                    {
                        implantProposalGenioModel.IncludeMiddlePlate = includeMiddlePlateToggle.CurrentValue;
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid Option index");
                    }
                }
                else if (result == GetResult.Cancel)
                {
                    return false;
                }
                else if (result == GetResult.Nothing)
                {
                    return true;
                }
            }
        }
    }
}
