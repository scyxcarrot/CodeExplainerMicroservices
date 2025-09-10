namespace IDS.CMF.Utilities
{
    public struct ToggleTransparencyBlock
    {
        public string PartNamePattern { get; set; }
        public string SubLayer { get; set; }
        public double? ImplantDesignTransparencyOn { get; set; }
        public double? ImplantDesignTransparencyOff { get; set; }
        public double? GuideDesignTransparencyOn { get; set; }
        public double? GuideDesignTransparencyOff { get; set; }
    }
}
