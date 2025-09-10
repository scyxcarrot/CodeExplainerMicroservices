using IDS.Core.ImplantDirector;
using System;

namespace IDS.Core.Enumerators
{
    public class DesignPhaseProperty
    {
        public string Name { get; set; }

        public int Value { get; set; }

        public Func<IImplantDirector, bool> PhaseStopEvent { get; set; }

        public Func<IImplantDirector, bool> StartActionFromUp { get; set; }

        public Func<IImplantDirector, bool> StartActionFromDown { get; set; }

        public Func<IImplantDirector, bool> StartActionBoth {get; set; }

        public Func<IImplantDirector, DesignPhaseProperty, bool> StopAction { get; set; }
    }
}