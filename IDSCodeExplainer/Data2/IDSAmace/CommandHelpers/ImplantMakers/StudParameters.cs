namespace IDS.Amace.ImplantBuildingBlocks
{
    // This stuct takes care of the studparameters
    public struct StudParameters
    {
        public readonly double diameter; // (mm) Studd diameter
        public readonly double height; // (mm) Studd height that sticks out of cup surface
        public readonly double roundRad; // (mm) radius of the top rounding
        public readonly double overlap; // (mm) overlap distance for easy boolean
        public readonly double studArcSpace; // (mm) space between 2 studs in arc length
        public readonly double arcBetaSpace;  // (mm) vertical space between 2 studd circles
        public readonly double arcBetaDistFromTop; // (mm)minimum space between horizontal border and closest stud circle

        public StudParameters(double diameter, double height, double roundRad,
                                    double overlap, double studArcSpace, double arcBetaSpace,
                                    double arcBetaDistFromTop)
        {
            this.diameter = diameter;
            this.height = height;
            this.roundRad = roundRad;
            this.overlap = overlap;
            this.studArcSpace = studArcSpace;
            this.arcBetaSpace = arcBetaSpace;
            this.arcBetaDistFromTop = arcBetaDistFromTop;
        }
    }
}