using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Query;
using System;
using System.IO;
using System.Linq;

namespace IDS.Glenius.Quality
{
    public class QCDesignParameterFile
    {

        private GleniusImplantDirector director;
        private DesignParameters designParameter;

        public QCDesignParameterFile(GleniusImplantDirector director)
        {
            this.director = director;
            designParameter = new DesignParameters(director);
        }

        public bool GenerateDesignParameterFile(string outputDir, string fileName)
        {
            var fileFullName = fileName + ".txt";
            var fullOutputPath = Path.Combine(outputDir, fileFullName);

            try
            {
                using (var sw = new StreamWriter(fullOutputPath, false))
                {
                    WriteTraceabilityInformation(sw);
                    sw.WriteLine("");
                    sw.WriteLine($"Defect Side,{designParameter.GetDefectSide()}");
                    sw.WriteLine("");
                    WriteMCSInformation(sw, 4);
                    sw.WriteLine("");
                    WriteHeadParameters(sw);
                    sw.WriteLine("");
                    WriteRBVSpecifications(sw);
                    sw.WriteLine("");
                    WriteScrewSpecifications(sw);
                }
            }
            catch (System.Exception ex)
            {
                return false;
            }

            return true;
        }

        private void WriteTraceabilityInformation(StreamWriter sw)
        {
            sw.WriteLine($"IDS Version,{designParameter.GetIDSVersion()}");
            sw.WriteLine($"Timestamp,{designParameter.GetTimeStamp()}");
            sw.WriteLine($"Case ID,{designParameter.GetCaseID()}");
            sw.WriteLine($"Version,{designParameter.GetVersion()}");
            sw.WriteLine($"Draft,{designParameter.GetDraft()}");
        }

        private void WriteMCSInformation(StreamWriter sw, int decimalPlaces)
        {
            sw.WriteLine($"Axial Plane Center,{StringUtilities.PointStringify(designParameter.GetMCSAxialPlaneCenter(), decimalPlaces)}");
            sw.WriteLine($"Coronal Plane Center,{StringUtilities.PointStringify(designParameter.GetMCSCoronalPlaneCenter(), decimalPlaces)}");
            sw.WriteLine($"Sagittal Plane Center,{StringUtilities.PointStringify(designParameter.GetMCSSagittalPlaneCenter(), decimalPlaces)}");
            sw.WriteLine($"Axial Plane Normal,{StringUtilities.VectorStringify(designParameter.GetMCSAxialPlaneNormal(), decimalPlaces)}");
            sw.WriteLine($"Coronal Plane Normal,{StringUtilities.VectorStringify(designParameter.GetMCSAPAxis(), decimalPlaces)}"); //Use AP Axis, coronal normal does not reflect if the case is right of left as per SRS.
            sw.WriteLine($"Sagittal Plane Normal,{StringUtilities.VectorStringify(designParameter.GetMCSSagittalPlaneNormal(), decimalPlaces)}");
        }

        private void WriteHeadParameters(StreamWriter sw)
        {
            sw.WriteLine($"Head Diameter,{StringUtilities.DoubleStringify(designParameter.GetHeadDiameter())}");
            sw.WriteLine($"Head Liner Diameter,{StringUtilities.DoubleStringify(designParameter.GetHeadLinerDiameter())}");
            sw.WriteLine($"Head Version,{StringUtilities.DoubleStringify(designParameter.GetHeadVersion())}");
            sw.WriteLine($"Head Inclination,{StringUtilities.DoubleStringify(designParameter.GetHeadInclination())}");
            sw.WriteLine($"Head Reconstruction AntPos Distance,{StringUtilities.DoubleStringify(Math.Abs(designParameter.GetHeadAnteroPosteriorDistance()), 0)}");
            sw.WriteLine($"Head Reconstruction AntPos Description,{designParameter.GetHeadAnteroPosteriorDescription()}");
            sw.WriteLine($"Head Reconstruction LatMed Distance,{StringUtilities.DoubleStringify(Math.Abs(designParameter.GetLateroMedialDistance()), 0)}");
            sw.WriteLine($"Head Reconstruction LatMed Description,{designParameter.GetLateroMedialDescription()}");
            sw.WriteLine($"Head Reconstruction SupInf Distance,{StringUtilities.DoubleStringify(Math.Abs(designParameter.GetSuperiorInferiorDistance()), 0)}");
            sw.WriteLine($"Head Reconstruction SupInf Description,{designParameter.GetSuperiorInferiorDescription()}");
            sw.WriteLine($"Head Preop AntPos Distance,{designParameter.GetHeadPreopAnteroPosteriorDistanceInString()}");
            sw.WriteLine($"Head Preop AntPos Description,{designParameter.GetHeadPreopAnteroPosteriorDescription()}");
            sw.WriteLine($"Head Preop LatMed Distance,{designParameter.GetHeadPreopLateroMedialDistanceInString()}");
            sw.WriteLine($"Head Preop LatMed Description,{designParameter.GetHeadPreopLateroMedialDescription()}");
            sw.WriteLine($"Head Preop SupInf Distance,{designParameter.GetHeadPreopSuperiorInferiorDistanceInString()}");
            sw.WriteLine($"Head Preop SupInf Description,{designParameter.GetHeadPreopSuperiorInferiorDescription()}");
        }

        private void WriteRBVSpecifications(StreamWriter sw)
        {
            sw.WriteLine($"Head RBV,{StringUtilities.DoubleStringify(designParameter.GetRBVHeadVolumeCC())}");
            sw.WriteLine($"Implant RBV,{StringUtilities.DoubleStringify(designParameter.GetRBVImplantVolumeCC())}");
            sw.WriteLine($"Total RBV,{StringUtilities.DoubleStringify(designParameter.GetTotalRBVVolumeCC())}");
        }

        private void WriteScrewSpecifications(StreamWriter sw)
        {
            var objManager = new GleniusObjectManager(director);
            var screwsRhObj = objManager.GetAllBuildingBlocks(IBB.Screw);

            if (screwsRhObj != null && screwsRhObj.Any())
            {
                var screws = screwsRhObj.Select(x => x as Screw).ToList().OrderBy(x => x.Index);

                foreach (var screw in screws)
                {
                    var info = designParameter.GetScrewInfo(screw);
                    sw.WriteLine($"Screw {info.index} Screw Reference No,{info.referenceNumber}");
                    sw.WriteLine($"Screw {info.index} Diameter,{StringUtilities.DoubleStringify(info.diameter)}");
                    sw.WriteLine($"Screw {info.index} Type,{info.screwLockingType}");
                    sw.WriteLine($"Screw {info.index} Head,{StringUtilities.PointStringify(info.headPoint, 8)}");
                    sw.WriteLine($"Screw {info.index} Tip,{StringUtilities.PointStringify(info.tipPoint, 8)}");
                    sw.WriteLine($"Screw {info.index} Length,{StringUtilities.DoubleStringify(info.length)}");
                    sw.WriteLine($"Screw {info.index} In Bone,{StringUtilities.DoubleStringify(info.inBone)}");
                    sw.WriteLine($"Screw {info.index} Until Bone,{StringUtilities.DoubleStringify(info.untilBone)}");
                    sw.WriteLine($"Screw {info.index} Fixation,{info.fixation}");
                    sw.WriteLine($"Screw {info.index} Offset,{StringUtilities.DoubleStringify(info.offset)}");
                    sw.WriteLine("");
                }
            }
        }
    }
}
