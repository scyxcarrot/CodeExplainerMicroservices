using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.Glenius.Operations
{
    public class SolidWallCreator
    {
        public enum EResult
        {
            Canceled,
            Failed,
            Success
        }

        private readonly RhinoDoc document;
        public Dictionary<Curve, Mesh> SolidWalls { get;}

        private readonly Mesh topBottomConnectingMesh;
        private readonly Curve topCurve;
        private readonly Curve bottomCurve;

        //Preferably the topBottomConnectingMesh is generated here
        public SolidWallCreator(RhinoDoc document, Curve topCurve, Curve bottomCurve, Mesh topBottomConnectingMesh)
        {
            SolidWalls = new Dictionary<Curve, Mesh>();

            this.document = document;
            this.topBottomConnectingMesh = topBottomConnectingMesh;
            this.topCurve = topCurve;
            this.bottomCurve = bottomCurve;
        }

        public EResult CreateSolidWall()
        {
            SolidWallCurveCreator sideWallDrawer =
                new SolidWallCurveCreator(document, topCurve, bottomCurve, topBottomConnectingMesh);

            var res = sideWallDrawer.Draw();
            if (res == SolidWallCurveCreator.EResult.Success)
            {
                var solidWallCreator = new SolidWallWrapCreator(sideWallDrawer.SolidWallCurve, topBottomConnectingMesh);

                if (solidWallCreator.Create())
                {
                    SolidWalls.Add(sideWallDrawer.SolidWallCurve, solidWallCreator.SolidWall);
                    return EResult.Success;
                }

                return EResult.Failed;
            }

            if (res == SolidWallCurveCreator.EResult.Canceled)
            {
                return EResult.Canceled;
            }

            return EResult.Failed;
        }

        public EResult EditSolidWall(Curve existingCurve, out KeyValuePair<Curve, Mesh> editedSolidWall)
        {
            editedSolidWall = new KeyValuePair<Curve, Mesh>();

            SolidWallCurveCreator curveCreator = new SolidWallCurveCreator(document, topCurve, bottomCurve, topBottomConnectingMesh);

            Curve editedCurve;
            var editResult = curveCreator.Edit(existingCurve.DuplicateCurve(), out editedCurve);

            if (editResult == SolidWallCurveCreator.EResult.Success)
            {

                SolidWallWrapCreator wrapCreator = new SolidWallWrapCreator(editedCurve, topBottomConnectingMesh);
                if (wrapCreator.Create())
                {
                    editedSolidWall = new KeyValuePair<Curve, Mesh>(editedCurve, wrapCreator.SolidWall);
                    return EResult.Success;
                }
            }
            else if (editResult == SolidWallCurveCreator.EResult.Canceled)
            {
                return EResult.Canceled;
            }
            else { }

            return EResult.Failed;
        }


    }
}
