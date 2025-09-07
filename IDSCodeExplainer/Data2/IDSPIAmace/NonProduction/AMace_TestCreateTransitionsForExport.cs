#if INTERNAL

using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Common;
using IDS.Core.Enumerators;
using IDS.Core.NonProduction;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

namespace IDS.NonProduction.Commands
{
    [System.Runtime.InteropServices.Guid("4a0c0816-1c82-4b0b-b09a-dbc221c0934c")]
    [IDSCommandAttributes(true, DesignPhase.Plate, IBB.ROIContour)]
    public class AMace_TestCreateTransitionsForExport : Command
    {
        static AMace_TestCreateTransitionsForExport _instance;
        public AMace_TestCreateTransitionsForExport()
        {
            _instance = this;
        }

        ///<summary>The only instance of the AMace_TestCreateTransitionsForExport command.</summary>
        public static AMace_TestCreateTransitionsForExport Instance => _instance;

        public override string EnglishName => "AMace_TestCreateTransitionsForExports";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = new ImplantDirector(doc, PlugInInfo.PluginModel);

            var forImplantQc = PlateWithTransitionForExportCreator.CreateForImplantQc(director);

            if (forImplantQc != null)
            {
                InternalUtilities.AddObject(forImplantQc, "ImplantQCExport - Plate Holes with Transition (STL)");
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to create ImplantQCExport - Plate Holes with Transition (STL)");
            }

            var forQcApproved = PlateWithTransitionForExportCreator.CreateForQcApproved(director);

            if (forQcApproved != null)
            {
                InternalUtilities.AddObject(forQcApproved.PlateWithTransitionForReporting, "QCApproved - Plate with Transition (Reporting)");
                InternalUtilities.AddObject(forQcApproved.FlangeTransitionForFinalization, "QCApproved - Flange Transition (Finalization)");
                InternalUtilities.AddObject(forQcApproved.BumpTransitionForFinalization, "QCApproved - Bump Transition (Finalization)");
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "QCApproved - Plate with Transition (Reporting)");
            }

            return Result.Success;
        }
    }
}

#endif