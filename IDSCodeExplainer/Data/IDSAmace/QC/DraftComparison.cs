using IDS.Amace;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IDS.Quality
{
    internal class DraftComparison
    {
        // Compare the current project to another 3dm file
        // Input:   director = the director of the currently opened file file = the file you want to
        //          extract the implantbuildingblocks from
        public static Dictionary<string, string> GetDocumentDifference(ImplantDirector director, string file, out Dictionary<string, bool> changed)
        {
            // Read old file
            var prevFile = Rhino.FileIO.File3dm.Read(file);

            // Init
            changed = new Dictionary<string, bool>();
            var information = new Dictionary<string, string>();

            // Objects from current file
            var cup = director.cup;
            var inspector = director.Inspector;
            var ax = inspector.AxialPlane; // axial
            var co = inspector.CoronalPlane; // coronal
            var sa = inspector.SagittalPlane; // sagittal

            // Init objects from previous file
            Cup prevCup = null;
            Mesh originalPelvisPrev = null;
            Mesh boneGraftPrev = null;
            var reamCupPrev = new Mesh();
            var reamAddPrev = new Mesh();
            var reamTotalPrev = new Mesh();
            var reamBlocksPrev = 0;
            Curve skirtBoneCurvePrev = null;
            Curve skirtCupCurvePrev = null;
            Mesh scaffoldTempPrev = null;
            Mesh scaffoldFinalPrev = null;
            var screwsPrev = new List<Screw>();
            var screwsAmountPrev = 0;
            Curve plateTopPrev = null;
            Curve plateBottomPrev = null;
            Curve roiPrev = null;

            // Get parameters from document notes (only parameters that are not part of an object)
            var prevParameters = prevFile.Notes.Notes.Split('\n').ToList();
            var scaffoldUndercutInclPrev = double.MinValue;
            var scaffoldUndercutAvPrev = double.MinValue;
            var plateThicknessPrev = double.MinValue;
            var plateClearancePrev = double.MinValue;
            var drillBitDiameterPrev = double.MinValue;
            foreach (var entry in prevParameters)
            {
                var entryParts = entry.Split(',');
                if (entryParts.Count() < 2)
                {
                    continue;
                }

                switch (entryParts[0])
                {
                    case ("Undercut Anteversion"):
                        scaffoldUndercutAvPrev = double.Parse(entryParts[1], CultureInfo.InvariantCulture);
                        break;

                    case ("Undercut Inclination"):
                        scaffoldUndercutInclPrev = double.Parse(entryParts[1], CultureInfo.InvariantCulture);
                        break;

                    case ("Plate Thickness"):
                        plateThicknessPrev = double.Parse(entryParts[1], CultureInfo.InvariantCulture);
                        break;

                    case ("Plate Clearance"):
                        plateClearancePrev = double.Parse(entryParts[1], CultureInfo.InvariantCulture);
                        break;

                    case ("Drill Bit Diameter"):
                        drillBitDiameterPrev = double.Parse(entryParts[1], CultureInfo.InvariantCulture);
                        break;
                }
            }

            ///////////////////////////////
            // Gather objects from old file
            ///////////////////////////////
            foreach (var obj in prevFile.Objects)
            {
                // Determine block type
                IBB blockType;
                var rc = obj.Attributes.UserDictionary.TryGetEnumValue<IBB>(ImplantBuildingBlockProperties.KeyBlockType, out blockType);
                if (!rc)
                {
                    continue;
                }

                // Gather
                switch (blockType)
                {
                    case (IBB.OriginalReamedPelvis):
                        break;

                    case (IBB.WrapSunkScrew):
                        break;

                    case (IBB.Cup):
                        {
                            var overrideAttr = obj.Attributes; // Override to avoid other functions to confuse it with the current cup
                            overrideAttr.Name = "";
                            var objId = director.Document.Objects.Add(obj.Geometry, overrideAttr);
                            if (objId == Guid.Empty)
                            {
                                continue;
                            }

                            prevCup = Cup.CreateFromArchived(director.Document.Objects.Find(objId), false);

                            // Delete the object from the document again
                            director.Document.Objects.Unlock(objId, true);
                            director.Document.Objects.Delete(objId, true);

                            break;
                        }
                    case (IBB.Screw):
                        {
                            var overrideAttr = obj.Attributes; // Override to avoid other functions to confuse it with the current screw
                            overrideAttr.Name = "";
                            var objId = director.Document.Objects.Add(obj.Geometry, overrideAttr);
                            if (objId == Guid.Empty)
                            {
                                continue;
                            }

                            var oldScrew = Screw.CreateFromArchived(director.Document.Objects.Find(objId), false);
                            screwsPrev.Add(oldScrew);
                            screwsAmountPrev++;

                            // Delete the object from the document again
                            director.Document.Objects.Unlock(objId, true);
                            director.Document.Objects.Delete(objId, true);

                            break;
                        }
                    case (IBB.ScaffoldVolume):
                        scaffoldTempPrev = (Mesh)obj.Geometry;
                        break;

                    case (IBB.ScaffoldFinalized):
                        scaffoldFinalPrev = (Mesh)obj.Geometry;
                        break;

                    case (IBB.CupRbv):
                        reamCupPrev.Append((Mesh)obj.Geometry);
                        break;

                    case (IBB.AdditionalRbv):
                        reamAddPrev.Append((Mesh)obj.Geometry);
                        break;

                    case (IBB.TotalRbv):
                        reamTotalPrev.Append((Mesh)obj.Geometry);
                        break;

                    case (IBB.ExtraReamingEntity):
                        reamBlocksPrev++;
                        break;

                    case (IBB.SkirtBoneCurve):
                        skirtBoneCurvePrev = (Curve)obj.Geometry;
                        break;

                    case (IBB.SkirtCupCurve):
                        skirtCupCurvePrev = (Curve)obj.Geometry;
                        break;

                    case (IBB.PlateContourTop):
                        plateTopPrev = (Curve)obj.Geometry;
                        break;

                    case (IBB.PlateContourBottom):
                        plateBottomPrev = (Curve)obj.Geometry;
                        break;
                    case (IBB.DefectPelvis):
                        originalPelvisPrev = (Mesh)obj.Geometry;
                        break;
                    case (IBB.BoneGraft):
                        boneGraftPrev = (Mesh)obj.Geometry;
                        break;
                    case (IBB.ROIContour):
                        roiPrev = (Curve)obj.Geometry;
                        break;
                }
            }

            //Get previous screw brands
            var prevScrewBrand = ImplantDirector.ExtractScrewBrand(screwsPrev);

            //////////////
            // Cup changes
            //////////////

            if (prevCup != null)
            {

                // InfSup
                var cupInfSupPrev = Math.Round(director.SignInfSupPcs *
                                               MathUtilities.GetOffset(ax.Normal, ax.Origin, prevCup.centerOfRotation));
                var cupInfSupCurr = Math.Round(director.SignInfSupPcs *
                                               MathUtilities.GetOffset(ax.Normal, ax.Origin, cup.centerOfRotation));
                AddDiffToDictionary(ref information, ref changed, cupInfSupCurr, cupInfSupPrev, "CUP_POSITION_INFSUP",
                    0);
                // LatMed
                var cupLatMedPrev = Math.Round(director.SignMedLatPcs *
                                               MathUtilities.GetOffset(sa.Normal, ax.Origin, prevCup.centerOfRotation));
                var cupLatMedCurr = Math.Round(director.SignMedLatPcs *
                                               MathUtilities.GetOffset(sa.Normal, ax.Origin, cup.centerOfRotation));
                AddDiffToDictionary(ref information, ref changed, cupLatMedCurr, cupLatMedPrev, "CUP_POSITION_LATMED",
                    0);
                // AntPos
                var cupPosAntPrev = Math.Round(director.SignAntPosPcs *
                                               MathUtilities.GetOffset(co.Normal, ax.Origin, prevCup.centerOfRotation));
                var cupPosAntCurr = Math.Round(director.SignAntPosPcs *
                                               MathUtilities.GetOffset(co.Normal, ax.Origin, cup.centerOfRotation));
                AddDiffToDictionary(ref information, ref changed, cupPosAntCurr, cupPosAntPrev, "CUP_POSITION_POSANT",
                    0);
                // Anteversion
                var cupOrientationAvPrev = prevCup.anteversion;
                var cupOrientationAvCurr = cup.anteversion;
                AddDiffToDictionary(ref information, ref changed, cupOrientationAvCurr, cupOrientationAvPrev,
                    "CUP_ORIENTATION_AV", 0);
                // Inclination
                var cupOrientationInclPrev = prevCup.inclination;
                var cupOrientationInclCurr = cup.inclination;
                AddDiffToDictionary(ref information, ref changed, cupOrientationInclCurr, cupOrientationInclPrev,
                    "CUP_ORIENTATION_INCL", 0);
                // Diameter
                var cupInnerDiameterPrev = prevCup.innerCupDiameter;
                var cupInnerDiameterCurr = cup.innerCupDiameter;
                AddDiffToDictionary(ref information, ref changed, cupInnerDiameterCurr, cupInnerDiameterPrev,
                    "CUP_INNERDIAMETER", 0);
                // Cup type
                var cupTypePrev = string.Format("{0:F0}+{1:F0} {2}", prevCup.cupType.CupThickness,
                    prevCup.cupType.PorousThickness, prevCup.cupType.CupDesign);
                var cupTypeCurr = string.Format("{0:F0}+{1:F0} {2}", cup.cupType.CupThickness,
                    cup.cupType.PorousThickness, cup.cupType.CupDesign);
                AddDiffToDictionary(ref information, ref changed, cupTypeCurr, cupTypePrev, "CUP_TYPE");
                // Aperture
                var cupAperturePrev = prevCup.apertureAngle;
                var cupApertureCurr = cup.apertureAngle;
                AddDiffToDictionary(ref information, ref changed, cupApertureCurr, cupAperturePrev, "CUP_APERTURE", 0);
            }
            else
            {
                throw new Core.PluginHelper.IDSException("prevCup is null! This should not happen!");
            }
            //////////////////
            // Bone Grafts
            //////////////////

            var objManager = new AmaceObjectManager(director);

            // Final scaffold
            Mesh boneGraft = null;
            if (objManager.HasBuildingBlock(IBB.BoneGraft))
            {
                boneGraft = (Mesh)objManager.GetBuildingBlock(IBB.BoneGraft).Geometry;
            }
            AddDiffToDictionary(ref information, ref changed, boneGraft, boneGraftPrev, "GRAFTS_IMPORTED", -1);
            // Graft Volume
            AddDiffToDictionary(ref information, ref changed, boneGraft, boneGraftPrev, "GRAFTS_VOLUME", 1);
            // Original Pelvis Volume
            var originalPelvis = (Mesh)objManager.GetBuildingBlock(IBB.DefectPelvis).Geometry;
            AddDiffToDictionary(ref information, ref changed, originalPelvis, originalPelvisPrev, "ORIGINAL_PELVIS_VOLUME", 1);

            //////////////////
            // Reaming changes
            //////////////////
            // Cup RBV
            var reamCupCurr = objManager.GetAllBuildingBlocks(IBB.CupRbv).ToList();
            AddDiffToDictionary(ref information, ref changed, reamCupCurr, reamCupPrev, "REAMING_CUPRBV", 1);
            // Additional RBV
            var reamAddCurr = objManager.GetAllBuildingBlocks(IBB.AdditionalRbv).ToList();
            AddDiffToDictionary(ref information, ref changed, reamAddCurr, reamAddPrev, "REAMING_ADDITIONALRBV", 1);
            // Total RBV
            var reamTotalCurr = objManager.GetAllBuildingBlocks(IBB.TotalRbv).ToList();
            AddDiffToDictionary(ref information, ref changed, reamTotalCurr, reamTotalPrev, "REAMING_TOTALRBV", 1);
            // Number of reaming blocks
            var reamBlocksCurr = objManager.GetAllBuildingBlocks(IBB.ExtraReamingEntity).Count();
            AddDiffToDictionary(ref information, ref changed, reamBlocksCurr, reamBlocksPrev, "REAMING_NUMBERREAMINGBLOCKS");

            ////////////////
            // Skirt changes
            ////////////////
            // Cup Skirt Curve
            var skirtCupCurveCurr = (Curve)objManager.GetBuildingBlock(IBB.SkirtCupCurve).Geometry;
            AddDiffToDictionary(ref information, ref changed, skirtCupCurveCurr, skirtCupCurvePrev, "SKIRT_CUPCURVE");
            // Bone Skirt Curve
            var skirtBoneCurveCurr = (Curve)objManager.GetBuildingBlock(IBB.SkirtBoneCurve).Geometry;
            AddDiffToDictionary(ref information, ref changed, skirtBoneCurveCurr, skirtBoneCurvePrev, "SKIRT_BONECURVE");

            ///////////////////
            // Scaffold changes
            ///////////////////
            // Undercut Av
            var scaffoldUndercutAvCurr = director.InsertionAnteversionDegrees;
            AddDiffToDictionary(ref information, ref changed, scaffoldUndercutAvCurr, scaffoldUndercutAvPrev, "SCAFFOLD_UNDERCUT_AV", 0);
            // Undercut Incl
            var scaffoldUndercutInclCurr = director.InsertionInclinationDegrees;
            AddDiffToDictionary(ref information, ref changed, scaffoldUndercutInclCurr, scaffoldUndercutInclPrev, "SCAFFOLD_UNDERCUT_INCL", 0);
            // Temp scaffold
            var scaffoldTempCurr = (Mesh)objManager.GetBuildingBlock(IBB.ScaffoldVolume).Geometry;
            AddDiffToDictionary(ref information, ref changed, scaffoldTempCurr, scaffoldTempPrev, "SCAFFOLD_TEMP", -1);
            // Final scaffold
            Mesh scaffoldFinalCurr = null;
            if (objManager.HasBuildingBlock(IBB.ScaffoldFinalized))
            {
                scaffoldFinalCurr = (Mesh)objManager.GetBuildingBlock(IBB.ScaffoldFinalized).Geometry;
            }
            AddDiffToDictionary(ref information, ref changed, scaffoldFinalCurr, scaffoldFinalPrev, "SCAFFOLD_FINALIZED", -1);

            ////////////////
            // Screw changes
            ////////////////
            var screwManager = new ScrewManager(director.Document);
            var screwsCurr = screwManager.GetAllScrews().ToList();
            // Amount
            var screwsAmountCurr = screwsCurr.Count();
            AddDiffToDictionary(ref information, ref changed, screwsAmountCurr, screwsAmountPrev, "SCREWS_AMOUNT");
            // Individual screws
            AddDiffToDictionary(ref information, ref changed, screwsCurr, screwsPrev, "SCREWS_INDIVIDUAL");
            // Plate clearance
            var drillBitDiameterCurr = ScrewAideManager.ConvertToDrillBitDiameter(director.DrillBitRadius);
            AddDiffToDictionary(ref information, ref changed, drillBitDiameterCurr, drillBitDiameterPrev, "SCREWS_DRILLBIT", 1);
            //Screw Brand
            var currentScrewBrand = director.GetCurrentScrewBrand();
            AddDiffToDictionary(ref information, ref changed, currentScrewBrand, prevScrewBrand, "SCREWS_BRAND");

            ////////////////
            // Plate changes
            ////////////////
            // Plate Bottom Curve
            Curve plateBottomCurr = null;
            if (objManager.HasBuildingBlock(IBB.PlateContourBottom))
                plateBottomCurr = objManager.GetBuildingBlock(IBB.PlateContourBottom).Geometry as Curve;
            AddDiffToDictionary(ref information, ref changed, plateBottomCurr, plateBottomPrev, "PLATE_BOTTOMCONTOUR");
            // Plate Top Curve
            Curve plateTopCurr = null;
            if (objManager.HasBuildingBlock(IBB.PlateContourTop))
                plateTopCurr = objManager.GetBuildingBlock(IBB.PlateContourTop).Geometry as Curve;
            AddDiffToDictionary(ref information, ref changed, plateTopCurr, plateTopPrev, "PLATE_TOPCONTOUR");
            // Plate thickness
            double plateThicknessCurr = director.PlateThickness;
            AddDiffToDictionary(ref information, ref changed, plateThicknessCurr, plateThicknessPrev, "PLATE_THICKNESS", 1);
            // Plate clearance
            double plateClearanceCurr = director.PlateClearance;
            AddDiffToDictionary(ref information, ref changed, plateClearanceCurr, plateClearancePrev, "PLATE_CLEARANCE", 1);
            // ROI
            Curve roiCurr = null;
            if (objManager.HasBuildingBlock(IBB.ROIContour))
            {
                roiCurr = objManager.GetBuildingBlock(IBB.ROIContour).Geometry as Curve;
            }
            AddDiffToDictionary(ref information, ref changed, roiCurr, roiPrev, "PLATE_ROI");

            // Dispose previous file
            prevFile.Dispose();

            return information;
        }

        private static void AddDiffToDictionary(ref Dictionary<string, string> dict, ref Dictionary<string, bool> changed, List<RhinoObject> curr, List<RhinoObject> prev, string key, int volumeDecimals)
        {
            var currMesh = new Mesh();
            foreach (var rhobj in curr)
            {
                currMesh.Append(rhobj.Geometry as Mesh);
            }

            var prevMesh = new Mesh();
            foreach (var rhobj in prev)
            {
                prevMesh.Append(rhobj.Geometry as Mesh);
            }

            AddDiffToDictionary(ref dict, ref changed, currMesh, prevMesh, key, volumeDecimals);
        }

        private static void AddDiffToDictionary(ref Dictionary<string, string> dict, ref Dictionary<string, bool> changed, Mesh curr, List<RhinoObject> prev, string key, int volumeDecimals)
        {
            var prevMesh = new Mesh();
            foreach (var rhobj in prev)
            {
                prevMesh.Append(rhobj.Geometry as Mesh);
            }

            AddDiffToDictionary(ref dict, ref changed, curr, prevMesh, key, volumeDecimals);
        }

        private static void AddDiffToDictionary(ref Dictionary<string, string> dict, ref Dictionary<string, bool> changed, List<RhinoObject> curr, Mesh prev, string key, int volumeDecimals)
        {
            var currMesh = new Mesh();
            foreach (var rhobj in curr)
            {
                currMesh.Append(rhobj.Geometry as Mesh);
            }
            
            AddDiffToDictionary(ref dict, ref changed, currMesh, prev, key, volumeDecimals);
        }

        private static void AddDiffToDictionary(ref Dictionary<string, string> dict, ref Dictionary<string, bool> changed, Mesh curr, Mesh prev, string key, int volumeDecimals)
        {
            // Volume calculations (if volumeDecimals > -1)
            var prevVol = prev != null && prev.Vertices.Count > 0 ? Volume.MeshVolume(prev, true) : 0;
            var currVol = curr != null && curr.Vertices.Count > 0 ? Volume.MeshVolume(curr, true) : 0;
            var diffVol = currVol - prevVol;

            // Mesh to mesh distances
            var change = false;
            var changeDescription = string.Empty;
            var zeros = 0.0.ToString("F" + volumeDecimals.ToString("D"), new NumberFormatInfo { NumberDecimalSeparator = "." });

            if (prev != null && prev.Vertices.Count > 0 && curr != null && curr.Vertices.Count > 0)
            {
                // Mesh to mesh distance using mesh with largest amount of vertices as source
                change = DetermineIfMeshChanged(curr, prev);

                if (change)
                {
                    changeDescription = volumeDecimals > -1 ? string.Format("Changed, {0:+" + zeros + ";-" + zeros + ";0}", diffVol) : "Changed";
                }
                else
                {
                    changeDescription = volumeDecimals > -1 ? string.Format("No Change, {0:+" + zeros + ";-" + zeros + ";0}", diffVol) : "Changed";
                }
            }
            else if (prev != null && prev.Vertices.Count == 0 && curr != null && curr.Vertices.Count == 0)
            {
                changeDescription = "No change, 0";
            }
            else if (prev != null && prev.Vertices.Count > 0 && (curr == null || curr.Vertices.Count == 0))
            {
                change = true;
                changeDescription = volumeDecimals > -1 ? string.Format("Removed, {0:+" + zeros + ";-" + zeros + ";0}", diffVol) : "Removed";
            }
            else if ((prev == null || prev.Vertices.Count == 0) && curr != null && curr.Vertices.Count > 0)
            {
                change = true;
                changeDescription = volumeDecimals > -1 ? string.Format("New, {0:+" + zeros + ";-" + zeros + ";0}", diffVol) : "New";
            }

            var currText = (curr != null && curr.Vertices.Count > 0) ? "&#x2713;" : "&#x2717;"; // only used if volume is not reported
            var prevText = (prev != null && prev.Vertices.Count > 0) ? "&#x2713;" : "&#x2717;"; // only used if volume is not reported

            dict.Add($"{key.ToUpper()}_PREV", volumeDecimals > -1 ? string.Format("{0:F" + volumeDecimals.ToString("D") + "}", prevVol) : prevText);
            dict.Add($"{key.ToUpper()}_CURR", volumeDecimals > -1 ? string.Format("{0:F" + volumeDecimals.ToString("D") + "}", currVol) : currText);
            dict.Add($"{key.ToUpper()}_DIFF", changeDescription);
            changed.Add(key.ToUpper(), change);
        }

        private static bool DetermineIfMeshChanged(Mesh curr, Mesh prev)
        {
            const double threshold = 0.01;

            List<double> d;
            if (prev.Vertices.Count > curr.Vertices.Count)
            {
                MeshAnalysis.MeshToMeshAnalysis(prev, curr, out d);
            }
            else
            {
                MeshAnalysis.MeshToMeshAnalysis(curr, prev, out d);
            }
            var change = d.Max() > threshold;
            return change;
        }

        private static void AddDiffToDictionary(ref Dictionary<string, string> dict, ref Dictionary<string, bool> changed, List<Screw> curr, List<Screw> prev, string key)
        {
            // Threshold for similarity check
            const double threshold = 0.01;

            // Sort by index
            curr.Sort();
            prev.Sort();

            foreach (var newScrew in curr)
            {
                // Init to no match found
                var currText = "";
                var prevText = "";
                var diffText = "";
                var matchFound = false;

                // Search for a match in the old screw
                foreach (var oldScrew in prev)
                {
                    var headDist = (oldScrew.HeadPoint - newScrew.HeadPoint).Length;
                    var tipDist = (oldScrew.TipPoint - newScrew.TipPoint).Length;

                    // Check if the distance between heads and tips is small enough for the screws to
                    // be a match
                    if (!(headDist < threshold) || !(tipDist < threshold))
                    {
                        continue;
                    }

                    matchFound = true;

                    // Check if the screws have the same index (i.e. full match)
                    if (oldScrew.Index != newScrew.Index)
                    {
                        matchFound = false;
                        diffText = "Possible index change";
                        currText = newScrew.Index.ToString("D");
                        prevText = oldScrew.Index.ToString("D");
                    }

                    break;
                }

                // Set the diff text in case no match was found
                if (!matchFound && diffText == string.Empty)
                {
                    diffText = "Changed or New";
                }

                dict.Add($"{key.ToUpper()}_{newScrew.Index:D}_PREV", prevText);
                dict.Add($"{key.ToUpper()}_{newScrew.Index:D}_CURR", currText);
                dict.Add($"{key.ToUpper()}_{newScrew.Index:D}_DIFF", diffText);
                var subKey = $"{key}_{newScrew.Index:D}";
                changed.Add(subKey.ToUpper(), !matchFound);
            }
        }

        private static void AddDiffToDictionary(ref Dictionary<string, string> dict, ref Dictionary<string, bool> changed, Curve curr, Curve prev, string key)
        {
            var equal = CurveUtilities.Equal(curr, prev);
            var currText = curr == null ? "&#x2717;" : "&#x2713;";
            var prevText = prev == null ? "&#x2717;" : "&#x2713;";
            string diffText; // default, no curves present
            if (curr != null && prev == null)
            {
                diffText = "New";
            }
            else if (curr == null && prev != null)
            {
                diffText = "Removed";
            }
            else if (!equal)
            {
                diffText = "Changed";
            }
            else
            {
                diffText = "No change";
            }

            dict.Add($"{key.ToUpper()}_PREV", prevText);
            dict.Add($"{key.ToUpper()}_CURR", currText);
            dict.Add($"{key.ToUpper()}_DIFF", diffText);
            changed.Add(key.ToUpper(), !equal);
        }

        private static void AddDiffToDictionary(ref Dictionary<string, string> dict, ref Dictionary<string, bool> changed, double curr, double prev, string key, int decimals)
        {
            var threshold = decimals < 1 ? 1 : Math.Pow(10, -decimals);
            var zeros = string.Format("{0:F" + decimals.ToString("D") + "}", 0.0);

            var diff = curr - prev;
            dict.Add($"{key.ToUpper()}_PREV", Math.Abs(prev - double.MinValue) > 0.0001 ? string.Format("{0:F" + decimals.ToString("D") + "}", prev) : "?");
            dict.Add($"{key.ToUpper()}_CURR", string.Format("{0:F" + decimals.ToString("D") + "}", curr));
            dict.Add($"{key.ToUpper()}_DIFF", Math.Abs(prev - double.MinValue) > 0.0001 ? string.Format("{0:+" + zeros + ";-" + zeros + ";0}", diff) : "?");
            changed.Add(key.ToUpper(), Math.Abs(curr - prev) >= threshold);
        }

        private static void AddDiffToDictionary(ref Dictionary<string, string> dict, ref Dictionary<string, bool> changed, int curr, int prev, string key)
        {
            var diff = curr - prev;
            dict.Add($"{key.ToUpper()}_PREV", prev.ToString("D"));
            dict.Add($"{key.ToUpper()}_CURR", curr.ToString("D"));
            dict.Add($"{key.ToUpper()}_DIFF", diff.ToString("+#;-#;0"));
            changed.Add(key.ToUpper(), diff != 0);
        }

        private static void AddDiffToDictionary(ref Dictionary<string, string> dict, ref Dictionary<string, bool> changed, string curr, string prev, string key)
        {
            dict.Add($"{key.ToUpper()}_PREV", prev);
            dict.Add($"{key.ToUpper()}_CURR", curr);
            dict.Add($"{key.ToUpper()}_DIFF", curr != prev ? "Changed" : "No change");
            changed.Add(key.ToUpper(), curr != prev);
        }
    }
}