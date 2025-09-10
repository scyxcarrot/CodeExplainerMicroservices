using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Query
{
    public static class Queries
    {
        public static double PastilleDiameter(string screwBrandType, string implantTypeValue, string screwTypeValue)
        {
            var regType = Converter.ToEScrewBrandType(screwBrandType);
            var screwBrandCasePref = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(regType);
            var implant = screwBrandCasePref.Implants.FirstOrDefault(impl => impl.ImplantType == implantTypeValue);
            return implant.Screw.FirstOrDefault(screw => screw.ScrewType == screwTypeValue).PastilleDiameter;
        }

        public static Dictionary<double, string> GetAvailableScrewLengthsDictionary(string screwTypeValue, string screwStyle)
        {
            return new Dictionary<double, string>(GetScrewStyle(screwTypeValue, screwStyle).Lengths);
        }

        public static List<double> GetAvailableScrewLengths(string screwTypeValue, string screwStyle)
        {
            return GetAvailableScrewLengthsDictionary(screwTypeValue, screwStyle).Keys.ToList();
        }

        public static List<double> GetDefaultScrewStyleAvailableScrewLengths(string screwTypeValue)
        {
            return GetDefaultScrewStyleAvailableScrewLengthsDictionary(screwTypeValue).Keys.ToList();
        }

        public static Dictionary<double, string> GetDefaultScrewStyleAvailableScrewLengthsDictionary(string screwTypeValue)
        {
            return new Dictionary<double, string>(GetDefaultScrewStyle(screwTypeValue).Lengths);
        }

        public static string GetDefaultScrewStyleName(string screwTypeValue)
        {
            return GetDefaultScrewStyle(screwTypeValue).Name;
        }

        public static List<string> GetScrewStyleNames(string screwTypeValue)
        {
            return GetScrewStyles(screwTypeValue).Select(s => s.Name).ToList();
        }

        private static ScrewStyle GetDefaultScrewStyle(string screwTypeValue)
        {
            //default to Self-Tapping (unless no such option)
            var selfTappingOption = "Self-Tapping";
            var selfTappingScrewStyle = GetScrewStyle(screwTypeValue, selfTappingOption);

            if (selfTappingScrewStyle != null)
            {
                return selfTappingScrewStyle;
            }

            return GetScrewStyles(screwTypeValue).FirstOrDefault();
        }

        private static ScrewStyle GetScrewStyle(string screwTypeValue, string screwStyle)
        {
            var screwStyleList = GetScrewStyles(screwTypeValue);
            return screwStyleList.FirstOrDefault(s => s.Name.ToLower() == screwStyle.ToLower());
        }

        private static List<ScrewStyle> GetScrewStyles(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            return screwLengthList.Styles;
        }

        public static double GetScrewQCCylinderDiameter(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            return screwLengthList.QCCylinderDiameter;
        }

        public static double GetDefaultForGuideFixationScrewScrewLength(string screwTypeValue, string screwStyle)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);

            // A workaround to define data relation for Guide Fixation Screw and Screw Style
            var availableScrewLengths = GetAvailableScrewLengths(screwTypeValue, screwStyle);
            return GetNearestAvailableScrewLength(availableScrewLengths, screwLengthList.DefaultForGuideFixation);
        }

        public static double GetStampImprintShapeOffset(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            return screwLengthList.StampImprintShapeOffset;
        }

        public static double GetStampImprintShapeWidth(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            return screwLengthList.StampImprintShapeWidth;
        }

        public static double GetStampImprintShapeHeight(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            return screwLengthList.StampImprintShapeHeight;
        }

        public static double GetStampImprintShapeSectionHeightRatio(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            if (screwLengthList.StampImprintShapeSectionHeightRatio > 1)
            {
                return 1;
            }

            if (screwLengthList.StampImprintShapeSectionHeightRatio < 0)
            {
                return 0;
            }

            return screwLengthList.StampImprintShapeSectionHeightRatio;
        }

        public static double GetStampImprintShapeCreationMaxPastilleThickness(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            return screwLengthList.StampImprintShapeCreationMaxPastilleThickness;
        }

        public static double GetNearestAvailableScrewLength(List<double> availableScrewLengths, double currentLength)
        {
            availableScrewLengths.Sort();
            if (currentLength < availableScrewLengths.First())
            {
                return availableScrewLengths.First();
            }

            if (currentLength > availableScrewLengths.Last())
            {
                return availableScrewLengths.Last();
            }

            //match and rounded to the upper limit
            var nearestLength = currentLength;
            foreach (var length in availableScrewLengths)
            {
                if (length < currentLength && Math.Abs(length - currentLength) > 0.0001)
                {
                    continue;
                }

                nearestLength = length;
                break;
            }

            return nearestLength;
        }

        public static string GetScrewTypeForDesignParameter(string currentScrewType)
        {
            var screwType = GetScrewTypeWithoutSymbolAndBarrel(currentScrewType, true);

            var matrixAllCaps = "MATRIX";
            if (screwType.ToUpper().StartsWith(matrixAllCaps))
            {
                // Matrix Screw Types should be in All Caps
                // E.g: Matrix Mandible => MatrixMANDIBLE
                var matrixType = screwType.ToUpper().Replace(matrixAllCaps, string.Empty).Trim();
                screwType = $"{screwType.Substring(0, matrixAllCaps.Length)}{matrixType}";
            }

            var mini = "mini";
            var micro = "micro";
            var crossed = "crossed";
            if (screwType.ToLower().StartsWith(mini) || screwType.ToLower().StartsWith(micro) && screwType.ToLower().Contains(crossed))
            {
                // Converting Crossed to Cross-headed
                // E.g: Mini Crossed => Mini Crossed-headed

                var words = screwType.Split(' ');
                var headTypeStr = words[1];
                if (headTypeStr == "Crossed")
                {
                    headTypeStr = "Cross-headed";
                }

                screwType = $"{words[0]} {headTypeStr}";
            }

            return screwType;
        }

        private static string GetScrewTypeWithoutSymbolAndBarrel(string currentScrewType, bool trimDiameterValue)
        {
            var screwType = currentScrewType;
            var diameterChar = "Ø";
            var diameterCharIndex = currentScrewType.IndexOf(diameterChar);
            if (diameterCharIndex > 0)
            {
                if (trimDiameterValue)
                {
                    screwType = screwType.Substring(0, diameterCharIndex);
                }
                else
                {
                    screwType = screwType.Replace(diameterChar, string.Empty);
                }
            }
            screwType = screwType.Trim();

            var substringsToReplace = new[] { "self-drilling", "self-tapping", "hex", "barrel" };
            foreach (var substring in substringsToReplace)
            {
                var index = screwType.IndexOf(substring, StringComparison.OrdinalIgnoreCase);
                if (index > 0)
                {
                    screwType = screwType.Remove(index, substring.Length);
                }
            }

            screwType = screwType.Trim();
            return screwType;
        }

        public static double GetScrewDiameter(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            return screwLengthList.ScrewDiameter;
        }

        public static double GetGuideVicinityClearance(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            return screwLengthList.GuideVicinityClearance;
        }

        public static Color GetScrewTypeColor(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);

            return screwLengthList != null
                ? Color.FromArgb(screwLengthList.BbColorRed, screwLengthList.BbColorGreen, screwLengthList.BbColorBlue)
                : Color.Red;
        }

        public static ScrewLength GetScrewLength(string screwTypeValue)
        {
            var screwLengthsData = CasePreferencesHelper.LoadScrewLengthData();
            var screwLengthList = screwLengthsData.ScrewLengths.FirstOrDefault(screw => screw.ScrewType == screwTypeValue);

            return screwLengthList;
        }

        public static double GetGuideVicinityClearanceHeight(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            return screwLengthList.GuideVicinityClearanceHeight;
        }

        public static string GetDefaultBarrelTypeName(string screwTypeValue)
        {
            return GetBarrelTypes(screwTypeValue).FirstOrDefault();
        }

        public static List<string> GetBarrelTypes(string screwTypeValue)
        {
            var barrelTypesAndBarrelNamesList = GetBarrelTypesAndBarrelNames(screwTypeValue);
            return barrelTypesAndBarrelNamesList.Keys.ToList();
        }

        public static Dictionary<string, string> GetBarrelTypesAndBarrelNames(string screwTypeValue)
        {
            var screwLengthList = GetScrewLength(screwTypeValue);
            return screwLengthList.BarrelTypesAndBarrelNames;
        }
    }
}
