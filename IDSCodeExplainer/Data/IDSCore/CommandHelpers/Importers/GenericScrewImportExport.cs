using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace IDS.Core.Importer

{
    public static class GenericScrewImportExport
    {
        private const string EntitiesRootNode = "Entities";

        private const string ScrewNode = "Cylinder";
        private const string ScrewNameNode = "Name";
        private const string ScrewHeadNode = "TopPoint";
        private const string ScrewTipNode = "BottomPoint";
        private const string ScrewRadiusNode = "Radius";

        //////////////////////////////////////////////////////////////////////////////////
        //Importer////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////
        public static List<T> ReadScrewXml<T>(string xmlFile, RhinoDoc doc, IReadScrewXmlComponent<T> component) where T : class
        {
            if (component != null)
            {
                // XML parsing variables
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFile);
                var screwList = new List<T>();

                // Parse
                int index = 1;
                foreach (XmlNode nodeCylinder in xmlDoc.SelectNodes("/" + EntitiesRootNode + "/" + ScrewNode))
                {
                    // Get strings
                    string Sname = nodeCylinder.SelectSingleNode(ScrewNameNode).InnerText;
                    string Sheadxyz = nodeCylinder.SelectSingleNode(ScrewHeadNode).InnerText;
                    string Stipxyz = nodeCylinder.SelectSingleNode(ScrewTipNode).InnerText;
                    string Sradius = nodeCylinder.SelectSingleNode(ScrewRadiusNode).InnerText;

                    // Process name
                    var screwNameParts = Sname.Split('_').ToList();

                    if (screwNameParts[0].Equals("Unset"))
                    {
                        screwNameParts.RemoveAt(0);
                    }
                    else
                    {
                        screwNameParts.RemoveRange(0, 2);
                    }

                    // parse head
                    List<string> screwHeadParts = Sheadxyz.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    Point3d head = new Point3d(double.Parse(screwHeadParts[0], CultureInfo.InvariantCulture), 
                        double.Parse(screwHeadParts[1], CultureInfo.InvariantCulture),
                        double.Parse(screwHeadParts[2], CultureInfo.InvariantCulture));

                    // parse tip
                    List<string> screwTipParts = Stipxyz.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    Point3d tip = new Point3d(double.Parse(screwTipParts[0], CultureInfo.InvariantCulture), 
                        double.Parse(screwTipParts[1], CultureInfo.InvariantCulture),
                        double.Parse(screwTipParts[2], CultureInfo.InvariantCulture));

                    T screw;
                    component.OnReadScrewNodeXml(doc, screwNameParts, index, Sradius, head, tip, out screw);

                    //By right screw should not be null
                    if (screw != null)
                    {
                        screwList.Add(screw);
                    }
                    else
                    {
                        throw new IDSOperationFailed("Screw import failed, screw is null");
                    }

                    // increase the index
                    index++;
                }

                return screwList;
            }
            else
            {
                throw new IDSOperationFailed("Screw import failed, ReadScrewNodeXML component is null!");
            }
        }

        //////////////////////////////////////////////////////////////////////////////////
        //Export//////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////

        //FileExporter
        // Export all screws to a csv file for reporting
        public static bool ExportBookletCsv<TImplantDirector, TScrewType, TScrewAideType>
            (IEnumerable<ScrewBase<TImplantDirector, TScrewType, TScrewAideType>> screwList, string saveFolder, string prefix)
            where TImplantDirector : class, IImplantDirector
        {
            string filePath = string.Format("{0}\\{1}_Reporting_Screws.csv", saveFolder, prefix);
            return WriteScrewsCsvForReporting<TImplantDirector, TScrewType, TScrewAideType>(screwList.ToList(), filePath);
        }

        //ExportScrews
        // Export all screws to an xml file for mimics/3-matic import
        public static string ExportMimicsXml<TImplantDirector, TScrewType, TScrewAideType>
            (string caseId, IEnumerable<ScrewBase<TImplantDirector, TScrewType, TScrewAideType>> screwList, string folderPath) 
            where TImplantDirector : class, IImplantDirector
        {
            string suffix = "Exported4Mimics";
            string prefix = caseId;

            return WriteScrewXml<TImplantDirector, TScrewType, TScrewAideType>(screwList.ToList(), folderPath, prefix, suffix);
        }

        // Method that actually writes the screw parametrization to an xml file
        private static string WriteScrewXml<TImplantDirector, TScrewType, TScrewAideType>
            (List<ScrewBase<TImplantDirector, TScrewType, TScrewAideType>> screwList, string folderPath, string filePrefix, string fileSuffix) 
            where TImplantDirector : class, IImplantDirector
        {
            // Open file for writing
            string xmlPath = folderPath + "\\" + filePrefix + "_Screws_" + fileSuffix + ".xml";
            XmlDocument xmlDoc = new XmlDocument();

            // Write header
            XmlDeclaration nodeDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", string.Empty);
            xmlDoc.AppendChild(nodeDeclaration);
            // Open entities tag
            XmlNode nodeEntities = xmlDoc.CreateElement(EntitiesRootNode);
            XmlAttribute attrMaterialise = xmlDoc.CreateAttribute("xmlns", "mat", "http://www.w3.org/2000/xmlns/");
            attrMaterialise.Value = "urn:materialise";
            nodeEntities.Attributes.Append(attrMaterialise);
            xmlDoc.AppendChild(nodeEntities);

            // Loop over all screws and write screw parameterization
            foreach (ScrewBase<TImplantDirector, TScrewType, TScrewAideType> theScrew in screwList)
            {
                if(theScrew != null)
                {
                    // Create screw node
                    XmlNode nodeScrew = xmlDoc.CreateElement(ScrewNode);

                    // Screw name
                    XmlNode nodeName = xmlDoc.CreateElement(ScrewNameNode);
                    nodeName.InnerText = theScrew.GenerateNameForMimics();
                    nodeScrew.AppendChild(nodeName);

                    // Screw radius
                    XmlNode nodeRadius = xmlDoc.CreateElement(ScrewRadiusNode);
                    nodeRadius.InnerText = theScrew.Radius.ToString("F8", CultureInfo.InvariantCulture);
                    nodeScrew.AppendChild(nodeRadius);

                    // Screw head point
                    XmlNode nodeHead = xmlDoc.CreateElement(ScrewHeadNode);
                    nodeHead.InnerText = string.Format(CultureInfo.InvariantCulture, 
                        "{0:F8} {1:F8} {2:F8}", theScrew.HeadPoint.X, theScrew.HeadPoint.Y, theScrew.HeadPoint.Z);
                    nodeScrew.AppendChild(nodeHead);

                    // Screw tip point
                    XmlNode nodeTip = xmlDoc.CreateElement(ScrewTipNode);
                    nodeTip.InnerText = string.Format(CultureInfo.InvariantCulture, 
                        "{0:F8} {1:F8} {2:F8}", theScrew.TipPoint.X, theScrew.TipPoint.Y, theScrew.TipPoint.Z);
                    nodeScrew.AppendChild(nodeTip);

                    // Add to entities
                    nodeEntities.AppendChild(nodeScrew);
                }
                else
                {
                    throw new IDSOperationFailed("Screw import failed");
                }

            }

            xmlDoc.Save(xmlPath);

            // Success
            return xmlPath;
        }

        // Export all screws from a screw list to a csv file for reporting
        private static bool WriteScrewsCsvForReporting<TImplantDirector, TScrewType, TScrewAideType>
            (List<ScrewBase<TImplantDirector, TScrewType, TScrewAideType>> screwList, string filePath) 
            where TImplantDirector : class, IImplantDirector
        {
            // file writer
            System.IO.StreamWriter file = new System.IO.StreamWriter(filePath);

            // Loop over screws and write to csv
            file.WriteLine("sep=,");
            file.WriteLine("Screw,ScrewNumber,UniBi,TotalScrewLength,BoneLength,UntilBoneLength");
            foreach (ScrewBase<TImplantDirector, TScrewType, TScrewAideType> theScrew in screwList)
            {
                // TODO: All of the parameters used for exporting + qc reporting of screws should be
                //       centralized somewhere
                // Get the text line for the reporting csv file for one screw
                Func<ScrewBase<TImplantDirector, TScrewType, TScrewAideType>, string> screwCSVReportLine = (screw) =>
                {
                    // TODO: This is not calculated correctly
                    string uniBi = "Unicortical";
                    if (screw.IsBicortical)
                    {
                        uniBi = "Bicortical";
                    }

                    return string.Format(CultureInfo.InvariantCulture, "{0},{1:D},{2},{3:F0},{4:F0},{5:F0}",
                        screw.GetScrewLetter(), screw.Index, uniBi, screw.TotalLength, screw.GetDistanceInBone(), screw.GetDistanceUntilBone());
                };

                file.WriteLine(screwCSVReportLine(theScrew));
            }
                
            // Close file
            file.Close();

            return true;
        }

    }
}