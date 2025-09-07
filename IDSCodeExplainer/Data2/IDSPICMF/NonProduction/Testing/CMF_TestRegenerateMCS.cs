using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ExternalTools;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using System.Windows;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("A66AD782-2EBE-48A8-872A-8AA51118804B")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ProPlanImport)]
    public class CMF_TestRegenerateMCS : CmfCommandBase
    {
        public CMF_TestRegenerateMCS()
        {
            Instance = this;
        }

        public static CMF_TestRegenerateMCS Instance { get; private set; }

        public override string EnglishName => "CMF_TestRegenerateMCS";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var sppcFilePath = FileUtilities.GetFileDir("Please select an SPPC file", "SPPC files (*.sppc)|*.sppc||", string.Empty);

            if (string.IsNullOrEmpty(sppcFilePath))
            {
                return Result.Cancel;
            }

            var proPlanPlanesExtractor = new ProPlanPlanesExtractor(new IDSRhinoConsole());
            if (!proPlanPlanesExtractor.GetPlanesFromSppc(sppcFilePath))
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Extract Planes failed.");
                return Result.Failure;
            }

            director.MedicalCoordinateSystem = new MedicalCoordinateSystem(
                proPlanPlanesExtractor.SagittalPlane.ToRhinoPlane(), 
                proPlanPlanesExtractor.AxialPlane.ToRhinoPlane(), 
                proPlanPlanesExtractor.CoronalPlane.ToRhinoPlane(),
                proPlanPlanesExtractor.MidSagittalPlane.ToRhinoPlane());
            
            MessageBox.Show("MedicalCoordinateSystem has been updated. Please make sure to save this file!", "MedicalCoordinateSystem", MessageBoxButton.OK, MessageBoxImage.Exclamation);

            return Result.Success;
        }
    }

#endif
}
