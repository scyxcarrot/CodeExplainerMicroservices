using IDS.Core.V2.Logic;
using IDS.Core.V2.Utilities;
using IDS.Interface.Logic;
using IDS.Interface.Tools;

using System.Linq;

namespace IDS.CMF.V2.Logics
{
    public abstract class ImportBaseLogic<TParams, TResult> :
        Logic<TParams, TResult>
        where TParams : LogicParameters
        where TResult : LogicResult
    {
        protected ImportBaseLogic(IConsole console, IImportBaseHelper<TParams, TResult> logicHelper) : base(console, logicHelper)
        {

        }

        public LogicStatus ImportStl(ImportBaseParameters parameters, out ImportBaseResults result)
        {
            result = new ImportBaseResults();
            var filenameMeshDict = ImportUtilities.ImportStlWithFilenameDict(parameters.FileNames);

            if (!filenameMeshDict.Any())
            {
                console.WriteErrorLine("STL files failed to be imported.");
                return LogicStatus.Failure;
            }

            result.Meshes = filenameMeshDict;

            return LogicStatus.Success;
        }
    }
}