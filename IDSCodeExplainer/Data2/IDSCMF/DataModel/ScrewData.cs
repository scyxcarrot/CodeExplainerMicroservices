using System;

namespace IDS.CMF.DataModel
{
    public class ScrewData : IScrew
    {
        public static string SerializationLabelConst => "ScrewData";
        public string SerializationLabel { get; set; }

        public Guid Id { get; set; }

        public ScrewData()
        {
            SerializationLabel = SerializationLabelConst;
        }

        public object Clone()
        {
            return new ScrewData
            {
                Id = Id
            };
        }
    }
}
