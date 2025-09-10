using System;
using System.Collections.Generic;

namespace IDS.CMF.V2.Logics
{
    public static class InputFileTypeConverter
    {
        private static readonly Dictionary<InputFileType, string> _designInputFileTypeMap 
            = new Dictionary<InputFileType, string> { 
                { InputFileType.FileTypeNotSet, "N/A"},
                { InputFileType.SppcFile, "ProPlan" },
                { InputFileType.EnlightMcsFile, "Enlight CMF" }
            };

        public static string GetStringValue(InputFileType inputFileType)
        {
            if (!_designInputFileTypeMap.TryGetValue(inputFileType, out var inputFileTypeString))
            {
                throw new ArgumentException("Invalid InputFileType value." +
                                            $"The value passed was {inputFileType} ");
            }
            return inputFileTypeString;
        }
    }
}
