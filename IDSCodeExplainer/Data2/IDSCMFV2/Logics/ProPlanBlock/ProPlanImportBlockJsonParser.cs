using IDS.CMF.DataModel;
using IDS.CMF.V2.FileSystem;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace IDS.CMF.V2.Logics
{
    public class ProPlanImportBlockJsonParser
    {
        public struct ProPlanImportJsonBlock
        {
            public string Part { get; set; }
            public List<int> Color { get; set; }
            public ProPlanImportPartType PartType { get; set; }
            public string SubLayer { get; set; }
            public bool IsImplantPlacable { get; set; }
            public bool IsDefaultAnatomicalObstacle { get; set; }
            public bool ImportInIDS { get; set; }
        }

        public List<ProPlanImportBlock> LoadBlocks()
        {
            var resource = new CMFResourcesV2();
            var jsonText = File.ReadAllText(resource.ProPlanImportJsonFile);

            var blocks = new List<ProPlanImportBlock>();

            var blocksNodes = JsonConvert.DeserializeObject<List<ProPlanImportJsonBlock>>(jsonText);
            foreach (var blockNode in blocksNodes)
            {
                blocks.Add(new ProPlanImportBlock
                {
                    PartNamePattern = blockNode.Part,
                    Color = GetColor(blockNode.Color),
                    PartType = blockNode.PartType,
                    SubLayer = blockNode.SubLayer,
                    IsImplantPlacable = blockNode.IsImplantPlacable,
                    IsDefaultAnatomicalObstacle = blockNode.IsDefaultAnatomicalObstacle,
                    ImportInIDS = blockNode.ImportInIDS
                });
            }

            return blocks;
        }

        private Color GetColor(List<int> values)
        {
            var r = values[0];
            var g = values[1];
            var b = values[2];
            return Color.FromArgb(r, g, b);
        }
    }
}
