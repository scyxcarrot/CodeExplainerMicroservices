using IDS.Interface.Logic;
using IDS.Interface.Tools;

namespace IDS.CMF.V2.Logics
{
    public class ImportTeethBlockLogic : ImportBaseLogic<ImportTeethBlockParameters, ImportTeethBlockResults>
    {
        public ImportTeethBlockLogic(IConsole console, IImportTeethBlockHelper logicHelper) : base(console, logicHelper)
        {

        }

        protected override LogicStatus OnExecute(ImportTeethBlockParameters parameters, out ImportTeethBlockResults result)
        {
            var success = ImportStl(parameters,
                out var importResult);

            result = new ImportTeethBlockResults()
            {
                Meshes = importResult.Meshes
            };
            return success;
        }
    }
}