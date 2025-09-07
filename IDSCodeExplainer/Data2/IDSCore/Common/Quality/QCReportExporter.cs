using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IDS.Core.Quality
{
    /// <summary>
    /// QCReportExporter generates a QC report based on the current state of the design
    /// </summary>
    public abstract class QualityReportExporter
    {
        protected const int Height = 1000;
        protected const int Width = 1000;

        protected DocumentType ReportDocumentType { get; set; }

        // Main method that calls the FillReport and QCReportBuilder
        public virtual void ExportReport(IImplantDirector director, string fullPathAndFilename, IQCResources resources)
        {
            // Fill the ReportDict
            Dictionary<string, string> reportDict;
            var success = FillReport(director, fullPathAndFilename, out reportDict);
            if (!success)
                throw new IDSException("Could not fill report.");

            // Fill the template
            var template = File.ReadAllText(resources.qcDocumentHtmlFile);

            var css = File.ReadAllText(resources.qcDocumentCssFile);
            template = template.Replace("[CSS_STYLE]", css);

            var javascript = File.ReadAllText(resources.qcDocumentJavaScriptFile);
            template = template.Replace("[JAVASCRIPT]", javascript);

            var report = QCReportUtilities.FormatFromDictionary(template, reportDict);

            // Export
            File.WriteAllText(fullPathAndFilename, report);
        }

        // This method fills a dictionary with info for the QC report
        protected abstract bool FillReport(IImplantDirector director, string filename, out Dictionary<string, string> reportValues);

        protected static string CreateJavaScriptArrayOfArrays(string[][] imageStringsMatrix, string arrayName, string subArrayName)
        {
            var javaScriptArray = new StringBuilder($"var {arrayName} = new Array();\n");
            var i = 0;
            foreach (var imageStringsArray in imageStringsMatrix)
            {
                javaScriptArray.Append(CreateJavaScriptArray(imageStringsArray, $"{subArrayName}{i:D}"));
                javaScriptArray.Append($"{arrayName}.push({subArrayName}{i:D});\n");
                i++;
            }

            return javaScriptArray.ToString();
        }

        protected static string CreateJavaScriptArray(string[] imageStringsArray, string arrayName)
        {
            var javaScriptArray = new StringBuilder($"var {arrayName} = new Array();\n");
            foreach (var imageString in imageStringsArray)
            {
                javaScriptArray.Append($"{arrayName}.push('{imageString}');\n");
            }

            return javaScriptArray.ToString();
        }

    }
}