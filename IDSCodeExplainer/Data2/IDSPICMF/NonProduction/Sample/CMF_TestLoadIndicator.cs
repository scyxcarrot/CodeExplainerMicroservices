using System;
using IDS.CMF;
using IDS.PICMF;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace IDSPICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("a089aabb-9b2c-4635-ae17-6200a88932da")]
    public sealed class CMF_TestLoadIndicator : CmfCommandBase
    {
        public static CMF_TestLoadIndicator Instance { get; private set; }

        public static bool Proceed = true;

        public override string EnglishName => "CMF_TestLoadIndicator";

        public CMF_TestLoadIndicator()
        {
            Instance = this;
            // Each command can independently subscribe to the loading screen event. 
            // The loading screen will only trigger for subscribed commands.
            SubscribedLoadEvent = true;
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!Proceed)
            {
                return Result.Cancel;
            }

            
            //Simulate some operation
            System.Threading.Thread.Sleep(1500);


            throw new Exception("This is an exception");

            return Result.Success;
        }

        // Override for custom logic for Showing/Hiding the Indicator.
        // The derived class can determines what happens when the event is triggered,
        // such as updating UI elements, logging, or other custom operations (e.g. confirmation dialog)
        //protected override void HandleLoadingIndicator(object sender, bool showIndicator)
        //{
        //    if (showIndicator)
        //    {
        //        var result = Dialogs.ShowMessage(
        //            "Confirm to proceed?",
        //            "Testing",
        //            ShowMessageButton.YesNo,
        //            ShowMessageIcon.Exclamation);
        //        if (result == ShowMessageResult.Yes)
        //        {
        //            Proceed = true;
        //            ShowLoadIndicator(true);
        //        }
        //        else
        //        {
        //            Proceed = false;
        //        }

        //    }
        //    else
        //    {
        //        Proceed = false;
        //        ShowLoadIndicator(false);
        //    }
        //}

    }
}