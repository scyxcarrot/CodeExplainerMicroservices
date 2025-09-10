using IDS.Glenius.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Testing.Commands
{
    [System.Runtime.InteropServices.Guid("8D866C28-57E6-4F28-856B-C6BB48956B38")]
    public class TestScaffoldCreator : Command
    {
        public TestScaffoldCreator()
        {
            Instance = this;
        }
        
        public static TestScaffoldCreator Instance { get; private set; }

        public override string EnglishName => "TestScaffoldCreator";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var everythingSucceeded = RunFullTest();

            return everythingSucceeded ? Result.Success : Result.Failure;
        }

        public static bool RunFullTest()
        {
            var everythingSucceeded = true;

            try
            {
                TestCreateTop();
            }
            catch
            {
                //Assertion failed
                everythingSucceeded = false;
            }
           
            return everythingSucceeded;
        }

        private static void TestCreateTop()
        {
            //Arrange
            var points = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(0, 1, 0),
                new Point3d(1, 0, 0),
                new Point3d(0, 0, 0)
            };
            var curve = Curve.CreateControlPointCurve(points);

            //Act
            var creator = new ScaffoldCreator();
            var created = creator.CreateTop(curve);

            //Assert
            Assert.IsTrue(created);
            Assert.IsNotNull(creator.ScaffoldTop);
        }
    }
}
