using IDS.CMF.FileSystem;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace IDS.CMF.Utilities
{
    public class ToggleTransparencyComponentJsonParser
    {
        public struct JsonLayerTransparency
        {
            public string Part { get; set; }
            public string SubLayer { get; set; }
            public double? ImplantDesignTransparencyOn { get; set; }
            public double? ImplantDesignTransparencyOff { get; set; }
            public double? GuideDesignTransparencyOn { get; set; }
            public double? GuideDesignTransparencyOff { get; set; }
        }

        public List<ToggleTransparencyBlock> LoadTransparencyInfo()
        {
            var resource = new CMFResources();
            var jsonText = File.ReadAllText(resource.ToggleTransparencyJsonFile);
            var blocksNodes = JsonConvert.DeserializeObject<List<JsonLayerTransparency>>(jsonText);

            var blocks = new List<ToggleTransparencyBlock>();
            foreach (var blockNode in blocksNodes)
            {
                blocks.Add(new ToggleTransparencyBlock
                {
                    PartNamePattern = blockNode.Part,
                    SubLayer = blockNode.SubLayer,
                    ImplantDesignTransparencyOn = blockNode.ImplantDesignTransparencyOn,
                    ImplantDesignTransparencyOff = blockNode.ImplantDesignTransparencyOff,
                    GuideDesignTransparencyOn = blockNode.GuideDesignTransparencyOn,
                    GuideDesignTransparencyOff = blockNode.GuideDesignTransparencyOff
                });
            }
            return blocks;
        }
    }
}
