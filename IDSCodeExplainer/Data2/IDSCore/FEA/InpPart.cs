using System.Collections.Generic;

namespace IDS.Core.Fea
{
    public class InpPart
    {
        /// <summary>
        /// Gets or sets the elements.
        /// </summary>
        /// <value>
        /// The elements.
        /// </value>
        public List<int[]> Elements { get; set; }

        /// <summary>
        /// Gets or sets the type of the element.
        /// </summary>
        /// <value>
        /// The type of the element.
        /// </value>
        public string ElementType { get; set; }

        /// <summary>
        /// Gets or sets the name of the element set.
        /// </summary>
        /// <value>
        /// The name of the element set.
        /// </value>
        public string ElementSetName { get; set; }

        /// <summary>
        /// Gets or sets the nodes.
        /// </summary>
        /// <value>
        /// The nodes.
        /// </value>
        public List<double[]> Nodes { get; set; }

        /// <summary>
        /// Gets or sets the name of the part.
        /// </summary>
        /// <value>
        /// The name of the part.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InpPart"/> class.
        /// </summary>
        public InpPart()
        {
            Elements = new List<int[]>();
            ElementSetName = string.Empty;
            ElementType = string.Empty;
            Nodes = new List<double[]>();
            Name = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InpPart"/> class.
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="name">The name.</param>
        /// <param name="elementSetName">Name of the element set.</param>
        /// <param name="elementType">Type of the element.</param>
        public InpPart(List<double[]> nodes, List<int[]> elements, string name, string elementSetName, string elementType) : this()
        {
            Elements = elements;
            ElementSetName = elementSetName;
            ElementType = elementType;
            Nodes = nodes;
            Name = name;
        }
    }
}
