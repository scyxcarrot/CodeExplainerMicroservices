using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.V2.Logics;
using IDS.Core.ImplantBuildingBlocks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class ProPlanImportComponent
    {
        public const IBB StaticIBB = IBB.ProPlanImport;
        public List<ProPlanImportBlock> Blocks { get; private set; }
        private readonly List<ProPlanImportPartType> _castPartType = new List<ProPlanImportPartType>
        {
            ProPlanImportPartType.MandibleCast,
            ProPlanImportPartType.MaxillaCast
        };
        public IReadOnlyList<ProPlanImportPartType> CastPartType => _castPartType;
        public ProPlanImportComponent()
        {
            LoadPrePlanImportBlocks();
        }
        private string GetLayer(string partName)
        {
            var subLayer = GetBlock(partName).SubLayer;
            string layer;

            if (ProPlanPartsUtilitiesV2.IsPreopPart(partName))
            {
                layer = $"{Constants.ProPlanImport.PreopLayer}::{subLayer}";
            }
            else if (ProPlanPartsUtilitiesV2.IsOriginalPart(partName))
            {
                layer = $"{Constants.ProPlanImport.OriginalLayer}::{subLayer}";
            }
            else
            {
                layer = $"{Constants.ProPlanImport.PlannedLayer}::{subLayer}";
            }

            return layer;
        }

        public ProPlanImportBlock GetBlock(string partName)
        {
            foreach (var block in Blocks)
            {
                var pattern = new Regex($"^{block.PartNamePattern}$", RegexOptions.IgnoreCase);
                if (pattern.IsMatch(partName))
                {
                    return block;
                }
            }

            //unable to find matching block
            throw new Exception($"Invalid block: {partName}");
        }

        public ExtendedImplantBuildingBlock GetProPlanImportBuildingBlock(string partName)
        {
            var staticBlock = BuildingBlocks.Blocks[StaticIBB];
            return new ExtendedImplantBuildingBlock
            {
                Block = new ImplantBuildingBlock
                {
                    ID = staticBlock.ID,
                    Name = string.Format(staticBlock.Name, partName),
                    GeometryType = staticBlock.GeometryType,
                    Layer = GetLayer(partName),
                    Color = GetColor(partName)
                },
                PartOf = StaticIBB
            };
        }

        public string GetPartName(string blockName)
        {
            var staticBlock = BuildingBlocks.Blocks[StaticIBB];
            var staticName = string.Format(staticBlock.Name, string.Empty);
            return blockName.Replace(staticName, string.Empty);
        }

        public string ConstructProPlanName(string surgeryState, string purePartName)
        {
            var staticBlock = BuildingBlocks.Blocks[StaticIBB];
            return string.Format(staticBlock.Name, $"{surgeryState}{purePartName}");
        }

        public bool GetPurePartNameFromBlockName(string blockName, out string surgeryState, out string purePartName)
        {
            var partName = GetPartName(blockName);
            return GetPurePartName(partName, out surgeryState, out purePartName);
        }

        public bool GetPurePartName(string partName, out string surgeryState, out string purePartName)
        {
            var rx = new Regex(@"^(\d{2})?([a-z][a-z0-9_]{1,})$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            surgeryState = null;
            purePartName = null;

            var matches = rx.Matches(partName);
            if (matches.Count != 1)
            {
                return false;
            }

            var groups = matches[0].Groups;

            if (groups.Count != 3)
            {
                return false;
            }

            surgeryState = groups[1].Value;
            purePartName = groups[2].Value;

            return true;
        }

        private Color GetColor(string partName)
        {
            return GetBlock(partName).Color;
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

        private void LoadPrePlanImportBlocks()
        {
            var parser = new ProPlanImportBlockJsonParser();
            Blocks = parser.LoadBlocks();
        }

        public IEnumerable<string> GetImplantPlacablePartNames()
        {
            var partNamePatterns = Blocks.Where(b => b.IsImplantPlacable).Select(b => $"{b.PartNamePattern}$");
            return partNamePatterns;
        }

        public IEnumerable<string> GetOriginalPartNames()
        {
            var partNamePatterns = Blocks.Where(b => ProPlanPartsUtilitiesV2.IsOriginalPart(b.PartNamePattern)).Select(b => $"{b.PartNamePattern}$");
            return partNamePatterns;
        }

        public IEnumerable<string> GetOriginalOsteotomyPartNames()
        {
            var partNamePatterns = Blocks.Where(b => ProPlanPartsUtilitiesV2.IsOriginalPart(b.PartNamePattern) &&
                                                     b.PartType == ProPlanImportPartType.OsteotomyPlane).Select(b => $"{b.PartNamePattern}$");
            return partNamePatterns;
        }

        public bool IsMatch(IEnumerable<string> partNamePatterns, string partName)
        {
            foreach (var partNamePattern in partNamePatterns)
            {
                if (Regex.IsMatch(partName, $"^{partNamePattern}$", RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsProPlanImportPart(string fullName)
        {
            return fullName.StartsWith(ProPlanImport.ObjectPrefix);
        }

        public IEnumerable<string> GetConstraintMeshesNameForImplant(IEnumerable<string> partNames)
        {
            var partNamePatterns = GetImplantPlacablePartNames();
            var regex = string.Join("|", partNamePatterns.Select(r => $"({r})"));
            var contraintMeshNames = new List<string>();
            foreach (var name in partNames)
            {
                if (Regex.IsMatch(name, regex, RegexOptions.IgnoreCase))
                {
                    contraintMeshNames.Add(name);
                }
            }
            return contraintMeshNames;
        }

        public bool IsCastPartType(string partName)
        {
            return CastPartType.Contains(GetBlock(partName).PartType);
        }

        public bool IsExistsInProPlan(string partName)
        {
            return Blocks.Any(item => Regex.IsMatch(partName, $"^{item.PartNamePattern}$", RegexOptions.IgnoreCase));
        }
    }
}
