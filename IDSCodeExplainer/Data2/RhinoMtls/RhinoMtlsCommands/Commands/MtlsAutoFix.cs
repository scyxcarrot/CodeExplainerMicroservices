using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using System;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("3423678b-d0bf-4843-a562-4ca97cda8d78")]
    public class MtlsAutoFix : Command
    {
        public MtlsAutoFix()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MtlsAutoFix command.</summary>
        public static MtlsAutoFix Instance { get; private set; }

        public override string EnglishName => "MtlsAutoFix";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Mesh mesh;
            Getter.GetMesh("Select mesh",out mesh);

            try
            {
                var fixedMesh = AutoFix.PerformAutoFix(mesh, 30);

                if (fixedMesh != null)
                {
                    doc.Objects.Add(fixedMesh);
                    doc.Views.Redraw();
                    return Result.Success;
                }
                RhinoApp.WriteLine("[MDCK::Error] AutoFixed failed...");
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
