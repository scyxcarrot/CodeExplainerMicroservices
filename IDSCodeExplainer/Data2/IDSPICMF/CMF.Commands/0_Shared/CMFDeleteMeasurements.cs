using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("00FD092C-261A-4D5D-9178-48B07FDC1E50")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMFDeleteMeasurements : CmfCommandBase
    {
        static CMFDeleteMeasurements _instance;
        public CMFDeleteMeasurements()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFDoMeasurements command.</summary>
        public static CMFDeleteMeasurements Instance => _instance;

        public override string EnglishName => IDS.CMF.Constants.CommandEnglishName.CMFDeleteMeasurements;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            foreach (var rhinoObject in doc.Objects)
            {
                if (rhinoObject.Geometry is LinearDimension)
                {
                    doc.Objects.Unlock(rhinoObject.Id, true);
                }
            }

            var selectMeasurement = new GetObject();
            selectMeasurement.SetCommandPrompt("Select measurement(s) to delete.");
            selectMeasurement.DisablePreSelect();
            selectMeasurement.AcceptNothing(true);
            selectMeasurement.EnableHighlight(true);
            
            var selectedObj = new List<ObjRef>();
            while (true)
            {
                var res = selectMeasurement.Get();
                if (res == GetResult.Object)
                {
                    var obj = selectMeasurement.Object(0);

                    selectedObj.Add(obj);
                    DimensionVisualizer.Instance.SetColor(Color.Yellow, obj);
                    doc.Views.Redraw();
                }
                else if (res == GetResult.Cancel)
                {
                    selectedObj.ForEach(x =>
                    {
                        DimensionVisualizer.Instance.SetColorToDefault(x);
                    });

                    return Result.Cancel;
                }
                else
                {
                    break;
                }
            }

            selectedObj.ForEach(x => doc.Objects.Delete(x.ObjectId, true));

            doc.Views.Redraw();

            return Result.Success;
        }

    }

}