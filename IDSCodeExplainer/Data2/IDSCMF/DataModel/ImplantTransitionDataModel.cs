using Rhino.Geometry;
using System;

namespace IDS.CMF.DataModel
{
    public class ImplantTransitionDataModel
    {
        public ImplantTransitionInputCurveDataModel CurveA { get; set; }
        public ImplantTransitionInputCurveDataModel CurveB { get; set; }
    }

    public class ImplantTransitionInputCurveDataModel
    {
        //Derived object can be:
        //Mode [Margin]: ImplantMargin
        //Mode [Cut]: ImplantSupportTransitionGuidingOutline
        //Mode [Bone]: ProPlanImport (ImplantPlacableBone)
        public Guid DerivedObjectGuid { get; set; }

        public Curve FullCurve { get; set; }

        public Curve TrimmedCurve { get; set; }
    }
}
