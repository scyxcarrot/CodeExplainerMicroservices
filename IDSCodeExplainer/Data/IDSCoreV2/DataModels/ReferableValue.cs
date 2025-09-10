namespace IDS.Core.V2.DataModels
{
    public class ReferableValue<T> where T: struct
    {
        public T Value { get; set; }

        public ReferableValue()
        {
            Value = default;
        }

        public ReferableValue(T defaultValue)
        {
            Value = defaultValue;
        }
    }
}
