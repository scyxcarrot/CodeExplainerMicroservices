using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Loader;
using IDS.CMF.V2.SystemInteraction;
using IDS.CMF.V2.Utilities;
using IDS.Core.V2.Logic;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Loader;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IDS.CMF.V2.Logics
{
    public class ImportPreopsLogic: LogicV2<BlankImportPreopsContext>
    {
        public ImportPreopsLogic(IConsole console) : base(console)
        {
        }

        protected virtual IPreopLoader GetLoader(string filePath)
        {
            var factory = new PreopLoaderFactory();
            return factory.GetLoader(console, filePath);
        }

        public override LogicStatus Execute(BlankImportPreopsContext context)
        {
            var filePath = context.FilePath;

            if (filePath == string.Empty)
            {
                return LogicStatus.Failure;
            }

            if (!DirectoryStructureV2.CheckDirectoryIntegrity(Path.GetDirectoryName(filePath),
                    new List<string>() { "inputs", "extrainputs", "extra_inputs" },
                    new List<string>(), new List<string>() { "3dm" }, out var errorTitle, out var errorMessage))
            {
                context.ShowErrorMessage(errorTitle, errorMessage);
                return LogicStatus.Failure;
            }

            var confirmationScrewBrandSurgeryParameter = context.ConfirmationScrewBrandSurgery;
            if (confirmationScrewBrandSurgeryParameter.Status != LogicStatus.Success)
            {
                return confirmationScrewBrandSurgeryParameter.Status;
            }

            var screwBrandSurgeryParameter = confirmationScrewBrandSurgeryParameter.Parameter;
            context.UpdateScrewBrandSurgery(screwBrandSurgeryParameter.ScrewBrand,
                screwBrandSurgeryParameter.SurgeryType);

            var preopLoader = GetLoader(context.FilePath);
#if (INTERNAL)
            var timer = new Stopwatch();
            timer.Start();
#endif
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Check Preops
            var preLoadData = preopLoader.PreLoadPreop();

            stopwatch.Stop();
            context.TrackingInfo.AddTrackingParameterSafely("PreLoad ProPlan", StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            if (preLoadData == null)
            {
                preopLoader.CleanUp();
                return LogicStatus.Failure;
            }

            if (!HasAllBoneTypes(preLoadData))
            {
                preopLoader.CleanUp();
                return LogicStatus.Failure;
            }

            var proceed = context.AskConfirmationToProceed(preLoadData);
            if (!proceed)
            {
                preopLoader.CleanUp();
                return LogicStatus.Failure;
            }

            // Load Preop
            stopwatch.Restart();
            var preopData = preopLoader.ImportPreop();
            stopwatch.Stop();
            context.TrackingInfo.AddTrackingParameterSafely("Import ProPlan", StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            if (preopData == null)
            {
                preopLoader.CleanUp();
                return LogicStatus.Failure;
            }

            stopwatch.Restart();

            // Load Planes
            if (!preopLoader.GetPlanes(out var sagittalPlane, out var axialPlane, out var coronalPlane, out var midSagittalPlane))
            {
                console.WriteErrorLine("Extract Planes failed.");
                preopLoader.CleanUp();
                return LogicStatus.Failure;
            }

            stopwatch.Stop();
            context.TrackingInfo.AddTrackingParameterSafely("Extract Planes",
                StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            preopLoader.CleanUp();
            context.SagittalPlane = sagittalPlane;
            context.AxialPlane = axialPlane;
            context.CoronalPlane = coronalPlane;
            context.MidSagittalPlane = midSagittalPlane;

            context.AddProPlanParts(preopData);

            // Saves Osteotomy Handler to building block for MCS file
            if (preopLoader.GetOsteotomyHandler(out var osteotomyHandler))
            {
                context.AddOsteotomyHandlerToBuildingBlock(osteotomyHandler);
            }

#if (INTERNAL)
            timer.Stop();
            console.WriteLine($"Time spent LoadPreOp { (timer.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) } seconds");
#endif

            // This is a workaround and it should be removed in the near future (Tech Debt - REQUIREMENT 1144220)
            if (Path.GetExtension(filePath).ToLower() == ".mcs")
            {
                context.DuplicateOriginalToPlannedPart("01MAN_remaining_L");
                context.DuplicateOriginalToPlannedPart("01MAN_remaining_R");
            }

            return context.PostProcessData();
        }

        private bool HasAllBoneTypes(List<IPreopLoadResult> preLoadData)
        {
            var boneType = ProPlanImportUtilitiesV2.GetBoneTypes(preLoadData.Select(T => T.Name).ToArray());
            var preOpExist = boneType.Contains(ProplanBoneType.Preop);
            var originalExist = boneType.Contains(ProplanBoneType.Original);
            var plannedExist = boneType.Contains(ProplanBoneType.Planned);

            if (!preOpExist || !originalExist || !plannedExist)
            {
                console.WriteErrorLine("The following layer(s) has empty parts:" +
                                                            $"{(!preOpExist ? " \"Pre-op\"" : null)}{(!originalExist ? " \"Original\"" : null)}{(!plannedExist ? " \"Planned\"" : null)}");

                return false;
            }

            return true;
        }
    }
}
