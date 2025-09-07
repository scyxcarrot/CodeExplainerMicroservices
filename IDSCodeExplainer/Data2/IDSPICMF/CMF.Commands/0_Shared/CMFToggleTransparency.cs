using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Logics;
using IDS.CMF.Visualization;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("32b24f25-6f57-4311-8caf-71a066eac9eb")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning | DesignPhase.Implant | DesignPhase.Guide)]
    public class CMFToggleTransparency : CmfCommandBase
    {
        static CMFToggleTransparency _instance;
        public CMFToggleTransparency()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFToggleTransparency command.</summary>
        public static CMFToggleTransparency Instance => _instance;

        public override string EnglishName => CommandEnglishName.CMFToggleTransparency;

        private bool _isTransparent = false;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            _isTransparent = !_isTransparent;

            var parser = new ToggleTransparencyComponentJsonParser();
            var blocks = parser.LoadTransparencyInfo();
          
            GetPreOpBlocks(blocks).ForEach(x =>
            {
                HandleTransparency(ProPlanImport.PreopLayer, x, doc, director);
            });

            GetOriginalBlocks(blocks).ForEach(x =>
            {
                HandleTransparency(ProPlanImport.OriginalLayer, x, doc, director);
            });

            List<ToggleTransparencyBlock> plannedBlocks;
            List<ToggleTransparencyBlock> othersBlocks;

            GetPlannedandOthersBlocks(blocks, out plannedBlocks, out othersBlocks);
            plannedBlocks.ForEach(x =>
            {
                HandleTransparency(ProPlanImport.PlannedLayer, x, doc, director);
            });

            HandleTransparencyOthersLayers(othersBlocks, doc, director);

            #region Custom Transparencies
            ApplyImplantSupportCustomTransparencies(director);
            #endregion

            doc.Views.Redraw();

            return Result.Success;
        }

        public List<ToggleTransparencyBlock> GetPreOpBlocks(List<ToggleTransparencyBlock> listOfBlocks)
        {
            var res = new List<ToggleTransparencyBlock>();
            listOfBlocks.ForEach(x =>
            {
                if (x.PartNamePattern.Substring(0, 2) == "00")
                {
                    res.Add(x);
                }
            });
            return res;
        }

        public List<ToggleTransparencyBlock> GetOriginalBlocks(List<ToggleTransparencyBlock> listOfBlocks)
        {
            var res = new List<ToggleTransparencyBlock>();

            listOfBlocks.ForEach(x =>
            {
                if (x.PartNamePattern.Substring(0, 2) == "01")
                {
                    res.Add(x);
                }
            });

            return res;
        }

        public void GetPlannedandOthersBlocks(List<ToggleTransparencyBlock> listOfBlocks,
            out List<ToggleTransparencyBlock> plannedBlocks, out List<ToggleTransparencyBlock> othersBlocks)
        {
            var proplanParser = new ProPlanImportBlockJsonParser();
            var propanBlocks = proplanParser.LoadBlocks();

            plannedBlocks = new List<ToggleTransparencyBlock>();
            othersBlocks = new List<ToggleTransparencyBlock>();
            foreach (var block in listOfBlocks)
            {
                if (block.PartNamePattern.Substring(0, 2) != "00" && block.PartNamePattern.Substring(0, 2) != "01")
                {
                    if (!(propanBlocks.Any(pro => pro.PartNamePattern.ToLower() == block.PartNamePattern.ToLower())))
                    {
                        othersBlocks.Add(block);
                    }
                    else
                    {
                        plannedBlocks.Add(block);
                    }
                }
            }
        }

        private void HandleTransparencyOthersLayers(List<ToggleTransparencyBlock> othersBlocks, 
            RhinoDoc doc, CMFImplantDirector director)
        {
            var rhinoObjects = doc.Objects.GetObjectList(new ObjectEnumeratorSettings()
            {
                HiddenObjects = true,
                LockedObjects = true
            }).ToList();

            foreach (var it in rhinoObjects)
            {
                var layer = doc.Layers[it.Attributes.LayerIndex];
                var block = othersBlocks.Find(x => x.SubLayer == layer.Name);
                if (block.PartNamePattern != null)
                {
                    ApplyTransparency(it, director, block, _isTransparent);
                }
            }
        }


        private void HandleTransparency(string parent, ToggleTransparencyBlock block, RhinoDoc doc, 
            CMFImplantDirector director)
        {
            var rhinoObjects = doc.Objects.GetObjectList(new ObjectEnumeratorSettings()
            {
                HiddenObjects = true,
                LockedObjects = true 
            }).ToList();

            foreach (var it in rhinoObjects)
            {
                var layer = doc.Layers[it.Attributes.LayerIndex];
                if (layer.FullPath == $"{parent}::{block.SubLayer}")
                {
                    ApplyTransparency(it, director, block, _isTransparent);
                }
            }
        }

        private void ApplyTransparency(RhinoObject rhinobject, CMFImplantDirector director, 
            ToggleTransparencyBlock block,
        bool isTransparent)
        {
            var curMat = rhinobject.GetMaterial(true);
            var transparencyValue = curMat.Transparency;

            if (director.CurrentDesignPhase == DesignPhase.Planning ||
                director.CurrentDesignPhase == DesignPhase.Implant)
            {
                if (isTransparent)
                {
                    if (block.ImplantDesignTransparencyOn.HasValue)
                    {
                        transparencyValue = block.ImplantDesignTransparencyOn.Value;
                    }
                }
                else
                {
                    if (block.ImplantDesignTransparencyOff.HasValue)
                    {
                        transparencyValue = block.ImplantDesignTransparencyOff.Value;
                    }
                }
            }
            else if (director.CurrentDesignPhase == DesignPhase.Guide)
            {
                if (_isTransparent)
                {
                    if (block.GuideDesignTransparencyOn.HasValue)
                    {
                        transparencyValue = block.GuideDesignTransparencyOn.Value;
                    }
                }
                else
                {
                    if (block.GuideDesignTransparencyOff.HasValue)
                    {
                        transparencyValue = block.GuideDesignTransparencyOff.Value;
                    }
                }
            }

            if (Math.Abs(transparencyValue - curMat.Transparency) > 0.0001)
            {
                curMat.Transparency = transparencyValue;
                curMat.CommitChanges();
            }
        }

        private double GetTransparencyValue(bool isTransparentOn)
        {
            var parser = new ToggleTransparencyComponentJsonParser();
            var blocks = parser.LoadTransparencyInfo();
            var implantSupportFullLayerPath = BuildingBlocks.Blocks[IBB.ImplantSupport].Layer;

            var implantDesignTransparencyInfo = blocks.First(b =>
                implantSupportFullLayerPath.Contains(b.SubLayer));

            return (isTransparentOn ? implantDesignTransparencyInfo.ImplantDesignTransparencyOn :
                implantDesignTransparencyInfo.GuideDesignTransparencyOff) ?? double.NaN;
        }

        public void ApplyImplantSupportCustomTransparencies(CMFImplantDirector director, bool isTransparent)
        {
            if (director.CurrentDesignPhase != DesignPhase.Implant)
            {
                return;
            }

            var outdatedImplantSupportRhObjects = OutdatedImplantSupportHelper.GetOutdatedImplantSupports(director);
            RhinoObjectUtilities.ResetRhObjTransparencies(director, outdatedImplantSupportRhObjects);
            
            var transparencyValue = GetTransparencyValue(isTransparent);
            var validImplantSupportRhObjects = OutdatedImplantSupportHelper.GetValidImplantSupports(director);
            RhinoObjectUtilities.SetRhObjTransparencies(director, validImplantSupportRhObjects, transparencyValue);
        }

        public void ApplyImplantSupportCustomTransparencies(CMFImplantDirector director)
        {
            ApplyImplantSupportCustomTransparencies(director, _isTransparent);
        }
    }
}
