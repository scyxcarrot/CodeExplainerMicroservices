using Rhino;
using Rhino.Commands;

namespace IDS.Testing.Commands
{
    [System.Runtime.InteropServices.Guid("E21A9CB1-5680-4C0D-9778-7BDB090C7A44")]
    public class RunGleniusUnitTests : Command
    {
        public RunGleniusUnitTests()
        {
            Instance = this;
        }

        ///<summary>The only instance of the RunGleniusUnitTests command.</summary>
        public static RunGleniusUnitTests Instance { get; private set; }

        public override string EnglishName => "RunGleniusUnitTests";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var everythingIsOk = true;
            
            everythingIsOk &= TestScaffoldCreator.RunFullTest();

            return everythingIsOk ? Result.Success : Result.Failure;
        }
    }
}
