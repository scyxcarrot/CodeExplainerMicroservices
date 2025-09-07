using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;

namespace IDS.CMF.V2.DataModel
{
    public class TeethBlockCreatorInput
    {
        public IConsole Console { get; set; }
        public Dictionary<Guid, IMesh> LimitingSurfaces { get; set; }
        public Dictionary<Guid, IMesh> TeethCast { get; set; }
        public Dictionary<Guid, IMesh> BracketRegions { get; set; }
        public Dictionary<Guid, IMesh> ReinforcementRegions { get; set; }
        public Dictionary<Guid, IMesh> TeethBaseRegions { get; set; }
    }
}
