using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using System;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("9B63CE69-670F-476C-ADE5-C1AA398BF25B")]
    public class MtlsRemoveNoiseShells : Command
    {
        public MtlsRemoveNoiseShells()
        {
            Instance = this;
        }

        public static MtlsRemoveNoiseShells Instance { get; private set; }

        public override string EnglishName => "MtlsRemoveNoiseShells";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Mesh mesh;
            Getter.GetMesh("Select mesh",out mesh);

            try
            {
                var fixedMesh = AutoFix.RemoveNoiseShells(mesh);

                if (fixedMesh != null)
                {
                    doc.Objects.Add(fixedMesh);
                    doc.Views.Redraw();
                    return Result.Success;
                }
                RhinoApp.WriteLine("[MDCK::Error] RemoveNoiseShells failed...");
                return Result.Failure;
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.Message);
                if(e.InnerException != null)
                    RhinoApp.WriteLine(e.InnerException.Message);
            }

            return Result.Failure;
        }
    }
}
