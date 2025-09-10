using IDS.CMF.FileSystem;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace IDS.CMF.DataModel

{
    public enum ImplantTemplateConnectionType
    {
        DefaultConnection,

        [XmlEnum("Plate")]
        Plate,

        [XmlEnum("Link")]
        Link,
    }

    [XmlType("ImplantTemplateSegmentedBone")]
    public class ImplantTemplateSegmentedBone
    {
        [XmlAttribute("X")]
        public int X { get; set; }

        [XmlAttribute("Y")]
        public int Y { get; set; }

        [XmlAttribute("Width")]
        public int Width { get; set; }

        [XmlAttribute("Height")]
        public int Height { get; set; }
    }

    [XmlType("Screw")]
    public class ImplantTemplateScrew
    {
        [XmlAttribute("X")]
        public int X { get; set; }

        [XmlAttribute("Y")]
        public int Y { get; set; }
    }

    [XmlType("Connection")]
    public class ImplantTemplateConnection
    {
        [XmlAttribute("A")]
        public int A { get; set; }

        [XmlAttribute("B")]
        public int B { get; set; }

        [XmlAttribute("Type")]
        public ImplantTemplateConnectionType Type { get; set; }
    }

    [XmlType("ImplantTemplate")]
    public class ImplantTemplateDataModel
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Guid")]
        public string TemplateGuid { get; set; }

        [XmlArray("SegmentedBones")]
        [XmlArrayItem("ImplantTemplateSegmentedBone")]
        public List<ImplantTemplateSegmentedBone> SegmentedBones { get; set; }

        [XmlArray("Screws")]
        [XmlArrayItem("Screw")]
        public List<ImplantTemplateScrew> Screws { get; set; }

        [XmlArray("Connections")]
        [XmlArrayItem("Connection")]
        public List<ImplantTemplateConnection> Connections { get; set; }
    }

    [XmlType("ImplantTemplatesGroup")]
    public class ImplantTemplateGroupDataModel
    {
        [XmlAttribute("ImplantType")]
        public string ImplantType { get; set; }

        [XmlArray("ImplantTemplates")]
        [XmlArrayItem("ImplantTemplate")]
        public List<ImplantTemplateDataModel> ImplantTemplates { get; set; }
    }

    [XmlRoot(ElementName = "ImplantTemplatesGroups")]
    public class ImplantTemplateGroupsDataModel
    {
        [XmlElement("ImplantTemplatesGroup")]
        public List<ImplantTemplateGroupDataModel> ImplantTemplatesGroups { get; set; }
    }

    public static class ImplantTemplateGroupsDataModelManager
    {
        private static ImplantTemplateGroupsDataModel _instance;

        public static ImplantTemplateGroupsDataModel Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = LoadImplantTemplate();

                if (_instance != null)
                {
                    var resource = new CMFResources();
                    var implantTemplateDataModelValidator = new ImplantTemplateDataModelValidator(true);
                    if (implantTemplateDataModelValidator.IsValidImplantTemplateXml(resource.ImplantTemplateXmlPath) &&
                        implantTemplateDataModelValidator.IsValidImplantTemplateGroupsDataModel(_instance))
                    {
                        return _instance;
                    }
                }

                _instance = new ImplantTemplateGroupsDataModel()
                {
                    ImplantTemplatesGroups = new List<ImplantTemplateGroupDataModel>()
                };

                return _instance;
            }
        }

        public static ImplantTemplateGroupsDataModel LoadImplantTemplate()
        {
            try
            {
                var resource = new CMFResources();

                using (var fileStream = File.OpenRead(resource.ImplantTemplateXmlPath))
                {
                    var serializer = new XmlSerializer(typeof(ImplantTemplateGroupsDataModel));
                    return (ImplantTemplateGroupsDataModel)serializer.Deserialize(fileStream);
                }
            }
            catch (Exception e)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to load implant template due to exception thrown: {e.Message}");
            }

            return null;
        }

        public static ImplantTemplateDataModel FindImplantTemplate(string implantType, string implantTemplateId)
        {
            foreach (var implantTemplateGroupDataModel in Instance.ImplantTemplatesGroups)       
            {
                if (string.Equals(implantTemplateGroupDataModel.ImplantType, implantType, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var implantTemplateDataModel in implantTemplateGroupDataModel.ImplantTemplates)
                    {
                        if (implantTemplateDataModel.TemplateGuid == implantTemplateId)
                        {
                            return implantTemplateDataModel;
                        }
                    }
                }
            }

            return null;
        }
    }

    public class ImplantTemplateDataModelValidator
    {
        private bool _noXsdError;
        private readonly bool _verbose;

        public ImplantTemplateDataModelValidator(bool verbose = false)
        {
            _noXsdError = false;
            _verbose = verbose;
        }

        public bool IsValidImplantTemplateXml(string implantTemplateFilePath)
        {
            try
            {
                _noXsdError = true;
                var settings = new XmlReaderSettings();
                var resource = new CMFResources();
                settings.Schemas.Add(null, resource.ImplantTemplateXmlXsdPath);
                settings.ValidationType = ValidationType.Schema;
                settings.IgnoreWhitespace = true;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                settings.ValidationEventHandler += XmlSchemaValidationEventHandler;
                
                using (var reader = XmlReader.Create(implantTemplateFilePath, settings))
                {
                    var doc = new XmlDocument();
                    doc.Load(reader);
                }
            }
            catch (Exception e)
            {
                DebuggingLogger(LogCategory.Error, $"Failed to load implant template due to exception thrown: {e.Message}");
                _noXsdError = false;
            }

            return _noXsdError;
        }

        private void XmlSchemaValidationEventHandler(object sender, ValidationEventArgs e)
        {
            switch (e.Severity)
            {
                case XmlSeverityType.Error:
                    DebuggingLogger(LogCategory.Error, e.Message);
                    _noXsdError = false;
                    break;
                case XmlSeverityType.Warning:
                    DebuggingLogger(LogCategory.Warning, e.Message);
                    break;
            }
        }

        private bool IsValidImplantTemplateDataModel(ImplantTemplateDataModel implantTemplateDataModel, List<string> implantTemplateIds)
        {
            var warningMessageHeader = $"[{implantTemplateDataModel.Name}] ";
            var result = true;
            const double tolerance = 0.1;

            if (implantTemplateIds.Contains(implantTemplateDataModel.TemplateGuid))
            {
                DebuggingLogger(LogCategory.Warning, warningMessageHeader + $", template guid had duplicated, {implantTemplateDataModel.TemplateGuid}");
                result = false;
            }

            #region Connection Checking
            var count = 0;
            var screwUsed = Enumerable.Repeat(false, implantTemplateDataModel.Screws.Count).ToArray();
            var maxPointIdx = implantTemplateDataModel.Screws.Count - 1;
            var checkedConnections = new List<ImplantTemplateConnection>();

            foreach (var implantTemplateConnection in implantTemplateDataModel.Connections)
            {
                var connectionWarningMessageHeader = $"Connection [{count++}], ";
                if (implantTemplateConnection.Type == ImplantTemplateConnectionType.DefaultConnection)
                {
                    DebuggingLogger(LogCategory.Warning, warningMessageHeader + connectionWarningMessageHeader +
                                                         "attribute Type is missing");
                    result = false;
                }

                if (implantTemplateConnection.A == implantTemplateConnection.B)
                {
                    DebuggingLogger(LogCategory.Warning, warningMessageHeader + connectionWarningMessageHeader +
                                                         $"A is same as B");
                    result = false;
                }
                else
                {
                    if (implantTemplateConnection.A > maxPointIdx)
                    { 
                        DebuggingLogger(LogCategory.Warning, warningMessageHeader + connectionWarningMessageHeader +
                                                                     $"A(implantTemplateConnection.A) is exceed {maxPointIdx}");
                        result = false;
                    }
                    else
                    {
                        screwUsed[implantTemplateConnection.A] = true;
                    }

                    if (implantTemplateConnection.B > maxPointIdx)
                    {
                        DebuggingLogger(LogCategory.Warning, warningMessageHeader + connectionWarningMessageHeader +
                                                             $"B(implantTemplateConnection.B) is exceed {maxPointIdx}");
                        result = false;
                    }
                    else
                    {
                        screwUsed[implantTemplateConnection.B] = true;
                    }
                }

                foreach (var checkedConnection in checkedConnections)
                {
                    if ((implantTemplateConnection.A == checkedConnection.A && implantTemplateConnection.B == checkedConnection.B) ||
                        (implantTemplateConnection.B == checkedConnection.A && implantTemplateConnection.A == checkedConnection.B))
                    {
                        DebuggingLogger(LogCategory.Warning, warningMessageHeader + connectionWarningMessageHeader +
                                                                       "is a duplicate connection");
                        
                        result = false;
                        break;
                    }
                }
                checkedConnections.Add(implantTemplateConnection);
            }
            #endregion

            #region Screw Checking
            count = 0;
            var checkedScrews = new List<ImplantTemplateScrew>();
            foreach (var implantTemplateScrew in implantTemplateDataModel.Screws)
            {
                var pointWarningMessageHeader = $"Screw [{count++}], ";

                foreach (var checkedScrew in checkedScrews)
                {
                    if ((Math.Abs(implantTemplateScrew.X - checkedScrew.X) <= tolerance) &&
                        (Math.Abs(implantTemplateScrew.Y - checkedScrew.Y) <= tolerance))
                    {
                        DebuggingLogger(LogCategory.Warning, warningMessageHeader + pointWarningMessageHeader +
                                                                       "is a duplicate point");
                        result = false;
                        break;
                    }
                }
                checkedScrews.Add(implantTemplateScrew);
                count++;
            }

            for (var i = 0; i < screwUsed.Length; i++)
            {
                var pointWarningMessageHeader = $"Screw [{i}], ";
                if (!screwUsed[i])
                {
                    DebuggingLogger(LogCategory.Warning, warningMessageHeader + pointWarningMessageHeader +
                                                                   "haven't connect with any other point");
                    result = false;
                }
            }
            #endregion

            return result;
        }

        public bool IsValidImplantTemplateGroupsDataModel(ImplantTemplateGroupsDataModel implantTemplateGroupsDataModel)
        {
            var implantTypeName = new List<string>();
            var implantTemplateIds = new List<string>();
            var result = true;

            foreach (var implantTemplateGroupDataModel in implantTemplateGroupsDataModel.ImplantTemplatesGroups)
            {
                if (implantTypeName.Contains(implantTemplateGroupDataModel.ImplantType))
                {
                    DebuggingLogger(LogCategory.Warning, "Found duplicate attribute ImplantType on multiple ImplantTemplatesGroup in file, \"ImplantTemplate.xml\"");
                    result = false;
                }

                implantTypeName.Add(implantTemplateGroupDataModel.ImplantType);
                implantTemplateGroupDataModel.ImplantTemplates.ForEach(t =>
                {
                    result &= IsValidImplantTemplateDataModel(t, implantTemplateIds);
                });
            }

            return result;
        }

        // IDSPluginHelper will cause unit test failed
        private void DebuggingLogger(LogCategory category, string message, params object[] formatArgs)
        {
            if (_verbose)
            {
                IDSPluginHelper.WriteLine(category, message, formatArgs);
            }
        }
    }

}

