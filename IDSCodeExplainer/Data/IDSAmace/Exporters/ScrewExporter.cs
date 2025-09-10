using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Visualization;
using IDS.Core.DataTypes;
using IDS.Core.Enumerators;
using IDS.Core.Visualization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace IDS.Amace.Operations
{
    // ScrewExporter provides functionality for exporting IDS screws to xml files
    public static class ScrewExporter
    {
        // Export all screws to a csv file for reporting
        public static bool ExportBookletCSV(ImplantDirector director, string saveFolder, string prefix)
        {
            var screwManager = new ScrewManager(director.Document);
            var screwList = screwManager.GetAllScrews().ToList();
            string filePath = $"{saveFolder}\\{prefix}_Reporting_Screws.csv";
            return WriteScrewsCsvForReporting(screwList, filePath);
        }

        // Export all screws from a screw list to a csv file for reporting
        public static void WriteScrewsCsvForGuide(List<Screw> screwList, string filePath)
        {
            // file writer
            var file = new System.IO.StreamWriter(filePath);

            // Loop over screws and write to csv
            file.WriteLine("sep=,");
            file.WriteLine("ScrewNumber,CupYN,Diameter,headX,headY,headZ,tipX,tipY,tipZ");
            foreach (var theScrew in screwList)
            {
                file.WriteLine(ScrewCsvGuideLine(theScrew));
            }

            // Close file
            file.Close();
        }

        // Get the text line for the guide csv file for one screw
        public static string ScrewCsvGuideLine(Screw screw)
        {
            // CupYN parameter
            var cupYn = 0;
            if (screw.positioning == ScrewPosition.Cup)
            {
                cupYn = 1;
            }

            // Make string
            return string.Format(CultureInfo.InvariantCulture, "{0:D},{1:D},{2:F1},{3:F8},{4:F8},{5:F8},{6:F8},{7:F8},{8:F8}", 
                screw.Index, cupYn, screw.Diameter, screw.HeadPoint.X, screw.HeadPoint.Y, screw.HeadPoint.Z, 
                screw.TipPoint.X, screw.TipPoint.Y, screw.TipPoint.Z);
        }

        // Export all screws from a screw list to a csv file for reporting
        public static bool WriteScrewsCsvForReporting(List<Screw> screwList, string filePath)
        {
            // file writer
            var file = new System.IO.StreamWriter(filePath);

            // Loop over screws and write to csv
            file.WriteLine("sep=,");
            file.WriteLine("Screw,ScrewNumber,UniBi,TotalScrewLength,BoneLength,UntilBoneLength");
            foreach (var theScrew in screwList)
            {
                file.WriteLine(ScrewCsvReportLine(theScrew));
            }

            // Close file
            file.Close();

            return true;
        }

        // TODO: All of the parameters used for exporting + qc reporting of screws should be
        //       centralized somewhere

        // Get the text line for the reporting csv file for one screw
        public static string ScrewCsvReportLine(Screw screw)
        {
            // TODO: This is not calculated correctly
            var uniBi = "Unicortical";
            if (screw.IsBicortical)
            {
                uniBi = "Bicortical";
            }

            return string.Format(CultureInfo.InvariantCulture, "{0},{1:D},{2},{3:F0},{4:F0},{5:F0}", GetScrewLetter(screw),
                screw.Index, uniBi, screw.TotalLength, screw.GetDistanceInBone(), screw.GetDistanceUntilBone());
        }

        /// <summary>
        /// Gets the letter.
        /// </summary>
        /// <value>
        /// The letter.
        /// </value>
        private static string GetScrewLetter(Screw screw)
        {
            // First letter
            double indexD = screw.Index;
            var screwChar = Convert.ToChar((screw.Index - 1) % 26 + 65);
            var screwChars = screwChar.ToString();

            // If the index is lower than 26, one character suffices; return the result.
            if (screw.Index <= 26)
            {
                return screwChars;
            }

            // Add more characters for indices larger than 26, larger than 676,...
            var group = (int)Math.Floor((indexD - 1) / 26);
            var i = 2;
            while (group != 0)
            {
                screwChar = Convert.ToChar(group - 1 + 65);
                screwChars = screwChar + screwChars;
                group = (int)Math.Floor((indexD - 1) / (Math.Pow(26, i)));
                i += 1;
            }
            return screwChars;
        }

        // Export all screws to an xml file for mimics/3-matic import
        public static string ExportMimicsXml(ImplantDirector director, string folderPath)
        {
            var screwManager = new ScrewManager(director.Document);
            var screwList = screwManager.GetAllScrews().ToList();
            var suffix = "Exported4Mimics";
            var prefix = director.Inspector.CaseId;

            return WriteScrewXml(screwList, folderPath, prefix, suffix, director.Inspector.CaseId);
        }

        // Export all screws to a csv file for use in the guide tool
        public static string ExportGuideToolCsv(ImplantDirector director, string folderPath)
        {
            var screwManager = new ScrewManager(director.Document);
            var screwList = screwManager.GetAllScrews().ToList();
            var filePath =
                $"{folderPath}\\{director.Inspector.CaseId}_v{director.version:D}_Draft{director.draft:D}_Screws_Exported4GuideTool.csv";
            WriteScrewsCsvForGuide(screwList, filePath);
            return filePath;
        }

        // Export all screws to an xml file for use in the guide tool
        public static string ExportGuideToolXml(ImplantDirector director, string folderPath)
        {
            var screwManager = new ScrewManager(director.Document);
            var screwList = screwManager.GetAllScrews().ToList();
            var suffix = "Exported4GuideTool";
            var prefix = string.Format("{0}_v{2:D}_Draft{1:D}", director.Inspector.CaseId, director.draft, director.version);

            return WriteScrewXml(screwList, folderPath, prefix, suffix, director.Inspector.CaseId);
        }

        // Method that actually writes the screw parametrization to an xml file
        public static string WriteScrewXml(List<Screw> screwList, string folderPath, string filePrefix, string fileSuffix, string caseId)
        {
            // Open file for writing
            var xmlPath = folderPath + "\\" + filePrefix + "_Screws_" + fileSuffix + ".xml";
            var xmlDoc = new XmlDocument();

            // Write header
            var nodeDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", string.Empty);
            xmlDoc.AppendChild(nodeDeclaration);
            // Open entities tag
            var nodeEntities = xmlDoc.CreateElement("Entities");
            var attrMaterialise = xmlDoc.CreateAttribute("xmlns", "mat", "http://www.w3.org/2000/xmlns/");
            attrMaterialise.Value = "urn:materialise";
            nodeEntities.Attributes.Append(attrMaterialise);
            xmlDoc.AppendChild(nodeEntities);

            // Loop over all screws and write screw parameterization
            foreach (var theScrew in screwList)
            {
                // Create cylinder node
                var nodeScrew = WriteCylinderXml(xmlDoc, theScrew, caseId);

                // Add to entities
                nodeEntities.AppendChild(nodeScrew);
            }

            xmlDoc.Save(xmlPath);

            // Success
            return xmlPath;
        }

        public static XmlNode WriteCylinderXml(XmlDocument xmlDoc, Screw screw, string caseId)
        {
            // Create cylinder node
            var nodeScrew = xmlDoc.CreateElement("Cylinder");

            // Screw name
            var nodeName = xmlDoc.CreateElement("Name");
            nodeName.InnerText = GetScrewMimicsName(screw, caseId);
            nodeScrew.AppendChild(nodeName);

            // Screw radius
            var nodeRadius = xmlDoc.CreateElement("Radius");
            nodeRadius.InnerText = screw.Radius.ToString("F8", CultureInfo.InvariantCulture);
            nodeScrew.AppendChild(nodeRadius);

            // Screw head point
            var nodeHead = xmlDoc.CreateElement("TopPoint");
            nodeHead.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8} {1:F8} {2:F8}", screw.HeadPoint.X, screw.HeadPoint.Y, screw.HeadPoint.Z);
            nodeScrew.AppendChild(nodeHead);

            // Screw tip point
            var nodeTip = xmlDoc.CreateElement("BottomPoint");
            nodeTip.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8} {1:F8} {2:F8}", screw.TipPoint.X, screw.TipPoint.Y, screw.TipPoint.Z);
            nodeScrew.AppendChild(nodeTip);

            return nodeScrew;
        }

        /// <summary>
        /// Gets the Mimics name of the screw.
        /// </summary>
        /// <param name="screw">The screw.</param>
        /// <param name="caseId">The case identifier.</param>
        /// <returns></returns>
        private static string GetScrewMimicsName(Screw screw, string caseId)
        {
            return $"{caseId}_{screw.screwBrandType}_{screw.screwAlignment}_{screw.Index:D}";
        }

        // Export a detail of the implant and screws with the screw numbers as shown in the QC doc
        public static void ExportScrewNumberImage(ImplantDirector director, string filename, CameraView cameraView, DocumentType docType)
        {
            const int targetResolution = 1000;

            var img = ScreenshotsScrews.GenerateScrewNumberImage(director.Document, targetResolution, targetResolution, cameraView, ScrewConduitMode.NoWarnings, false, docType);
            img.MakeTransparent(Color.White);
            img.Save(filename);
            img.Dispose();
        }
    }
}