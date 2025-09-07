using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using System;

namespace IDS.Core.Drawing
{
    public class EditCurveDeletable : DrawCurve
    {
        public bool DisableDeleteCurveRequest { get; set; }
        public bool DeleteCurveRequested { get; private set; }
        private readonly Guid existingCurveGuid;
        private readonly RhinoDoc document;

        //In order to delete, remember to set the Guid of the Curve
        public EditCurveDeletable(RhinoDoc doc, Guid existingCurveGuid, bool closed, bool lockedEnds) : base(doc)
        {
            this.document = doc;
            DisableDeleteCurveRequest = false;
            DeleteCurveRequested = false;

            var theCurveToEdit = document.Objects.Find(existingCurveGuid).Geometry as Curve;

            if (theCurveToEdit != null)
            {
                base.SetExistingCurve(theCurveToEdit.DuplicateCurve(), closed, lockedEnds);
                this.existingCurveGuid = existingCurveGuid;
            }
            else
            {
                throw new IDSException("EditCurveDeletable - existingCurveGuid is not a Curve type object!");
            }

        }

        protected new void OnKeyboard(int key)
        {
            base.OnKeyboard(key);

            if (key == 46 && !DisableDeleteCurveRequest)
            {
                DeleteCurveRequested = true;
                RhinoApp.SendKeystrokes("!", true); //escape from GetBaseClass
            }
        }

        [System.Obsolete("This method is not working for this class, re-instantiate the class instead with the curve you want to set", true)]
        public new void SetExistingCurve(Curve existing, bool _closed, bool lockedEnds)
        {
            throw new NotSupportedException("This method is not working for this class, re-instantiate the class instead with the curve you want to set");
        }

        [System.Obsolete("This method is not working for this class as it is only for editing by calling Edit()", true)]
        public void Draw()
        {
            throw new NotSupportedException("This method is not working for this class as it is only for editing by calling Edit()");
        }

        //Returns true if something has changed or user press Enter
        public bool Edit()
        {
            if (existingCurve == null || existingCurveGuid == Guid.Empty)
            {
                return false;
            }

            document.Objects.Unlock(existingCurveGuid, true);

            DeleteCurveRequested = false;
            RhinoApp.KeyboardEvent += OnKeyboard;

            var resCurve = base.Draw();

            RhinoApp.KeyboardEvent -= OnKeyboard;

            //Delete Curve
            //deleteCurveRequested is only possible to be true if DisableDeleteCurveRequest is false
            //If curve deletion is requestion, Result is Cancel and deleteCurveRequested is true
            if (Result() == GetResult.Cancel && DeleteCurveRequested)
            {
                RhinoDoc.ActiveDoc.Objects.Delete(existingCurveGuid, true);
                document.Views.Redraw();
                return true;
            }

            //Curve is edited
            if (resCurve != null)
            {
                document.Objects.Replace(existingCurveGuid, resCurve);
                document.Views.Redraw();
                return true;
            }

            //Nothing has changed
            return false;
        }
    }
}
