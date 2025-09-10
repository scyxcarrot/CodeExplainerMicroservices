using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace IDS.CMFImplantCreation.Configurations
{
    internal class Configuration : IConfiguration
    {
        private readonly string _directoryPath;

        public Configuration()
        {
            _directoryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Configurations");
        }

        public PastilleConfiguration GetPastilleConfiguration(string screwType)
        {
            var path = Path.Combine(_directoryPath, "PastilleConfigurations.json");            
            var configuration = File.ReadAllText(path);
            var pastilleConfigs = JsonConvert.DeserializeObject<PastilleConfigurationList>(configuration);
            return pastilleConfigs.Pastilles.First(p => p.ScrewType.ToLower() == screwType.ToLower());
        }

        public OverallImplantParams GetOverallImplantParameter()
        {
            var path = Path.Combine(_directoryPath, "ImplantConfigurations.xml");
            var document = new XmlDocument();
            document.Load(path);

            var nodePath = document.SelectSingleNode("/Preferences/ActualImplant/Overall");
            return new OverallImplantParams()
            {
                WrapOperationSmallestDetails = ExtractDouble(nodePath, "WrapOperation_SmallestDetail"),
                WrapOperationGapClosingDistance = ExtractDouble(nodePath, "WrapOperation_GapClosingDistance"),
                WrapOperationOffset = ExtractDouble(nodePath, "WrapOperation_Offset"),
                FixingIterations = ExtractInteger(nodePath, "FixingIterations")
            };
        }

        public IndividualImplantParams GetIndividualImplantParameter()
        {
            var path = Path.Combine(_directoryPath, "ImplantConfigurations.xml");
            var document = new XmlDocument();
            document.Load(path);

            var nodePath = document.SelectSingleNode("/Preferences/ActualImplant/Individual");
            return new IndividualImplantParams()
            {
                WrapOperationSmallestDetails = ExtractDouble(nodePath, "WrapOperation_SmallestDetail"),
                WrapOperationGapClosingDistance = ExtractDouble(nodePath, "WrapOperation_GapClosingDistance"),
                WrapOperationOffsetInDistanceRatio = ExtractDouble(nodePath, "WrapOperation_Offset_InDistanceRatio"),
                TubeRadiusModifier = ExtractDouble(nodePath, "TubeRadiusModifier")
            };
        }

        public LandmarkImplantParams GetLandmarkImplantParameter()
        {
            var path = Path.Combine(_directoryPath, "ImplantConfigurations.xml");
            var document = new XmlDocument();
            document.Load(path);

            var nodePath = document.SelectSingleNode("/Preferences/ActualImplant/Landmark");
            return new LandmarkImplantParams()
            {
                SquareExtensionFromPastilleCircumference = ExtractDouble(nodePath, "SquareExtensionFromPastilleCircumference"),
                CircleCenterRatioWithPastilleRadius = ExtractDouble(nodePath, "CircleCenterRatioWithPastilleRadius"),
                SquareWidthRatioWithPastilleRadius = ExtractDouble(nodePath, "SquareWidthRatioWithPastilleRadius"),
                CircleRadiusRatioWithPastilleRadius = ExtractDouble(nodePath, "CircleRadiusRatioWithPastilleRadius"),
                TriangleHeightRatioWithDefault = ExtractDouble(nodePath, "TriangleHeightRatioWithDefault")
            };
        }

        private double ExtractDouble(XmlNode nodePath, string key)
        {
            return Convert.ToDouble(nodePath.SelectSingleNode(key)?.InnerText, CultureInfo.InvariantCulture);
        }

        private int ExtractInteger(XmlNode nodePath, string key)
        {
            return Convert.ToInt32(nodePath.SelectSingleNode(key)?.InnerText, CultureInfo.InvariantCulture);
        }
    }
}
