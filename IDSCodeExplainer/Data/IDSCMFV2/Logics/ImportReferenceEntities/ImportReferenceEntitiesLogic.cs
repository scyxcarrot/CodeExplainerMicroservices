using IDS.Interface.Logic;
using IDS.Interface.Tools;

namespace IDS.CMF.V2.Logics
{
    public class ImportReferenceEntitiesLogic : ImportBaseLogic<ImportReferenceEntitiesParameters, ImportReferenceEntitiesResults>
    {
        public ImportReferenceEntitiesLogic(IConsole console, IImportReferenceEntitiesHelper logicHelper) : base(console, logicHelper)
        {

        }

        protected override LogicStatus OnExecute(ImportReferenceEntitiesParameters parameters, out ImportReferenceEntitiesResults result)
        {
            var success = ImportStl(parameters, out var importResult);

            result = new ImportReferenceEntitiesResults
            {
                Meshes = importResult.Meshes
            };
            return success;
        }
    }
}