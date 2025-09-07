using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.Fea;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Proxies;
using IDS.Amace.Quality;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Common.Enumerators;
using IDS.Common.Operations;
using IDS.Common.Quality;
using IDS.Common.Utilities;
using IDS.Common.Visualization;
using IDS.DataTypes;
using Rhino;
using Rhino.Geometry;
using RhinoMatSDKOperations.Boolean;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.Quality
{
    /// <summary>
    /// QCReportExporter generates a QC report based on the current state of the design
    /// </summary>
    public class QualityReportExporter
    {
        /// <summary>
        /// Generates the warnings HTML.
        /// </summary>
        /// <param name="screws">The screws.</param>
        /// <param name="plateBumps">The bumps plate.</param>
        /// <param name="cup">The cup.</param>
        /// <param name="erraticScrews">The erratic screws.</param>
        /// <returns></returns>
        public static string GenerateScrewWarningsHtml(List<Screw> screws, Mesh plateBumps, Cup cup, double drillBitRadius, out List<int> erraticScrews)
        {
            // Initialize report string
            string reportString = "<h4>Warnings</h4>\n";

            // Calculate all complex screw analyses
            Dictionary<int, List<int>> bumpProblemsOtherScrews;
            List<int> bumpProblemsCupZone;
            List<int> insertProblems;
            List<int> shaftProblems;
            var screwAnalysis = new AmaceScrewAnalysis();
            screwAnalysis.PerformSlowScrewChecks(screws, plateBumps, cup, out bumpProblemsOtherScrews, out bumpProblemsCupZone, out insertProblems, out shaftProblems);

            // Calculate screw overlaps
            Dictionary<int, List<int>> screwIntersections = screwAnalysis.PerformScrewIntersectionCheck(screws, 0);
            Dictionary<int, List<int>> screwVicinityIssues = screwAnalysis.PerformScrewIntersectionCheck(screws, 1);
            Dictionary<int, List<int>> screwBooleanSafetyZoneIntersection =
                screwAnalysis.PerformGuideHoleBooleanIntersectionCheck(screws, drillBitRadius);

            // Loop over the screws list and generate html for each screw problem
            erraticScrews = new List<int>();
            foreach (Screw screw in screws)
            {
                // start with empty string
                string reportStringPart = string.Empty;

                // Report screw issues
                reportStringPart += GenerateScrewLengthIssuesHtml(screw);
                reportStringPart += GenerateCupRimAngleIssuesHtml(screw);
                reportStringPart += GenerateBonePenetrationIssuesHtml(screw);
                reportStringPart += GenerateScrewInCupZoneIssuesHtml(screw);
                reportStringPart += GenerateInsertTrajectoryIssuesHtml(insertProblems, screw);
                reportStringPart += GenerateShaftTrajectoryIssuesHtml(shaftProblems, screw);
                reportStringPart += GenerateCupZoneDestroyingBumpIssuesHtml(bumpProblemsCupZone, screw);
                reportStringPart += GenerateScrewDestroyingBumpIssuesHtml(bumpProblemsOtherScrews, screw);
                reportStringPart += GenerateScrewIntersectionsHtml(screwIntersections, screw);
                reportStringPart += GenerateScrewVicinityIssuesHtml(screwVicinityIssues, screwIntersections, screw);
                reportStringPart += GenerateGuideHoleSafetyZoneIntersectionHtml(screwBooleanSafetyZoneIntersection, screw);

                // Only add screw qc text if the screw has problems
                if (reportStringPart != "")
                {
                    erraticScrews.Add(screw.index);
                    reportStringPart = string.Format("<h5>Screw {0:F0}</h5>\n", screw.index) + reportStringPart;
                    reportString += reportStringPart;
                }
            }

            // return the html report string
            return reportString;
        }

        private static string GenerateScrewIntersectionsHtml(Dictionary<int, List<int>> screwOverlaps, Screw screw)
        {
            string reportStringPart = string.Empty;
            if (screwOverlaps.ContainsKey(screw.index))
            {
                List<int> tempOverlap = screwOverlaps[screw.index].Select(i => i).ToList();
                string overlapList = string.Join(", ", tempOverlap);
                reportStringPart = string.Format("<p class=\"screwwarning\">Screw intersects screws: {0}</p>\n", overlapList);
            }

            return reportStringPart;
        }

        /// <summary>
        /// Generates the screw table row HTML.
        /// </summary>
        /// <param name="highlightRow">if set to <c>true</c> [highlight row].</param>
        /// <returns></returns>
        private static string GenerateScrewTableRowHtml(Screw screw, bool highlightRow)
        {
            // Create highlight class text if necessary
            string augmentHighlight = GenerateScrewTableAugmentHighlightText(screw);
            string fixationHighlight = GenerateScrewTableFixationHighlightText(screw);
            string diameterHIghlight = GenerateScrewTableDiameterHighlightText(screw);
            string lengthHighlight = GenerateScrewTableLengthHighlightText(screw);
            string angleHighlight = GenerateScrewTableAngleHighlightText(screw);

            // Create the table cells code
            string cellIndex = string.Format("<th>{0:D}</th>", screw.index);
            string cellScrewType = string.Format("<td>{0}</td>", screw.screwBrandType.ToString());
            string cellTotalLength = string.Format("<td{1}>{0:F0}</td>", screw.totalLength, lengthHighlight);
            string cellInBone = string.Format("<td>{0:F0}</td>", screw.GetDistanceInBone());
            string cellUntilBone = string.Format("<td>{0:F0}</td>", GenerateScrewTableDistanceUntilBoneText(screw));
            string cellFixation = string.Format("<td{1}>{0}</td>", screw.fixation, fixationHighlight);
            string cellDiameter = string.Format("<td{1}>{0:F1}</td>", screw.diameter, diameterHIghlight);
            string cellAxialOffset = string.Format("<td>{0:F1}</td>", screw.axialOffset);
            string cellScrewAlignment = string.Format("<td>{0}</td>", screw.screwAlignment.ToString()[0]);
            string cellPositioning = string.Format("<td>{0}</td>", screw.positioning.ToString()[0]);
            string cellBumps = string.Format("<td{1}>{0}</td>", screw.augmentsText, augmentHighlight);
            string cellAngle = string.Format("<td{1}>{0:F1}&deg;</td>", screw.cupRimAngleDegrees, angleHighlight);

            // Create the full HTML code
            string highlightRowClass = highlightRow ? " class=\"highlight\"" : "";
            string tablerow = string.Format(    CultureInfo.InvariantCulture,
                                                "<tr{12}>{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}</tr>",
                                                cellIndex,
                                                cellScrewType,
                                                cellTotalLength,
                                                cellInBone,
                                                cellUntilBone,
                                                cellFixation,
                                                cellDiameter,
                                                cellAxialOffset,
                                                cellScrewAlignment,
                                                cellPositioning,
                                                cellBumps,
                                                cellAngle,
                                                highlightRowClass);

            return tablerow;
        }

        private static string GenerateScrewTableAugmentHighlightText(Screw screw)
        {
            string highlightAugment = "";
            if ((screw.screwAides.ContainsKey(ScrewAideType.MedialBump)) && (screw.positioning == ScrewPosition.Flange))
                highlightAugment = " class=\"highlight\"";
            return highlightAugment;
        }

        private static string GenerateScrewTableFixationHighlightText(Screw screw)
        {
            string highlightFixation = "";
            if (screw.positioning == ScrewPosition.Flange && screw.corticalBites == 1)
                highlightFixation = " class=\"highlight\"";
            return highlightFixation;
        }

        private static string GenerateScrewTableDistanceUntilBoneText(Screw screw)
        {
            string distanceUntilBoneText = "";
            if (screw.GetDistanceUntilBone() == double.MaxValue)
                distanceUntilBoneText = "? ";
            else
                distanceUntilBoneText = screw.GetDistanceUntilBone().ToString("F0");
            return distanceUntilBoneText;
        }

        private static string GenerateScrewTableDiameterHighlightText(Screw screw)
        {
            return (screw.diameter != 6.5) ? " class=\"highlight\"" : "";
        }

        private static string GenerateScrewTableLengthHighlightText(Screw screw)
        {
            double roundedLength = Math.Round(screw.totalLength, 0); // to avoid that string format rounded to 25 is highlighted
            string highlightLength = (roundedLength < 25 || roundedLength > 60) ? " class=\"highlight\"" : "";
            return highlightLength;
        }

        private static string GenerateScrewTableAngleHighlightText(Screw screw)
        {
            double roundedAngleDegrees = Math.Round(screw.cupRimAngleDegrees, 0); // to avoid that string format rounded to 25 is highlighted
            string highlightAngle = roundedAngleDegrees < Screw.cupRimAngleThresholdDegrees ? " class=\"highlight\"" : "";
            return highlightAngle;
        }

        private static string GenerateScrewVicinityIssuesHtml(Dictionary<int, List<int>> screwVicinityIssues, Dictionary<int, List<int>> screwIntersections, Screw screw)
        {
            string reportStringPart = string.Empty;
            if (screwVicinityIssues.ContainsKey(screw.index) && !screwIntersections.ContainsKey(screw.index))
            {
                List<int> tempOverlap = screwVicinityIssues[screw.index].Select(i => i).ToList();
                string overlapList = string.Join(", ", tempOverlap);
                reportStringPart = string.Format("<p class=\"screwwarning\">Screw close to screws: {0}</p>\n", overlapList);
            }

            return reportStringPart;
        }

        private static string GenerateScrewDestroyingBumpIssuesHtml(Dictionary<int, List<int>> bumpProblemsOtherScrews, Screw screw)
        {
            string reportStringPart = string.Empty;
            if (bumpProblemsOtherScrews.ContainsKey(screw.index))
            {
                List<int> tempDestroy = bumpProblemsOtherScrews[screw.index].Select(i => i).ToList();
                string destroyList = string.Join(", ", tempDestroy);
                reportStringPart = string.Format("<p class=\"screwwarning\">Screw hole destroys bump of screws: {0}</p>\n", destroyList);
            }

            return reportStringPart;
        }

        private static string GenerateCupZoneDestroyingBumpIssuesHtml(List<int> bumpProblemsCupZone, Screw screw)
        {
            string reportStringPart = string.Empty;
            if (bumpProblemsCupZone.Contains(screw.index))
                reportStringPart = "<p class=\"screwwarning\">Cup Zone destroys screw bump</p>\n";
            return reportStringPart;
        }

        private static string GenerateShaftTrajectoryIssuesHtml(List<int> shaftProblems, Screw screw)
        {
            string reportStringPart = string.Empty;
            if (shaftProblems.Contains(screw.index))
                reportStringPart = "<p class=\"screwwarning\">Shaft trajectory intersects plate</p>\n";
            return reportStringPart;
        }

        private static string GenerateInsertTrajectoryIssuesHtml(List<int> insertProblems, Screw screw)
        {
            string reportStringPart = string.Empty;
            if (insertProblems.Contains(screw.index))
                reportStringPart = "<p class=\"screwwarning\">Insert trajectory obstructed</p>\n";
            return reportStringPart;
        }

        private static string GenerateScrewInCupZoneIssuesHtml(Screw screw)
        {
            string reportStringPart = string.Empty;
            var checkCupZone = screw.CheckCupZone();
            if (checkCupZone == QualityCheckResult.NotOK)
                reportStringPart = "<p class=\"screwwarning\">Cup Zone intersection</p>\n";
            else if (checkCupZone == QualityCheckResult.Error)
                reportStringPart = "<p class=\"screwwarning\">Cup Zone intersection ?</p>\n";
            return reportStringPart;
        }

        private static string GenerateGuideHoleSafetyZoneIntersectionHtml(Dictionary<int, List<int>> guideHoleSafetyZoneIntersections, Screw screw)
        {
            var reportStringPart = string.Empty;

            if (guideHoleSafetyZoneIntersections.ContainsKey(screw.index))
            {
                var intersectedIndexes = guideHoleSafetyZoneIntersections[screw.index].Select(i => i).ToList();
                var intersectList = string.Join(", ", intersectedIndexes);
                reportStringPart = string.Format("<p class=\"screwwarning\">Guide Hole Boolean intersection with Guide Hole: {0}</p>\n", intersectList);
            }

            return reportStringPart;
        }

        private static string GenerateBonePenetrationIssuesHtml(Screw screw)
        {
            string reportStringPart = string.Empty;
            QualityCheckResult checkBonePenetration = screw.CheckBonePenetration();
            if (checkBonePenetration == QualityCheckResult.NotOK)
                reportStringPart = string.Format("<p class=\"screwwarning\">Bone Penetration = {0:F0}mm</p>\n", screw.GetDistanceInBone());
            else if (checkBonePenetration == QualityCheckResult.Error)
                reportStringPart = "<p class=\"screwwarning\">Bone Penetration = ???</p>\n";
            return reportStringPart;
        }

        private static string GenerateCupRimAngleIssuesHtml(Screw screw)
        {
            string reportStringPart = string.Empty;
            QualityCheckResult checkCupRimAngle = screw.CheckCupRimAngle();
            if (checkCupRimAngle == QualityCheckResult.Error)
                reportStringPart = "<p class=\"screwwarning\">Cup Rim Angle = ???</p>\n";
            else if (checkCupRimAngle == QualityCheckResult.NotOK)
                reportStringPart = string.Format("<p class=\"screwwarning\">Cup Rim Angle = {0:F1}°</p>\n", screw.cupRimAngleDegrees);
            return reportStringPart;
        }

        private static string GenerateScrewLengthIssuesHtml(Screw screw)
        {
            string reportStringPart = string.Empty;
            QualityCheckResult checkLength = screw.CheckScrewLength();
            if (checkLength == QualityCheckResult.NotOK)
                reportStringPart = "<p class=\"screwwarning\">Length < 14mm</p>\n";
            else if (checkLength == QualityCheckResult.Error)
                reportStringPart = "<p class=\"screwwarning\">Length issue ?</p>\n";
            return reportStringPart;
        }

        // Main method that calls the FillReport and QCReportBuilder
        public static void ExportReport(ImplantDirector director, string filename)
        {
            // Fill the ReportDict
            Dictionary<string, string> ReportDict;
            bool success = FillReport(director, filename, out ReportDict);
            if (!success)
                throw new IDSException("Could not fill report.");

            // Fill the template
            var resources = new AmaceResources();
            string template = File.ReadAllText(resources.qcDocumentHtmlFile);

            string css = File.ReadAllText(resources.qcDocumentCssFile);
            template = template.Replace("[CSS_STYLE]", css);

            string javascript = File.ReadAllText(resources.qcDocumentJavaScriptFile);
            template = template.Replace("[JAVASCRIPT]", javascript);

            string report = FormatFromDictionary(template, ReportDict);

            // Export
            File.WriteAllText(filename, report);
        }

        // This method fills a dictionary with info for the QC report
        private static bool FillReport(ImplantDirector director, string filename, out Dictionary<string, string> reportValues)
        {
            // Init
            reportValues = new Dictionary<string, string>();
            RhinoDoc doc = director.document;
            Plane axial = director.inspector.axialPlane; // axial
            Plane coronal = director.inspector.coronalPlane; // coronal
            Plane sagittal = director.inspector.sagittalPlane; // sagittal
            int imageWidth = 1000;
            int imageHeight = 1000;

#if DEBUG
            Stopwatch stopWatchQcReport = new Stopwatch();
            stopWatchQcReport.Start();
#endif
            // Header
            ////////////////
            AddHeaderInformation(ref reportValues, director.inspector.caseId, director.currentDesignPhase, director.defectIsLeft);
            AddHeaderImages(ref reportValues, doc, imageWidth, imageHeight);

#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report header: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif
           
            // Changes
            ////////////////
            AddChangesInformation(ref reportValues, director);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report changes: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif

            // Bone grafts
            //////////////
            var objManager = new AmaceObjectManager(director);
            var boneGraftImported = objManager.HasBuildingBlock(IBB.BoneGraft);
            Mesh originalMeshDifference = null;
            var graftVolume = 0.0;
            if (boneGraftImported)
            {

                AmaceAnalysisMeshMaker.CreateOriginalPelvisDifference(director, out originalMeshDifference);
                graftVolume = Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.BoneGraft], true);
            }
            var preopPelvisVolume = Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.PreopPelvis], true);
            var originalPelvisVolume = Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.DefectPelvis], true);

            AddBoneGraftInformation(ref reportValues, boneGraftImported, graftVolume, originalPelvisVolume, preopPelvisVolume);
            AddBoneGraftImages(ref reportValues, boneGraftImported, originalMeshDifference, doc, imageWidth, imageHeight);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report bone grafts: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif

            // Bone mesh
            ////////////////
            Mesh designPelvis = objManager.GetBuildingBlock(IBB.DesignPelvis).Geometry as Mesh;
            Mesh defectPelvis = objManager.GetBuildingBlock(IBB.DefectPelvis).Geometry as Mesh;
            // Determine if design pelvis differs from
            bool designPelvisUsed = false;
            designPelvisUsed |= (defectPelvis.Vertices.Count != designPelvis.Vertices.Count); // true if number of vertices differs;
            if (!designPelvisUsed) // if same number of vertices
            {
                List<double> distances = new List<double>();
                MeshAnalysis.MeshToMeshAnalysis(designPelvis, defectPelvis, out distances);
                designPelvisUsed |= (distances.Max() > 0.01);
            }
            // Add to values
            AddBoneMeshInformation(ref reportValues, designPelvisUsed, designPelvis, defectPelvis);
            AddBoneMeshImages(ref reportValues, designPelvisUsed, doc, imageWidth, imageHeight);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report mesh: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif
            // Collidable entities
            ////////////////
            bool hasCollidables = objManager.GetBuildingBlock(IBB.CollisionEntity) != null;
            AddCollidablesInformation(ref reportValues, hasCollidables);
            AddCollidablesImages(ref reportValues, hasCollidables, doc, imageWidth, imageHeight);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report collidables: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif
            // Cup
            ////////////////
            AddCupInformation(ref reportValues, director.cup, axial.Origin, director.centerOfRotationDefectFemur, director.centerOfRotationContralateralFemurMirrored, director.centerOfRotationDefectSsm, director.centerOfRotationContralateralSsmMirrored, axial, coronal, sagittal, director);
            AddCupImages(ref reportValues, doc, imageWidth, imageHeight);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report cup: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif
            // Reaming
            ////////////////
            AddReamingInformation(ref reportValues, director.cup, director);
            AddReamingImages(ref reportValues, doc, imageWidth, imageHeight);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report reaming: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif
            // Skirt
            ////////////////
            AddSkirtImages(ref reportValues, doc, imageWidth, imageHeight);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report skirt: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif

            if (director.currentDesignPhase != DesignPhase.CupQC)
            {
                // Scaffold
                ////////////////
                AddScaffoldInformation(ref reportValues, director.insertionAnteversionDegrees, director.insertionInclinationDegrees, director);
                AddScaffoldImages(ref reportValues, doc, imageWidth, imageHeight);
#if DEBUG
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report scaffold: {0}", stopWatchQcReport.Elapsed);
                stopWatchQcReport.Restart();
#endif
                // Screws
                ////////////////
                ScrewManager screwManager = new ScrewManager(director.document);
                List<Screw> screws = screwManager.GetAllScrews().ToList();
                Mesh plateBumps = objManager.GetBuildingBlock(IBB.PlateBumps).Geometry as Mesh;
                Cup cup = director.cup;
                AddScrewInformation(ref reportValues, screws, plateBumps, cup, director.DrillBitRadius);
                AddScrewImages(ref reportValues, doc, imageWidth, imageHeight);
#if DEBUG
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report screws: {0}", stopWatchQcReport.Elapsed);
                stopWatchQcReport.Restart();
#endif
                // Plate
                ////////////////
                Mesh reamedPelvis = objManager.GetBuildingBlock(IBB.OriginalReamedPelvis).Geometry as Mesh;
                Mesh solidPlateBottom = objManager.GetBuildingBlock(IBB.SolidPlateBottom).Geometry as Mesh;
                Mesh solidPlateTop = objManager.GetBuildingBlock(IBB.SolidPlateTop).Geometry as Mesh;
                Mesh solidPlateSide = objManager.GetBuildingBlock(IBB.SolidPlateSide).Geometry as Mesh;
                AddPlateInformation(ref reportValues, reamedPelvis, solidPlateBottom, solidPlateTop, solidPlateSide, director.cup.filledCupMesh, director.cup.innerReamingVolumeMesh, director.plateThickness, director.amaceFea, director.cup);
                AddPlateImages(ref reportValues, doc, imageWidth, imageHeight, director.amaceFea);
#if DEBUG
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report plate: {0}", stopWatchQcReport.Elapsed);
                stopWatchQcReport.Restart();
#endif
            }

            // Traceability
            ////////////////
            AddTraceability(ref reportValues, director.componentVersions, director.inspector.preOperativeId, director.draft, director.version, doc.Path, director.inputFiles, filename);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report traceability: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Stop();
#endif

            // Visibility Settings
            ////////////////
            SetVisibility(ref reportValues, director.currentDesignPhase, director.inputFiles);

            return true;
        }

        // TODO: refactor to remove director from arguments
        private static void AddChangesInformation(ref Dictionary<string, string> valueDictionary, ImplantDirector director)
        {
            if(director.currentDesignPhase != DesignPhase.Export)
            {
                string inputfile = Path.Combine(DirectoryStructure.GetWorkingDir(director.document), "..", Path.GetFileName(director.inputFiles.First()));

                // Only second or later draft (i.e. started from a 3dm file) needs to have a changes section)
                if (Path.GetExtension(inputfile) == ".3dm")
                {
                    Dictionary<string, bool> changed = new Dictionary<string, bool>();
                    Dictionary<string, string> changes = DraftComparison.GetDocumentDifference(director, inputfile, out changed);
                    valueDictionary = DictionaryUtilities.MergeDictionaries(valueDictionary, changes);

                    // Add colors
                    string red = "#D70000";
                    string green = "#28A828";

                    // Indicate changes on three levels
                    AddChangesPerSubsection(valueDictionary, changed, changes, red, green);
                    AddChangesPerSection(valueDictionary, changed, changes, red, green);
                    AddChangesPerPhase(valueDictionary, changed, changes, red, green);
                }
            }
        }

        /// <summary>
        /// Adds the changes per phase.
        /// </summary>
        /// <param name="valueDictionary">The value dictionary.</param>
        /// <param name="changed">The changed.</param>
        /// <param name="changes">The changes.</param>
        /// <param name="red">The red.</param>
        /// <param name="green">The green.</param>
        private static void AddChangesPerPhase(Dictionary<string, string> valueDictionary, Dictionary<string, bool> changed, Dictionary<string, string> changes, string red, string green)
        {
            // Add phase color to indicate if change occured
            foreach (string key in changes.Keys)
            {
                string baseKey = GetChangesBaseKey(key);
                List<string> keyParts = key.Split(new char[] { '_' }).ToList();
                string phaseColorKey = keyParts.Count > 1 ? string.Format("{0}_COLOR", string.Join("_", keyParts.GetRange(0, 1))) : "";
                if (phaseColorKey != "")
                {
                    // Not in dictionary, add always
                    if (!valueDictionary.ContainsKey(phaseColorKey))
                        valueDictionary.Add(phaseColorKey, changed[baseKey] ? red : green);
                    // Change from green to red if a subvalue is colored red
                    else if (changed[baseKey] && valueDictionary[phaseColorKey] == green)
                        valueDictionary[phaseColorKey] = red;
                }
            }
        }

        /// <summary>
        /// Adds the changes per section.
        /// </summary>
        /// <param name="valueDictionary">The value dictionary.</param>
        /// <param name="changed">The changed.</param>
        /// <param name="changes">The changes.</param>
        /// <param name="red">The red.</param>
        /// <param name="green">The green.</param>
        private static void AddChangesPerSection(Dictionary<string, string> valueDictionary, Dictionary<string, bool> changed, Dictionary<string, string> changes, string red, string green)
        {
            foreach (string key in changes.Keys)
            {
                // Add section color to indicate if change occured
                List<string> keyParts = key.Split(new char[] { '_' }).ToList();
                string baseKey = GetChangesBaseKey(key);
                string sectionColorKey = keyParts.Count > 2 ? string.Format("{0}_COLOR", string.Join("_", keyParts.GetRange(0, 2))) : "";
                if (sectionColorKey != "")
                {
                    // Not in dictionary, add always
                    if (!valueDictionary.ContainsKey(sectionColorKey))
                        valueDictionary.Add(sectionColorKey, changed[baseKey] ? red : green);
                    // Change from green to red if a subvalue is colored red
                    else if (changed[baseKey] && valueDictionary[sectionColorKey] == green)
                        valueDictionary[sectionColorKey] = red;
                }
            }
        }

        /// <summary>
        /// Adds the changes per subsection.
        /// </summary>
        /// <param name="valueDictionary">The value dictionary.</param>
        /// <param name="changed">The changed.</param>
        /// <param name="changes">The changes.</param>
        /// <param name="red">The red.</param>
        /// <param name="green">The green.</param>
        private static void AddChangesPerSubsection(Dictionary<string, string> valueDictionary, Dictionary<string, bool> changed, Dictionary<string, string> changes, string red, string green)
        {
            // Single row in the table for a screw
            string screwStringTemplate = @"<tr class=""datarow"" style=""display:none;"">
                                                <td class=""precolumn""></td><td class=""subsection"" style=""background-color:[SCREW_N_COLOR];"">[SCREW_N_NAME]</td>
                                                <td>[SCREW_N_DIFF]</td>
                                                <td>[SCREW_N_PREV]</td>
                                                <td>[SCREW_N_CURR]</td>
                                                </tr>";
            // All text for separate screws will be stored in this variable
            string screwRows = "";
            foreach (string key in changes.Keys)
            {
                string baseKey = GetChangesBaseKey(key);
                List<string> keyParts = key.Split(new char[] { '_' }).ToList();
                string subsectionColorKey = keyParts.Count > 3 ? string.Format("{0}_COLOR", string.Join("_", keyParts.GetRange(0, 3))) : "";

                // Check if the key refers to a key or not
                if (baseKey.ToUpper().Contains("SCREWS_INDIVIDUAL") && keyParts[keyParts.Count - 1].ToUpper() == "DIFF" && baseKey.ToUpper() != "SCREWS_INDIVIDUAL")
                {
                    string diffKey = string.Format("{0}_DIFF", baseKey);
                    string currKey = string.Format("{0}_CURR", baseKey);
                    string prevKey = string.Format("{0}_PREV", baseKey);

                    string screwString = screwStringTemplate;
                    screwString = screwString.Replace("[SCREW_N_CURR]", changes[currKey]);
                    screwString = screwString.Replace("[SCREW_N_PREV]", changes[prevKey]);
                    screwString = screwString.Replace("[SCREW_N_DIFF]", changes[diffKey]);
                    screwString = screwString.Replace("[SCREW_N_NAME]", string.Format("Screw {0}", keyParts[2]));

                    if (changed[baseKey])
                        screwString = screwString.Replace("[SCREW_N_COLOR]", red);
                    else
                        screwString = screwString.Replace("[SCREW_N_COLOR]", green);

                    screwRows += screwString;
                }
                else
                {
                    // Not an individual screw
                    if (subsectionColorKey != "" && !valueDictionary.ContainsKey(subsectionColorKey))
                        valueDictionary.Add(subsectionColorKey, changed[baseKey] ? red : green);
                }
            }
            // Add screw change information
            valueDictionary.Add("SCREWS_INDIVIDUAL_SCREWS", screwRows);
            if (screwRows != string.Empty)
                valueDictionary.Add("SCREWS_INDIVIDUAL_CLASS", "datarow");
            else
                valueDictionary.Add("SCREWS_INDIVIDUAL_CLASS", "alwayshidden");
        }

        private static string GetChangesBaseKey(string key)
        {
            List<string> keyParts = key.Split(new char[] { '_' }).ToList();
            string baseKey = string.Join("_", keyParts.GetRange(0, keyParts.Count - 1));
            return baseKey;
        }

        private static void AddHeaderInformation(ref Dictionary<string, string> valueDictionary,
                                            string caseId,
                                            DesignPhase currentDesignPhase,
                                            bool defectIsLeft)
        {
            valueDictionary.Add("CASE_ID", caseId);
            valueDictionary.Add("PHASE", currentDesignPhase.ToString());
            valueDictionary.Add("DEFECT_SIDE", defectIsLeft ? "Left" : "Right");
        }

        private static void AddHeaderImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height)
        {
            valueDictionary.Add("IMG_PREOP", ScreenshotsOverview.GeneratePreOpImageString(doc, width, height));
            valueDictionary.Add("IMG_OVERVIEW_ANTERIOR", ScreenshotsOverview.GenerateOverviewImageString(doc, width, height, CameraView.Acetabular));
            valueDictionary.Add("IMG_OVERVIEW_POSTERIOR", ScreenshotsOverview.GenerateOverviewImageString(doc, width, height, CameraView.Acetabularinverse));
        }

        private static void AddBoneGraftInformation(ref Dictionary<string, string> valueDictionary, bool graftsImported, double graftVolume, double originalPelvisVolume, double preopPelvisVolume)
        {
            valueDictionary.Add("GRAFTS_IMPORTED", graftsImported ? "Yes" : "No");

            valueDictionary.Add("PREOP_PELVIS_VOLUME", preopPelvisVolume.ToString("F1"));
            valueDictionary.Add("GRAFTS_VOLUME", graftVolume.ToString("F1"));
            valueDictionary.Add("ORIGINAL_PELVIS_VOLUME", originalPelvisVolume.ToString("F1"));

            valueDictionary.Add("GRAFT_IMAGE_DISPLAY", graftsImported ? "block" : "none");
        }

        private static void AddBoneGraftImages(ref Dictionary<string, string> valueDictionary, bool graftsImported, Mesh differenceMesh, RhinoDoc doc, int width, int height)
        {
            // Generate images if necessary
            if (graftsImported)
            {
                // Difference maps
                valueDictionary.Add("IMG_PREOP_AND_GRAFT_ACETABULAR", ScreenshotsBoneGraft.GenerateBoneGraftImageString(doc, width, height, CameraView.Acetabular));
                valueDictionary.Add("IMG_ORIGINAL_PELVIS_ACETABULAR", ScreenshotsBoneGraft.GenerateOriginalPelvisImageString(doc, width, height, CameraView.Acetabular));
                valueDictionary.Add("IMG_GRAFT_COMPARISON_ACETABULAR", ScreenshotsBoneGraft.GenerateBoneGraftDifferenceMapImageString(doc, differenceMesh, width, height, CameraView.Acetabular));
            }
        }

        private static void AddBoneMeshInformation(ref Dictionary<string, string> valueDictionary, bool designPelvisUsed, Mesh designPelvis, Mesh defectPelvis)
        {
            Mesh diffMesh = new Mesh();
            MDCKBoolean.OperatorBooleanDifference(defectPelvis, out diffMesh, new Mesh[] { designPelvis });
            double vol_diff = VolumeMassProperties.Compute(designPelvis).Volume - VolumeMassProperties.Compute(defectPelvis).Volume;
            vol_diff = Math.Round(vol_diff / 1000, 1);

            valueDictionary.Add("PELVIS_VOL_DIFF", vol_diff.ToString("F0"));
            valueDictionary.Add("PELVIS_FIXED", designPelvisUsed ? "Yes" : "No");
            valueDictionary.Add("MESH_IMAGE_DISPLAY", designPelvisUsed ? "block" : "none");
        }

        private static void AddBoneMeshImages(ref Dictionary<string, string> valueDictionary, bool designPelvisUsed, RhinoDoc doc, int width, int height)
        {
            // Generate images if necessary
            if (designPelvisUsed)
            {
                // Difference maps
                valueDictionary.Add("IMG_PELVIS_FIXED_ANTERIOR", ScreenshotsOverview.GenerateBoneMeshImageString(doc, width, height, CameraView.Acetabular));
                valueDictionary.Add("IMG_PELVIS_FIXED_LATERAL", ScreenshotsOverview.GenerateBoneMeshImageString(doc, width, height, CameraView.Acetabularinverse));
            }
        }

        private static void AddCollidablesInformation(ref Dictionary<string, string> valueDictionary, bool hasCollidables)
        {
            if (hasCollidables)
            {
                valueDictionary.Add("COLLIDABLES_IMAGE_DISPLAY", "block");
                valueDictionary.Add("COLLIDABLES_IMPORTED", "Yes");
            }
            else
            {
                valueDictionary.Add("COLLIDABLES_IMAGE_DISPLAY", "none");
                valueDictionary.Add("COLLIDABLES_IMPORTED", "No");
            }
        }

        private static void AddCollidablesImages(ref Dictionary<string, string> valueDictionary, bool hasCollidables, RhinoDoc doc, int width, int height)
        {
            if (hasCollidables)
            {
                valueDictionary.Add("IMG_COLLIDABLES_ACETABULAR", ScreenshotsOverview.GenerateCollidablesImageString(doc, width, height, CameraView.Acetabular));
                valueDictionary.Add("IMG_COLLIDABLES_ACETABULARINVERSE", ScreenshotsOverview.GenerateCollidablesImageString(doc, width, height, CameraView.Acetabularinverse));
            }
        }

        // TODO: refactor to keep director out of argument list
        private static void AddCupInformation(ref Dictionary<string, string> valueDictionary,
                                                    Cup cup,
                                                    Point3d refPCS,
                                                    Point3d refDefect,
                                                    Point3d refContralateral,
                                                    Point3d refDefectSSM,
                                                    Point3d refContralateralSSM,
                                                    Plane axial,
                                                    Plane coronal,
                                                    Plane sagittal,
                                                    ImplantDirector director)
        {
            // Parameters
            AddCupParametersToDictionary(valueDictionary, cup);
            // Orientation
            AddCupOrientationToDictionary(valueDictionary, cup);
            // ABsolute position in PCS
            AddAbsoluteCupPositionToDictionary(valueDictionary, cup.centerOfRotation, refPCS, axial, coronal, sagittal, director.signInfSupClat, director.signMedLatPCS, director.signAntPosPCS, "CUP_INF_SUP_PCS", "CUP_LAT_MED_PCS", "CUP_POST_ANT_PCS");
            // Relative position
            AddRelativeCupPositionToDictionary(valueDictionary, cup.centerOfRotation, refDefect, axial, coronal, sagittal, director.signInfSupDef, director.signMedLatDef, director.signAntPosDef, "CUP_INF_SUP_DEF", "CUP_LAT_MED_DEF", "CUP_POST_ANT_DEF");
            AddRelativeCupPositionToDictionary(valueDictionary, cup.centerOfRotation, refContralateral, axial, coronal, sagittal, director.signInfSupClat, director.signMedLatClat, director.signAntPosClat, "CUP_INF_SUP_CLAT", "CUP_LAT_MED_CLAT", "CUP_POST_ANT_CLAT");
            AddRelativeCupPositionToDictionary(valueDictionary, cup.centerOfRotation, refDefectSSM, axial, coronal, sagittal, director.signInfSupDef, director.signMedLatDef, director.signAntPosDef, "CUP_INF_SUP_SSM_DEF", "CUP_LAT_MED_SSM_DEF", "CUP_POST_ANT_SSM_DEF");
            AddRelativeCupPositionToDictionary(valueDictionary, cup.centerOfRotation, refContralateralSSM, axial, coronal, sagittal, director.signInfSupClat, director.signMedLatClat, director.signAntPosClat, "CUP_INF_SUP_SSM_CLAT", "CUP_LAT_MED_SSM_CLAT", "CUP_POST_ANT_SSM_CLAT");
        }

        private static void AddCupParametersToDictionary(Dictionary<string, string> valueDictionary, Cup cup)
        {
            valueDictionary.Add("CUP_THICK", cup.cupType.cupThickness.ToString("F0"));
            valueDictionary.Add("CUP_POR_THICK", cup.cupType.porousThickness.ToString("F0"));
            valueDictionary.Add("CUP_DESIGN", cup.cupType.cupDesign.ToString());
            valueDictionary.Add("CUP_DIAM_INNER", cup.innerCupDiameter.ToString("F0"));
            valueDictionary.Add("CUP_DIAM_OUTER", cup.outerCupDiameter.ToString("F0"));
            valueDictionary.Add("CUP_DIAM_POR_OUTER", ((cup.outerCupRadius + cup.cupType.porousThickness) * 2).ToString("F0"));
        }

        private static void AddCupOrientationToDictionary(Dictionary<string, string> valueDictionary, Cup cup)
        {
            valueDictionary.Add("CUP_ANG_AV", cup.anteversion.ToString("F1"));
            valueDictionary.Add("CUP_ANG_INCL", cup.inclination.ToString("F1"));
        }

        private static void AddAbsoluteCupPositionToDictionary(Dictionary<string, string> valueDictionary, 
                                                                Point3d cupCenterOfRotation, Point3d referencePoint, 
                                                                Plane axial, Plane coronal, Plane sagittal, 
                                                                int signInfSup, int signMedLat, int signAntPos,
                                                                string keyInfSup, string keyMedLat, string keyAntPos)
        {
            valueDictionary.Add(keyInfSup, (signInfSup * MathUtilities.GetOffset(axial.Normal, referencePoint, cupCenterOfRotation)).ToString("F0"));
            valueDictionary.Add(keyMedLat, (signMedLat * MathUtilities.GetOffset(sagittal.Normal, referencePoint, cupCenterOfRotation)).ToString("F0"));
            valueDictionary.Add(keyAntPos, (signAntPos * MathUtilities.GetOffset(coronal.Normal, referencePoint, cupCenterOfRotation)).ToString("F0"));
        }

        private static void AddRelativeCupPositionToDictionary(Dictionary<string, string> valueDictionary, 
                                                                Point3d cupCenterOfRotation, Point3d referencePoint, 
                                                                Plane axial, Plane coronal, Plane sagittal, 
                                                                int signInfSup, int signMedLat, int signAntPos,
                                                                string keyInfSup, string keyMedLat, string keyAntPos)
        {
            valueDictionary.Add(keyInfSup, referencePoint != Point3d.Unset ? (signInfSup * MathUtilities.GetOffset(axial.Normal, referencePoint, cupCenterOfRotation)).ToString("+#;-#;0") : "/");
            valueDictionary.Add(keyMedLat, referencePoint != Point3d.Unset ? (signMedLat * MathUtilities.GetOffset(sagittal.Normal, referencePoint, cupCenterOfRotation)).ToString("+#;-#;0") : "/");
            valueDictionary.Add(keyAntPos, referencePoint != Point3d.Unset ? (signAntPos * MathUtilities.GetOffset(coronal.Normal, referencePoint, cupCenterOfRotation)).ToString("+#;-#;0") : "/");
        }

        private static void AddCupImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height)
        {
            valueDictionary.Add("IMG_CUP_AV", ScreenshotsCup.GenerateCupImageString(doc, width, height, CupImageType.Anteversion));
            valueDictionary.Add("IMG_CUP_INCL", ScreenshotsCup.GenerateCupImageString(doc, width, height, CupImageType.Inclination));
            valueDictionary.Add("IMG_CUP_POSANT", ScreenshotsCup.GenerateCupImageString(doc, width, height, CupImageType.Position));
        }

        private static void AddReamingInformation(ref Dictionary<string, string> valueDictionary, Cup cup, ImplantDirector director)
        {
            var objManager = new AmaceObjectManager(director);

            /// \todo Refactor to keep director out of argument list

            valueDictionary.Add("CUP_REAM_DIAM", cup.outerReamingDiameter.ToString("F0"));

            var additionalReaming = objManager.GetBuildingBlockId(IBB.ExtraReamingEntity) == Guid.Empty ? "N" : "Y";
            valueDictionary.Add("ADDIT_REAMONG_YN", additionalReaming);

            var rbvCupVolumeCc = Volume.RBVCupVolumeCC(director);
            valueDictionary.Add("RBV_CUP", rbvCupVolumeCc.ToString("F1"));

            var rbvCupGraftVolumeCc = objManager.HasBuildingBlock(IBB.CupRbvGraft) ? Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.CupRbvGraft], true) : 0;
            valueDictionary.Add("RBV_CUP_GRAFT", rbvCupGraftVolumeCc.ToString("F1"));

            var rbvAdditionalVolumeCc = Volume.RBVAdditionalVolumeCC(director);
            valueDictionary.Add("RBV_ADDITIONAL", rbvAdditionalVolumeCc.ToString("F1"));

            var rbvAdditionalGraftVolumeCc = objManager.HasBuildingBlock(IBB.AdditionalRbvGraft) ? Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.AdditionalRbvGraft], true) : 0;
            valueDictionary.Add("RBV_ADDITIONAL_GRAFT", rbvAdditionalGraftVolumeCc.ToString("F1"));

            var rbvTotalVolumeCc = Volume.RBVTotalVolumeCC(director);
            valueDictionary.Add("RBV_TOTAL", rbvTotalVolumeCc.ToString("F1"));

            var rbvTotalGraftVolumeCc = rbvCupGraftVolumeCc + rbvAdditionalGraftVolumeCc;
            valueDictionary.Add("RBV_TOTAL_GRAFT", rbvTotalGraftVolumeCc.ToString("F1"));
        }

        private static void AddReamingImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height)
        {
            valueDictionary.Add("IMG_REAMING_PIECES", ScreenshotsReaming.GenerateReamingImageString(doc, width, height, true, false));
            valueDictionary.Add("IMG_REAMING_TOTAL", ScreenshotsReaming.GenerateReamingImageString(doc, width, height, true, true));
            valueDictionary.Add("IMG_REAMING_AFTER", ScreenshotsReaming.GenerateReamingImageString(doc, width, height, false));
        }

        private static void AddSkirtImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height)
        {
            valueDictionary.Add("IMG_SKIRT_ACETABULAR", ScreenshotsSkirt.GenerateSkirtImageString(doc, width, height, CameraView.Acetabular));
            valueDictionary.Add("IMG_SKIRT_ILLIUM", ScreenshotsSkirt.GenerateSkirtImageString(doc, width, height, CameraView.Illium));
        }

        // TODO: refactor to keep director out of argument list
        private static void AddScaffoldInformation(ref Dictionary<string, string> valueDictionary, double insertionAv, double insertionIncl, ImplantDirector director)
        {
            valueDictionary.Add("MBV_VOLUME", Volume.FinalizedScaffoldVolumeCC(director).ToString("F1"));
            valueDictionary.Add("UC_AV", insertionAv.ToString("F1"));
            valueDictionary.Add("UC_INCL", insertionIncl.ToString("F1"));
        }

        private static void AddScaffoldImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height)
        {
            valueDictionary.Add("IMG_SCAFFOLD_INSERTION", ScreenshotsScaffold.GenerateScaffoldImageString(doc, width, height, CameraView.Insertion));
            valueDictionary.Add("IMG_SCAFFOLD_INSERTION_INVERSE", ScreenshotsScaffold.GenerateScaffoldImageString(doc, width, height, CameraView.Insertioninverse));
        }

        private static void AddPlateInformation(ref Dictionary<string, string> valueDictionary, Mesh reamedPelvis, Mesh solidPlateBottom, Mesh solidPlateTop, Mesh solidPlateSide, Mesh cupFilledMesh, Mesh cupInnerReamingVolumeMesh, double plateThickness, Amace.Fea.AmaceFea fea, Cup cup)
        {
            /// \todo Remove ImplantDirector dependency

            List<double> distances = MeshUtilities.Mesh2MeshDistance(solidPlateBottom, reamedPelvis);
            PlateAnalyzer plateAnalyzer = new PlateAnalyzer(solidPlateTop, solidPlateBottom, solidPlateSide, new List<Mesh>() { cupFilledMesh, cupInnerReamingVolumeMesh }, plateThickness);
            List<Tuple<Line, double>> edgeAngles = plateAnalyzer.GetSideSurfaceLinesAndAngles();

            double min = edgeAngles.Min(x => x.Item2);
            double max = edgeAngles.Max(x => x.Item2);

            valueDictionary.Add("PLATE_MINIMAL_CLEARANCE", distances.Min().ToString("F4"));
            valueDictionary.Add("POLISHING_OFFSET", cup.CupRingPolishingOffset.ToString("F1"));
            valueDictionary.Add("PLATE_THICKNESS", plateThickness.ToString("F1"));
            valueDictionary.Add("PLATE_MIN_EDGE_ANGLE", min.ToString("F0"));
			valueDictionary.Add("PLATE_MAX_EDGE_ANGLE", max.ToString("F0"));

            if(fea != null)
            {
                valueDictionary.Add("FEA_LOAD_MAGNITUDE", fea.loadMagnitude.ToString("F0"));
                valueDictionary.Add("FEA_LOAD_DEG_THRESH", fea.loadMeshDegreesThreshold.ToString("F1"));

                valueDictionary.Add("FEA_BC_DIST_THRESH", fea.boundaryConditionsDistanceThreshold.ToString("F1"));
                valueDictionary.Add("FEA_BC_NOISE_THRESH", fea.boundaryConditionsNoiseShellThreshold.ToString("F1"));

                valueDictionary.Add("FEA_MESH_EDGE_LENGTH", fea.targetEdgeLength.ToString("F2"));

                valueDictionary.Add("FEA_MATERIAL_EMOD", fea.material.elasticityEModulus.ToString("F0"));
                valueDictionary.Add("FEA_MATERIAL_POISSON", fea.material.elasticityPoissonRatio.ToString("F2"));
                valueDictionary.Add("FEA_MATERIAL_UTS", fea.material.ultimateTensileStrength.ToString("F0"));
                valueDictionary.Add("FEA_MATERIAL_FATIGUELIM", fea.material.fatigueLimit.ToString("F0"));

                valueDictionary.Add("FEA_SAFETY_LOW", TuneFeaVisualisation.SafetyFactorLow.ToString("F0"));
                valueDictionary.Add("FEA_SAFETY_MIDDLE", TuneFeaVisualisation.SafetyFactorMiddle.ToString("F0"));
                valueDictionary.Add("FEA_SAFETY_HIGH", TuneFeaVisualisation.SafetyFactorHigh.ToString("F0"));

                valueDictionary.Add("FEA_STATUS", "A VBT was performed on this draft.");
            }
            else
            {
                valueDictionary.Add("FEA_STATUS", "No VBT was performed on this draft.");
                valueDictionary.Add("FEA_DISPLAY", "none");
            }
        }

        private static void AddPlateImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height, AmaceFea fea)
        {
            valueDictionary.Add("IMG_IMPLANT_CLEARANCE_ACETABULAR", ScreenshotsPlate.GeneratePlateClearanceImageString(doc, width, height, CameraView.Acetabular));
            valueDictionary.Add("IMG_IMPLANT_CLEARANCE_ILLIUM", ScreenshotsPlate.GeneratePlateClearanceImageString(doc, width, height, CameraView.Illium));
            valueDictionary.Add("IMG_IMPLANT_EDGE_LENGTH_ACETABULAR", ScreenshotsPlate.GeneratePlateAngleImageString(doc, width, height, CameraView.Acetabular));
            valueDictionary.Add("IMG_IMPLANT_EDGE_LENGTH_ILLIUM", ScreenshotsPlate.GeneratePlateAngleImageString(doc, width, height, CameraView.Illium));

            if (fea != null)
            {
                FeaConduit feaConduit = new FeaConduit(fea);

                string[][] feaImageStrings = ScreenshotsPlate.GeneratePlateFeaImageStrings(doc, width, height, fea, feaConduit, IBB.PlateHoles, false, TuneFeaVisualisation.SafetyFactorLow, TuneFeaVisualisation.SafetyFactorMiddle, TuneFeaVisualisation.SafetyFactorHigh, TuneFeaVisualisation.FatigueLimit, TuneFeaVisualisation.UltimateTensileStrength);
                string feaJavaScriptArrayString = Convert3dImageArrayToJavaScriptString(valueDictionary, feaImageStrings, "feaList", "feaSubList");
                valueDictionary.Add("IMAGE_ARRAY_FEA", feaJavaScriptArrayString);

                string[][] feaBCLoadImageStrings = ScreenshotsPlate.GeneratePlateFeaImageStrings(doc, width, height, fea, feaConduit, IBB.PlateHoles, true, TuneFeaVisualisation.SafetyFactorLow, TuneFeaVisualisation.SafetyFactorMiddle, TuneFeaVisualisation.SafetyFactorHigh, TuneFeaVisualisation.FatigueLimit, TuneFeaVisualisation.UltimateTensileStrength);
                string feaBcLoadJavascriptArrayString = Convert3dImageArrayToJavaScriptString(valueDictionary, feaBCLoadImageStrings, "feaBcLoadList", "feaBcLoadSublist");
                valueDictionary.Add("IMAGE_ARRAY_FEABCLOAD", feaBcLoadJavascriptArrayString);

                valueDictionary.Add("IMG_FATIGUE_COLOR_SCALE", Screenshots.GenerateImageString(feaConduit.CreateColorScaleBitmap(), true));
            }
        }

        private static string Convert3dImageArrayToJavaScriptString(Dictionary<string, string> valueDictionary, string[][] imageStrings, string listName, string subListName)
        {
            // Add base64 tag
            for (int i = 0; i < imageStrings.Length; i++)
                for (int j = 0; j < imageStrings[i].Length; j++)
                    imageStrings[i][j] = "data: image / jpeg; base64," + imageStrings[i][j];
            // Create string
            string feaJavascriptArray = CreateJavaScriptArrayOfArrays(imageStrings, listName, subListName);
            return feaJavascriptArray;
        }

        public static string CreateJavaScriptArrayOfArrays(string[][] imageStringsMatrix, string arrayName, string subArrayName)
        {
            string javaScriptArray = string.Format("var {0} = new Array();\n", arrayName);
            int i = 0;
            foreach (string[] imageStringsArray in imageStringsMatrix)
            {
                javaScriptArray += CreateJavaScriptArray(imageStringsArray, string.Format("{0}{1:D}", subArrayName,i));
                javaScriptArray += string.Format("{0}.push({1}{2:D});\n", arrayName, subArrayName, i);
                i++;
            }

            return javaScriptArray;
        }

        public static string CreateJavaScriptArray(string[] imageStringsArray, string arrayName)
        {
            string javaScriptArray = string.Format("var {0} = new Array();\n", arrayName);
            foreach (string imageString in imageStringsArray)
            {
                javaScriptArray += string.Format("{0}.push('{1}');\n", arrayName, imageString);
            }

            return javaScriptArray;
        }

        private static void AddScrewInformation(ref Dictionary<string, string> valueDictionary, List<Screw> screws, Mesh plateBumps, Cup cup, double drillBitRadius)
        {
            screws.Sort();

            List<int> erraticScrews;
            valueDictionary.Add("SCREW_WARNINGS", GenerateScrewWarningsHtml(screws, plateBumps, cup, drillBitRadius, out erraticScrews));

            var screwinfo = string.Empty;
            foreach (var screw in screws)
            {
                screwinfo = screwinfo + GenerateScrewTableRowHtml(screw, erraticScrews.Contains(screw.index));
            }
            valueDictionary.Add("SCREW_INFO", screwinfo);

            var screwBrand = ImplantDirector.ExtractScrewBrand(screws);
            valueDictionary.Add("SCREW_BRAND_TEXT", screwBrand);

            valueDictionary.Add("SCREW_DRILL_BIT_DIAMETER", ScrewAideManager.ConvertToDrillBitDiameter(drillBitRadius).ToString("F1"));
            if (Math.Abs(drillBitRadius - ImplantDirector.DefaultDrillBitRadius) > 0.001)
            {
                valueDictionary.Add("SCREW_DRILL_BIT_WARNINGCOLOR", "warning");
            }
        }

        private static void AddScrewImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height)
        {
            valueDictionary.Add("IMG_SCREWS_ACETABULAR", ScreenshotsScrews.GenerateScrewNumberImageString(doc, width, height, CameraView.Acetabular, ScrewConduitMode.WarningColors, true));
            valueDictionary.Add("IMG_SCREWS_ILLIUM", ScreenshotsScrews.GenerateScrewNumberImageString(doc, width, height, CameraView.Illium, ScrewConduitMode.WarningColors, true));
        }

        private static void AddTraceability(ref Dictionary<string, string> valueDictionary,
                                                Dictionary<string, Dictionary<string, string>> componentVersions,
                                                string preopId,
                                                int draft,
                                                int version,
                                                string workFilePath,
                                                List<string> inputFilePath,
                                                string reportFilePath)

        {
            valueDictionary.Add("GIT_PREOP", preopId);
            // Software versions
            foreach (string versionType in new string[] { "commit", "build" })
            {
                foreach (string component in new string[] { "IDS", "RhinoMatSDKOperations", "MatSDK_DLL", "MatSAXLite", "PyGeneralFunctions", "Documentation" })
                {
                    string value = "Unknown";
                    try
                    {
                        value = componentVersions[component][versionType];
                    }
                    catch { }
                    valueDictionary.Add(string.Format("{0}_{1}", component.ToUpper(), versionType.ToUpper()), value);
                }
            }
            // Project references
            valueDictionary.Add("VERSION", version.ToString("D"));
            valueDictionary.Add("DRAFT", draft.ToString("D"));
            valueDictionary.Add("PREOP_ID", preopId);
            valueDictionary.Add("WORK_FILE", workFilePath);
            valueDictionary.Add("INPUT_FILE", inputFilePath.First());
            valueDictionary.Add("REPORT", reportFilePath);
            // Timestamp
            valueDictionary.Add("TIMESTAMP", DateTime.Now.ToString());
        }

        private static void SetVisibility(ref Dictionary<string, string> ValueDict, DesignPhase currentDesignPhase, List<string> inputFile)
        {
            ValueDict.Add("MESH_DISPLAY", "block");
            ValueDict.Add("COLLIDABLES_DISPLAY", "block");
            ValueDict.Add("CUP_DISPLAY", "block");
            ValueDict.Add("REAMING_DISPLAY", "block");
            ValueDict.Add("SKIRT_DISPLAY", "block");
            ValueDict.Add("SCAFFOLD_DISPLAY", currentDesignPhase == DesignPhase.CupQC ? "none" : "block");
            ValueDict.Add("PLATE_DISPLAY", currentDesignPhase == DesignPhase.CupQC ? "none" : "block");
            ValueDict.Add("SCREW_DISPLAY", currentDesignPhase == DesignPhase.CupQC ? "none" : "block");
            ValueDict.Add("TRACEABILITY_DISPLAY", "block");
            if(Path.GetExtension(inputFile.First()) == ".3dm" && currentDesignPhase != DesignPhase.Export)
                ValueDict.Add("CHANGES_DISPLAY", "block");
            else
                ValueDict.Add("CHANGES_DISPLAY", "none");
        }

        // Replace occurrences of {key} in the given format string by value associated with that key
        // in the given dictionary.
        public static string FormatFromDictionary(string formatString, Dictionary<string, string> ValueDict)
        {
            int i = 0;
            StringBuilder newFormatString = new StringBuilder(formatString);
            Dictionary<string, int> keyToInt = new Dictionary<string, int>();
            // Temporarily escape curly braces
            newFormatString = newFormatString.Replace("{", "{{");
            newFormatString = newFormatString.Replace("}", "}}");
            // Convert each <key> in formatString to a number (order in supplied dict) so we can use string.Format()
            foreach (KeyValuePair<string, string> pair in ValueDict)
            {
                newFormatString = newFormatString.Replace("[" + pair.Key + "]", "{" + i.ToString() + "}");
                keyToInt.Add(pair.Key, i);
                i++;
            }
            // Apply standard string formatting Supply values in same order as they were traversed in loop
            string outputString = newFormatString.ToString();
            outputString = string.Format(outputString, ValueDict.OrderBy(x => keyToInt[x.Key]).Select(x => x.Value).ToArray());

            // Restore original curly braces
            outputString = outputString.Replace("{{", "{");
            outputString = outputString.Replace("}}", "}");

            return outputString;
        }
    }
}