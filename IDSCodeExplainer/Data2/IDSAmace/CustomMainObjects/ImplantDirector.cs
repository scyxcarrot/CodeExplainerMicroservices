using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.GUI;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Common;
using IDS.Core.Enumerators;
using IDS.Core.Fea;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using IDS.Core.SplashScreen;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using IDS.Operations.Export;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using View = IDS.Amace.Visualization.View;
using Visibility = IDS.Amace.Visualization.Visibility;

namespace IDS
{
    /// <summary>
    /// The ImplantDirector manages implant building blocks in the document
    /// and coordinates the design flow(transition between design phases).
    /// </summary>
    public class ImplantDirector : ImplantDirectorBase, IPlateConduitProperties
    {
#if (INTERNAL)
        public static bool IsDebugMode = false;
#endif

        /// <summary>
        /// The archive version
        /// </summary>
        private const int archiveVersion = 1;
        
        /// <summary>
        /// The insertion key
        /// </summary>
        private const string KeyInsertion = "insertion_direction";

        /// <summary>
        /// Plane Information for ROI to be drawn on to
        /// </summary>
        private const string KeyContourPlane = "contour_plane";

        /// <summary>
        /// The phase key
        /// </summary>
        private const string KeyPlateThickness = "plate_thickness";

        /// <summary>
        /// The phase key
        /// </summary>
        private const string KeyDrillBitRadius = "drill_bit_radius";
        
        /// <summary>
        /// The plate clearance
        /// </summary>
        public readonly double PlateClearance = 0.3;

        /// <summary>
        /// The insertion direction
        /// </summary>
        public Vector3d InsertionDirection = Vector3d.Unset;

        /// <summary>
        /// The contour plane for drawing ROI Curve
        /// </summary>
        public Plane ContourPlane = Plane.Unset;

        public double DrillBitRadius { get; set; } = DefaultDrillBitRadius;
        public static double DefaultDrillBitRadius = 1.7;

        private string _currentScrewBrand;

        public string CurrentScrewBrand
        {
            get
            {
                UpdateCurrentScrewBrandIfContainsScrews();
                return _currentScrewBrand;
            }
            set { _currentScrewBrand = value; }
        }

        /// <summary>
        /// The last FEA performed on the implant
        /// </summary>
        public Amace.Fea.AmaceFea AmaceFea { get; private set; }

        private const string KeyFeaLoadMagnitude = "fea_load_magnitude";
        private const string KeyFeaLoadVectorType = "fea_load_vector_type";
        private const string KeyFeaCameraTarget = "fea_camera_target";
        private const string KeyFeaCameraUp = "fea_camera_up";
        private const string KeyFeaCameraDirection = "fea_^camera_direction";
        private const string KeyFeaMaterial = "fea_material";
        private const string KeyFeaFrdStressTensors = "key_frd_stress_tensors";
        private const string KeyFeaLoadMesh = "fea_load_mesh";
        private const string KeyFeaBoundaryConditions = "fea_boundary_conditions";
        private const string KeyFeaFrdNodes = "fea_frd_nodes";
        private const string KeyFeaImplantRemeshed = "fea_implant_remeshed";
        private const string KeyFeaTargetEdgeLength = "fea_target_edge_length";
        private const string KeyFeaLoadMeshDegreesThreshold = "fea_load_mesh_degrees_threshold";
        private const string KeyFeaBoundaryConditionsDistanceThreshold = "fea_boundary_conditions_distance_threshold";
        private const string KeyFeaBoundaryConditionsNoiseShellThreshold = "fea_boundary_conditions_noise_shell_threshold";
        private const string KeyFeaInp = "fea_inp_string";

        /// <summary>
        /// Initializes a new instance of the <see cref="ImplantDirector"/> class.
        /// </summary>
        /// <param name="doc">The document.</param>
        public ImplantDirector(RhinoDoc doc, IPluginInfoModel pluginInfoModel) : base(doc, pluginInfoModel)
        {
            // Set inspector to null
            // NOTE: don't use setters b/c panel not initialized
            this.Inspector = null;

            // Init
            this.CurrentDesignPhase = DesignPhase.Initialization;

            IsTestingMode = false;

            ValidateScrewDatabaseXml();

            _currentScrewBrand = GetCurrentScrewBrand();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImplantDirector"/> class.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="archive">The archive.</param>
        /// <param name="pluginMajorVersion">The plugin major version.</param>
        /// <param name="pluginMinorVersion">The plugin minor version.</param>
        /// <param name="splashInfoModel">SplashScreen Info</param>
        /// <exception cref="NotImplementedException">The archive version for the ImplantDirector is larger than the one supported.</exception>
        public ImplantDirector(RhinoDoc doc, Rhino.FileIO.BinaryArchiveReader archive, ArchivableDictionary dict, int pluginMajorVersion, int pluginMinorVersion, IPluginInfoModel pluginInfoModel) : 
            base(doc, archive, dict, pluginMajorVersion, pluginMinorVersion, pluginInfoModel)
        {
            IsTestingMode = false;
            RestoreDesignParameters(dict);
            RestoreFeaResults(dict);
        }

        private void RestoreDesignParameters(ArchivableDictionary dict)
        {
            // Insertion direction
            InsertionDirection = dict.GetVector3d(KeyInsertion, Vector3d.Unset);
            // Plate Thickness
            if (dict.ContainsKey(KeyPlateThickness)) // from IDS 2.0 on
            {
                PlateThickness = dict.GetDouble(KeyPlateThickness);
            }
            // Drill bit radius
            if (dict.ContainsKey(KeyDrillBitRadius)) // from IDS 3.1 on
            {
                DrillBitRadius = dict.GetDouble(KeyDrillBitRadius);
            }

            object contourPlane;
            dict.TryGetValue(KeyContourPlane, out contourPlane);

            if (contourPlane is Plane)
            {
                ContourPlane = (Plane)contourPlane;
            }
        }

        protected override void RestoreTraceabilityInformation(ArchivableDictionary dict)
        {
            // Design Phase
            DesignPhase storedPhase;
            var gotDesignPhase = dict.TryGetEnumValue<DesignPhase>(KeyPhase, out storedPhase);
            if (gotDesignPhase)
            {
                CurrentDesignPhase = storedPhase;
            }

            base.RestoreTraceabilityInformation(dict);
        }

        public void InvalidateFea()
        {
            RhinoDoc.ActiveDoc.UndoRecordingEnabled = false;
            AmaceFea = null;
            Amace.Proxies.PerformFea.InvalidateFeaConduit();
            RhinoDoc.ActiveDoc.UndoRecordingEnabled = true;
        }

        /// <summary>
        /// Restores the fea.
        /// </summary>
        /// <param name="dict">The dictionary.</param>
        private void RestoreFeaResults(ArchivableDictionary dict)
        {
            // Camera settings
            var cameraDirection = dict.GetVector3d(KeyFeaCameraDirection, Vector3d.Unset);
            var cameraTarget = dict.GetPoint3d(KeyFeaCameraTarget, Point3d.Unset);
            var cameraUp = dict.GetVector3d(KeyFeaCameraUp, Vector3d.Unset);

            // Parameters
            var loadMagnitude = dict.GetDouble(KeyFeaLoadMagnitude, double.MinValue);
            var targetEdgeLength = dict.GetDouble(KeyFeaTargetEdgeLength, double.MinValue);
            var loadMeshDegreesThreshold = dict.GetDouble(KeyFeaLoadMeshDegreesThreshold, double.MinValue);
            var boundaryConditionsDistanceThreshold = dict.GetDouble(KeyFeaBoundaryConditionsDistanceThreshold, double.MinValue);
            var boundaryConditionsNoiseShellThreshold = dict.GetDouble(KeyFeaBoundaryConditionsNoiseShellThreshold, double.MinValue);
            var loadVectorType = LoadVectorType.FDAConstruct;
            var gotLoadVectorType = dict.TryGetEnumValue<LoadVectorType>(KeyFeaLoadVectorType, out loadVectorType);

            // Frd
            Frd frd = null;
            if(dict.ContainsKey(KeyFeaFrdStressTensors) && dict.ContainsKey(KeyFeaFrdNodes))
            {
                frd = new Frd
                {
                    StressTensors =
                        StressTensor.ConvertListOfDoubleArraysToListOfStressTensors(
                            ArrayUtilities.ConvertListOfDoublesToListOfDoubleArrays(
                                ((double[]) dict[KeyFeaFrdStressTensors]).ToList(), 6)),
                    Nodes = ArrayUtilities.ConvertListOfDoublesToListOfDoubleArrays(
                        ((double[]) dict[KeyFeaFrdNodes]).ToList(), 3)
                };
            }

            // Meshes
            var implantRemeshed = dict.ContainsKey(KeyFeaImplantRemeshed) ? (Mesh) dict[KeyFeaImplantRemeshed] : null;
            var boundaryConditions = dict.ContainsKey(KeyFeaBoundaryConditions) ? (Mesh) dict[KeyFeaBoundaryConditions] : null;
            var loadMesh = dict.ContainsKey(KeyFeaLoadMesh) ? (Mesh) dict[KeyFeaLoadMesh] : null;

            // Material
            Core.Fea.Material material = dict.ContainsKey(KeyFeaMaterial) ? new Core.Fea.Material(dict.GetDictionary(KeyFeaMaterial)) : null;

            // INP file
            Inp inp = null;
            if(dict.ContainsKey(KeyFeaInp) && dict[KeyFeaInp].GetType() == (new byte[] { }).GetType())
            {
                string inpString = System.Text.Encoding.UTF8.GetString(dict.GetBytes(KeyFeaInp));
                inp = new Inp();
                inp.Read(inpString);
            }
            
            // Check if all information is present
            var feaInformationComplete = cameraDirection != Vector3d.Unset
                                            && cameraTarget != Point3d.Unset
                                            && cameraUp != Vector3d.Unset
                                            && Math.Abs(loadMagnitude - double.MinValue) > double.Epsilon
                                            && gotLoadVectorType
                                            && frd != null
                                            && implantRemeshed != null
                                            && boundaryConditions != null
                                            && Math.Abs(targetEdgeLength - double.MinValue) > double.Epsilon
                                            && Math.Abs(loadMeshDegreesThreshold - double.MinValue) > double.Epsilon
                                            && Math.Abs(boundaryConditionsDistanceThreshold - double.MinValue) > double.Epsilon
                                            && Math.Abs(boundaryConditionsNoiseShellThreshold - double.MinValue) > double.Epsilon
                                            && loadMesh != null
                                            && material != null
                                            && inp != null;

            // Set the FEA (not complete, but enough information to recreate visualization)
            if (feaInformationComplete)
            {
                AmaceFea = new Amace.Fea.AmaceFea(  material, 
                                                    targetEdgeLength, 
                                                    loadVectorType, 
                                                    loadMagnitude, 
                                                    loadMeshDegreesThreshold, 
                                                    boundaryConditionsDistanceThreshold, 
                                                    boundaryConditionsNoiseShellThreshold, 
                                                    cameraTarget, 
                                                    cameraUp, 
                                                    cameraDirection, 
                                                    implantRemeshed, 
                                                    boundaryConditions,
                                                    loadMesh, 
                                                    frd,
                                                    inp);
            }
        }
        
        /// <summary>
        /// Gets the cup.
        /// </summary>
        /// <value>
        /// The cup.
        /// </value>
        public Cup cup
        {
            get
            {
                var objManager = new AmaceObjectManager(this);
                return (Cup)objManager.GetBuildingBlock(IBB.Cup);
            }
        }

        /// <summary>
        /// Gets the current design phase.
        /// </summary>
        /// <value>
        /// The current design phase.
        /// </value>
        public DesignPhase CurrentDesignPhase { get; private set; }

        /// <summary>
        /// Gets the defect femur cor.
        /// </summary>
        /// <value>
        /// The defect femur cor.
        /// </value>
        public Point3d CenterOfRotationDefectFemur => 
            Inspector.DefectFemurCenterOfRotation.IsValid ? Inspector.DefectFemurCenterOfRotation : Point3d.Unset;

        /// <summary>
        /// Gets a value indicating whether [defect is left].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [defect is left]; otherwise, <c>false</c>.
        /// </value>
        public override bool defectIsLeft => Inspector.DefectSide.ToLower() == "left";

        public override string caseId => Inspector.CaseId;

        /// <summary>
        /// Gets the defect SSM cor.
        /// </summary>
        /// <value>
        /// The defect SSM cor.
        /// </value>
        public Point3d CenterOfRotationDefectSsm => 
            Inspector.DefectSsmCenterOfRotation.IsValid ? Inspector.DefectSsmCenterOfRotation : Point3d.Unset;

        /// <summary>
        /// Gets the contraleral SSM cor.
        /// </summary>
        /// <value>
        /// The defect SSM cor.
        /// </value>
        public Point3d CenterOfRotationContralateralFemur =>
            Inspector.ContralateralFemurCenterOfRotation.IsValid ? Inspector.ContralateralFemurCenterOfRotation : Point3d.Unset;

        /// <summary>
        /// Gets the contraleral SSM cor.
        /// </summary>
        /// <value>
        /// The defect SSM cor.
        /// </value>
        public Point3d CenterOfRotationContralateralSsm => 
            Inspector.ContralateralSsmCenterOfRotation.IsValid ? Inspector.ContralateralSsmCenterOfRotation : Point3d.Unset;

        /// <summary>
        /// Gets the insertion anteversion degrees.
        /// </summary>
        /// <value>
        /// The insertion anteversion degrees.
        /// </value>
        public double InsertionAnteversionDegrees
        {
            get
            {
                if (InsertionDirection == Vector3d.Unset)
                {
                    return 0;
                }

                // Compute angles
                var pcs = Inspector.AxialPlane;
                var lateralDir = defectIsLeft ? pcs.YAxis : -pcs.YAxis;
                var angAvRad = Math.Atan2(-InsertionDirection * pcs.XAxis, -InsertionDirection * lateralDir);
                var angAvDeg = RhinoMath.ToDegrees(angAvRad);
                return angAvDeg;
            }
        }

        /// <summary>
        /// Gets the insertion inclination degrees.
        /// </summary>
        /// <value>
        /// The insertion inclination degrees.
        /// </value>
        public double InsertionInclinationDegrees
        {
            get
            {
                if (InsertionDirection == Vector3d.Unset)
                    return 0;
                // Compute angles
                var pcs = Inspector.AxialPlane;
                var lateralDir = defectIsLeft ? pcs.YAxis : -pcs.YAxis;
                var angInclRad = Math.Atan2(-InsertionDirection * lateralDir, -InsertionDirection * -pcs.ZAxis);
                var angInclDeg = RhinoMath.ToDegrees(angInclRad);
                return angInclDeg;
            }
        }

        /// <summary>
        /// Gets or sets the inspector.
        /// </summary>
        /// <value>
        /// The inspector.
        /// </value>
        public PreOpInspector Inspector { get; set; }

        /// <summary>
        /// Gets the mirrored clat femur cor.
        /// </summary>
        /// <value>
        /// The mirrored clat femur cor.
        /// </value>
        public Point3d CenterOfRotationContralateralFemurMirrored
        {
            get
            {
                var cor = Point3d.Unset;
                if (!Inspector.ContralateralFemurCenterOfRotation.IsValid)
                {
                    return cor;
                }

                cor = Inspector.ContralateralFemurCenterOfRotation;
                var mirror = Transform.Mirror(Inspector.SagittalPlane);
                cor.Transform(mirror);
                return cor;
            }
        }


        /// <summary>
        /// Gets the center of rotation contralateral SSM mirrored.
        /// </summary>
        /// <value>
        /// The center of rotation contralateral SSM mirrored.
        /// </value>
        public Point3d CenterOfRotationContralateralSsmMirrored
        {
            get
            {
                var cor = Point3d.Unset;
                if (!Inspector.ContralateralSsmCenterOfRotation.IsValid)
                {
                    return cor;
                }

                cor = Inspector.ContralateralSsmCenterOfRotation;
                var mirror = Transform.Mirror(Inspector.SagittalPlane);
                cor.Transform(mirror);
                return cor;
            }
        }

        public Color CupColor => BuildingBlocks.Blocks[IBB.Cup].Color;

        /// <summary>
        /// Gets or sets the plate thickness.
        /// </summary>
        /// <value>
        /// The plate thickness.
        /// </value>
        public double PlateThickness { get; set; } = 3.0;

        public Plane Pcs => Inspector.AxialPlane;

        private File3dm _screwDatabase;

        /// <summary>
        /// Gets the screw database.
        /// </summary>
        /// <value>
        /// The screw database.
        /// </value>
        public File3dm ScrewDatabase => _screwDatabase ?? (_screwDatabase = LoadScrewDatabase());

        /// <summary>
        /// Gets the sign ant position clat.
        /// </summary>
        /// <value>
        /// The sign ant position clat.
        /// </value>
        public int SignAntPosClat => defectIsLeft ? -1 : 1;

        /// <summary>
        /// Gets the sign ant position definition.
        /// </summary>
        /// <value>
        /// The sign ant position definition.
        /// </value>
        public int SignAntPosDef => defectIsLeft ? -1 : 1;

        /// <summary>
        /// Gets the sign ant position PCS.
        /// </summary>
        /// <value>
        /// The sign ant position PCS.
        /// </value>
        public int SignAntPosPcs => -1;

        /// <summary>
        /// Gets the sign inf sup clat.
        /// </summary>
        /// <value>
        /// The sign inf sup clat.
        /// </value>
        public int SignInfSupClat => 1;

        /// <summary>
        /// Gets the sign inf sup definition.
        /// </summary>
        /// <value>
        /// The sign inf sup definition.
        /// </value>
        public int SignInfSupDef => 1;

        /// <summary>
        /// Gets the sign inf sup PCS.
        /// </summary>
        /// <value>
        /// The sign inf sup PCS.
        /// </value>
        public int SignInfSupPcs => -1;

        /// <summary>
        /// Gets the sign med lat clat.
        /// </summary>
        /// <value>
        /// The sign med lat clat.
        /// </value>
        public int SignMedLatClat => defectIsLeft ? 1 : -1;

        /// <summary>
        /// Gets the sign med lat definition.
        /// </summary>
        /// <value>
        /// The sign med lat definition.
        /// </value>
        public int SignMedLatDef => defectIsLeft ? 1 : -1;

        /// <summary>
        /// Gets the sign med lat PCS.
        /// </summary>
        /// <value>
        /// The sign med lat PCS.
        /// </value>
        public int SignMedLatPcs => defectIsLeft ? 1 : -1;

        /// <summary>
        /// Checks the data available.
        /// </summary>
        /// <param name="printMessage">if set to <c>true</c> [print message].</param>
        /// <param name="checkInspector">if set to <c>true</c> [check inspector].</param>
        /// <param name="blocks">The blocks.</param>
        /// <returns></returns>
        private bool CheckDataAvailable(bool printMessage, bool checkInspector, params IBB[] blocks)
        {
            string msg;
            var available = IsDataAvailable(out msg, checkInspector, blocks);
            if (printMessage && msg != "")
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, msg);
            }

            return available;
        }

        /// <summary>
        /// Enters the design phase.
        /// </summary>
        /// <param name="toPhase">To phase.</param>
        public void EnterDesignPhase(DesignPhase toPhase)
        {
            // Swap current design phase
            CurrentDesignPhase = toPhase;
        }

        /// <summary>
        /// Value (in degrees) below which the angle between the plate side surface and the top surface becomes too sharp
        /// </summary>
        private const double CriticalEdgeAngle = 65;

        /// <summary>
        /// Gets the critical edge angle.
        /// </summary>
        /// <value>
        /// The critical edge angle.
        /// </value>
        public double criticalEdgeAngle => CriticalEdgeAngle;

        public Brep CupBrepGeometry => cup.BrepGeometry;

        /// <summary>
        /// Gets the length of the critical edge.
        /// </summary>
        /// <value>
        /// The length of the critical edge.
        /// </value>
        public double CriticalEdgeLength => MathUtilities.DiagonalEdgeLength(CriticalEdgeAngle, PlateThickness);

        /// <summary>
        /// Determines whether [is allowed in current phase] [the specified phase flag].
        /// </summary>
        /// <param name="phaseFlag">The phase flag.</param>
        /// <returns>
        ///   <c>true</c> if [is allowed in current phase] [the specified phase flag]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsAllowedInCurrentPhase(DesignPhase phaseFlag)
        {
            // Allowable when ALL flags in currentPhase are set to 1 in phaseFlags
            // - when the current phase is None (all zeros), this is always true
            // - when the phaseFlags are Any (all ones) this is always true
            return (CurrentDesignPhase & phaseFlag) == CurrentDesignPhase;
        }

        public override void EnterDesignPhase(DesignPhaseProperty toPhase)
        {
            var toDesignPhase = DesignPhases.Phases.Where(p => p.Value == toPhase).Select(p => p.Key).First();
            EnterDesignPhase(toDesignPhase);
        }

        /// <summary>
        /// Determines whether [is command runnable] [the specified command].
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="printMessage">if set to <c>true</c> [print message].</param>
        /// <returns>
        ///   <c>true</c> if [is command runnable] [the specified command]; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsCommandRunnable(Rhino.Commands.Command command, bool printMessage = false)
        {
            if (IDSPluginHelper.CloseAfterCommand)
            {
                return false;
            }


            var idsAttr = command.GetType().GetCustomAttributes(typeof(IDSCommandAttributes), false) as IEnumerable<IDSCommandAttributes>;

            // If no attributes defined: it is runnable
            if (null == idsAttr || !idsAttr.Any())
            {
                return true;
            }

            // Check phase and data
            var cmdAttr = idsAttr.First();
            return IsCommandRunnable(cmdAttr, printMessage);
        }

        /// <summary>
        /// Determines whether [is command runnable] [the specified attributes].
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="printMessage">if set to <c>true</c> [print message].</param>
        /// <returns>
        ///   <c>true</c> if [is command runnable] [the specified attributes]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsCommandRunnable(IDSCommandAttributes attributes, bool printMessage = false)
        {
            // Check the phase (bitwise operator or Enum.HasFlag)
            var phaseOk = IsAllowedInCurrentPhase(attributes.PhasesWhereRunnable);
            if (phaseOk)
            {
                return CheckDataAvailable(printMessage, attributes.RequiresInspector,
                    attributes.RequiredBlocks.ToArray());
            }

            if (printMessage)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "The command is not available in the {0} phase", CurrentDesignPhase);
            }

            return false;
        }

        /// <summary>
        /// Determines whether [is data available] [the specified message].
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="checkInspector">if set to <c>true</c> [check inspector].</param>
        /// <param name="blocks">The blocks.</param>
        /// <returns>
        ///   <c>true</c> if [is data available] [the specified message]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsDataAvailable(out string message, bool checkInspector, params IBB[] blocks)
        {
            var missingMsg = new List<string>();
            var available = true;

            // Check pre-op data
            available &= !checkInspector || (null != Inspector);
            if (!available)
            {
                missingMsg.Add("Pre-op data");
            }

            var objManager = new AmaceObjectManager(this);

            // Check all building blocks
            foreach (var block in blocks)
            {
                var blockAvailable = objManager.HasBuildingBlock(block);
                if (!blockAvailable)
                {
                    missingMsg.Add(BuildingBlocks.Blocks[block].Name);
                }

                available &= blockAvailable;
            }

            // Construct message
            message = "";
            if (missingMsg.Count <= 0)
            {
                return available;
            }

            var missingString = string.Join(", ", missingMsg);
            message = $"Missing data: {missingString}.";

            return available;
        }

        /// <summary>
        /// Prepares the objects for archive.
        /// </summary>
        public override void PrepareObjectsForArchive()
        {
            // Prepare all custom RhinoObject objects for serialization
            var settings = new ObjectEnumeratorSettings
            {
                HiddenObjects = true
            };
            var rhobjs = Document.Objects.FindByFilter(settings);

            foreach (var rhobj in rhobjs)
            {
                var block = rhobj as IBBinterface<ImplantDirector>;
                block?.PrepareForArchiving();
            }
        }

        /// <summary>
        /// Restores the custom rhino objects.
        /// </summary>
        /// <param name="doc">The document.</param>
        public override void RestoreCustomRhinoObjects(RhinoDoc doc)
        {
            var objManager = new AmaceObjectManager(this);
            objManager.RestoreCustomRhinoObjects(doc);

            // Backwards compatibility for Bone Graft Feature (IDS 2.1.0)
            ProvideBackwardCompatibilityBoneGraft();
            
            _currentScrewBrand = GetCurrentScrewBrand();
        }

        /// <summary>
        /// Provides the backward compatibility for the bone graft feature.
        /// </summary>
        public void ProvideBackwardCompatibilityBoneGraft()
        {
            var objManager = new AmaceObjectManager(this);
            if (objManager.HasBuildingBlock(IBB.PreopPelvis))
            {
                return;
            }

            var originalPelvis = objManager.GetBuildingBlock(IBB.DefectPelvis).Geometry;
            objManager.SetBuildingBlock(IBB.PreopPelvis, originalPelvis, Guid.Empty);
        }

        /// <summary>
        /// Write the current design parameters to the project notes
        /// </summary>
        /// <param name="director"></param>
        public void WriteParametersToNotes()
        {
            var entries = ParameterExporter.GetParameterFileEntries(this);
            Document.Notes = string.Join("\n", entries);
        }

        /// <summary>
        /// Writes to archive.
        /// </summary>
        /// <param name="archive">The archive.</param>
        public override void WriteToArchive(Rhino.FileIO.BinaryArchiveWriter archive)
        {
            var dict = new ArchivableDictionary(archiveVersion, "ImplantDirector")
            {
                Version = archiveVersion
            };
            // store version to anticipate future changes

            dict.SetEnumValue(KeyPhase, CurrentDesignPhase);

            WriteDesignParametersToArchive(dict);
            WriteFeaToArchive(dict);

            WriteToArchive(dict);

            archive.WriteDictionary(dict);
        }

        public override void SetVisibilityByPhase()
        {
            Visibility.SetVisibilityByPhase(this);
        }

        public override void OnInitialView(RhinoDoc openedDoc)
        {
            if (Inspector == null)
            {
                return;
            }

            var doc = Document;

            // Set appropriate view
            View.SetIDSDefaults(doc);
            SetVisibilityByPhase();

            // Disable conduits
            Amace.Proxies.TogglePlateAnglesVisualisation.Disable(this);

            // Refresh the cup panel
            var cupPanel = CupPanel.GetPanel();
            if (null != cupPanel)
            {
                cupPanel.document = openedDoc;
                cupPanel.UpdatePanelWithCup(cup);
                var panelId = CupPanel.panelId;
                if (CurrentDesignPhase == DesignPhase.Cup)
                {
                    Panels.OpenPanel(panelId);
                    cupPanel.Enabled = true;
                }
                else
                {
                    cupPanel.Enabled = false;
                }
            }

            // Refresh screw panel
            var screwPanel = ScrewPanel.GetPanel();
            if (screwPanel != null)
            {
                screwPanel.doc = openedDoc;
                screwPanel.RefreshPanelInfo();
            }

            // Set phase to draft to make sure no commands can be executed
            if (documentType == DocumentType.CupQC ||
                documentType == DocumentType.ImplantQC ||
                documentType == DocumentType.Export)
            {
                Amace.Relations.PhaseChanger.ChangePhase(this, DesignPhase.Draft, false);
            }
        }

        public override void OnObjectDeleted()
        {
            //Empty
        }

        private void WriteDesignParametersToArchive(ArchivableDictionary dict)
        {
            dict.Set(KeyInsertion, InsertionDirection);
            dict.Set(KeyPlateThickness, PlateThickness);
            dict.Set(KeyDrillBitRadius, DrillBitRadius);
            dict.Set(KeyContourPlane, ContourPlane);
        }

        private void WriteFeaToArchive(ArchivableDictionary dict)
        {
            if (AmaceFea == null)
            {
                return;
            }

            // Values used to create screenshots
            dict.Set(KeyFeaCameraDirection, AmaceFea.CameraDirection);
            dict.Set(KeyFeaCameraTarget, AmaceFea.CameraTarget);
            dict.Set(KeyFeaCameraUp, AmaceFea.CameraUp);
            // Values written in the QC report
            dict.Set(KeyFeaLoadMagnitude, AmaceFea.LoadMagnitude);
            dict.Set(KeyFeaLoadVectorType, AmaceFea.loadVectorType.ToString());
            dict.Set(KeyFeaTargetEdgeLength, AmaceFea.TargetEdgeLength);
            dict.Set(KeyFeaLoadMeshDegreesThreshold, AmaceFea.LoadMeshDegreesThreshold);
            dict.Set(KeyFeaBoundaryConditionsDistanceThreshold, AmaceFea.BoundaryConditionsDistanceThreshold);
            dict.Set(KeyFeaBoundaryConditionsNoiseShellThreshold, AmaceFea.BoundaryConditionsNoiseShellThreshold);
            // Values used to visualise the results (FEA conduit)
            dict.Set(KeyFeaFrdNodes, ArrayUtilities.ConvertListOfDoubleArraysToListOfDoubles(AmaceFea.frd.Nodes));
            dict.Set(KeyFeaImplantRemeshed, AmaceFea.ImplantRemeshed);
            dict.Set(KeyFeaBoundaryConditions, AmaceFea.BoundaryConditions);
            dict.Set(KeyFeaLoadMesh, AmaceFea.loadMesh);
            dict.Set(KeyFeaMaterial, AmaceFea.material.ToArchiveableDictionary());
            List<double[]> stressTensorArrays = StressTensor.ConvertListOfStressTensorsToListOfDoubleArrays(AmaceFea.frd.StressTensors);
            dict.Set(KeyFeaFrdStressTensors, ArrayUtilities.ConvertListOfDoubleArraysToListOfDoubles(stressTensorArrays));

            // INP file 
            dict.Set(KeyFeaInp, System.Text.Encoding.UTF8.GetBytes(AmaceFea.inp.ToString()));
        }

        /// <summary>
        /// Loads the screw database.
        /// </summary>
        public static File3dm LoadScrewDatabase()
        {
            // Load the screw database file
            var resources = new AmaceResources();
            var screwDbPath = resources.ScrewDatabasePath;

            try
            {
                var screwDatabase = File3dm.Read(screwDbPath);
                IDSPluginHelper.WriteLine(LogCategory.Default, "Screw database loaded successfully");
                return screwDatabase;
            }
            catch (FileNotFoundException)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Screw database file not found at location {0}", screwDbPath);
                return null;
            }
        }

        private static void ValidateScrewDatabaseXml()
        {
            var resources = new AmaceResources();

            var screwDbXml = resources.ScrewDatabaseXmlPath;
            if (!File.Exists(screwDbXml))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Screw database xml file not found at location {screwDbXml}");
                return;
            }

            var screwDbXmlXsd = resources.ScrewDatabaseXmlSchemaPath;
            if (!File.Exists(screwDbXmlXsd))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Screw database xml schema file not found at location {screwDbXmlXsd}");
                return;
            }
            
            var isValid = true;
            var settings = new XmlReaderSettings();
            settings.Schemas.Add(null, screwDbXmlXsd);
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationEventHandler += (sender, e) =>
            {
                switch (e.Severity)
                {
                    case XmlSeverityType.Error:
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"Line number: {e.Exception.LineNumber}, line position: {e.Exception.LinePosition}, {e.Message}");
                        break;
                    case XmlSeverityType.Warning:
                        IDSPluginHelper.WriteLine(LogCategory.Warning, $"Line number: {e.Exception.LineNumber}, line position: {e.Exception.LinePosition}, {e.Message}");
                        break;
                }

                isValid = false;
            };

            var reader = XmlReader.Create(screwDbXml, settings);
            while (reader.Read())
            {
            }

            if (isValid)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Screw database xml file is validated successfully");
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Screw database xml file is invalid");
            }
        }

        /// <summary>
        /// Overwrite the FEA currently in the director and set up to date to true
        /// </summary>
        public void SetFea(Amace.Fea.AmaceFea fea)
        {
            AmaceFea = fea;
        }
        
        public string GetCurrentScrewBrand()
        {
            var screwManager = new ScrewManager(Document);
            var screws = screwManager.GetAllScrews().ToList();
            return ExtractScrewBrand(screws);
        }

        public static string ExtractScrewBrand(List<Screw> screws)
        {
            if (screws.Any())
            {
                var currentBrand = screws.First().screwBrandType;

                //All should only contain one screw brand
                if (screws.Any(s => s.screwBrandType.Brand != currentBrand.Brand))
                {
                    throw new IDSException("Screw brand inconsistency detected! This shouldn't happen!");
                }

                return currentBrand.Brand;
            }
            else
            {
                var screwDatabaseQuery = new ScrewDatabaseQuery();
                var currentBrand = screwDatabaseQuery.GetDefaultScrewBrand();
                return currentBrand;
            }
        }

        private void UpdateCurrentScrewBrandIfContainsScrews()
        {
            var screwManager = new ScrewManager(Document);
            var screws = screwManager.GetAllScrews().ToList();
            if (!screws.Any())
            {
                return;
            }

            var brand = ExtractScrewBrand(screws);
            _currentScrewBrand = brand;
        }

        public override DesignPhaseProperty CurrentDesignPhaseProperty => DesignPhases.Phases[CurrentDesignPhase];

        public override string CurrentDesignPhaseName => CurrentDesignPhase.ToString();
    }
}