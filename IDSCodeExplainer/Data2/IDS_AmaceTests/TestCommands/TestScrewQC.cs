using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Quality;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.SplashScreen;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.Testing.Commands
{
    [System.Runtime.InteropServices.Guid("10a0a3c7-40c7-4bb0-8a2c-830d443161ed")]
    public class TestScrewQC : Command
    {
        private const string ExportFolder = @"C:\Dev\IDS\UnitTestExports";

        public TestScrewQC()
        {
            Instance = this;
        }

        ///<summary>The only instance of the TestScrewQC command.</summary>
        public static TestScrewQC Instance { get; private set; }

        public override string EnglishName => "TestScrewQC";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var everythingSucceeded = RunFullTest(doc);

            return everythingSucceeded ? Result.Success : Result.Failure;
        }

        public static bool RunFullTest(RhinoDoc doc)
        {
            var everythingSucceeded = true;

            var director = new ImplantDirector(doc, new PluginInfoModel());

            var logLines = new List<string>();

            AddDividerToLog(logLines);
            logLines.Add("SCREW QC UNIT TEST");
            AddDividerToLog(logLines);

            logLines.AddRange(VersionControl.GetVersionCheckText(null, true).Split(new char[] { '\n' }));
            AddDividerToLog(logLines);
            logLines.Add("");

            logLines.Add("TEST: Vicinity Screw Head");
            everythingSucceeded &= TestHeadIntersectionAndVicinity(director, 0, "VicinityScrewHead", ref logLines);
            LogResult(everythingSucceeded, logLines);
            AddDividerToLog(logLines);

            logLines.Add("TEST: Vicinity Screw Shaft");
            everythingSucceeded &= TestShaftIntersectionAndVicinity(director, 0, "VicinityScrewShaft", ref logLines);
            LogResult(everythingSucceeded, logLines);
            AddDividerToLog(logLines);

            logLines.Add("TEST: Vicinity Screw Shaft Rotated");
            everythingSucceeded &= TestShaftIntersectionAndVicinity(director, 25, "VicinityScrewShaftRotation", ref logLines);
            LogResult(everythingSucceeded, logLines);
            AddDividerToLog(logLines);

            logLines.Add("TEST: Vicinity Screw Head to Shaft");
            everythingSucceeded &= TestHeadShaftIntersectionAndVicinity(director, "VicinityScrewHeadShaft", ref logLines);
            LogResult(everythingSucceeded, logLines);
            AddDividerToLog(logLines);

            logLines.Add("UNIT TEST RESULT");
            LogResult(everythingSucceeded, logLines);

            File.WriteAllLines(Path.Combine(ExportFolder, "LogScrewQCTest.log"), logLines);

            Reporting.ShowResultsInCommandLine(everythingSucceeded, "Screw QC", logLines);
            return everythingSucceeded;
        }

        private static void AddDividerToLog(List<string> logLines)
        {
            logLines.Add("");
            logLines.Add("-----------------------------------------------------");
            logLines.Add("");
        }

        private static bool TestHeadShaftIntersectionAndVicinity(ImplantDirector director, string stlPrefix, ref List<string> logLines)
        {
            var source = new Screw(director, new Point3d(0, 0, 0), new Point3d(0, 0, 100), ScrewType.AO_D65, ScrewAlignment.Sunk, 1, 0);

            var justOverlapping = source.Radius + source.HeadRadius - 0.01;
            var justNotOverlapping = source.Radius + source.HeadRadius + 0.01;
            var justClose = source.Radius + source.HeadRadius + 0.99;
            var justNotClose = source.Radius + source.HeadRadius + 1.01;

            const double headY = 50;

            var screwJustOverlapping = new Screw(director, new Point3d(justOverlapping, headY, source.HeadCenter.Z), new Point3d(justOverlapping, -headY, source.HeadCenter.Z), ScrewType.AO_D65, ScrewAlignment.Sunk, 2, 0);
            var screwJustNotOverlapping = new Screw(director, new Point3d(justNotOverlapping, headY, source.HeadCenter.Z), new Point3d(justNotOverlapping, -headY, source.HeadCenter.Z), ScrewType.AO_D65, ScrewAlignment.Sunk, 3, 0);
            var screwJustClose = new Screw(director, new Point3d(justClose, headY, source.HeadCenter.Z), new Point3d(justClose, -headY, source.HeadCenter.Z), ScrewType.AO_D65, ScrewAlignment.Sunk, 4, 0);
            var screwJustNotClose = new Screw(director, new Point3d(justNotClose, headY, source.HeadCenter.Z), new Point3d(justNotClose, -headY, source.HeadCenter.Z), ScrewType.AO_D65, ScrewAlignment.Sunk, 5, 0);

            ExportScrews(director, stlPrefix, new List<Screw>() { source, screwJustOverlapping, screwJustNotOverlapping, screwJustClose, screwJustNotClose }, new List<string>() { "1_Source", "2_Overlapping", "3_NotOverlapping", "4_Close", "5_NotClose" });

            var intersectionsExpectations = new Dictionary<int, List<int>>
            {
                {1, new List<int>() {2}},
                {2, new List<int>() {1, 3, 4, 5}},
                {3, new List<int>() {2, 4, 5}},
                {4, new List<int>() {2, 3, 5}},
                {5, new List<int>() {2, 3, 4}}
            };

            var vicinityExpectations = new Dictionary<int, List<int>>
            {
                {1, new List<int>() {2, 3, 4}},
                {2, new List<int>() {1, 3, 4, 5}},
                {3, new List<int>() {1, 2, 4, 5}},
                {4, new List<int>() {1, 2, 3, 5}},
                {5, new List<int>() {2, 3, 4}}
            };

            var everythingSucceeded = TestScrewIntersectionAndVicinity(source, screwJustOverlapping, screwJustNotOverlapping, screwJustClose, screwJustNotClose, ref logLines, intersectionsExpectations, vicinityExpectations);

            return everythingSucceeded;
        }

        private static void LogResult(bool succeeded, List<string> logLines)
        {
            logLines.Add(succeeded ? "SUCCESS" : "FAILURE");
        }

        private static bool TestShaftIntersectionAndVicinity(ImplantDirector director, double deviationZtip, string stlPrefix, ref List<string> logLines)
        {
            var source = new Screw(director, new Point3d(0, 0, 0), new Point3d(0, 0, 100), ScrewType.AO_D65, ScrewAlignment.Sunk, 1, 0);

            var justOverlapping = source.Diameter - 0.01;
            var justNotOverlapping = source.Diameter + 0.01;
            var justClose = source.Diameter + 0.99;
            var justNotClose = source.Diameter + 1.01;

            const double headY = 50;

            var screwJustOverlapping = new Screw(director, new Point3d(justOverlapping, headY, 20), new Point3d(justOverlapping, -headY, 20 + deviationZtip), ScrewType.AO_D65, ScrewAlignment.Sunk, 2, 0);
            var screwJustNotOverlapping = new Screw(director, new Point3d(justNotOverlapping, headY, 40), new Point3d(justNotOverlapping, -headY, 40 + deviationZtip), ScrewType.AO_D65, ScrewAlignment.Sunk, 3, 0);
            var screwJustClose = new Screw(director, new Point3d(justClose, headY, 60), new Point3d(justClose, -headY, 60 + deviationZtip), ScrewType.AO_D65, ScrewAlignment.Sunk, 4, 0);
            var screwJustNotClose = new Screw(director, new Point3d(justNotClose, headY, 80), new Point3d(justNotClose, -headY, 80 + deviationZtip), ScrewType.AO_D65, ScrewAlignment.Sunk, 5, 0);

            ExportScrews(director, stlPrefix, new List<Screw>() { source, screwJustOverlapping, screwJustNotOverlapping, screwJustClose, screwJustNotClose }, new List<string>() { "1_Source", "2_Overlapping", "3_NotOverlapping", "4_Close", "5_NotClose" });

            var intersectionsExpectations = new Dictionary<int, List<int>> {{1, new List<int>() {2}}, {2, new List<int>() {1}}};

            var vicinityExpectations = new Dictionary<int, List<int>>
            {
                {1, new List<int>() {2, 3, 4}},
                {2, new List<int>() {1}},
                {3, new List<int>() {1}},
                {4, new List<int>() {1}}
            };

            var everythingSucceeded = TestScrewIntersectionAndVicinity(source, screwJustOverlapping, screwJustNotOverlapping, screwJustClose, screwJustNotClose, ref logLines, intersectionsExpectations, vicinityExpectations);

            return everythingSucceeded;
        }

        private static bool TestHeadIntersectionAndVicinity(ImplantDirector director, double outOffset, string stlPrefix, ref List<string> logLines)
        {
            var source = new Screw(director, new Point3d(0, 0, 0), new Point3d(0, 0, 50), ScrewType.AO_D65, ScrewAlignment.Sunk, 1, 0);

            var justOverlapping = source.HeadRadius * 2 - 0.01;
            var justNotOverlapping = source.HeadRadius * 2 + 0.01;
            var justClose = source.HeadRadius * 2 + 0.99;
            var justNotClose = source.HeadRadius * 2 + 1.01;

            var screwJustOverlapping = new Screw(director, new Point3d(0, justOverlapping, 0), new Point3d(0, justOverlapping + outOffset, 50), ScrewType.AO_D65, ScrewAlignment.Sunk, 2, 0);
            var screwJustNotOverlapping = new Screw(director, new Point3d(0, -justNotOverlapping, 0), new Point3d(0, -justNotOverlapping - outOffset, 50), ScrewType.AO_D65, ScrewAlignment.Sunk, 3, 0);
            var screwJustClose = new Screw(director, new Point3d(justClose, 0, 0), new Point3d(justClose + outOffset, 0, 50), ScrewType.AO_D65, ScrewAlignment.Sunk, 4, 0);
            var screwJustNotClose = new Screw(director, new Point3d(-justNotClose, 0, 0), new Point3d(-justNotClose - outOffset, 0, 50), ScrewType.AO_D65, ScrewAlignment.Sunk, 5, 0);

            ExportScrews(director, stlPrefix, new List<Screw>() { source, screwJustOverlapping, screwJustNotOverlapping, screwJustClose, screwJustNotClose }, new List<string>() { "1_Source", "2_Overlapping", "3_NotOverlapping", "4_Close", "5_NotClose" });

            var intersectionsExpectations = new Dictionary<int, List<int>> {{1, new List<int>() {2}}, {2, new List<int>() {1}}};

            var vicinityExpectations = new Dictionary<int, List<int>>
            {
                {1, new List<int>() {2, 3, 4}},
                {2, new List<int>() {1}},
                {3, new List<int>() {1}},
                {4, new List<int>() {1}}
            };

            var everythingSucceeded = TestScrewIntersectionAndVicinity(source, screwJustOverlapping, screwJustNotOverlapping, screwJustClose, screwJustNotClose, ref logLines, intersectionsExpectations, vicinityExpectations);

            return everythingSucceeded;
        }

        private static void ExportScrews(ImplantDirector director, string prefix, List<Screw> screws, List<string> suffixes)
        {
            var objectManager = new AmaceObjectManager(director);
            for (var i = 0; i < screws.Count; i++)
            {
                var id = objectManager.AddNewBuildingBlock(IBB.Screw, screws[i].Geometry);

                List<string> exportedFiles;
                var list = new List<IBB>() { IBB.Screw };
                BlockExporter.ExportBuildingBlocks(director, list.Select(block => BuildingBlocks.Blocks[block]).ToList(), ExportFolder, prefix, suffixes[i], out exportedFiles);

                objectManager.DeleteObject(id);
            }
        }

        private static bool TestScrewIntersectionAndVicinity(Screw source, Screw screwJustOverlapping, Screw screwJustNotOverlapping, Screw screwJustClose, Screw screwJustNotClose, ref List<string> logLines, Dictionary<int, List<int>> intersectionsExpectations, Dictionary<int, List<int>> vicinityExpectations)
        {
            var screwAnalysis = new AmaceScrewAnalysis();
            var intersectionsActual = screwAnalysis.PerformScrewIntersectionCheck(new List<Screw>() { source, screwJustOverlapping, screwJustNotOverlapping, screwJustClose, screwJustNotClose }, 0);
            var vicinityActual = screwAnalysis.PerformScrewIntersectionCheck(new List<Screw>() { source, screwJustOverlapping, screwJustNotOverlapping, screwJustClose, screwJustNotClose }, 1);

            var matching = true;

            logLines.Add("Intersection");
            matching &= DoComparisonTest(intersectionsActual, intersectionsExpectations, ref logLines);
            logLines.Add("Vicinity");
            matching &= DoComparisonTest(vicinityActual, vicinityExpectations, ref logLines);

            return matching;
        }

        private static bool DoComparisonTest(Dictionary<int, List<int>> actual, Dictionary<int, List<int>> expectation, ref List<string> logLines)
        {
            var matching = true;
            matching &= CompareList(actual, expectation);
            matching &= CompareList(expectation, actual);

            logLines.Add("Actual");
            AddDictToLog(actual, ref logLines);
            logLines.Add("Expectation");
            AddDictToLog(expectation, ref logLines);

            return matching;
        }

        private static void AddDictToLog(Dictionary<int, List<int>> dict, ref List<string> logLines)
        {
            foreach (var pair in dict)
            {
                var line = $"{pair.Key:D}: ";
                foreach (var val in pair.Value)
                {
                    line = $"{line} {val:D}";
                }
                logLines.Add(line);
            }
        }

        private static bool CompareList(Dictionary<int, List<int>> actual, Dictionary<int, List<int>> expectations)
        {
            var matches = true;

            foreach (var expectation in expectations)
            {
                foreach (var exp in expectation.Value)
                {
                    if (!actual.ContainsKey(expectation.Key) || !actual[expectation.Key].Contains(exp))
                    {
                        matches = false;
                    }
                }
            }

            return matches;
        }
    }
}
