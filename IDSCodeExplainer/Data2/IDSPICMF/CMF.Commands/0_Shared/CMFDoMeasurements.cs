using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Operations;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("C4110EF1-3E55-405E-9A57-A22D8CD50FFD")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMFDoMeasurements : CmfCommandBase
    {
        static CMFDoMeasurements _instance;
        public CMFDoMeasurements()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFDoMeasurements command.</summary>
        public static CMFDoMeasurements Instance => _instance;

        public override string EnglishName => IDS.CMF.Constants.CommandEnglishName.CMFDoMeasurements;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var measurementCreator = new MeasurementCreator(director);
            var success = measurementCreator.Execute();

            if (!success)
            {
                return Result.Cancel;
            }

            return Result.Success;
        }

    }

}