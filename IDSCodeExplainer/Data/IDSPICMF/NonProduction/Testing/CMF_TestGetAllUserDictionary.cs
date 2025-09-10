using System;
using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("8C004310-00DA-4139-976F-DBE80F10E6E4")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestGetAllUserDictionary : CmfCommandBase
    {
        public CMF_TestGetAllUserDictionary()
        {
            Instance = this;
        }
        
        public static CMF_TestGetAllUserDictionary Instance { get; private set; }

        public override string EnglishName => "CMF_TestGetAllUserDictionary";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Dictionary<string, Dictionary<string, string>> outputDictionary = new Dictionary<string, Dictionary<string, string>>();
            foreach (var layer in doc.Layers)
            {
                layer.IsVisible = true;
            }

            foreach (var rhinoObj in doc.Objects)
            {
                doc.Objects.Unlock(rhinoObj, true);
                var rhinoObjUserDict = rhinoObj.Attributes.UserDictionary;

                var tempDict = new Dictionary<string, string>();
                tempDict.Add("fromLayer", doc.Layers[rhinoObj.Attributes.LayerIndex].FullPath);

                foreach (var rhinoObjUserDictEntry in rhinoObjUserDict)
                {
                    tempDict[rhinoObjUserDictEntry.Key] = rhinoObjUserDictEntry.Value.ToString();
                }

                var dictKey = $"{rhinoObj.Attributes.Name}_{rhinoObj.Attributes.ObjectId}";
                outputDictionary[dictKey] = tempDict;
            }
            
            var sortedDictionary = outputDictionary.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            using (var streamWriter = new StreamWriter(Path.Combine(Path.GetDirectoryName(doc.Path), "UserDictionary.json")))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                jsonWriter.Formatting = Formatting.Indented;
                serializer.Serialize(jsonWriter, sortedDictionary);
            }

            Locking.LockAll(doc);
            
            return Result.Success;
        }
    }

#endif
}
