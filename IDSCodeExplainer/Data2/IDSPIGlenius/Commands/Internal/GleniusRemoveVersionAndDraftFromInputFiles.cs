using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.IO;
using System.Text.RegularExpressions;

namespace IDS.Glenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("31311ff0-3ae3-493b-9f37-f935634608de")]
    public class GleniusRemoveVersionAndDraftFromInputFiles : CommandBase<GleniusImplantDirector>
    {
        static GleniusRemoveVersionAndDraftFromInputFiles _instance;
        public GleniusRemoveVersionAndDraftFromInputFiles()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GleniusRemoveVersionAndDraftFromInputFiles command.</summary>
        public static GleniusRemoveVersionAndDraftFromInputFiles Instance => _instance;

        public override string EnglishName => "GleniusRemoveVersionAndDraftFromInputFiles";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {

            var inputFiles = director.InputFiles;

            foreach(var f in inputFiles)
            {
                RhinoApp.WriteLine("Input files stored are: " + f);
            }

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Proceed to update current 3dm file?");
            getOption.AcceptNothing(true);
            var update = new OptionToggle(true, "False", "True");
            getOption.AddOptionToggle("Proceed", ref update);
            var result = getOption.Get();
            if (result != GetResult.Cancel)
            {
                var updateFile = update.CurrentValue;

                if(updateFile)
                {
                    for (var i = 0; i < inputFiles.Count; ++i)
                    {
                        var path = inputFiles[i];
                        var fileDir = Path.GetDirectoryName(path);
                        var fileName = Path.GetFileName(path);
                        var extension = Path.GetExtension(path);

                        if (extension == ".stl")
                        {
                            var regex = @"_v[\d+]_draft[\d+]";
                            var r = new Regex(regex, RegexOptions.IgnoreCase);

                            var m = r.Match(fileName);
                            if (m.Success)
                            {
                                var versionAndDraftStr = m.Groups[0].Value;
                                var alteredFileName = fileName.Replace(versionAndDraftStr, "");

                                RhinoApp.WriteLine("Renaming input files in 3dm file from [" + fileName + "] to [" + alteredFileName + "]");

                                director.InputFiles[i] = Path.Combine(fileDir, alteredFileName);
                            }
                        }
                    }

                    var path2 = director.InputFiles;

                    RhinoApp.WriteLine("UPDATE INPUT FILES IN 3DM SUCCESS! SAVE THE DOCUMENT FIRST BEFORE EXIT RHINO!");
                    return Result.Success;
                }
            }
            
            RhinoApp.WriteLine("UPDATE INPUT FILES IN 3DM CANCELED");
            return Result.Failure;
        }
    }

#endif
}
