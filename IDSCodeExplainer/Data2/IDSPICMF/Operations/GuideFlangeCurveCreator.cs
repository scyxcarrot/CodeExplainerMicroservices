using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.Utilities;
using Rhino.Geometry;
using Rhino.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Operations
{
    public class GuideFlangeCurveCreator
    {
        public enum EResult
        {
            Canceled,
            Failed,
            Success
        }

        private readonly CMFImplantDirector _director;
        private readonly List<Curve> _intersectionCurves;
        public Curve FlangeCurve { get; private set; }
        public Mesh OsteotomyParts { get; private set; }

        public GuideFlangeCurveCreator(CMFImplantDirector director)
        {
            _director = director;

            var objManager = new CMFObjectManager(director);
            var osteotomyParts = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(_director.Document);
            var duplicatedMeshes = osteotomyParts.Select(mesh => mesh.DuplicateMesh());
            OsteotomyParts = MeshUtilities.AppendMeshes(duplicatedMeshes);
            if (OsteotomyParts == null)
            {
                throw new Exception("Osteotomy part invalid.");
            }

            var guideFlangeGuidingBlock = objManager.GetAllBuildingBlocks(IBB.GuideFlangeGuidingOutline);
            _intersectionCurves = new List<Curve>();
            guideFlangeGuidingBlock.ToList().ForEach(x => _intersectionCurves.Add((Curve)x.Geometry));
            if (_intersectionCurves.Count == 0)
            {
                throw new Exception("Guide Flange Guiding Outline invalid.");
            }
        }

        public EResult Draw()
        {
            while(true)
            {
                var dc = new DrawCurveWithAide(_director.Document, OutlineAide.GuideFlangeOutlineDefaultSphereRadius);
                dc.UniqueCurves = true;
                dc.AlwaysOnTop = true;
                dc.SnapCurves = _intersectionCurves;
                dc.ConstraintMesh = OsteotomyParts;
                dc.SetCurveColor(IDS.CMF.Visualization.Colors.GuideFlangeConduit);
                dc.AcceptNothing(true); // Pressing ENTER is allowed
                dc.AcceptUndo(true); // Enables ctrl-z
                dc.SetIsClosedCurve(true);
                dc.PermitObjectSnap(true);

                dc.OnNewCurveAddPoint += (currPt) =>
                {
                    if (dc.GetNumberOfControlPoints() < 1)
                    {
                        Curve endPtClosestCurve;
                        double curveonParams;
                        var ptClosest = CurveUtilities.GetClosestPointFromCurves(_intersectionCurves, currPt, out endPtClosestCurve, out curveonParams);

                        if (currPt.DistanceTo(ptClosest) > 0.001)
                        {
                            return false;
                        }
                    }
                    return true;
                };

                FlangeCurve = dc.Draw();               
                if (dc.Result() == GetResult.Cancel)
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Draw Guide Flange canceled.");
                    return EResult.Canceled;
                }
                else if (dc.Result() == Rhino.Input.GetResult.Nothing || FlangeCurve != null)  
                {
                    return EResult.Success;
                }
            }
            return EResult.Success;
        }

        public EResult EditOutline(Curve flangeOutline, out Curve editedFlangeCurve)
        {
            editedFlangeCurve = null;  
            var editDrawCurve = new DrawCurveWithAide(_director.Document, OutlineAide.GuideFlangeOutlineDefaultSphereRadius);
            editDrawCurve.AlwaysOnTop = true;
            editDrawCurve.ConstraintMesh = OsteotomyParts;
            editDrawCurve.SetExistingCurve(flangeOutline, true, false);
            editDrawCurve.SnapCurves = _intersectionCurves;
            editDrawCurve.SetCurveColor(IDS.CMF.Visualization.Colors.GuideFlangeConduit);
            editDrawCurve.AcceptNothing(true); // Pressing ENTER is allowed
            editDrawCurve.AcceptUndo(true); // Enables ctrl-z
            editDrawCurve.SetCommandPrompt("Drag points to adjust the curve. Press SHIFT or ALT to add/remove point. Enter to accept or Esc to cancel");
            var editedCurve = editDrawCurve.Draw();

            if (editDrawCurve.Result() == GetResult.Cancel)
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Edit Guide Flange Outline canceled.");
                return EResult.Canceled;
            }

            if (editDrawCurve.Result() == GetResult.Nothing)
            {
                if (editedCurve != null)
                {
                    editedFlangeCurve = editedCurve;
                    return EResult.Success;
                }
                return EResult.Failed;
            }
            return EResult.Success;
        }
    }
}
