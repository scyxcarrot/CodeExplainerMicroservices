using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace IDS.Core.Fea
{
    /// <summary>
    /// Abstract FEA class
    /// </summary>
    public abstract class Fea
    {
        /// <summary>
        /// The remeshing edge length
        /// </summary>
        public double TargetEdgeLength { get;}

        /// <summary>
        /// The load mesh
        /// </summary>
        protected Mesh LoadMesh;

        /// <summary>
        /// The implant mesh
        /// </summary>
        protected Mesh ImplantMesh;

        /// <summary>
        /// The boundary conditions
        /// </summary>
        private Mesh _boundaryConditions;

        /// <summary>
        /// Gets the load vector.
        /// </summary>
        /// <value>
        /// The load vector.
        /// </value>
        private Vector3d _loadVector;

        /// <summary>
        /// The FRD file path
        /// </summary>
        private readonly string _frdResultFile;

        /// <summary>
        /// The implant remeshed
        /// </summary>
        public Mesh ImplantRemeshed { get; private set; }

        /// <summary>
        /// The implant remeshed file
        /// </summary>
        private readonly string _implantRemeshedFile;

        /// <summary>
        /// The inp simulation file
        /// </summary>
        private string InpSimulationFile
        {
            get
            {
                return inp.InpFile;
            }
            set
            {
                inp.InpFile = value;
            }
        }

        protected Fea()
        {
            // for testing purposes
        }

        /// <summary>
        /// Constructor to perform a new FEA
        /// </summary>
        /// <param name="implantMesh">The implant mesh.</param>
        /// <param name="boundaryConditionType">Type of the boundary condition.</param>
        /// <param name="loadVectorMagnitudeType">Type of the load vector magnitude.</param>
        /// <param name="loadVectorType">Type of the load vector.</param>
        /// <param name="targetEdgeLength"></param>
        /// <param name="feaDirectory">The fea directory.</param>
        /// <param name="loadMagnitude"></param>
        /// <param name="material"></param>
        protected Fea(Mesh implantMesh,
                    BoundaryConditionsType boundaryConditionType,
                    LoadVectorType loadVectorType,
                    double loadMagnitude,
                    Material material,
                    double targetEdgeLength,
                    string feaDirectory)
        {
            inp = new Inp
            {
                HeaderLines = new List<string> {"Written by IDS"}
            };

            ImplantMesh = implantMesh;
            this.BoundaryConditionType = boundaryConditionType;
            this.loadVectorType = loadVectorType;
            this.FeaDirectory = feaDirectory;
            this.material = material;
            this.LoadMagnitude = loadMagnitude;
            TargetEdgeLength = targetEdgeLength;

            _implantRemeshedFile = Path.Combine(feaDirectory, "remeshed.stl");
            InpSimulationFile = Path.Combine(feaDirectory, "simulation.inp");
            _frdResultFile = Path.Combine(feaDirectory, "simulation.frd");

            loadMesh = null;
            LoadVector = Vector3d.Unset;

            CameraDirection = Vector3d.Unset;
            CameraUp = Vector3d.Unset;
            CameraTarget = Point3d.Unset;
        }

        /// <summary>
        /// Construtor to store FEA results upon loading a document
        /// </summary>
        protected Fea(Material material,
                    double targetEdgeLength, 
                    LoadVectorType loadVectorType,
                    double loadMagnitude,
                    Point3d cameraTarget, 
                    Vector3d cameraUp, 
                    Vector3d cameraDirection,
                    Mesh implantRemeshed,
                    Mesh boundaryConditions,
                    Mesh loadMesh,
                    Frd frd,
                    Inp inp) : this(null, BoundaryConditionsType.Unset, loadVectorType, loadMagnitude, material, targetEdgeLength, string.Empty)
        {
            // Overwrite defaults set through chained constructor with previously calculated values
            this.CameraTarget = cameraTarget;
            this.CameraUp = cameraUp;
            this.CameraDirection = cameraDirection;
            ImplantRemeshed = implantRemeshed;
            this.BoundaryConditions = boundaryConditions;
            this.loadMesh = loadMesh;
            this.frd = frd;

            this.inp = inp;
            this.material = material; // needs to be set again, since setting the inp overwrites it with uts/fatigue unset
        }

        /// <summary>
        /// Gets or sets the boundary conditions.
        /// </summary>
        /// <value>
        /// The boundary conditions.
        /// </value>
        public Mesh BoundaryConditions
        {
            get { return _boundaryConditions ?? (_boundaryConditions = GetBoundaryConditions()); }
            protected set
            {
                _boundaryConditions = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the boundary condition.
        /// </summary>
        /// <value>
        /// The type of the boundary condition.
        /// </value>
        public BoundaryConditionsType BoundaryConditionType { get; protected set; }

        /// <summary>
        /// Gets up vectorUp
        /// </summary>
        /// <value>
        /// . vector.
        /// </value>
        private Vector3d _cameraUpVector;

        /// <summary>
        /// Gets or sets the camera up.
        /// </summary>
        /// <value>
        /// The camera up.
        /// </value>
        public Vector3d CameraUp
        {
            get
            {
                if (_cameraUpVector == Vector3d.Unset)
                {
                    _cameraUpVector = GetCameraUp();
                }

                return _cameraUpVector;
            }
            protected set
            {
                _cameraUpVector = value;
            }
        }

        /// <summary>
        /// Gets or sets the material.
        /// </summary>
        /// <value>
        /// The material.
        /// </value>
        public Material material
        {
            get
            {
                return inp.Simulation.Material;
            }
            set
            {
                inp.Simulation.Material = value;
            }
        }

        /// <summary>
        /// Gets the camera up.
        /// </summary>
        /// <returns></returns>
        protected abstract Vector3d GetCameraUp();

        /// <summary>
        /// Gets or sets the fea directory.
        /// </summary>
        /// <value>
        /// The fea directory.
        /// </value>
        public string FeaDirectory { get; set; }

        /// <summary>
        /// The inp file path
        /// </summary>
        public LoadVectorType loadVectorType { get; protected set; }

        /// <summary>
        /// Gets or sets the FRD.
        /// </summary>
        /// <value>
        /// The FRD.
        /// </value>
        public Frd frd { get; protected set; }

        /// <summary>
        /// Gets the implant vector.
        /// </summary>
        /// <value>
        /// The implant vector.
        /// </value>
        public abstract Vector3d ImplantVector { get; }

        /// <summary>
        /// Gets or sets the inp.
        /// </summary>
        /// <value>
        /// The inp.
        /// </value>
        public Inp inp { get; set; }

        /// <summary>
        /// Gets or sets the load mesh.
        /// </summary>
        /// <value>
        /// The load mesh.
        /// </value>
        public Mesh loadMesh
        {
            get { return LoadMesh ?? (LoadMesh = GetLoadMesh()); }
            protected set
            {
                LoadMesh = value;
            }
        }

        /// <summary>
        /// Gets or sets the load magnitude.
        /// </summary>
        /// <value>
        /// The load magnitude.
        /// </value>
        public double LoadMagnitude { get; }

        /// <summary>
        /// Gets or sets the load vector.
        /// </summary>
        /// <value>
        /// The load vector.
        /// </value>
        public Vector3d LoadVector
        {
            get
            {
                if (_loadVector == Vector3d.Unset)
                {
                    _loadVector = GetLoadVector();
                }

                return _loadVector;
            }
            protected set
            {
                _loadVector = value;
            }
        }

        private Vector3d _cameraDirection;

        public Vector3d CameraDirection
        {
            get
            {
                if (_cameraDirection == Vector3d.Unset)
                {
                    _cameraDirection = GetCameraDirection();
                }

                return _cameraDirection;
            }
            protected set
            {
                _cameraDirection = value;
            }
        }

        protected abstract Vector3d GetCameraDirection();

        private Point3d _cameraTarget;

        public Point3d CameraTarget
        {
            get
            {
                if (_cameraTarget == Point3d.Unset)
                {
                    _cameraTarget = GetCameraTarget();
                }

                return _cameraTarget;
            }
            protected set
            {
                _cameraTarget = value;
            }
        }

        protected abstract Point3d GetCameraTarget();

        /// <summary>
        /// Sets up the requirements to run an FEA
        /// </summary>
        public void PrepareFea()
        {
            RemeshImplant();
            CreateTetMesh();
            _boundaryConditions = GetBoundaryConditions();
            SetSimulationParameters();

            // Write outputs
            inp.Write();
            // Additional outputs for testing
            var boundaryConditionsColor = Colors.GetColorArray(Color.LightGray);
            StlUtilities.RhinoMesh2StlBinary(BoundaryConditions, Path.Combine(FeaDirectory, "boundaryConditions.stl"), boundaryConditionsColor);
            var loadMeshColor = Colors.GetColorArray(Color.Red);
            StlUtilities.RhinoMesh2StlBinary(loadMesh, Path.Combine(FeaDirectory, "loadMesh.stl"), loadMeshColor);
        }

        /// <summary>
        /// Sets the simulation parameters.
        /// </summary>
        protected abstract void SetSimulationParameters();

        /// <summary>
        /// Runs calculix on the inp file.
        /// </summary>
        private bool RunCalculixOnInp()
        {
            return ExternalToolInterop.CalculixExecute(InpSimulationFile);
        }

        /// <summary>
        /// Performs the fea.
        /// </summary>
        public bool PerformFea()
        {
            PrepareFea();
            var succesfulSimulation = RunCalculixOnInp();

            if (succesfulSimulation)
            {
                ImportFrdResults();
            }

            return succesfulSimulation;
        }

        /// <summary>
        /// Determines the boundary conditions.
        /// </summary>
        protected abstract Mesh GetBoundaryConditions();

        /// <summary>
        /// Determines the load vector.
        /// </summary>
        protected abstract Vector3d GetLoadVector();

        /// <summary>
        /// Converts the mesh to simulation nset.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns></returns>
        private List<int> ConvertMeshToSimulationNset(Mesh mesh)
        {
            var nSet = new List<int>();
            const double tol = 0.01;

            foreach(Point3d point in mesh.Vertices)
            {
                var i = 1;
                foreach(var node in inp.Part.Nodes)
                {
                    if(Math.Abs(node[0] - point[0]) < tol 
                        && Math.Abs(node[1] - point[1]) < tol 
                        && Math.Abs(node[2] - point[2]) < tol)
                    {
                        nSet.Add(i);
                        break;
                    }

                    i++;
                }
            }

            return nSet;
        }

        /// <summary>
        /// Adds the mesh as simulation load n set.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="nSetName">Name of the n set.</param>
        public void AddMeshAsSimulationLoadNSet(Mesh mesh, string nSetName)
        {
            var nSet = ConvertMeshToSimulationNset(mesh);

            if (nSet.Count <= 0)
            {
                return;
            }

            inp.Simulation.NSetsLoad.Add(nSetName, nSet);
            for(var i = 0; i < 3; i++)
            {
                inp.Simulation.Loads.Add(new InpLoad(nSetName, i+1, LoadMagnitude * LoadVector[i] / nSet.Count));
            }
        }

        /// <summary>
        /// Adds the mesh as simulation boundary condition n set.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="nSetName">Name of the n set.</param>
        public void AddMeshAsSimulationBoundaryConditionNSet(Mesh mesh, string nSetName)
        {
            var nSet = ConvertMeshToSimulationNset(mesh);

            if (nSet.Count > 0)
            {
                inp.Simulation.NSetsBoundaryConditions.Add(nSetName, nSet);
            }
        }

        /// <summary>
        /// Imports the FRD results.
        /// </summary>
        protected void ImportFrdResults()
        {
            frd = new Frd(_frdResultFile);
        }

        /// <summary>
        /// Exports the implant mesh and makes it into a tet mesh inp file
        /// </summary>
        private void CreateTetMesh()
        {
            // Write to STL file
            StlUtilities.RhinoMesh2StlAscii(ImplantRemeshed, _implantRemeshedFile);

            // Create TetMesh
            var part = ExternalToolInterop.TetGenVolumeMesh(_implantRemeshedFile, TargetEdgeLength, 1, 0);
            inp.Part.Nodes = part.Nodes;
            inp.Part.Elements = part.Elements;
        }

        /// <summary>
        /// Gets the load mesh.
        /// </summary>
        /// <returns></returns>
        protected abstract Mesh GetLoadMesh();    

        /// <summary>
        /// Remesh the original implant mesh
        /// </summary>
        private void RemeshImplant()
        {
            // Remesh
            Mesh remeshed;
            MeshUtilities.Remesh(ImplantMesh, TargetEdgeLength, out remeshed);
            // Set as FEA property
            ImplantRemeshed = remeshed;
        }
    }
}