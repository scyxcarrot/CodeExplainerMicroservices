using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Fea;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using System;


namespace IDS.Amace.Fea
{
    public class AmaceFea : Core.Fea.Fea
    {
        /// <summary>
        /// The cup
        /// </summary>
        private Cup cup;

        /// <summary>
        /// The implant bottom mesh
        /// </summary>
        private readonly Mesh _implantBottomMesh;

        /// <summary>
        /// The reamed pelvis
        /// </summary>
        private readonly Mesh _reamedPelvis;

        /// <summary>
        /// The load mesh degrees threshold
        /// </summary>
        /// <value>
        /// The load mesh degrees threshold.
        /// </value>
        public double LoadMeshDegreesThreshold { get; }

        /// <summary>
        /// Gets the boundary conditions distance threshold.
        /// </summary>
        /// <value>
        /// The boundary conditions distance threshold.
        /// </value>
        public double BoundaryConditionsDistanceThreshold { get; }

        /// <summary>
        /// Gets the boundary conditions noise shell threshold.
        /// </summary>
        /// <value>
        /// The boundary conditions noise shell threshold.
        /// </value>
        public double BoundaryConditionsNoiseShellThreshold { get; }

        /// <summary>
        /// Empty constructor, reserved for testing purposes
        /// </summary>
        public AmaceFea()
        {
            // for testing purposes
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public AmaceFea(Mesh implantMesh,
                        Mesh implantBottomMesh,
                        Mesh reamedPelvis,
                        Cup cup,
                        Material material,
                        double targetEdgeLength,
                        BoundaryConditionsType boundaryConditionType,
                        LoadVectorType loadVectorType,
                        double loadMagnitude,
                        double loadMeshDegreesThreshold,
                        double boundaryConditionsDistanceThreshold,
                        double boundaryConditionsNoiseShellThreshold,
                        string feaDirectory)
            : base(implantMesh, boundaryConditionType, loadVectorType, loadMagnitude, material, targetEdgeLength, feaDirectory)
        {
            _implantBottomMesh = implantBottomMesh;
            _reamedPelvis = reamedPelvis;
            this.cup = cup;

            LoadMeshDegreesThreshold = loadMeshDegreesThreshold;
            BoundaryConditionsDistanceThreshold = boundaryConditionsDistanceThreshold;
            BoundaryConditionsNoiseShellThreshold = boundaryConditionsNoiseShellThreshold;
        }

        /// <summary>
        /// Construtor to store FEA results upon loading a document
        /// </summary>
        public AmaceFea(Material material,
                        double targetEdgeLength,
                        LoadVectorType loadVectorType,
                        double loadMagnitude,
                        double loadMeshDegreesThreshold,
                        double boundaryConditionsDistanceThreshold,
                        double boundaryConditionsNoiseShellThreshold,
                        Point3d cameraTarget,
                        Vector3d cameraUp,
                        Vector3d cameraDirection,
                        Mesh implantRemeshed,
                        Mesh boundaryConditions,
                        Mesh loadMesh,
                        Frd frd,
                        Inp inp
                        )
            : base(material, targetEdgeLength, loadVectorType, loadMagnitude, cameraTarget, cameraUp, cameraDirection, implantRemeshed, boundaryConditions, loadMesh, frd, inp)
        {
            LoadMeshDegreesThreshold = loadMeshDegreesThreshold;
            BoundaryConditionsDistanceThreshold = boundaryConditionsDistanceThreshold;
            BoundaryConditionsNoiseShellThreshold = boundaryConditionsNoiseShellThreshold;
        }

        /// <summary>
        /// Gets the up direction of the camera
        /// </summary>
        /// <value>
        /// . vector.
        /// </value>
        protected override Vector3d GetCameraUp()
        {
            // cam up vector
            var upDirection = cup.Director.Pcs.Normal;
            upDirection.Unitize();
            var crossVector = Vector3d.CrossProduct(CameraDirection, upDirection);
            if (crossVector.Length < 0.01)
            {
                crossVector = cup.Director.Pcs.XAxis;
                crossVector.Unitize();
            }
            upDirection = Vector3d.CrossProduct(crossVector, CameraDirection);
            upDirection.Unitize();

            return upDirection;
        }

        /// <summary>
        /// Gets the implant vector.
        /// </summary>
        /// <value>
        /// The implant vector.
        /// </value>
        public override Vector3d ImplantVector => cup.orientation;

        /// <summary>
        /// Gets the view vector.
        /// </summary>
        /// <value>
        /// The view vector.
        /// </value>
        protected override Vector3d GetCameraDirection()
        {
            // view orientation
            var viewDirection = cup.orientation;
            viewDirection.Unitize();

            return viewDirection;
        }

        /// <summary>
        /// Automatically determines the boundary conditions (see CreateBCMesh function in Peter's FEAMakerà
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override Mesh GetBoundaryConditions()
        {
            // Parameters
            const double meshTolerance = 0.15;

            // Figure out which parts of the remeshed plate are within boneDist of reamedPelvis
            var selectedMesh = MeshUtilities.SelectFromMeshToMesh(ImplantRemeshed, _reamedPelvis, BoundaryConditionsDistanceThreshold);

            // Figure out what part of the selection is close to the solid plate bottom to avoid wrong selections
            var boundaryConditionMesh = MeshUtilities.SelectFromMeshToMesh(selectedMesh, _implantBottomMesh, meshTolerance);

            // Remove noise shells
            var boundaryConditionMeshFiltered = MeshUtilities.RemoveNoiseShells(boundaryConditionMesh, BoundaryConditionsNoiseShellThreshold);

            return boundaryConditionMeshFiltered;
        }

        /// <summary>
        /// Gets the mesh that corresponds to the load area
        /// </summary>
        protected override Mesh GetLoadMesh()
        {
            // Select mesh subset based on load vector direction
            var dotProductThreshold = Math.Cos(LoadMeshDegreesThreshold / 180 * Math.PI);
            const double meshTolerance = 0.1;
            var loadSurface = cup.innerCupSurfaceMesh;
            var selectedMesh = MeshUtilities.SelectMeshSubSetByNormalDirection(LoadVector, loadSurface, dotProductThreshold, meshTolerance);

            // Get corresponding subset from remeshed implant
            var theLoadMesh = new Mesh();
            if (selectedMesh.Faces.Count > 0)
            {
                // Transfer the selected regions to the remeshed plate
                theLoadMesh = MeshUtilities.SelectFromMeshToMesh(ImplantRemeshed, selectedMesh, meshTolerance);
            }

            return theLoadMesh;
        }

        /// <summary>
        /// Determines the load vector.
        /// </summary>
        protected override Vector3d GetLoadVector()
        {
            var loadVector = Vector3d.Unset;

            // Get the load Vector based on method
            switch (loadVectorType)
            {
                case (LoadVectorType.FDAConstruct):

                    loadVector = CalculateFdaConstructLoadVector(cup.centerOfRotation, cup.orientation, cup.coordinateSystem.ZAxis);

                    break;

                case (LoadVectorType.CupVector):

                    loadVector = -cup.orientation;
                    loadVector.Unitize();

                    break;
            }

            return loadVector;
        }

        /// <summary>
        /// Calculates the fda construct load vector.
        /// Made public and static for testing purposes
        /// </summary>
        /// <param name="cupCenterOfRotation">The cup center of rotation.</param>
        /// <param name="cupOrientation">The cup orientation.</param>
        /// <param name="coordinateSystemZAxis">The PCS z axis.</param>
        /// <returns></returns>
        public static Vector3d CalculateFdaConstructLoadVector(Point3d cupCenterOfRotation, Vector3d cupOrientation, Vector3d coordinateSystemZAxis)
        {
            // Define the rotation axis as the cross product of cupvec and pcs z-axis
            var crossVec = Vector3d.CrossProduct(cupOrientation, coordinateSystemZAxis);

            // Transform cup vector
            var T45 = Transform.Rotation(RhinoMath.ToRadians(-45), crossVec, cupCenterOfRotation);
            var loadVector = cupOrientation;
            loadVector.Transform(T45);
            loadVector = -loadVector;
            loadVector.Unitize();

            return loadVector;
        }

        protected override void SetSimulationParameters()
        {
            // Part
            inp.Part.Name = "PLATE";
            inp.Part.ElementType = "C3D4";
            inp.Part.ElementSetName = "PLATE_ALLTET";

            // Boundary conditions
            inp.Simulation.BoundaryConditions.Add(new InpBoundaryCondition("IPLATE_BC_NSET", 1, 3));

            // Simulation nSets
            AddMeshAsSimulationLoadNSet(loadMesh, "IPLATE_LOAD_NSET");
            AddMeshAsSimulationBoundaryConditionNSet(BoundaryConditions, "IPLATE_BC_NSET");
        }

        protected override Point3d GetCameraTarget()
        {
            return cup.centerOfRotation;
        }
    }
}