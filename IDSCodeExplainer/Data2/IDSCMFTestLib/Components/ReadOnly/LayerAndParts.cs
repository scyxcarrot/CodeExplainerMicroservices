using System.Collections.Generic;

namespace IDS.CMF.TestLib.Components
{
    public class LayerAndParts
    {
        public string Layer { get; }
        public Dictionary<string, List<string>> Parts { get; }

        public LayerAndParts(string layer, Dictionary<string, List<string>> parts)
        {
            Layer = layer;
            Parts = parts;
        }
    }
}
