using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;

namespace IDS.PICMF.Operations
{
    public class BatchInvertedRotateImplantScrewManager
    {
        private readonly List<InvertedRotateImplantScrew> _invertedRotateOperations;

        public BatchInvertedRotateImplantScrewManager(IEnumerable<Screw> screws, Dictionary<CasePreferenceDataModel, RhinoObject> implantSupportsInvolved)
        {
            _invertedRotateOperations = new List<InvertedRotateImplantScrew>();

            foreach (var screw in screws)
            {
                var objectManager = new CMFObjectManager(screw.Director);
                var casePreference = objectManager.GetCasePreference(screw);
                var implantSupportRhSupport = implantSupportsInvolved[casePreference];

                var rotationCenterPastille =
                    ScrewUtilities.FindDotTheScrewBelongsTo(screw, casePreference.ImplantDataModel.DotList);

                var operation = new InvertedRotateImplantScrew(screw, RhinoPoint3dConverter.ToPoint3d(rotationCenterPastille.Location),
                    -RhinoVector3dConverter.ToVector3d(rotationCenterPastille.Direction), true)
                {
                    ConstraintMesh =
                        ImplantCreationUtilities.GetImplantRoIVolumeWithoutCheck(objectManager, casePreference,
                            ref implantSupportRhSupport),
                    OldImplantDataModel = casePreference.ImplantDataModel.Clone() as ImplantDataModel
                };

                _invertedRotateOperations.Add(operation);
            }
        }

        public Result RotateAllScrews()
        {
            var get = new GetPoint();
            var parameters = CMFPreferences.GetScrewAspectParameters().ScrewAngulationParams;
            var maxScrewAngulationInDegrees = parameters.StandardAngleInDegrees;

            get.SetCommandPrompt("Click on a point to rotate, all selected screws head will follow the reference point.");
            get.PermitObjectSnap(false);
            get.AcceptNothing(true); // accept ENTER to confirm
            get.EnableTransparentCommands(false);

            var cancelled = false;
            while (true)
            {
                get.ClearCommandOptions();
                var optionToggle = new OptionToggle(maxScrewAngulationInDegrees == parameters.StandardAngleInDegrees,
                    parameters.MaximumAngleInDegrees.ToString(), parameters.StandardAngleInDegrees.ToString());
                var screwAngulationIndex = get.AddOptionToggle("maxScrewAngulationInDegrees", ref optionToggle);

                _invertedRotateOperations.ForEach(implantScrew => implantScrew.ExternalRotateBegin(get, maxScrewAngulationInDegrees));

                var getRes = get.Get(); // function only returns after clicking
                if (getRes == GetResult.Cancel)
                {
                    cancelled = true;
                    break;
                }

                if (getRes == GetResult.Option)
                {
                    if (get.OptionIndex() == screwAngulationIndex)
                    {
                        maxScrewAngulationInDegrees = optionToggle.CurrentValue
                            ? parameters.StandardAngleInDegrees
                            : parameters.MaximumAngleInDegrees;
                    }

                    _invertedRotateOperations.ForEach(implantScrew => implantScrew.ExternalRotateEnd(get));

                    continue;
                }

                if (getRes != GetResult.Point)
                {
                    continue;
                }

                break;
            }

            if (!cancelled)
            {
                _invertedRotateOperations.ForEach(implantScrew => implantScrew.ExternalRotate(get.Point()));
            }

            _invertedRotateOperations.ForEach(implantScrew => implantScrew.ExternalRotateEnd(get));

            return cancelled ? Result.Cancel : Result.Success;
        }
    }
}
