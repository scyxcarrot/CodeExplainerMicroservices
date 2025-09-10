using IDS.Amace;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Quality;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IDS.Operations.Export
{
    public class ParameterExporter
    {
        public static bool ExportParameterFile(ImplantDirector director, string filename)
        {
            // Get entries
            List<string> entries = GetParameterFileEntries(director);

            // Write to file
            File.WriteAllLines(filename, entries);

            return true;
        }

        public static List<string> GetParameterFileEntries(ImplantDirector director)
        {
            // String list to store all lines in
            var entries = new List<string>();

            // Necessary variables
            var cup = director != null ? director.cup : null;
            var inspector = director?.Inspector;
            // Output variables Meta
            var preopID = inspector != null ? director.Inspector.PreOperativeId : "Unknown";
            var version = director?.version ?? 0;
            var draft = director?.draft ?? 0;
            // Preop
            var ax = inspector?.AxialPlane ?? Plane.Unset; // axial
            var PCS = inspector?.AxialPlane ?? Plane.Unset; // PCS
            var co = inspector?.CoronalPlane ?? Plane.Unset; // coronal
            var sa = inspector?.SagittalPlane ?? Plane.Unset; // sagittal
            // Cup
            if (director != null)
            {
                var cupPcsInfsup = ax != Plane.Unset ? (director.SignInfSupPcs * MathUtilities.GetOffset(ax.Normal, ax.Origin, cup.centerOfRotation)) : double.MinValue;
                var cupPcsLatMed = sa != Plane.Unset ? (director.SignMedLatPcs * MathUtilities.GetOffset(sa.Normal, ax.Origin, cup.centerOfRotation)) : double.MinValue;
                var cupPcsAntPos = co != Plane.Unset ? (director.SignAntPosPcs * MathUtilities.GetOffset(co.Normal, ax.Origin, cup.centerOfRotation)) : double.MinValue;
                var cupCOR = cup != null ? cup.centerOfRotation : Point3d.Unset;
                var defCOR = director.Inspector.DefectFemurCenterOfRotation != null ? director.Inspector.DefectFemurCenterOfRotation : Point3d.Unset;
                var clatCOR = director.CenterOfRotationContralateralFemur != null ? director.CenterOfRotationContralateralFemur : Point3d.Unset;
                var cupOrientation = cup != null ? cup.orientation : Vector3d.Unset;
                // Cup position image values
                var latCupPCS = ((cupCOR - PCS.Origin) * PCS.YAxis);
                var infCupClat = MathUtilities.GetOffset(PCS.ZAxis, clatCOR, cupCOR);
                var latClatPCS = MathUtilities.GetOffset(PCS.YAxis, PCS.Origin, clatCOR);
                var infCupDefect = MathUtilities.GetOffset(PCS.ZAxis, defCOR, cupCOR);
                var latCupDefect = MathUtilities.GetOffset(PCS.YAxis, defCOR, cupCOR);
                var txtCupClatInfSup = string.Format(CultureInfo.InvariantCulture, "{0:+#;-#;0}mm", director.SignInfSupClat * infCupClat);
                var txtClatPCSLatMed = string.Format(CultureInfo.InvariantCulture, "{0:F0}mm", -director.SignMedLatPcs * latClatPCS);
                var txtCupPCSLatMed = string.Format(CultureInfo.InvariantCulture, "{0:F0}mm ({1:+#;-#;0}mm)", director.SignMedLatPcs * latCupPCS, director.SignMedLatPcs * latCupPCS + director.SignMedLatPcs * latClatPCS);
                var txtCupDefLatMed = string.Format(CultureInfo.InvariantCulture, "{0:+#;-#;0}mm", director.SignMedLatDef * latCupDefect);
                var txtCupDefInfSup = string.Format(CultureInfo.InvariantCulture, "{0:+#;-#;0}mm", director.SignInfSupDef * infCupDefect);

                // Various
                var insertionDirection = director != null ? director.InsertionDirection : Vector3d.Unset;

                // Meta
                entries.Add(string.Format(CultureInfo.InvariantCulture, "IDS Version,{0}", VersionControl.GetIDSVersion(director.PluginInfoModel)));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Timestamp,{0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "PreOp ID,{0}", preopID));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Version,{0:D}", version));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Draft,{0:D}", draft));

                // PCS
                entries.Add(" ");
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Axial Plane Center,{0:F4},{1:F4},{2:F4}", ax.Origin.X, ax.Origin.Y, ax.Origin.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Axial Plane Normal,{0:F4},{1:F4},{2:F4}", ax.Normal.X, ax.Normal.Y, ax.Normal.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Axial Plane X,{0:F4},{1:F4},{2:F4}", ax.XAxis.X, ax.XAxis.Y, ax.XAxis.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Axial Plane Y,{0:F4},{1:F4},{2:F4}", ax.YAxis.X, ax.YAxis.Y, ax.YAxis.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Coronal Plane Center,{0:F4},{1:F4},{2:F4}", co.Origin.X, co.Origin.Y, co.Origin.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Coronal Plane Normal,{0:F4},{1:F4},{2:F4}", co.Normal.X, co.Normal.Y, co.Normal.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Coronal Plane X,{0:F4},{1:F4},{2:F4}", co.XAxis.X, co.XAxis.Y, co.XAxis.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Coronal Plane Y,{0:F4},{1:F4},{2:F4}", co.YAxis.X, co.YAxis.Y, co.YAxis.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Sagittal Plane Center,{0:F4},{1:F4},{2:F4}", sa.Origin.X, sa.Origin.Y, sa.Origin.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Sagittal Plane Normal,{0:F4},{1:F4},{2:F4}", sa.Normal.X, sa.Normal.Y, sa.Normal.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Sagittal Plane X,{0:F4},{1:F4},{2:F4}", sa.XAxis.X, sa.XAxis.Y, sa.XAxis.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Sagittal Plane Y,{0:F4},{1:F4},{2:F4}", sa.YAxis.X, sa.YAxis.Y, sa.YAxis.Z));

                // Cup
                entries.Add(" ");
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Center PCS,{0:F4},{1:F4},{2:F4}", cupPcsLatMed, cupPcsInfsup, cupPcsAntPos));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Center World,{0:F4},{1:F4},{2:F4}", cupCOR.X, cupCOR.Y, cupCOR.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Vector,{0:F4},{1:F4},{2:F4}", cupOrientation.X, cupOrientation.Y, cupOrientation.Z));

                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Anteversion,{0:F1}", cup != null ? cup.anteversion : double.MinValue));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Aperture Angle,{0:F1}", cup != null ? cup.apertureAngle : double.MinValue));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Diameter,{0:F0}", cup != null ? cup.innerCupDiameter : double.MinValue));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Reamer Diameter,{0:F0}", cup != null ? cup.outerReamingDiameter : double.MinValue));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Inclination,{0:F1}", cup != null ? cup.inclination : double.MinValue));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Porous Thickness,{0:F0}", cup != null ? cup.cupType.PorousThickness : double.MinValue));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Thickness,{0:F0}", cup != null ? cup.cupType.CupThickness : double.MinValue));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Type,{0:F0}+{1:F0}", cup != null ? cup.cupType.CupThickness : double.MinValue, cup != null ? cup.cupType.PorousThickness : double.MinValue));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Max Liner Diameter,{0:F0}", cup != null ? cup.linerDiameterMax : double.MinValue));

                // Cup position figure values
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Clat InfSup,{0}", txtCupClatInfSup));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Def LatMed,{0}", txtCupDefLatMed));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup Def InfSup,{0}", txtCupDefInfSup));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Cup PCS LatMed,{0}", txtCupPCSLatMed));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Clat PCS LatMed,{0}", txtClatPCSLatMed));

                // Various design parameters
                entries.Add(" ");
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Undercut Direction,{0:F4},{1:F4},{2:F4}", insertionDirection.X, insertionDirection.Y, insertionDirection.Z));
            }

            entries.Add(string.Format(CultureInfo.InvariantCulture, "Undercut Anteversion,{0:F1}", director != null ? director.InsertionAnteversionDegrees : double.MinValue));
            entries.Add(string.Format(CultureInfo.InvariantCulture, "Undercut Inclination,{0:F1}", director != null ? director.InsertionInclinationDegrees : double.MinValue));
            entries.Add(" ");
            entries.Add(string.Format(CultureInfo.InvariantCulture, "RBV Cup,{0:F1}", Amace.Utilities.Volume.RBVCupVolumeCC(director)));
            var rbvCupGraftVolumeCc = Amace.Utilities.Volume.RbvCupGraftVolumeCc(director);
            entries.Add(string.Format(CultureInfo.InvariantCulture, "RBV Cup Graft,{0:F1}", rbvCupGraftVolumeCc));
            entries.Add(string.Format(CultureInfo.InvariantCulture, "RBV Additional,{0:F1}", Amace.Utilities.Volume.RBVAdditionalVolumeCC(director)));
            var rbvAdditionalGraftVolumeCc = Amace.Utilities.Volume.RbvAdditionalGraftVolumeCc(director);
            entries.Add(string.Format(CultureInfo.InvariantCulture, "RBV Additional Graft,{0:F1}", rbvAdditionalGraftVolumeCc));
            entries.Add(string.Format(CultureInfo.InvariantCulture, "RBV Total,{0:F1}", Amace.Utilities.Volume.RBVTotalVolumeCC(director)));
            var rbvTotalGraftVolumeCc = rbvCupGraftVolumeCc + rbvAdditionalGraftVolumeCc;
            entries.Add(string.Format(CultureInfo.InvariantCulture, "RBV Total Graft,{0:F1}", rbvTotalGraftVolumeCc));
            entries.Add(" ");
            entries.Add(string.Format(CultureInfo.InvariantCulture, "Scaffold Volume,{0:F1}", Amace.Utilities.Volume.FinalizedScaffoldVolumeCC(director)));
            entries.Add(" ");
            entries.Add(string.Format(CultureInfo.InvariantCulture, "Plate Thickness,{0:F1}", director != null ? director.PlateThickness : double.MinValue));
            entries.Add(string.Format(CultureInfo.InvariantCulture, "Plate Clearance,{0:F1}", director != null ? director.PlateClearance : double.MinValue));
            entries.Add(" ");
            entries.Add(string.Format(CultureInfo.InvariantCulture, "Drill Bit Diameter,{0:F1}", director != null ? ScrewAideManager.ConvertToDrillBitDiameter(director.DrillBitRadius) : double.MinValue));

            // Screw parameters
            var screwManager = new ScrewManager(director.Document);
            var screws = director != null ? screwManager.GetAllScrews().ToList() : new List<Screw>();
            screws.Sort();
            // Calculate screw overlaps
            var analysis = new AmaceScrewAnalysis();
            var screwOverlaps = analysis.PerformScrewIntersectionCheck(screws,0);
            // Add intersections to entries (no doubles e.g. "1 intersects 2" instead of "1
            // intersects 2; 2 intersects 1")
            if (screwOverlaps.Count > 0)
            {
                entries.Add(" ");
                // Decouple sublists
                var decoupled = new List<List<int>>();
                foreach (var intersections in screwOverlaps)
                {
                    foreach (int target in intersections.Value)
                    {
                        decoupled.Add(new List<int>() { intersections.Key, target });
                    }
                }

                // Filter to remove doubles
                var filtered = new List<List<int>>();
                foreach (var pair in decoupled)
                {
                    var infiltered = false;
                    foreach (var testpair in filtered)
                    {
                        if (pair[0] == testpair[0] && pair[1] == testpair[1] ||
                            pair[0] == testpair[1] && pair[1] == testpair[0])
                        {
                            infiltered = true;
                        }
                    }
                    if (!infiltered)
                    {
                        filtered.Add(pair);
                    }
                }
                var screwOverlapsFiltered = new Dictionary<int, List<int>>();
                foreach (var pair in filtered)
                {
                    if (screwOverlapsFiltered.ContainsKey(pair[0]))
                    {
                        screwOverlapsFiltered[pair[0]].Add(pair[1]);
                    }
                    else
                    {
                        screwOverlapsFiltered.Add(pair[0], new List<int>() { pair[1] });
                    }
                }

                foreach (var screw in screws)
                {
                    if (screwOverlapsFiltered.ContainsKey(screw.Index))
                    {
                        entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Intersects,{1}", screw.Index, string.Join("/", screwOverlapsFiltered[screw.Index])));
                    }
                }
            }

            // Write characteristics per screw
            foreach (var screw in screws)
            {
                entries.Add(" ");
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Head,{1:F8},{2:F8},{3:F8}", screw.Index, screw.HeadPoint.X, screw.HeadPoint.Y, screw.HeadPoint.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Tip,{1:F8},{2:F8},{3:F8}", screw.Index, screw.TipPoint.X, screw.TipPoint.Y, screw.TipPoint.Z));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Alignment,{1}", screw.Index, screw.screwAlignment.ToString()));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Augments,{1}", screw.Index, screw.AugmentsText));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Diameter,{1:F4}", screw.Index, screw.Diameter));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Fixation,{1}", screw.Index, screw.Fixation));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} In Bone,{1:F4}", screw.Index, screw.GetDistanceInBone()));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Length,{1:F4}", screw.Index, screw.TotalLength));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Offset,{1:F4}", screw.Index, screw.AxialOffset));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Position,{1}", screw.Index, screw.positioning.ToString()));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Type,{1}", screw.Index, screw.screwBrandType.ToString()));
                entries.Add(string.Format(CultureInfo.InvariantCulture, "Screw {0} Until Bone,{1:F4}", screw.Index, screw.GetDistanceUntilBone()));
            }

            return entries;
        }
    }
}