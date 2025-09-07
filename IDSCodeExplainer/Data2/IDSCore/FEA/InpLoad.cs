namespace IDS.Core.Fea
{
    public class InpLoad
    {
        /// <summary>
        /// Gets or sets the c load axis.
        /// </summary>
        /// <value>
        /// The c load axis.
        /// </value>
        public int Axis { get; }

        /// <summary>
        /// Gets or sets the c load force value.
        /// </summary>
        /// <value>
        /// The c load force value.
        /// </value>
        public double ForceValue { get; }

        /// <summary>
        /// Gets or sets the name of the c load n set.
        /// </summary>
        /// <value>
        /// The name of the c load n set.
        /// </value>
        public string NSetName { get; }

        public InpLoad(string nSetName, int axis, double forceValue)
        {
            NSetName = nSetName;
            Axis = axis;
            ForceValue = forceValue;
        }
    }
}