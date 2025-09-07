using Rhino;
using Rhino.Commands;

namespace IDS.Testing.Commands
{
    [System.Runtime.InteropServices.Guid("ad87088d-8427-4ad7-9e1a-cfdc252449f4")]
    public class RunAmaceUnitTests : Command
    {
        public RunAmaceUnitTests()
        {
            Instance = this;
        }

        ///<summary>The only instance of the RunAmaceUnitTests command.</summary>
        public static RunAmaceUnitTests Instance { get; private set; }

        public override string EnglishName => "RunAmaceUnitTests";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var everythingIsOk = true;

            everythingIsOk &= TestCup.RunFullTest();
            everythingIsOk &= TestScrewQC.RunFullTest(doc);
            everythingIsOk &= TestFea.RunFullTest();
            everythingIsOk &= TestMedialBumpCriterion.RunFullTest(doc);
            everythingIsOk &= TestGuideHoleBooleanIntersection.RunFullTest(doc);

            return everythingIsOk ? Result.Success : Result.Failure;
        }
    }
}
