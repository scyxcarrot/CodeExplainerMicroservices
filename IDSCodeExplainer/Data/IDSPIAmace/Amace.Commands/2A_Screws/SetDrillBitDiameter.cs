using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Common;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using System;

namespace IDS.Commands.Screws
{
    [System.Runtime.InteropServices.Guid("F9BBAE8D-0636-400A-B6FF-053D9104C59F")]
    [IDSCommandAttributes(true, DesignPhase.Screws, IBB.Cup, IBB.WrapBottom, IBB.WrapTop, IBB.WrapSunkScrew)]
    public class SetDrillBitDiameter : CommandBase<ImplantDirector>
    {
        public SetDrillBitDiameter()
        {
            TheCommand = this;
        }

        public static SetDrillBitDiameter TheCommand { get; private set; }

        public override string EnglishName => "SetDrillBitDiameter";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Get user input
            var getDrillBitDiameter = new GetNumber();
            getDrillBitDiameter.SetCommandPrompt("Set Drill Bit Diameter in the interval [3.2,4]");
            getDrillBitDiameter.SetDefaultNumber(ScrewAideManager.ConvertToDrillBitDiameter(director.DrillBitRadius));
            getDrillBitDiameter.SetLowerLimit(3.2, false);
            getDrillBitDiameter.SetUpperLimit(4.0, false);
            getDrillBitDiameter.Get();

            if (getDrillBitDiameter.CommandResult() != Result.Success)
            {
                return Result.Failure;
            }

            var drillBitDiameterRoundedToNearestTenth = Math.Round(getDrillBitDiameter.Number(), 1, MidpointRounding.AwayFromZero);
            var drillBitRadius = ScrewAideManager.ConvertToDrillBitRadius(drillBitDiameterRoundedToNearestTenth);
            director.DrillBitRadius = drillBitRadius;

            // Update screw checks
            ScrewInfo.Update(doc);

            return Result.Success;
        }
    }
}