using System.Collections.Generic;
using System.IO;
using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.V2.Extensions;
using IDS.PICMF;
using Newtonsoft.Json;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDSPICMF.NonProduction.Testing
{
#if (STAGING)
    [System.Runtime.InteropServices.Guid("E8E05FC1-5DCC-47AD-9FBA-FA8ED3338C82")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestGetAllOsteotomyHandler : CmfCommandBase
    {
        public CMF_TestGetAllOsteotomyHandler()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static CMF_TestGetAllOsteotomyHandler Instance { get; private set; }

        public override string EnglishName => "CMF_TestGetAllOsteotomyHandler";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var outputDictionary = new Dictionary<string, Dictionary<string, Point3d>>();
            var proplanImportComponent = new ProPlanImportComponent();
            var list = new SmartDesignPartOsteotomyHandlerList();
            foreach (var layer in doc.Layers)
            {
                layer.IsVisible = true;
            }

            foreach (var rhinoObj in doc.Objects)
            {
                var partName = proplanImportComponent.GetPartName(rhinoObj.Attributes.Name);
                if (ProPlanImportUtilities.IsOsteotomyPlane(partName))
                {
                    doc.Objects.Unlock(rhinoObj, true);
                    var rhinoObjUserDict = rhinoObj.Attributes.UserDictionary;

                    var osteotomyHandler = new OsteotomyHandlerData();
                    osteotomyHandler.DeSerialize(rhinoObjUserDict);

                    var handlerDictionary = new Dictionary<string, double[]>();

                    if (osteotomyHandler.HandlerIdentifier == null)
                    {
                        continue;
                    }

                    for (var index = 0; index < osteotomyHandler.HandlerIdentifier.GetLength(0); index++)
                    {
                        handlerDictionary.Add(osteotomyHandler.HandlerIdentifier[index], osteotomyHandler.HandlerCoordinates.GetRow(index));
                    }

                    list.ExportedParts.Add(new SmartDesignPartOsteotomyHandler()
                    {
                        OsteotomyPartName = partName,
                        OsteotomyThickness = osteotomyHandler.OsteotomyThickness,
                        OsteotomyType = osteotomyHandler.OsteotomyType,
                        OsteotomyHandler = handlerDictionary,
                    });
                }
            }

            //Export
            using (var file = File.CreateText(Path.Combine(Path.GetDirectoryName(doc.Path), $"{SmartDesignStrings.OsteotomyHandlerFileName}.json")))
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, list);
            }

            Locking.LockAll(doc);

            return Result.Success;
        }
    }
#endif
}