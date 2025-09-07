using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Common;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace IDS.NonProduction.Commands
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("D0F838C7-2972-4114-B870-172BCE4812C0")]
    [IDSCommandAttributes(true, DesignPhase.Any)]
    public class AMace_TestDuplicateCurrentScrewBrandType : Command
    {
        public AMace_TestDuplicateCurrentScrewBrandType()
        {
            Instance = this;
        }

        public static AMace_TestDuplicateCurrentScrewBrandType Instance { get; private set; }

        public override string EnglishName => "AMace_TestDuplicateCurrentScrewBrandType";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = new ImplantDirector(doc, PlugInInfo.PluginModel);

            var selectedBrand = director.GetCurrentScrewBrand();
            var screwQuery = new ScrewQuery();
            var defaultScrewBrandType = screwQuery.GetDefaultScrewType(selectedBrand);

            var screwDatabaseQuery = new ScrewDatabaseQuery();
            var availableScrewBrands = screwDatabaseQuery.GetAvailableScrewBrands().ToList();

            var brand = "Brand1".ToUpper();
            var locking = defaultScrewBrandType.Locking;

            var getParametersOption = new GetOption();
            getParametersOption.SetCommandPrompt("Choose screw brand and locking");
            getParametersOption.AcceptNothing(true);
            var optionBrandId = getParametersOption.AddOption("Brand", brand);
            var optionScrewLockingId = getParametersOption.AddOptionEnumList<ScrewLocking>("Locking", locking);
           
            while (true)
            {
                var res = getParametersOption.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }

                if (res == GetResult.Option)
                {
                    var optId = getParametersOption.OptionIndex();
                    if (optId == optionBrandId)
                    {
                        var result = RhinoGet.GetString("Brand", false, ref brand);
                        brand = brand.ToUpper();
                        //update option
                        getParametersOption.ClearCommandOptions();
                        optionBrandId = getParametersOption.AddOption("Brand", brand);
                        optionScrewLockingId = getParametersOption.AddOptionEnumList<ScrewLocking>("Locking", locking);
                    }
                    else if (optId == optionScrewLockingId)
                    {
                        locking = getParametersOption.GetSelectedEnumValue<ScrewLocking>();
                    }
                }
            }
            var rc = getParametersOption.CommandResult();
            if (rc != Result.Success)
            {
                return Result.Cancel;
            }

            //check
            var checkScrewBrandType = new ScrewBrandType(brand, defaultScrewBrandType.Diameter, locking);
            if (availableScrewBrands.Any(br => br.Equals(brand, StringComparison.InvariantCultureIgnoreCase)))
            {
                var screwTypes = screwDatabaseQuery.GetAvailableScrewTypes(brand);
                if (screwTypes.Any(t => t.Equals(checkScrewBrandType.Type, StringComparison.InvariantCultureIgnoreCase)))
                {
                    RhinoApp.WriteLine($"ScrewBrandType {checkScrewBrandType} already exist");
                    return Result.Failure;
                }
            }

            //Get output folder
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Please select an output folder for screw database";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);

            var resources = new AmaceResources();
            var screwDatabase3dm = File3dm.Read(resources.ScrewDatabasePath);
            var screwDatabaseXml = new XmlDocument();
            screwDatabaseXml.Load(resources.ScrewDatabaseXmlPath);

            //Save a copy
            var screwDatabase3dmPath = $"{folderPath}\\IDS_Screw_Database_Full.3dm";
            var screwDatabaseXmlPath = $"{folderPath}\\Screw_Database.xml";
            screwDatabase3dm.Write(screwDatabase3dmPath, new File3dmWriteOptions());
            screwDatabaseXml.Save(screwDatabaseXmlPath);
            screwDatabase3dm.Dispose();

            //Duplicate
            var copyFromScrewBrandType = defaultScrewBrandType;
            var newScrewBrandType = checkScrewBrandType;
            DuplicateAndModifyValuesInXml(screwDatabaseXmlPath, copyFromScrewBrandType, newScrewBrandType);
            DuplicateAndModifyValuesIn3dm(screwDatabase3dmPath, copyFromScrewBrandType, newScrewBrandType);

            SystemTools.OpenExplorerInFolder(folderPath);

            RhinoApp.WriteLine("Screw Databases were generated to the following folder:");
            RhinoApp.WriteLine("{0}", folderPath);
            RhinoApp.WriteLine("Please update the available lengths accordingly");
            return Result.Success;
        }

        private void DuplicateAndModifyValuesInXml(string path, ScrewBrandType copyFromScrewBrandType, ScrewBrandType newScrewBrandType)
        {
            var screwDatabaseXml = new XmlDocument();
            screwDatabaseXml.Load(path);

            var brandNode =
                screwDatabaseXml.SelectSingleNode($"/screws/brands/brand[name='{newScrewBrandType.Brand}']");
            if (brandNode == null)
            {
                brandNode = screwDatabaseXml.CreateElement("brand");

                var nodeName = screwDatabaseXml.CreateElement("name");
                nodeName.InnerText = newScrewBrandType.Brand;
                brandNode.AppendChild(nodeName);

                var nodeDefaultType = screwDatabaseXml.CreateElement("defaultType");
                nodeDefaultType.InnerText = newScrewBrandType.Type;
                brandNode.AppendChild(nodeDefaultType);

                var nodeTypes = screwDatabaseXml.CreateElement("types");
                brandNode.AppendChild(nodeTypes);

                var brandsNode = screwDatabaseXml.SelectSingleNode($"/screws/brands");
                brandsNode.AppendChild(brandNode);
            }

            var availableLengthsNode = screwDatabaseXml.SelectSingleNode($"/screws/brands/brand[name='{copyFromScrewBrandType.Brand}']/types/type[name='{copyFromScrewBrandType.Type}']/availableLengths");
            var typesNode = brandNode.SelectSingleNode($"./types");
            if (typesNode != null && availableLengthsNode != null)
            {
                var nodeType = screwDatabaseXml.CreateElement("type");

                var nodeTypeName = screwDatabaseXml.CreateElement("name");
                nodeTypeName.InnerText = newScrewBrandType.Type;
                nodeType.AppendChild(nodeTypeName);

                var nodeAvailableLengths = screwDatabaseXml.CreateElement("availableLengths");
                nodeAvailableLengths.InnerText = availableLengthsNode.InnerText;
                nodeType.AppendChild(nodeAvailableLengths);

                typesNode.AppendChild(nodeType);

                screwDatabaseXml.Save(path);
            }
            else
            {
                throw new Exception("Error while modifying Xml!");
            }
        }

        private void DuplicateAndModifyValuesIn3dm(string path, ScrewBrandType copyFromScrewBrandType, ScrewBrandType newScrewBrandType)
        {
            var screwDatabase3dm = File3dm.Read(path);

            var parentLayer = screwDatabase3dm.Layers.FirstOrDefault(lyr => string.Equals(lyr.Name,
                newScrewBrandType.ToString(), StringComparison.InvariantCultureIgnoreCase));

            if (parentLayer == null)
            {
                //add new layer
                parentLayer = new Layer();
                parentLayer.Name = newScrewBrandType.ToString();
                screwDatabase3dm.Layers.Add(parentLayer);

                screwDatabase3dm.Write(path, new File3dmWriteOptions());

                screwDatabase3dm.Dispose();
                
                screwDatabase3dm = File3dm.Read(path);
            }

            /////////////////////////////////////////////////////////////////////
            
            parentLayer = screwDatabase3dm.Layers.FirstOrDefault(lyr => string.Equals(lyr.Name,
                newScrewBrandType.ToString(), StringComparison.InvariantCultureIgnoreCase));

            var objects = screwDatabase3dm.Objects.Where(rhobj => rhobj.Attributes.Name.StartsWith(
                copyFromScrewBrandType.ToString(),
                StringComparison.InvariantCultureIgnoreCase)).ToList();

            foreach (var obj in objects)
            {
                var oldLayer = screwDatabase3dm.Layers.FirstOrDefault(l => l.LayerIndex == obj.Attributes.LayerIndex);

                if (oldLayer == null)
                {
                    throw new Exception();
                }

                //add new layer
                var subLayer = new Layer();
                subLayer.Name = oldLayer.Name;
                subLayer.ParentLayerId = parentLayer.Id;
                screwDatabase3dm.Layers.Add(subLayer);
            }

            screwDatabase3dm.Write(path, new File3dmWriteOptions());

            screwDatabase3dm.Dispose();

            /////////////////////////////////////////////////////////////////////
            screwDatabase3dm = File3dm.Read(path);

            parentLayer = screwDatabase3dm.Layers.FirstOrDefault(lyr => string.Equals(lyr.Name,
                newScrewBrandType.ToString(), StringComparison.InvariantCultureIgnoreCase));

            objects = screwDatabase3dm.Objects.Where(rhobj => rhobj.Attributes.Name.StartsWith(
                copyFromScrewBrandType.ToString(),
                StringComparison.InvariantCultureIgnoreCase)).ToList();

            //add
            foreach (var obj in objects)
            {
                var oldlayer = screwDatabase3dm.Layers.FirstOrDefault(lyr => lyr.LayerIndex == obj.Attributes.LayerIndex);
                var newlayer = screwDatabase3dm.Layers.FirstOrDefault(lyr => lyr.Name == oldlayer.Name && lyr.ParentLayerId == parentLayer.Id);

                var attr = obj.Attributes.Duplicate();
                attr.Name = attr.Name.Replace($"{copyFromScrewBrandType}", $"{newScrewBrandType}");
                attr.LayerIndex = newlayer.LayerIndex;
                switch (obj.Geometry.ObjectType)
                {
                    case ObjectType.Brep:
                        screwDatabase3dm.Objects.AddBrep(obj.Geometry.Duplicate() as Brep, attr);
                        break;
                    case ObjectType.Curve:
                        screwDatabase3dm.Objects.AddCurve(obj.Geometry.Duplicate() as Curve, attr);
                        break;
                    case ObjectType.Mesh:
                        screwDatabase3dm.Objects.AddMesh(obj.Geometry.Duplicate() as Mesh, attr);
                        break;
                }
            }

            screwDatabase3dm.Write(path, new File3dmWriteOptions());

            screwDatabase3dm.Dispose();
        }
    }

#endif
}
