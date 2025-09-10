#if DEBUG

using System;
using System.Collections.Generic;
using System.Drawing;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Visualization;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.DocObjects.Custom;
using Rhino.Commands;

using IDS.Operations.CupPositioning;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.Commands.Hidden
{
    [System.Runtime.InteropServices.Guid("43d7c2bd-869c-4e12-9bdf-064be5391e11")]
    public class DrawCups : Command
    {
        public DrawCups()
        {
            Instance = this;
        }

        public static DrawCups Instance { get; private set; }

        public override string EnglishName => "DrawCups";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var getDiameter = new GetNumber();
            getDiameter.SetLowerLimit(Cup.innerDiameterMin,false);
            getDiameter.SetUpperLimit(Cup.innerDiameterMax,false);
            getDiameter.SetDefaultNumber(Cup.innerDiameterDefault);
            getDiameter.AcceptNothing(true);
            
            var res = getDiameter.Get();

            if (res == GetResult.Cancel)
            {
                return Result.Cancel;
            }

            if (res == GetResult.Nothing || res == GetResult.Number)
            {
                //AddCupCurvesToDocument(doc, new CupType(2, 1, CupDesign.v1),getDiameter.Number());
                //AddCupCurvesToDocument(doc, new CupType(2, 2, CupDesign.v1),getDiameter.Number());
                //AddCupCurvesToDocument(doc, new CupType(4, 1, CupDesign.v1),getDiameter.Number());
                //AddCupCurvesToDocument(doc, new CupType(2, 1, CupDesign.v2), getDiameter.Number());
                //AddCupCurvesToDocument(doc, new CupType(3, 1, CupDesign.v2), getDiameter.Number());
                AddCupCurvesToDocument(doc, new CupType(4, 1, CupDesign.v2),getDiameter.Number());
            }

            return Result.Success;
        }

        private static void AddCupToDocument(RhinoDoc doc, CupType cupType)
        {
            const double aperture = 170;
            const double anteversion = 0;
            const double inclination = 180;
            const double diameter = 54;

            var cup = new Cup(Point3d.Origin, cupType, anteversion, inclination, aperture, diameter, Plane.WorldZX,false)
            {
                Attributes =
                {
                    ObjectColor = IDS.Amace.Visualization.Colors.MetalCup,
                    MaterialSource = ObjectMaterialSource.MaterialFromObject,
                    ColorSource = ObjectColorSource.ColorFromMaterial
                }
            };
            doc.Objects.AddRhinoObject((CustomBrepObject)cup);

            var porousAttributes = new ObjectAttributes
            {
                ObjectColor = IDS.Amace.Visualization.Colors.PorousOrange,
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromMaterial
            };
            doc.Objects.AddBrep(cup.porousShell, porousAttributes);
        }

        private static void AddCupCurvesToDocument(RhinoDoc doc, CupType cupType, double diameter)
        {
            const double aperture = 170;
            const double anteversion = 0;
            const double inclination = 180;
            const double horizontalBorderWidth = 2;

            var cup = new Cup(Point3d.Origin, cupType, anteversion, inclination, aperture, diameter, Plane.WorldZX, false)
            {
                Attributes =
                {
                    ObjectColor = IDS.Amace.Visualization.Colors.MetalCup,
                    MaterialSource = ObjectMaterialSource.MaterialFromObject,
                    ColorSource = ObjectColorSource.ColorFromMaterial
                }
            };

            List<Curve> cupCurves;
            List<Curve> porousCurves;
            switch (cupType.CupDesign)
            {
                case CupDesign.v2:
                    cupCurves = Cup.GetRingDesignCupCurves(aperture, diameter, cupType.CupThickness,
                        horizontalBorderWidth, Cup.GetPolishingOffsetValue(cupType.CupDesign));
                    porousCurves = cup.GetRingDesignPorousShellCurves();
                    break;
                case CupDesign.v1:
                    cupCurves = Cup.GetSmoothDesignCupCurves(aperture, diameter, cupType.CupThickness);
                    porousCurves = cup.GetSmoothDesignPorousShellCurves();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            const double referenceEndArcLength = 8.0;
            var cupReamerMaker = new CupReamerMaker(cupType.CupThickness, cupType.PorousThickness, cup.InnerCupRadius, cup.AngleHorizontalBorder, referenceEndArcLength);
            var reamerCurve = cupReamerMaker.CreateCupReamerCurve(80, true);
            reamerCurve.Translate(new Vector3d(0, cup.InnerCupRadius, 0));

            var cupAttributes = new ObjectAttributes { ObjectColor = Colors.MetalCup, ColorSource = ObjectColorSource.ColorFromObject };
            foreach (var curve in cupCurves)
            {
                doc.Objects.AddCurve(curve, cupAttributes);
            }

            var porousAttributes = new ObjectAttributes { ObjectColor = Colors.PorousOrange, ColorSource = ObjectColorSource.ColorFromObject };
            foreach (var curve in porousCurves)
            {
                doc.Objects.AddCurve(curve, porousAttributes);
            }

            var reamerAttributes = new ObjectAttributes { ObjectColor = Color.Black, ColorSource = ObjectColorSource.ColorFromObject };
            doc.Objects.AddCurve(reamerCurve, reamerAttributes );

            doc.Views.Redraw();
        }
    }
}

#endif