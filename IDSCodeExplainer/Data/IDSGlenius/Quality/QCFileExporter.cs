using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System.Collections.Generic;

namespace IDS.Glenius.Quality
{
    public abstract class QCFileExporter
    {
        public abstract int DoExport(DocumentType documentType, string outputDirectory, out List<string> failedItems);

        protected void LogError(string entityName, ref List<string> failedItems)
        {
            IDSPluginHelper.WriteLine(LogCategory.Warning, "The {0} fails to be generated. Please review.", entityName);
            failedItems.Add(entityName);
        }
    }
}
