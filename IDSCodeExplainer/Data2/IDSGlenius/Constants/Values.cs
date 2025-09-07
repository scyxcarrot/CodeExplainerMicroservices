using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDS.Glenius.Constants
{
    public static class Plate
    {
        public const double BasePlateThickness = 1.5; // 0.5mm
        public const double MetalBackingPlaneOffsetFromHead = 1.5;
    }

    public static class Transparency
    {
        public const double Invisible = 1.0;
        public const double High = 0.75;
        public const double Medium = 0.5;
        public const double Low = 0.25;
        public const double Opaque = 0.0;
    }

    public static class AnatomicalSide
    {
        public const string Left = "left";
        public const string Right = "right";
        public const string LeftAbbr = "L";
        public const string RightAbbr = "R";
    }
}
