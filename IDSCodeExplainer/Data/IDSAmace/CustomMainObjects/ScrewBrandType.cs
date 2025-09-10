using IDS.Amace.Enumerators;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace IDS.Amace.ImplantBuildingBlocks
{
    public class ScrewBrandType
    {
        public string Brand { get; }

        public double Diameter { get; }

        public ScrewLocking Locking { get; }

        public string Type => GetTypeString();

        public ScrewBrandType(string brand, double diameter, ScrewLocking locking)
        {
            Brand = brand;
            Diameter = diameter;
            Locking = locking;
        }

        public override string ToString()
        {
            return $"{Brand}_{Type}".ToUpper();
        }

        private string GetTypeString()
        {
            return $"D{GetDiameterString(Diameter)}{GetLockingString(Locking)}";
        }

        #region static methods

        public static bool TryParse(string name, out ScrewBrandType brandType)
        {
            brandType = null;
            
            var pattern = new Regex(@"(?<brand>\w+)_D(?<diameter>\d+)(?<locking>\w*)");
            var match = pattern.Match(name);

            if (!match.Success)
            {
                return false;
            }

            var brand = match.Groups["brand"].Value;
            var diameter = GetDiameterValue(match.Groups["diameter"].Value);
            var locking = GetLockingValue(match.Groups["locking"].Value);
            brandType = new ScrewBrandType(brand, diameter, locking);
            return true;
        }

        public static ScrewBrandType Parse(string brand, string type)
        {
            var name = $"{brand}_{type}";
            ScrewBrandType brandType;
            if (!TryParse(name, out brandType))
            {
                throw new Exception($"{name} is invalid.");
            }
            return brandType;
        }

        private static string GetDiameterString(double diameter)
        {
            var numberFormat = new NumberFormatInfo();
            numberFormat.NumberDecimalSeparator = " "; //using string.Empty will cause Exception to be thrown
            var diameterString = diameter.ToString("0.0#", numberFormat);
            diameterString = diameterString.Replace(" ", string.Empty);
            return diameterString;
        }

        private static double GetDiameterValue(string diameterString)
        {
            //diameter should be less than 10mm
            var parsedValue = double.Parse(diameterString);
            var numberOfDigits = Math.Truncate(Math.Log10(parsedValue));
            var diameter = parsedValue / Math.Pow(10, numberOfDigits);
            return diameter;
        }

        private static string GetLockingString(ScrewLocking locking)
        {
            var lockingString = string.Empty;
            switch (locking)
            {
                case ScrewLocking.Locking:
                    lockingString = "L";
                    break;
                case ScrewLocking.NonLocking:
                    lockingString = "NL";
                    break;
            }
            return lockingString;
        }

        private static ScrewLocking GetLockingValue(string lockingString)
        {
            var locking = ScrewLocking.None;
            switch (lockingString.ToUpper())
            {
                case "L":
                    locking = ScrewLocking.Locking;
                    break;
                case "NL":
                    locking = ScrewLocking.NonLocking;
                    break;
            }
            return locking;
        }

        #endregion
    }
}