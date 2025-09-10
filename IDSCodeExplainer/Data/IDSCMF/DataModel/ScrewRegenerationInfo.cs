using System;

namespace IDS.CMF.DataModel
{
    /// <summary>
    /// When moving control points (dot pastille), the screw regeneration info is used to regenerate the screw
    /// Encapsulates all information needed to regenerate a screw building block
    /// </summary>
    public class ScrewRegenerationInfo
    {
        public DotPastille Pastille { get; set; }
        public int ScrewIndex { get; set; }
        public int ScrewGroupIndex { get; set; }

        /// <summary>
        /// Gets the old screw ID from the pastille
        /// </summary>
        public Guid OldScrewId => Pastille?.Screw?.Id ?? Guid.Empty;

        public ScrewRegenerationInfo(DotPastille pastille, int screwIndex, int screwGroupIndex)
        {
            Pastille = pastille;
            ScrewIndex = screwIndex;
            ScrewGroupIndex = screwGroupIndex;
        }
    }
}