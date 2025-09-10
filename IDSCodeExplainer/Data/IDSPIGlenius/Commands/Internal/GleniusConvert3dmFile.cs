using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace IDS.Glenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("E30CA87B-A73E-4B11-81D3-341BCD00966C")]
    [CommandStyle(Style.ScriptRunner)]
    public class GleniusConvert3dmFile : Command
    {
        public GleniusConvert3dmFile()
        {
            Instance = this;
        }

        public static GleniusConvert3dmFile Instance { get; private set; }

        public override string EnglishName => "GleniusConvert3dmFile";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            if (director == null)
            {
                return Result.Failure;
            }

            foreach (var obj in doc.Objects)
                doc.Objects.Unlock(obj.Id, true);

            var objectManager = new GleniusObjectManager(director);

            UpdateDirector(director, objectManager);

            RhinoApp.WriteLine("Updating Layers and Colors");

            foreach (var block in BuildingBlocks.Blocks)
            {
                var blocks = objectManager.GetAllBuildingBlocks(block.Key);
                var layerIndex = ImplantBuildingBlockProperties.GetLayer(block.Value, doc);
                var color = ImplantBuildingBlockProperties.GetColor(block.Value);
                foreach (var rhinoObject in blocks)
                {
                    rhinoObject.Attributes.LayerIndex = layerIndex;
                    rhinoObject.Attributes.ObjectColor = color;
                    rhinoObject.CommitChanges();
                }

                var materials = doc.Materials.Where(mat => mat.Name == block.Value.Name);
                foreach (var mat in materials)
                {
                    mat.DiffuseColor = color;
                    mat.SpecularColor = color;
                    mat.AmbientColor = color;
                    mat.CommitChanges();
                }
            }

            RhinoApp.WriteLine("Layers and Colors updated");

            var headObject = objectManager.GetBuildingBlock(IBB.Head);
            if (headObject != null && !headObject.Attributes.UserDictionary.ContainsKey("HeadType"))
            {
                RhinoApp.WriteLine("Updating HeadType to: HeadType.TYPE_36_MM");

                headObject.Attributes.UserDictionary.SetEnumValue<HeadType>("HeadType", HeadType.TYPE_36_MM);
                headObject.CommitChanges();

                RhinoApp.WriteLine("HeadType updated");
            }

            RhinoApp.WriteLine("Updating ScrewMantles");
            
            var screws = new List<Screw>();
            var screwObjects = objectManager.GetAllBuildingBlocks(IBB.Screw);
            foreach (var rhinoObject in screwObjects)
            {
                var screw = rhinoObject as Screw;
                if (screw == null)
                {
                    var restored = new Screw(rhinoObject, true, true);
                    var oldRef = new ObjRef(rhinoObject);
                    doc.Objects.Replace(oldRef, restored);
                    restored.Director = director;
                    screws.Add(restored);
                }
                else
                {
                    screws.Add(screw);
                }
            }
            
            foreach (var thisScrew in screws)
            {
                var screwMantleId = thisScrew.ScrewAides[ScrewAideType.Mantle];
                var screwMantleObj = doc.Objects.Find(screwMantleId);
                var screwMantle = screwMantleObj as ScrewMantle;
                if (screwMantle != null)
                {
                    if (!screwMantle.IsDataValid())
                    {
                        screwMantle.ConstructData(thisScrew);
                    }

                    if (screwMantle.ExtensionLength > 0.0)
                    {
                        var restored = new ScrewMantle(screwMantle.ScrewType, screwMantle.StartExtension,
                            screwMantle.ExtensionDirection, screwMantle.ExtensionLength);
                        objectManager.AddNewBuildingBlock(IBB.ScrewMantle, restored, true);
                        objectManager.DeleteObject(screwMantleId);
                        thisScrew.ScrewAides[ScrewAideType.Mantle] = restored.Id;
                    }
                }
            }

            RhinoApp.WriteLine("ScrewMantles updated");

            foreach (var obj in doc.Objects)
                doc.Objects.Lock(obj.Id, true);

            doc.Views.Redraw();

            return Result.Success;
        }

        private List<IBB> scapulaComponents;
        private List<IBB> humerusComponents;
        private List<IBB> preopComponents;
        private XmlNodeList partNodes;

        private void UpdateDirector(GleniusImplantDirector director, GleniusObjectManager objectManager)
        {
            if (!director.BlockToKeywordMapping.Any())
            {
                RhinoApp.WriteLine("Updating GleniusImplantDirector's BlockToPartNameMapping");

                SetupFields();

                var mapping = new Dictionary<IBB, string>();
                foreach (var component in preopComponents)
                {
                    if (objectManager.HasBuildingBlock(component))
                    {
                        mapping.Add(component, GetKeyword(component, director.defectIsLeft ? "L" : "R"));
                    }
                }
                director.BlockToKeywordMapping = mapping;

                RhinoApp.WriteLine("GleniusImplantDirector's BlockToPartNameMapping updated");
            }
        }

        private void SetupFields()
        {
            if (scapulaComponents == null)
            {
                scapulaComponents = new List<IBB>
                {
                    IBB.Scapula,
                    IBB.ScapulaBoneFragments,
                    IBB.BoneGraft,
                    IBB.ScapulaCalcifiedTissue,
                    IBB.ScapulaCement,
                    IBB.ScapulaMetalPieces,
                    IBB.Baseplate,
                    IBB.Glenosphere,
                    IBB.ScapulaScrews,
                };
            }

            if (humerusComponents == null)
            {
                humerusComponents = new List<IBB>
                {
                    IBB.Humerus,
                    IBB.HumerusBoneFragments,
                    IBB.HumerusCalcifiedTissue,
                    IBB.Spacer,
                    IBB.HumerusCement,
                    IBB.Liner,
                    IBB.HumerusMetalPieces,
                    IBB.SpacerRod,
                    IBB.HumeralHead,
                    IBB.HumerusScrews,
                    IBB.CerclageWire,
                    IBB.Stem
                };
            }

            if (preopComponents == null)
            {
                preopComponents = BuildingBlocks.GetAllPossibleNonConflictingConflictingEntities().ToList();
                preopComponents.Add(IBB.Scapula);
                preopComponents.Add(IBB.Humerus);
            }

            if (partNodes == null)
            {
                var resource = new Resources();
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(resource.GleniusColorsXmlFile);
                partNodes = xmlDocument.SelectNodes("colordefinitions/partdefaults/part");
            }
        }

        private string GetScapulaOrHumerus(IBB block)
        {
            var scapulaOrHumerus = string.Empty;
            if (scapulaComponents.Contains(block))
            {
                scapulaOrHumerus = "S";
            }
            else if (humerusComponents.Contains(block))
            {
                scapulaOrHumerus = "H";
            }
            return scapulaOrHumerus;
        }

        private string GetKeyword(IBB block, string defectSide)
        {
            var blockName = block.ToString();
            if (block != IBB.Scapula && block != IBB.Humerus)
            {
                blockName = blockName.Replace("Scapula", "").Replace("Humerus", "");
            }
            foreach (XmlNode node in partNodes)
            {
                var value = node.Attributes.GetNamedItem("name").Value;
                if (value.Replace(" ", "") == blockName)
                {
                    var keyword = node.InnerXml;
                    var splits = keyword.Split('_');
                    var count = splits.Count();
                    if (count >= 2)
                    {
                        var scapulaOrHumerus = splits[0].Substring(0, 1);
                        var side = splits[0].Substring(splits[0].Count() - 1, 1);
                        if (GetScapulaOrHumerus(block) == scapulaOrHumerus && side == defectSide)
                        {
                            return keyword;
                        }
                    }
                }
            }
            return string.Empty;
        }
    }

#endif
}