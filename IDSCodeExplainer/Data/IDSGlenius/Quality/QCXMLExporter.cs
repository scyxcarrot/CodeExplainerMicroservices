using IDS.Core.Enumerators;
using IDS.Glenius.Operations;
using System.Collections.Generic;
using System.IO;

namespace IDS.Glenius.Quality
{
    public class QCXMLExporter : QCFileExporter
    {
        private readonly GleniusImplantDirector director;
        private readonly ImplantDerivedEntities implantDerivedEntities;
        private readonly ImplantFileNameGenerator generator;

        public QCXMLExporter(GleniusImplantDirector director)
        {
            this.director = director;
            implantDerivedEntities = new ImplantDerivedEntities(director);
            generator = new ImplantFileNameGenerator(director);
        }

        public override int DoExport(DocumentType documentType, string outputDirectory, out List<string> failedItems)
        {
            var totalItems = 0;
            failedItems = new List<string>();

            switch (documentType)
            {
                case DocumentType.ScrewQC:
                case DocumentType.ScaffoldQC:
                    totalItems = ExportXMLsQC(outputDirectory, ref failedItems);
                    break;
                case DocumentType.ApprovedQC:
                    var guideOutputDirectory = Path.Combine(outputDirectory, "Guide");
                    totalItems = ExportXMLsQC(guideOutputDirectory, ref failedItems);
                    var finalizationOutputDirectory = Path.Combine(outputDirectory, "Finalization");
                    totalItems += ExportXMLsQC(finalizationOutputDirectory, ref failedItems);
                    var reportingOutputDirectory = Path.Combine(outputDirectory, "Reporting");
                    totalItems += ExportDesignParameterFile(reportingOutputDirectory, ref failedItems);
                    ExportCoordinateSystem(reportingOutputDirectory, ref totalItems, ref failedItems);
                    break;
                default:
                    break;
            }

            return totalItems;
        }

        private int ExportDesignParameterFile(string outputPath, ref List<string> failedItems)
        {
            var entityName = string.Format($"{director.caseId}_Design_Parameters");
            var fileMaker = new QCDesignParameterFile(director);
            if (!fileMaker.GenerateDesignParameterFile(outputPath, entityName))
            {
                failedItems.Add(entityName);
            }

            return 1;
        }

        private int ExportXMLsQC(string outputDirectory, ref List<string> failedItems)
        {
            var totalItems = 0;
            ExportCoordinateSystem(outputDirectory, ref totalItems, ref failedItems);
            return totalItems;
        }

        //Create the generator for XML file
        private void ExportCoordinateSystem(string outputDirectory, ref int totalItems, ref List<string> failedItems)
        {
            totalItems += 1;
            var entityName = "Coordinate_System";

            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var fileName = generator.GenerateFileName(entityName);
                var xmlPath = outputDirectory + "\\" + fileName + ".xml";

                var xmlDoc = MedicalCoordinateSystemXMLGenerator.GenerateXMLDocument(director);
                xmlDoc.Save(xmlPath);
            }
            catch
            {
                LogError(entityName, ref failedItems);
            }
        }
    }
}
