using IDS.Core.V2.Logic;
using IDS.Interface.Geometry;

using System.Collections.Generic;

namespace IDS.CMF.V2.Logics
{
    public class ImportBaseResults : LogicResult
    {
        public Dictionary<string, IMesh> Meshes { get; set; }
    }
}