using IDS.CMF.DataModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.V2.Logics
{
    public class ProPlanImportComponentV2
    {
        public List<ProPlanImportBlock> Blocks { get; private set; }

        public ProPlanImportComponentV2()
        {
            LoadPrePlanImportBlocks();
        }

        private void LoadPrePlanImportBlocks()
        {
            var parser = new ProPlanImportBlockJsonParser();
            Blocks = parser.LoadBlocks();
        }

        public IEnumerable<string> GetRequiredPartNames(IEnumerable<string> partNames)
        {
            var list = new List<string>();
            foreach (var partName in partNames)
            {
                if (IsBlockRequired(partName))
                {
                    list.Add(partName);
                }
            }

            //for Planned (02 - 09) parts, get the highest stage
            var plannedParts = list.Where(ProPlanPartsUtilitiesV2.IsPlannedPart);
            var groups = plannedParts.GroupBy(partName => partName.Substring(2, partName.Length - 2)).Where(group => group.Count() > 1);
            var toFilterOut = new List<string>();
            foreach (var group in groups)
            {
                var ordered = group.OrderBy(name => name).ToList();
                ordered.RemoveAt(ordered.Count - 1);
                toFilterOut.AddRange(ordered);
            }

            var filteredList = list.Except(toFilterOut);
            return filteredList;
        }

        public bool IsBlockRequired(string partName)
        {
            foreach (var block in Blocks)
            {
                if (Regex.IsMatch(partName, $"^{block.PartNamePattern}$", RegexOptions.IgnoreCase))
                {
                    return block.ImportInIDS;
                }
            }

            return false;
        }
    }
}
