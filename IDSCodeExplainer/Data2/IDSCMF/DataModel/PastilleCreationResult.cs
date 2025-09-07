using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public class PastilleCreationResult
    {
        public Mesh FinalPastille { get; set; }
        public Mesh IntermediatePastille { get; set; }
        public Mesh IntermediateLandmark { get; set; }
        public Mesh PastilleCylinder { get; set; }
        public Guid DotPastilleId { get; set; }
        public string PreviousCreationAlgoMethod { get; set; }
        public List<string> ErrorMessages { get; set; }
        public bool Success { get; set; }
    }
}
