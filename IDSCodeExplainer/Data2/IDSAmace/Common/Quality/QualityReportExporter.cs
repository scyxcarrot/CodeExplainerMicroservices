using IDS.Amace.Enumerators;
using IDS.Amace.Fea;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Proxies;
using IDS.Amace.Visualization;
using IDS.Core.DataTypes;
using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Quality;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using IDS.Quality;
using Rhino;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IDS.Amace.Quality
{
    public class QualityReportExporter : Core.Quality.QualityReportExporter
    {
        public Mesh PlateWithTransitionCache = null;

        public QualityReportExporter(DocumentType documentType)
        {
            ReportDocumentType = documentType;
        }

        /// <summary>
        /// Generates the warnings HTML.
        /// </summary>
        /// <param name="screws">The screws.</param>
        /// <param name="plateBumps">The bumps plate.</param>
        /// <param name="cup">The cup.</param>
        /// <param name="drillBitRadius">The drill bit radius</param>
        /// <param name="erraticScrews">The erratic screws.</param>
        /// <returns></returns>
        public static string GenerateScrewWarningsHtml(List<Screw> screws, Mesh plateBumps, Cup cup, double drillBitRadius, out List<int> erraticScrews)
        {
            // Initialize report string
            var reportString = "<h4>Warnings</h4>\n";

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
            foreach (var screw in screws)
            {
                // start with empty string
                var reportStringPart = string.Empty;

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
                if (reportStringPart == "") continue;
                erraticScrews.Add(screw.Index);
                reportStringPart = $"<h5>Screw {screw.Index:F0}</h5>\n" + reportStringPart;
                reportString += reportStringPart;
            }

            // return the html report string
            return reportString;
        }

        private static string GenerateScrewIntersectionsHtml(Dictionary<int, List<int>> screwOverlaps, Screw screw)
        {
            var reportStringPart = string.Empty;
            if (!screwOverlaps.ContainsKey(screw.Index)) return reportStringPart;
            var tempOverlap = screwOverlaps[screw.Index].Select(i => i).ToList();
            var overlapList = string.Join(", ", tempOverlap);
            reportStringPart = $"<p class=\"screwwarning\">Screw intersects screws: {overlapList}</p>\n";

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
            var augmentHighlight = GenerateScrewTableAugmentHighlightText(screw);
            var fixationHighlight = GenerateScrewTableFixationHighlightText(screw);
            var diameterHIghlight = GenerateScrewTableDiameterHighlightText(screw);
            var lengthHighlight = GenerateScrewTableLengthHighlightText(screw);
            var angleHighlight = GenerateScrewTableAngleHighlightText(screw);

            // Create the table cells code
            var cellIndex = $"<th>{screw.Index:D}</th>";
            var cellScrewType = $"<td>{screw.screwBrandType}</td>";
            var cellTotalLength = string.Format("<td{1}>{0:F0}</td>", screw.TotalLength, lengthHighlight);
            var cellInBone = $"<td>{screw.GetDistanceInBone():F0}</td>";
            var cellUntilBone = string.Format("<td>{0:F0}</td>", GenerateScrewTableDistanceUntilBoneText(screw));
            var cellFixation = string.Format("<td{1}>{0}</td>", screw.Fixation, fixationHighlight);
            var cellDiameter = string.Format("<td{1}>{0:F1}</td>", screw.Diameter, diameterHIghlight);
            var cellAxialOffset = $"<td>{screw.AxialOffset:F1}</td>";
            var cellScrewAlignment = $"<td>{screw.screwAlignment.ToString()[0]}</td>";
            var cellPositioning = $"<td>{screw.positioning.ToString()[0]}</td>";
            var cellBumps = string.Format("<td{1}>{0}</td>", screw.AugmentsText, augmentHighlight);
            var cellAngle = string.Format("<td{1}>{0:F1}&deg;</td>", screw.CupRimAngleDegrees, angleHighlight);

            // Create the full HTML code
            var highlightRowClass = highlightRow ? " class=\"highlight\"" : "";
            var tablerow = string.Format(CultureInfo.InvariantCulture,
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
            var highlightAugment = "";
            if ((screw.ScrewAides.ContainsKey(ScrewAideType.MedialBump)) && (screw.positioning == ScrewPosition.Flange))
                highlightAugment = " class=\"highlight\"";
            return highlightAugment;
        }

        private static string GenerateScrewTableFixationHighlightText(Screw screw)
        {
            var highlightFixation = "";
            if (screw.positioning == ScrewPosition.Flange && screw.CorticalBites == 1)
                highlightFixation = " class=\"highlight\"";
            return highlightFixation;
        }

        private static string GenerateScrewTableDistanceUntilBoneText(Screw screw)
        {
            var distanceUntilBoneText = Math.Abs(screw.GetDistanceUntilBone() - double.MaxValue) < 0.00001 ? "? " : screw.GetDistanceUntilBone().ToString("F0");
            return distanceUntilBoneText;
        }

        private static string GenerateScrewTableDiameterHighlightText(Screw screw)
        {
            return (Math.Abs(screw.Diameter - 6.5) > 0.00001) ? " class=\"highlight\"" : "";
        }

        private static string GenerateScrewTableLengthHighlightText(Screw screw)
        {
            var roundedLength = Math.Round(screw.TotalLength, 0); // to avoid that string format rounded to 25 is highlighted
            var highlightLength = (roundedLength < 25 || roundedLength > 60) ? " class=\"highlight\"" : "";
            return highlightLength;
        }

        private static string GenerateScrewTableAngleHighlightText(Screw screw)
        {
            var roundedAngleDegrees = Math.Round(screw.CupRimAngleDegrees, 0); // to avoid that string format rounded to 25 is highlighted
            var highlightAngle = roundedAngleDegrees < Screw.CupRimAngleThresholdDegrees ? " class=\"highlight\"" : "";
            return highlightAngle;
        }

        private static string GenerateScrewVicinityIssuesHtml(Dictionary<int, List<int>> screwVicinityIssues, Dictionary<int, List<int>> screwIntersections, Screw screw)
        {
            var reportStringPart = string.Empty;
            if (!screwVicinityIssues.ContainsKey(screw.Index) || screwIntersections.ContainsKey(screw.Index))
                return reportStringPart;
            var tempOverlap = screwVicinityIssues[screw.Index].Select(i => i).ToList();
            var overlapList = string.Join(", ", tempOverlap);
            reportStringPart = $"<p class=\"screwwarning\">Screw close to screws: {overlapList}</p>\n";

            return reportStringPart;
        }

        private static string GenerateScrewDestroyingBumpIssuesHtml(Dictionary<int, List<int>> bumpProblemsOtherScrews, Screw screw)
        {
            var reportStringPart = string.Empty;
            if (!bumpProblemsOtherScrews.ContainsKey(screw.Index)) return reportStringPart;
            var tempDestroy = bumpProblemsOtherScrews[screw.Index].Select(i => i).ToList();
            var destroyList = string.Join(", ", tempDestroy);
            reportStringPart = $"<p class=\"screwwarning\">Screw hole destroys bump of screws: {destroyList}</p>\n";

            return reportStringPart;
        }

        private static string GenerateCupZoneDestroyingBumpIssuesHtml(List<int> bumpProblemsCupZone, Screw screw)
        {
            var reportStringPart = string.Empty;
            if (bumpProblemsCupZone.Contains(screw.Index))
                reportStringPart = "<p class=\"screwwarning\">Cup Zone destroys screw bump</p>\n";
            return reportStringPart;
        }

        private static string GenerateShaftTrajectoryIssuesHtml(List<int> shaftProblems, Screw screw)
        {
            var reportStringPart = string.Empty;
            if (shaftProblems.Contains(screw.Index))
                reportStringPart = "<p class=\"screwwarning\">Shaft trajectory intersects plate</p>\n";
            return reportStringPart;
        }

        private static string GenerateInsertTrajectoryIssuesHtml(List<int> insertProblems, Screw screw)
        {
            var reportStringPart = string.Empty;
            if (insertProblems.Contains(screw.Index))
                reportStringPart = "<p class=\"screwwarning\">Insert trajectory obstructed</p>\n";
            return reportStringPart;
        }

        private static string GenerateScrewInCupZoneIssuesHtml(Screw screw)
        {
            var reportStringPart = string.Empty;
            var checkCupZone = screw.CheckCupZone();
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (checkCupZone)
            {
                case QualityCheckResult.NotOK:
                    reportStringPart = "<p class=\"screwwarning\">Cup Zone intersection</p>\n";
                    break;
                case QualityCheckResult.Error:
                    reportStringPart = "<p class=\"screwwarning\">Cup Zone intersection ?</p>\n";
                    break;
            }
            return reportStringPart;
        }

        private static string GenerateGuideHoleSafetyZoneIntersectionHtml(Dictionary<int, List<int>> guideHoleSafetyZoneIntersections, Screw screw)
        {
            var reportStringPart = string.Empty;

            if (!guideHoleSafetyZoneIntersections.ContainsKey(screw.Index)) return reportStringPart;
            var intersectedIndexes = guideHoleSafetyZoneIntersections[screw.Index].Select(i => i).ToList();
            var intersectList = string.Join(", ", intersectedIndexes);
            reportStringPart =
                $"<p class=\"screwwarning\">Guide Hole Boolean intersection with Guide Hole: {intersectList}</p>\n";

            return reportStringPart;
        }

        private static string GenerateBonePenetrationIssuesHtml(Screw screw)
        {
            var reportStringPart = string.Empty;
            var checkBonePenetration = screw.CheckBonePenetration();
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (checkBonePenetration)
            {
                case QualityCheckResult.NotOK:
                    reportStringPart =
                        $"<p class=\"screwwarning\">Bone Penetration = {screw.GetDistanceInBone():F0}mm</p>\n";
                    break;
                case QualityCheckResult.Error:
                    reportStringPart = "<p class=\"screwwarning\">Bone Penetration = ???</p>\n";
                    break;
            }
            return reportStringPart;
        }

        private static string GenerateCupRimAngleIssuesHtml(Screw screw)
        {
            var reportStringPart = string.Empty;
            var checkCupRimAngle = screw.CheckCupRimAngle();
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (checkCupRimAngle)
            {
                case QualityCheckResult.Error:
                    reportStringPart = "<p class=\"screwwarning\">Cup Rim Angle = ???</p>\n";
                    break;
                case QualityCheckResult.NotOK:
                    reportStringPart = $"<p class=\"screwwarning\">Cup Rim Angle = {screw.CupRimAngleDegrees:F1}°</p>\n";
                    break;
            }
            return reportStringPart;
        }

        private static string GenerateScrewLengthIssuesHtml(Screw screw)
        {
            var reportStringPart = string.Empty;
            var checkLength = screw.CheckScrewLength();
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (checkLength)
            {
                case QualityCheckResult.NotOK:
                    reportStringPart = "<p class=\"screwwarning\">Length < 14mm</p>\n";
                    break;
                case QualityCheckResult.Error:
                    reportStringPart = "<p class=\"screwwarning\">Length issue ?</p>\n";
                    break;
            }
            return reportStringPart;
        }

        // This method fills a dictionary with info for the QC report
        protected override bool FillReport(IImplantDirector directorInterface, string filename, out Dictionary<string, string> reportValues)
        {
            var director = (ImplantDirector)directorInterface;
            // Init
            reportValues = new Dictionary<string, string>();
            var doc = director.Document;
            var axial = director.Inspector.AxialPlane; // axial
            var coronal = director.Inspector.CoronalPlane; // coronal
            var sagittal = director.Inspector.SagittalPlane; // sagittal

#if DEBUG
            var stopWatchQcReport = new Stopwatch();
            stopWatchQcReport.Start();
#endif
            // Header
            ////////////////
            AddHeaderInformation(ref reportValues, director.Inspector.CaseId, ReportDocumentType.ToString(), director.defectIsLeft);
            AddHeaderImages(ref reportValues, doc, Width, Height, ReportDocumentType);

#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report header: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif

            // Changes
            ////////////////
            AddChangesInformation(ref reportValues, director, ReportDocumentType);
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
                var preopPelvis = objManager.GetBuildingBlock(IBB.PreopPelvis).Geometry as Mesh;
                var originalPelvis = objManager.GetBuildingBlock(IBB.DefectPelvis).Geometry as Mesh;
                originalMeshDifference = AnalysisMeshMaker.CreateMeshDifference(originalPelvis, preopPelvis);
                graftVolume = Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.BoneGraft], true);
            }
            var preopPelvisVolume = Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.PreopPelvis], true);
            var originalPelvisVolume = Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.DefectPelvis], true);

            AddBoneGraftInformation(ref reportValues, boneGraftImported, graftVolume, originalPelvisVolume, preopPelvisVolume);
            AddBoneGraftImages(ref reportValues, boneGraftImported, originalMeshDifference, doc, Width, Height);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report bone grafts: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif

            // Bone mesh
            ////////////////
            var designPelvis = objManager.GetBuildingBlock(IBB.DesignPelvis).Geometry as Mesh;
            var defectPelvis = objManager.GetBuildingBlock(IBB.DefectPelvis).Geometry as Mesh;
            // Determine if design pelvis differs from
            var designPelvisUsed = false;
            designPelvisUsed |= (defectPelvis.Vertices.Count != designPelvis.Vertices.Count); // true if number of vertices differs;
            if (!designPelvisUsed) // if same number of vertices
            {
                List<double> distances;
                MeshAnalysis.MeshToMeshAnalysis(designPelvis, defectPelvis, out distances);
                designPelvisUsed |= (distances.Max() > 0.01);
            }
            // Add to values
            AddBoneMeshInformation(ref reportValues, designPelvisUsed, designPelvis, defectPelvis);
            AddBoneMeshImages(ref reportValues, designPelvisUsed, doc, Width, Height);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report mesh: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif
            // Collidable entities
            ////////////////
            var hasCollidables = objManager.GetBuildingBlock(IBB.CollisionEntity) != null;
            AddCollidablesInformation(ref reportValues, hasCollidables);
            AddCollidablesImages(ref reportValues, hasCollidables, doc, Width, Height);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report collidables: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif
            // Cup
            ////////////////
            AddCupInformation(ref reportValues, director.cup, axial.Origin, director.CenterOfRotationDefectFemur, director.CenterOfRotationContralateralFemurMirrored, director.CenterOfRotationDefectSsm, director.CenterOfRotationContralateralSsmMirrored, axial, coronal, sagittal, director);
            AddCupImages(ref reportValues, doc, Width, Height);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report cup: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif
            // Reaming
            ////////////////
            AddReamingInformation(ref reportValues, director.cup, director);
            AddReamingImages(ref reportValues, doc, Width, Height);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report reaming: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif
            // Skirt
            ////////////////
            AddSkirtImages(ref reportValues, doc, Width, Height);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report skirt: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Restart();
#endif

            if (ReportDocumentType != DocumentType.CupQC)
            {
                // Scaffold
                ////////////////
                AddScaffoldInformation(ref reportValues, director.InsertionAnteversionDegrees, director.InsertionInclinationDegrees, director);
                AddScaffoldImages(ref reportValues, doc, Width, Height);
#if DEBUG
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report scaffold: {0}", stopWatchQcReport.Elapsed);
                stopWatchQcReport.Restart();
#endif
                // Screws
                ////////////////
                var screwManager = new ScrewManager(director.Document);
                var screws = screwManager.GetAllScrews().ToList();
                var plateBumps = objManager.GetBuildingBlock(IBB.PlateBumps).Geometry as Mesh;
                var cup = director.cup;
                AddScrewInformation(ref reportValues, screws, plateBumps, cup, director.DrillBitRadius);
                AddScrewImages(ref reportValues, doc, Width, Height, ReportDocumentType);
#if DEBUG
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report screws: {0}", stopWatchQcReport.Elapsed);
                stopWatchQcReport.Restart();
#endif
                // Plate
                ////////////////
                var reamedPelvis = objManager.GetBuildingBlock(IBB.OriginalReamedPelvis).Geometry as Mesh;
                var solidPlateBottom = objManager.GetBuildingBlock(IBB.SolidPlateBottom).Geometry as Mesh;
                var solidPlateTop = objManager.GetBuildingBlock(IBB.SolidPlateTop).Geometry as Mesh;
                var solidPlateSide = objManager.GetBuildingBlock(IBB.SolidPlateSide).Geometry as Mesh;
                var plateWithTransition = PlateWithTransitionCache ?? PlateWithTransitionForExportCreator.CreatePlateWithTransition(ReportDocumentType, director);
                AddPlateInformation(ref reportValues, reamedPelvis, solidPlateBottom, solidPlateTop, solidPlateSide, director.cup.filledCupMesh, director.cup.innerReamingVolumeMesh, director.PlateThickness, director.AmaceFea, director.cup, plateWithTransition);
                AddPlateImages(ref reportValues, doc, Width, Height, director.AmaceFea, reamedPelvis, plateWithTransition);
#if DEBUG
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report plate: {0}", stopWatchQcReport.Elapsed);
                stopWatchQcReport.Restart();
#endif
            }

            // Traceability
            ////////////////
            AddTraceability(ref reportValues, director.ComponentVersions, director.Inspector.PreOperativeId, director.draft, director.version, doc.Path, director.InputFiles, filename);
#if DEBUG
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "QC report traceability: {0}", stopWatchQcReport.Elapsed);
            stopWatchQcReport.Stop();
#endif

            // Visibility Settings
            ////////////////
            SetVisibility(ref reportValues, ReportDocumentType, director.InputFiles);

            return true;
        }

        // TODO: refactor to remove director from arguments
        private static void AddChangesInformation(ref Dictionary<string, string> valueDictionary, ImplantDirector director, DocumentType documentType)
        {
            if (documentType == DocumentType.Export) return;
            var inputfile = Path.Combine(DirectoryStructure.GetWorkingDir(director.Document), "..", Path.GetFileName(director.InputFiles.First()));

            // Only second or later draft (i.e. started from a 3dm file) needs to have a changes section)
            if (Path.GetExtension(inputfile) != ".3dm") return;
            Dictionary<string, bool> changed;
            var changes = DraftComparison.GetDocumentDifference(director, inputfile, out changed);
            valueDictionary = DictionaryUtilities.MergeDictionaries(valueDictionary, changes);

            // Add colors
            const string red = "#D70000";
            const string green = "#28A828";

            // Indicate changes on three levels
            AddChangesPerSubsection(valueDictionary, changed, changes, red, green);
            AddChangesPerSection(valueDictionary, changed, changes, red, green);
            AddChangesPerPhase(valueDictionary, changed, changes, red, green);
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
            foreach (var key in changes.Keys)
            {
                var baseKey = GetChangesBaseKey(key);
                var keyParts = key.Split(new char[] { '_' }).ToList();
                var phaseColorKey = keyParts.Count > 1 ? $"{string.Join("_", keyParts.GetRange(0, 1))}_COLOR" : "";
                if (phaseColorKey == "") continue;
                // Not in dictionary, add always
                if (!valueDictionary.ContainsKey(phaseColorKey))
                    valueDictionary.Add(phaseColorKey, changed[baseKey] ? red : green);
                // Change from green to red if a subvalue is colored red
                else if (changed[baseKey] && valueDictionary[phaseColorKey] == green)
                    valueDictionary[phaseColorKey] = red;
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
            foreach (var key in changes.Keys)
            {
                // Add section color to indicate if change occured
                var keyParts = key.Split(new char[] { '_' }).ToList();
                var baseKey = GetChangesBaseKey(key);
                var sectionColorKey = keyParts.Count > 2 ? $"{string.Join("_", keyParts.GetRange(0, 2))}_COLOR" : "";
                if (sectionColorKey == "") continue;
                // Not in dictionary, add always
                if (!valueDictionary.ContainsKey(sectionColorKey))
                    valueDictionary.Add(sectionColorKey, changed[baseKey] ? red : green);
                // Change from green to red if a subvalue is colored red
                else if (changed[baseKey] && valueDictionary[sectionColorKey] == green)
                    valueDictionary[sectionColorKey] = red;
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
            const string screwStringTemplate = @"<tr class=""datarow"" style=""display:none;"">
                                                <td class=""precolumn""></td><td class=""subsection"" style=""background-color:[SCREW_N_COLOR];"">[SCREW_N_NAME]</td>
                                                <td>[SCREW_N_DIFF]</td>
                                                <td>[SCREW_N_PREV]</td>
                                                <td>[SCREW_N_CURR]</td>
                                                </tr>";
            // All text for separate screws will be stored in this variable
            var screwRows = "";
            foreach (var key in changes.Keys)
            {
                var baseKey = GetChangesBaseKey(key);
                var keyParts = key.Split(new char[] { '_' }).ToList();
                var subsectionColorKey = keyParts.Count > 3 ? $"{string.Join("_", keyParts.GetRange(0, 3))}_COLOR" : "";

                // Check if the key refers to a key or not
                if (baseKey.ToUpper().Contains("SCREWS_INDIVIDUAL") && keyParts[keyParts.Count - 1].ToUpper() == "DIFF" && baseKey.ToUpper() != "SCREWS_INDIVIDUAL")
                {
                    var diffKey = $"{baseKey}_DIFF";
                    var currKey = $"{baseKey}_CURR";
                    var prevKey = $"{baseKey}_PREV";

                    var screwString = screwStringTemplate;
                    screwString = screwString.Replace("[SCREW_N_CURR]", changes[currKey]);
                    screwString = screwString.Replace("[SCREW_N_PREV]", changes[prevKey]);
                    screwString = screwString.Replace("[SCREW_N_DIFF]", changes[diffKey]);
                    screwString = screwString.Replace("[SCREW_N_NAME]", $"Screw {keyParts[2]}");

                    screwString = screwString.Replace("[SCREW_N_COLOR]", changed[baseKey] ? red : green);

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
            valueDictionary.Add("SCREWS_INDIVIDUAL_CLASS", screwRows != string.Empty ? "datarow" : "alwayshidden");
        }

        private static string GetChangesBaseKey(string key)
        {
            var keyParts = key.Split(new char[] { '_' }).ToList();
            var baseKey = string.Join("_", keyParts.GetRange(0, keyParts.Count - 1));
            return baseKey;
        }

        private static void AddHeaderInformation(ref Dictionary<string, string> valueDictionary,
            string caseId,
            string currentDesignPhase,
            bool defectIsLeft)
        {
            valueDictionary.Add("CASE_ID", caseId);
            valueDictionary.Add("PHASE", currentDesignPhase);
            valueDictionary.Add("DEFECT_SIDE", defectIsLeft ? "Left" : "Right");
        }

        private static void AddHeaderImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height, DocumentType docType)
        {
            valueDictionary.Add("IMG_PREOP", ScreenshotsOverview.GeneratePreOpImageString(doc, width, height));
            valueDictionary.Add("IMG_OVERVIEW_ANTERIOR", ScreenshotsOverview.GenerateOverviewImageString(doc, width, height, CameraView.Acetabular, docType));
            valueDictionary.Add("IMG_OVERVIEW_POSTERIOR", ScreenshotsOverview.GenerateOverviewImageString(doc, width, height, CameraView.Acetabularinverse, docType));
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
            if (!graftsImported) return;
            // Difference maps
            valueDictionary.Add("IMG_PREOP_AND_GRAFT_ACETABULAR", ScreenshotsBoneGraft.GenerateBoneGraftImageString(doc, width, height, CameraView.Acetabular));
            valueDictionary.Add("IMG_ORIGINAL_PELVIS_ACETABULAR", ScreenshotsBoneGraft.GenerateOriginalPelvisImageString(doc, width, height, CameraView.Acetabular));
            valueDictionary.Add("IMG_GRAFT_COMPARISON_ACETABULAR", ScreenshotsBoneGraft.GenerateBoneGraftDifferenceMapImageString(doc, differenceMesh, width, height, CameraView.Acetabular));
        }

        private static void AddBoneMeshInformation(ref Dictionary<string, string> valueDictionary, bool designPelvisUsed, Mesh designPelvis, Mesh defectPelvis)
        {
            //var diffMesh = Booleans.PerformBooleanSubtraction(defectPelvis, designPelvis);
            var volDiff = VolumeMassProperties.Compute(designPelvis).Volume - VolumeMassProperties.Compute(defectPelvis).Volume;
            volDiff = Math.Round(volDiff / 1000, 1);

            valueDictionary.Add("PELVIS_VOL_DIFF", volDiff.ToString("F0"));
            valueDictionary.Add("PELVIS_FIXED", designPelvisUsed ? "Yes" : "No");
            valueDictionary.Add("MESH_IMAGE_DISPLAY", designPelvisUsed ? "block" : "none");
        }

        private static void AddBoneMeshImages(ref Dictionary<string, string> valueDictionary, bool designPelvisUsed, RhinoDoc doc, int width, int height)
        {
            // Generate images if necessary
            if (!designPelvisUsed) return;
            // Difference maps
            valueDictionary.Add("IMG_PELVIS_FIXED_ANTERIOR", ScreenshotsOverview.GenerateBoneMeshImageString(doc, width, height, CameraView.Acetabular));
            valueDictionary.Add("IMG_PELVIS_FIXED_LATERAL", ScreenshotsOverview.GenerateBoneMeshImageString(doc, width, height, CameraView.Acetabularinverse));
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
            if (!hasCollidables) return;
            valueDictionary.Add("IMG_COLLIDABLES_ACETABULAR", ScreenshotsOverview.GenerateCollidablesImageString(doc, width, height, CameraView.Acetabular));
            valueDictionary.Add("IMG_COLLIDABLES_ACETABULARINVERSE", ScreenshotsOverview.GenerateCollidablesImageString(doc, width, height, CameraView.Acetabularinverse));
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
            AddAbsoluteCupPositionToDictionary(valueDictionary, cup.centerOfRotation, refPCS, axial, coronal, sagittal, director.SignInfSupClat, director.SignMedLatPcs, director.SignAntPosPcs, "CUP_INF_SUP_PCS", "CUP_LAT_MED_PCS", "CUP_POST_ANT_PCS");
            // Relative position
            AddRelativeCupPositionToDictionary(valueDictionary, cup.centerOfRotation, refDefect, axial, coronal, sagittal, director.SignInfSupDef, director.SignMedLatDef, director.SignAntPosDef, "CUP_INF_SUP_DEF", "CUP_LAT_MED_DEF", "CUP_POST_ANT_DEF");
            AddRelativeCupPositionToDictionary(valueDictionary, cup.centerOfRotation, refContralateral, axial, coronal, sagittal, director.SignInfSupClat, director.SignMedLatClat, director.SignAntPosClat, "CUP_INF_SUP_CLAT", "CUP_LAT_MED_CLAT", "CUP_POST_ANT_CLAT");
            AddRelativeCupPositionToDictionary(valueDictionary, cup.centerOfRotation, refDefectSSM, axial, coronal, sagittal, director.SignInfSupDef, director.SignMedLatDef, director.SignAntPosDef, "CUP_INF_SUP_SSM_DEF", "CUP_LAT_MED_SSM_DEF", "CUP_POST_ANT_SSM_DEF");
            AddRelativeCupPositionToDictionary(valueDictionary, cup.centerOfRotation, refContralateralSSM, axial, coronal, sagittal, director.SignInfSupClat, director.SignMedLatClat, director.SignAntPosClat, "CUP_INF_SUP_SSM_CLAT", "CUP_LAT_MED_SSM_CLAT", "CUP_POST_ANT_SSM_CLAT");
        }

        private static void AddCupParametersToDictionary(Dictionary<string, string> valueDictionary, Cup cup)
        {
            valueDictionary.Add("CUP_THICK", cup.cupType.CupThickness.ToString("F0"));
            valueDictionary.Add("CUP_POR_THICK", cup.cupType.PorousThickness.ToString("F0"));
            valueDictionary.Add("CUP_DESIGN", cup.cupType.CupDesign.ToString());
            valueDictionary.Add("CUP_DIAM_INNER", cup.innerCupDiameter.ToString("F0"));
            valueDictionary.Add("CUP_DIAM_OUTER", cup.outerCupDiameter.ToString("F0"));
            valueDictionary.Add("CUP_DIAM_POR_OUTER", ((cup.outerCupRadius + cup.cupType.PorousThickness) * 2).ToString("F0"));
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

            var rbvCupVolumeCc = Amace.Utilities.Volume.RBVCupVolumeCC(director);
            valueDictionary.Add("RBV_CUP", rbvCupVolumeCc.ToString("F1"));

            var rbvCupGraftVolumeCc = objManager.HasBuildingBlock(IBB.CupRbvGraft) ? Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.CupRbvGraft], true) : 0;
            valueDictionary.Add("RBV_CUP_GRAFT", rbvCupGraftVolumeCc.ToString("F1"));

            var rbvAdditionalVolumeCc = Amace.Utilities.Volume.RBVAdditionalVolumeCC(director);
            valueDictionary.Add("RBV_ADDITIONAL", rbvAdditionalVolumeCc.ToString("F1"));

            var rbvAdditionalGraftVolumeCc = objManager.HasBuildingBlock(IBB.AdditionalRbvGraft) ? Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.AdditionalRbvGraft], true) : 0;
            valueDictionary.Add("RBV_ADDITIONAL_GRAFT", rbvAdditionalGraftVolumeCc.ToString("F1"));

            var rbvTotalVolumeCc = Amace.Utilities.Volume.RBVTotalVolumeCC(director);
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
            valueDictionary.Add("MBV_VOLUME", Amace.Utilities.Volume.FinalizedScaffoldVolumeCC(director).ToString("F1"));
            valueDictionary.Add("UC_AV", insertionAv.ToString("F1"));
            valueDictionary.Add("UC_INCL", insertionIncl.ToString("F1"));
        }

        private static void AddScaffoldImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height)
        {
            valueDictionary.Add("IMG_SCAFFOLD_INSERTION", ScreenshotsScaffold.GenerateScaffoldImageString(doc, width, height, CameraView.Insertion));
            valueDictionary.Add("IMG_SCAFFOLD_INSERTION_INVERSE", ScreenshotsScaffold.GenerateScaffoldImageString(doc, width, height, CameraView.Insertioninverse));
        }

        private static void AddPlateInformation(ref Dictionary<string, string> valueDictionary, Mesh reamedPelvis, Mesh solidPlateBottom, Mesh solidPlateTop, Mesh solidPlateSide, Mesh cupFilledMesh, Mesh cupInnerReamingVolumeMesh, double plateThickness, Amace.Fea.AmaceFea fea, Cup cup, Mesh plateWithTransition)
        {
            /// \todo Remove ImplantDirector dependency

            var distances = MeshUtilities.Mesh2MeshDistance(solidPlateBottom, reamedPelvis);
            var plateAnalyzer = new PlateAnalyzer(solidPlateTop, solidPlateBottom, solidPlateSide, new List<Mesh>() { cupFilledMesh, cupInnerReamingVolumeMesh }, plateThickness);
            var edgeAngles = plateAnalyzer.GetSideSurfaceLinesAndAngles();

            var min = edgeAngles.Min(x => x.Item2);
            var max = edgeAngles.Max(x => x.Item2);
            
            var intersection = Booleans.PerformBooleanIntersection(reamedPelvis, plateWithTransition);
            var intersectionVolumeCc = intersection != null ? Volume.MeshVolume(intersection, true) : 0;

            valueDictionary.Add("PLATE_MINIMAL_CLEARANCE", distances.Min().ToString("F4"));
            valueDictionary.Add("POLISHING_OFFSET", cup.CupRingPolishingOffset.ToString("F1"));
            valueDictionary.Add("PLATE_THICKNESS", plateThickness.ToString("F1"));
            valueDictionary.Add("PLATE_MIN_EDGE_ANGLE", min.ToString("F0"));
            valueDictionary.Add("PLATE_MAX_EDGE_ANGLE", max.ToString("F0"));
            valueDictionary.Add("PLATE_TRANSITION_INTERSECTION", intersectionVolumeCc.ToString("F1"));

            if (fea != null)
            {
                valueDictionary.Add("FEA_LOAD_MAGNITUDE", fea.LoadMagnitude.ToString("F0"));
                valueDictionary.Add("FEA_LOAD_DEG_THRESH", fea.LoadMeshDegreesThreshold.ToString("F1"));

                valueDictionary.Add("FEA_BC_DIST_THRESH", fea.BoundaryConditionsDistanceThreshold.ToString("F1"));
                valueDictionary.Add("FEA_BC_NOISE_THRESH", fea.BoundaryConditionsNoiseShellThreshold.ToString("F1"));

                valueDictionary.Add("FEA_MESH_EDGE_LENGTH", fea.TargetEdgeLength.ToString("F2"));

                valueDictionary.Add("FEA_MATERIAL_EMOD", fea.material.ElasticityEModulus.ToString("F0"));
                valueDictionary.Add("FEA_MATERIAL_POISSON", fea.material.ElasticityPoissonRatio.ToString("F2"));
                valueDictionary.Add("FEA_MATERIAL_UTS", fea.material.UltimateTensileStrength.ToString("F0"));
                valueDictionary.Add("FEA_MATERIAL_FATIGUELIM", fea.material.FatigueLimit.ToString("F0"));

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

        private static void AddPlateImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height, AmaceFea fea, Mesh reamedPelvis, Mesh plateWithTransition)
        {
            //duplications are made here so that changes to individual vertex color will not affect other instances
            var plateWithTransitionDuplicate = plateWithTransition.DuplicateMesh();
            var reamedPelvisDuplicate = reamedPelvis.DuplicateMesh();
            
            var implantClearance = AnalysisMeshMaker.CreateImplantWithTransitionClearanceMesh(plateWithTransitionDuplicate, reamedPelvisDuplicate).DuplicateMesh();
            if (implantClearance == null)
            {
                throw new Exception("Could not create plate clearance");
            }

            plateWithTransitionDuplicate.VertexColors.CreateMonotoneMesh(Visualization.Colors.Metal);
            reamedPelvisDuplicate.VertexColors.CreateMonotoneMesh(BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Color);

            valueDictionary.Add("IMG_IMPLANT_CLEARANCE_ACETABULAR", ScreenshotsPlate.GeneratePlateClearanceImageString(doc, width, height, CameraView.Acetabular, reamedPelvisDuplicate, plateWithTransitionDuplicate));
            valueDictionary.Add("IMG_IMPLANT_CLEARANCE_MEDIAL", ScreenshotsPlate.GeneratePlateClearanceImageString(doc, width, height, CameraView.Medial, implantClearance));
            valueDictionary.Add("IMG_IMPLANT_CLEARANCE_ACETABULARINVERSE", ScreenshotsPlate.GeneratePlateClearanceImageString(doc, width, height, CameraView.Acetabularinverse, implantClearance));
            valueDictionary.Add("IMG_IMPLANT_EDGE_LENGTH_ACETABULAR", ScreenshotsPlate.GeneratePlateAngleImageString(doc, width, height, CameraView.Acetabular));
            valueDictionary.Add("IMG_IMPLANT_EDGE_LENGTH_ILLIUM", ScreenshotsPlate.GeneratePlateAngleImageString(doc, width, height, CameraView.Illium));

            if (fea == null) return;
            var feaConduit = new FeaConduit(fea);

            var feaImageStrings = ScreenshotsPlate.GeneratePlateFeaImageStrings(doc, width, height, fea, feaConduit, IBB.PlateHoles, false, TuneFeaVisualisation.SafetyFactorLow, TuneFeaVisualisation.SafetyFactorMiddle, TuneFeaVisualisation.SafetyFactorHigh, TuneFeaVisualisation.FatigueLimit, TuneFeaVisualisation.UltimateTensileStrength);
            var feaJavaScriptArrayString = Convert3DImageArrayToJavaScriptString(valueDictionary, feaImageStrings, "feaList", "feaSubList");
            valueDictionary.Add("IMAGE_ARRAY_FEA", feaJavaScriptArrayString);

            var feaBcLoadImageStrings = ScreenshotsPlate.GeneratePlateFeaImageStrings(doc, width, height, fea, feaConduit, IBB.PlateHoles, true, TuneFeaVisualisation.SafetyFactorLow, TuneFeaVisualisation.SafetyFactorMiddle, TuneFeaVisualisation.SafetyFactorHigh, TuneFeaVisualisation.FatigueLimit, TuneFeaVisualisation.UltimateTensileStrength);
            var feaBcLoadJavascriptArrayString = Convert3DImageArrayToJavaScriptString(valueDictionary, feaBcLoadImageStrings, "feaBcLoadList", "feaBcLoadSublist");
            valueDictionary.Add("IMAGE_ARRAY_FEABCLOAD", feaBcLoadJavascriptArrayString);

            valueDictionary.Add("IMG_FATIGUE_COLOR_SCALE", Screenshots.GenerateImageString(feaConduit.CreateColorScaleBitmap(), true));
        }

        private static string Convert3DImageArrayToJavaScriptString(Dictionary<string, string> valueDictionary, string[][] imageStrings, string listName, string subListName)
        {
            // Add base64 tag
            foreach (var t in imageStrings)
                for (var j = 0; j < t.Length; j++)
                    t[j] = "data: image / jpeg; base64," + t[j];
            // Create string
            var feaJavascriptArray = CreateJavaScriptArrayOfArrays(imageStrings, listName, subListName);
            return feaJavascriptArray;
        }

        private static void AddScrewInformation(ref Dictionary<string, string> valueDictionary, List<Screw> screws, Mesh plateBumps, Cup cup, double drillBitRadius)
        {
            screws.Sort();

            List<int> erraticScrews;
            valueDictionary.Add("SCREW_WARNINGS", GenerateScrewWarningsHtml(screws, plateBumps, cup, drillBitRadius, out erraticScrews));

            var screwinfo = string.Empty;
            foreach (var screw in screws)
            {
                screwinfo = screwinfo + GenerateScrewTableRowHtml(screw, erraticScrews.Contains(screw.Index));
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

        private static void AddScrewImages(ref Dictionary<string, string> valueDictionary, RhinoDoc doc, int width, int height, DocumentType docType)
        {
            valueDictionary.Add("IMG_SCREWS_ACETABULAR", ScreenshotsScrews.GenerateScrewNumberImageString(doc, width, height, CameraView.Acetabular, ScrewConduitMode.WarningColors, true, docType));
            valueDictionary.Add("IMG_SCREWS_ILLIUM", ScreenshotsScrews.GenerateScrewNumberImageString(doc, width, height, CameraView.Illium, ScrewConduitMode.WarningColors, true, docType));
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
            foreach (var versionType in new string[] { "commit", "build" })
            {
                foreach (var component in new string[] { "IDS", "RhinoMatSDKOperations", "MatSDK_DLL", "MatSAXLite", "PyGeneralFunctions", "Documentation" })
                {
                    var value = "Unknown";
                    try
                    {
                        value = componentVersions[component][versionType];
                    }
                    catch { }
                    valueDictionary.Add($"{component.ToUpper()}_{versionType.ToUpper()}", value);
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

        private static void SetVisibility(ref Dictionary<string, string> valueDict, DocumentType currentDocumentType, List<string> inputFile)
        {
            valueDict.Add("MESH_DISPLAY", "block");
            valueDict.Add("COLLIDABLES_DISPLAY", "block");
            valueDict.Add("CUP_DISPLAY", "block");
            valueDict.Add("REAMING_DISPLAY", "block");
            valueDict.Add("SKIRT_DISPLAY", "block");
            valueDict.Add("SCAFFOLD_DISPLAY", currentDocumentType == DocumentType.CupQC ? "none" : "block");
            valueDict.Add("PLATE_DISPLAY", currentDocumentType == DocumentType.CupQC ? "none" : "block");
            valueDict.Add("SCREW_DISPLAY", currentDocumentType == DocumentType.CupQC ? "none" : "block");
            valueDict.Add("TRACEABILITY_DISPLAY", "block");
            if (Path.GetExtension(inputFile.First()) == ".3dm" && currentDocumentType != DocumentType.Export)
                valueDict.Add("CHANGES_DISPLAY", "block");
            else
                valueDict.Add("CHANGES_DISPLAY", "none");
        }


    }
}
