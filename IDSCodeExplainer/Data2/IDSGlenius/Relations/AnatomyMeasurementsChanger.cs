using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;

namespace IDS.Glenius.Relations
{
    public class AnatomyMeasurementsChanger
    {
        public event EventHandler<CustomUndoEventArgs> UndoneRedone;

        private readonly GleniusImplantDirector director;
        private readonly string description;

        public AnatomyMeasurementsChanger(GleniusImplantDirector director, string description)
        {
            this.director = director;
            this.description = description;
        }

        public void SubscribeUndoRedoEvent(RhinoDoc document)
        {
            var defaultValue = director.DefaultAnatomyMeasurements;
            var currentValue = director.AnatomyMeasurements;
            document.AddCustomUndoEvent(description, OnUndoRedo, new List<AnatomicalMeasurements> { defaultValue, currentValue});
        }

        private void OnUndoRedo(object sender, CustomUndoEventArgs e)
        {
            SubscribeUndoRedoEvent(e.Document);

            var list = (List<AnatomicalMeasurements>)e.Tag;
            var previousDefaultValue = list[0];
            var previousValue = list[1];
            Change(previousDefaultValue, previousValue, sender, e);
        }

        private void Change(AnatomicalMeasurements defaultValue, AnatomicalMeasurements currentValue, object sender, CustomUndoEventArgs e)
        {
            director.DefaultAnatomyMeasurements = defaultValue;
            director.AnatomyMeasurements = currentValue;
            UndoneRedone?.Invoke(sender, e);
        }
    }
}