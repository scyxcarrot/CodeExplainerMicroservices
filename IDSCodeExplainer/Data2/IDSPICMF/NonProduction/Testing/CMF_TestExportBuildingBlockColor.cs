using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Newtonsoft.Json;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("911726FE-A941-46D1-9D45-175FE20BB752")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ProPlanImport)]
    public class CMF_TestExportBuildingBlockColor : CmfCommandBase
    {
        private class MeshPartVertexColor
        {
            public string Name { get; }
            public Color Color { get; }

            public MeshPartVertexColor(string name, Color color)
            {
                Name = name;
                Color = color;
            }
        }

        public CMF_TestExportBuildingBlockColor()
        {
            Instance = this;
        }

        public static CMF_TestExportBuildingBlockColor Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportBuildingBlockColor";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var buildingBlock = IBB.ImplantSupport;

            if (mode == RunMode.Scripted)
            {
                var buildingBlockName = BuildingBlocks.Blocks[buildingBlock].Name;

                var result = RhinoGet.GetString("BuildingBlock", false, ref buildingBlockName);
                if (result != Result.Success || string.IsNullOrEmpty(buildingBlockName) || !Enum.TryParse(buildingBlockName, true, out buildingBlock))
                {
                    RhinoApp.WriteLine($"Invalid building block: {buildingBlockName}");
                    return Result.Failure;
                }
            }
            else
            {
                var getOption = new GetOption();
                getOption.SetCommandPrompt("Select a building block");
                getOption.AddOptionEnumList("IBB", buildingBlock);

                while (true)
                {
                    var result = getOption.Get();
                    
                    if (result == GetResult.Option)
                    {
                        buildingBlock = getOption.GetSelectedEnumValue<IBB>();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            RhinoApp.WriteLine($"Selected building block: {buildingBlock}");

            var list = new List<MeshPartVertexColor>();
            list.AddRange(GetImplantCaseComponentMeshPartVertexColors(director, buildingBlock));
            list.AddRange(GetGuideCaseComponentMeshPartVertexColors(director, buildingBlock));

            if (!list.Any())
            {
                list.Add(GetBuildingBlockMeshPartVertexColor(director, BuildingBlocks.Blocks[buildingBlock], BuildingBlocks.Blocks[buildingBlock].Color, BuildingBlocks.Blocks[buildingBlock].Name));
            }

            var directory = DirectoryStructure.GetWorkingDir(doc);
            using (var file = File.CreateText($"{directory}\\BuildingBlockMeshVertexColors.json"))
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, list);
            }
            SystemTools.OpenExplorerInFolder(directory);

            return Result.Success;
        }

        private List<MeshPartVertexColor> GetImplantCaseComponentMeshPartVertexColors(CMFImplantDirector director, IBB buildingBlock)
        {
            var implantCaseComponent = new ImplantCaseComponent();

            if (!implantCaseComponent.GetImplantComponents().Contains(buildingBlock))
            {
                return new List<MeshPartVertexColor>();
            }

            var list = new List<MeshPartVertexColor>();

            var color = BuildingBlocks.Blocks[buildingBlock].Color;

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var eBlock = implantCaseComponent.GetImplantBuildingBlock(buildingBlock, casePreferenceData);

                list.Add(GetExtendedBuildingBlockMeshPartVertexColor(director, eBlock, color, string.Format(BuildingBlocks.Blocks[buildingBlock].Name, $"I{casePreferenceData.NCase}")));
            }

            return list;
        }

        private List<MeshPartVertexColor> GetGuideCaseComponentMeshPartVertexColors(CMFImplantDirector director, IBB buildingBlock)
        {            
            var guideCaseComponent = new GuideCaseComponent();

            if (!guideCaseComponent.GetGuideComponents().Contains(buildingBlock))
            {
                return new List<MeshPartVertexColor>();
            }

            var list = new List<MeshPartVertexColor>();

            var color = BuildingBlocks.Blocks[buildingBlock].Color;

            foreach (var guidePreference in director.CasePrefManager.GuidePreferences)
            {
                var eBlock = guideCaseComponent.GetGuideBuildingBlock(buildingBlock, guidePreference);

                list.Add(GetExtendedBuildingBlockMeshPartVertexColor(director, eBlock, color, string.Format(BuildingBlocks.Blocks[buildingBlock].Name, $"G{guidePreference.NCase}")));
            }

            return list;
        }

        private MeshPartVertexColor GetExtendedBuildingBlockMeshPartVertexColor(CMFImplantDirector director, ExtendedImplantBuildingBlock eBlock, Color originalColor, string name)
        {
            return GetBuildingBlockMeshPartVertexColor(director, eBlock.Block, originalColor, name);
        }

        private MeshPartVertexColor GetBuildingBlockMeshPartVertexColor(CMFImplantDirector director, ImplantBuildingBlock eBlock, Color originalColor, string name)
        {
            var objectManager = new CMFObjectManager(director);

            var color = originalColor;

            var buildingBlockObject = objectManager.GetBuildingBlock(eBlock);
            if (buildingBlockObject != null && buildingBlockObject.ObjectType == ObjectType.Mesh)
            {
                var mesh = (Mesh)buildingBlockObject.Geometry;
                if (mesh.VertexColors.Any())
                {
                    color = mesh.VertexColors[0];
                }
            }

            return new MeshPartVertexColor(name, color);
        }
    }

#endif
}
