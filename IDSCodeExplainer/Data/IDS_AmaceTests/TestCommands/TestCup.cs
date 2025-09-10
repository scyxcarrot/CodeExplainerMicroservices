using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.Commands
{
    [System.Runtime.InteropServices.Guid("66c41661-a0e8-4d30-a280-29ab2c001829")]
    public class TestCup : Command
    {
        public TestCup()
        {
            Instance = this;
        }

        ///<summary>The only instance of the TestCupClass command.</summary>
        public static TestCup Instance { get; private set; }

        public override string EnglishName => "TestCup";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var everythingSucceeded = RunFullTest();

            return everythingSucceeded ? Result.Success : Result.Failure;
        }

        public static bool RunFullTest()
        {
            var everythingIsOk = true;

            var coordinateSystem = Plane.WorldXY;
            var centerOfRotation = new Point3d(0, 0, 0);

            // Default parameters
            var fixedAnteversion = new List<double> { Cup.anteversionDefault };
            var fixedInclination = new List<double> { Cup.inclinationDefault };
            var fixedAperture = new List<double> { Cup.apertureAngleDefault };
            var fixedInnerDiameter = new List<double> { Cup.innerDiameterDefault };
            var fixedThickness = new List<double> { 4 };
            var fixedPorous = new List<double> { 1 };

            // Variable parameters
            var varyingAnteversion = MathUtilities.Range(0, 360.0, 1).ToList();
            var varyingInclination = MathUtilities.Range(0, 360.0, 1).ToList();
            var varyingAperture = MathUtilities.Range(Cup.apertureAngleMin, Cup.apertureAngleMax, 1).ToList();
            var varyingInnerDiameter = MathUtilities.Range(Cup.innerDiameterMin, Cup.innerDiameterMax, 1).ToList();
            var varyingThickness = MathUtilities.Range(3.0, 4.0, 1).ToList();
            var varyingPorous = MathUtilities.Range(0, 2.0, 1).ToList();

            // Vary anteversion
            everythingIsOk &= TestParameterVariation(coordinateSystem,
                                                        centerOfRotation,
                                                        varyingAnteversion,
                                                        fixedInclination,
                                                        fixedAperture,
                                                        fixedInnerDiameter,
                                                        fixedThickness,
                                                        fixedPorous);

            // Vary inclination
            everythingIsOk &= TestParameterVariation(coordinateSystem,
                                                        centerOfRotation,
                                                        fixedAnteversion,
                                                        varyingInclination,
                                                        fixedAperture,
                                                        fixedInnerDiameter,
                                                        fixedThickness,
                                                        fixedPorous);

            // Vary aperture
            everythingIsOk &= TestParameterVariation(coordinateSystem,
                                                        centerOfRotation,
                                                        fixedAnteversion,
                                                        fixedInclination,
                                                        varyingAperture,
                                                        fixedInnerDiameter,
                                                        fixedThickness,
                                                        fixedPorous);

            // Vary inner diameter
            everythingIsOk &= TestParameterVariation(coordinateSystem,
                                                        centerOfRotation,
                                                        fixedAnteversion,
                                                        fixedInclination,
                                                        fixedAperture,
                                                        varyingInnerDiameter,
                                                        fixedThickness,
                                                        fixedPorous);

            // Vary thickness
            everythingIsOk &= TestParameterVariation(coordinateSystem,
                                                        centerOfRotation,
                                                        fixedAnteversion,
                                                        fixedInclination,
                                                        fixedAperture,
                                                        fixedInnerDiameter,
                                                        varyingThickness,
                                                        fixedPorous);

            // Vary porous thickness
            everythingIsOk &= TestParameterVariation(coordinateSystem,
                                                        centerOfRotation,
                                                        fixedAnteversion,
                                                        fixedInclination,
                                                        fixedAperture,
                                                        fixedInnerDiameter,
                                                        fixedThickness,
                                                        varyingPorous);

            Reporting.ShowResultsInCommandLine(everythingIsOk, "Cup Class");

            return everythingIsOk;
        }

        private static bool TestParameterVariation(Plane coordinateSystem, 
                                Point3d centerOfRotation, 
                                List<double> anteversionList,
                                List<double> inclinationList,
                                List<double> apertureList,
                                List<double> innerDiameterList,
                                List<double> thicknessList,
                                List<double> porousThicknessList)
        {
            var everythingIsOk = true;

            foreach (var anteversion in anteversionList)
            {
                foreach (var inclination in inclinationList)
                {
                    foreach (var apertureAngle in apertureList)
                    {
                        foreach (var innerDiameter in innerDiameterList)
                        {
                            foreach (var thickness in thicknessList)
                            {
                                foreach (var porousThickness in porousThicknessList)
                                {
                                    foreach (var defectIsLeft in new List<bool> { true, false })
                                    {
                                        foreach (var design in new List<CupDesign> { CupDesign.v1, CupDesign.v2})
                                        {
                                            var cupType = new CupType(thickness, porousThickness, design);
                                            var cup = new Cup(centerOfRotation, cupType, anteversion, inclination, apertureAngle, innerDiameter, coordinateSystem, defectIsLeft);
                                            // Input properties
                                            everythingIsOk &= Reporting.CompareValues(anteversion, cup.anteversion, "anteversion");
                                            everythingIsOk &= Reporting.CompareValues(inclination, cup.inclination, "inclination");
                                            everythingIsOk &= Reporting.CompareValues(apertureAngle, cup.apertureAngle, "apertureAngle");
                                            everythingIsOk &= Reporting.CompareValues(innerDiameter, cup.innerCupDiameter, "innerDiameter");
                                            everythingIsOk &= Reporting.CompareValues(thickness, cup.cupType.CupThickness, "thickness");
                                            everythingIsOk &= Reporting.CompareValues(porousThickness, cup.cupType.PorousThickness, "porousThickness");
                                            // Derived values
                                            var innerReamingDiameter = cup.innerCupDiameter;
                                            everythingIsOk &= Reporting.CompareValues(innerReamingDiameter, cup.innerReamingDiameter, "innerReamingDiameter");
                                            var outerReamingDiameter = cup.innerCupDiameter + 2 * cup.cupType.CupThickness + 2 * cup.cupType.PorousThickness;
                                            everythingIsOk &= Reporting.CompareValues(outerReamingDiameter, cup.outerReamingDiameter, "outerReamingDiameter");
                                            
                                            everythingIsOk &= cup.innerReamingVolumeMesh.IsClosed;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return everythingIsOk;
        }   
    }
}
