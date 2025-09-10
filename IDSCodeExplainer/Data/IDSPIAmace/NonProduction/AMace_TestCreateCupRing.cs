#if (INTERNAL)

using IDS.Amace;
using IDS.Core.NonProduction;
using Rhino;
using Rhino.Commands;

namespace IDS.Commands.NonProduction
{
    [System.Runtime.InteropServices.Guid("81299848-aa5d-4e4a-9f10-983fc2fd925c")]
    public class AMace_TestCreateCupRing : Command
    {
        static AMace_TestCreateCupRing _instance;
        public AMace_TestCreateCupRing()
        {
            _instance = this;
        }

        ///<summary>The only instance of the TestCreateCupRing command.</summary>
        public static AMace_TestCreateCupRing Instance => _instance;

        public override string EnglishName => "AMace_TestCreateCupRing";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = new ImplantDirector(doc, PlugInInfo.PluginModel);

            if (director.cup != null && director.cup.cupType.CupDesign == Amace.ImplantBuildingBlocks.CupDesign.v2)
            {
                var v2CupRing = director.cup.GetCupRing();
                InternalUtilities.AddObject(v2CupRing, "v2CupRing", "Testing::CupRing");

                return Result.Success;
            }

            return Result.Failure;
        }
    }
}

#endif