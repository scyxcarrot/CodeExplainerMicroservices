#if (STAGING)

using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("EA3E2924-396D-4210-8048-F8FC76649174")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ProPlanImport)]
    public class CMF_TestViews : CmfCommandBase
    {
        public CMF_TestViews()
        {
            Instance = this;
        }

        public static CMF_TestViews Instance { get; private set; }

        public override string EnglishName => "CMF_TestViews";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var go = new GetOption();
            go.SetCommandPrompt("Choose plane.");
            go.AcceptNothing(true);

            var selectedView = CameraView.Front;
            go.AddOptionEnumList("View", selectedView);

            while (true)
            {
                var result = go.Get();
                if (result == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (result == GetResult.Nothing)
                {
                    break;
                }

                if (result == GetResult.Option)
                {
                    selectedView = go.GetSelectedEnumValue<CameraView>();
                    break;
                }
            }

            View.SetView(doc, director.MedicalCoordinateSystem.AxialPlane.Origin, selectedView);

            return Result.Success;
        }
    }
}

#endif