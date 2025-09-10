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
using System.Windows.Forms;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("8C004310-00DA-4139-976F-CBD80F10E6E4")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestModifyUserDictionary : CmfCommandBase
    {
        public CMF_TestModifyUserDictionary()
        {
            Instance = this;
        }
        
        public static CMF_TestModifyUserDictionary Instance { get; private set; }

        public override string EnglishName => "CMF_TestModifyUserDictionary";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            foreach (var rhinoObj in doc.Objects)
            {
                doc.Objects.Unlock(rhinoObj, true);
            }

            string rhinoObjGuidStr = string.Empty;
            string userDictionaryKey = string.Empty;
            string userDictionaryValue = string.Empty;

            RhinoObject rhinoObject = null;

            if (mode == RunMode.Scripted)
            {
                var guidResult = RhinoGet.GetString("Object GUID", false, ref rhinoObjGuidStr);

                rhinoObject = doc.Objects.FindId(new Guid(rhinoObjGuidStr));
                if (guidResult != Result.Success || string.IsNullOrEmpty(rhinoObjGuidStr) || rhinoObject is null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid GUID given: {rhinoObjGuidStr}");
                    return Result.Failure;
                }
            }
            else
            {
                var selectObject = new GetObject();
                selectObject.SetCommandPrompt("Select object to add user dictionary");
                selectObject.EnablePreSelect(false, false);
                selectObject.EnablePostSelect(true);
                selectObject.AcceptNothing(false);
                selectObject.EnableTransparentCommands(false);

                var result = selectObject.Get();

                if (result == GetResult.Object)
                {
                    rhinoObject = selectObject.Object(0).Object();
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "No object chosen");
                    return Result.Failure;
                }
            }
            
            var userDictionaryKeyResult = RhinoGet.GetString("User Dictionary Key", false, ref userDictionaryKey);
            if (userDictionaryKeyResult != Result.Success || string.IsNullOrEmpty(userDictionaryKey))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "No dictionary key given");
                return Result.Failure;
            }

            var userDictionaryValueResult = RhinoGet.GetString("User Dictionary Value", false, ref userDictionaryValue);
            if (userDictionaryValueResult != Result.Success || string.IsNullOrEmpty(userDictionaryValue))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "No dictionary value given");
                return Result.Failure;
            }

            var isUserDictionaryModified = UserDictionaryUtilities.ModifyUserDictionary(rhinoObject, userDictionaryKey, userDictionaryValue);
            if (isUserDictionaryModified)
            {
                return Result.Success;
            }

            return Result.Failure;
        }
    }

#endif
}
