using IDS.Core.Operations;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using System;

namespace IDS.Glenius.Operations
{
    public class TranslateScrew
    {
        public bool DoTranslate(Screw screw)
        {
            if (screw != null)
            {
                var director = screw.Director;

                //Create a copy, because after transformation the original screw will get deleted.
                var xformHeadPoint = new Point3d(screw.HeadPoint);
                var xformTipPoint = new Point3d(screw.TipPoint);
                var screwType = screw.ScrewType;
                var index = screw.Index;
                var gumballAchorPoint = (screw.HeadPoint + screw.TipPoint) / 2;

                var gTransform = new GumballTranslateBrep(director.Document, false);

                //Transforming using GumballTranslateBrep will invoke RhinoDoc Delete and Undelete event, 
                //which invalidates the screw index via ScrewManager
                director.ScrewObjectManager.UnSubscribeScrewInvalidation();
                Transform objectTransform =
                    gTransform.TranslateBrep(screw.Id, gumballAchorPoint, new Guid[] { screw.ScrewAides[ScrewAideType.Mantle] });
                director.ScrewObjectManager.SubscribeScrewInvalidation();

                //Transform the original head and tip points
                xformHeadPoint.Transform(objectTransform);
                xformTipPoint.Transform(objectTransform);

                //Create new screw and set it
                Screw newScrew = new Screw(director, xformHeadPoint, xformTipPoint, screwType, index);
                newScrew.Set(screw.Id, false, false);

                director.Document.Objects.Unlock(newScrew.Id, true);
                director.Document.Views.Redraw();
                return true;
            }

            return false;
        }

    }
}
