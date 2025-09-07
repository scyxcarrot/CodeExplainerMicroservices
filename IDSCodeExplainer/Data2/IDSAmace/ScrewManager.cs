using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using RhinoMtlsCore.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Amace
{
    public class ScrewManager
    {
        /// <summary>
        /// The document
        /// </summary>
        private RhinoDoc _document;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrewManager"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        public ScrewManager(RhinoDoc document)
        {
            _document = document;
        }

        /// <summary>
        /// Creates the medial bumps in region where necessary and get.
        /// </summary>
        /// <param name="screwRegion">The screw region.</param>
        /// <param name="fillEmpty">if set to <c>true</c> [fill empty].</param>
        /// <returns></returns>
        public List<Mesh> GetMedialBumpsInRegion(ScrewPosition screwRegion, bool fillEmpty = true)
        {
            var medialBumpList = new List<Mesh>();
            foreach (var screw in GetAllScrews())
            {
                // Get medial bump for sunk screws
                if (screw.screwAlignment == ScrewAlignment.Sunk && screw.positioning == screwRegion)
                {
                    medialBumpList.Add(screw.MedialBump); // get
                }
                // Add empty mesh for other screws
                else if (fillEmpty)
                {
                    medialBumpList.Add(new Mesh());
                }
            }
            return medialBumpList;
        }

        public List<Mesh> GetLateralBumpsInRegion(ScrewPosition screwRegion)
        {
            return (from screw in GetAllScrews() where screw.positioning == screwRegion select screw.LateralBump).ToList();
        }

        /// <summary>
        /// Gets the region trimmed medial bumps.
        /// </summary>
        /// <param name="screwPos">The screw position.</param>
        /// <param name="fillEmpty">if set to <c>true</c> [fill empty].</param>
        /// <returns></returns>
        public List<Mesh> GetTrimmedMedialBumpsInRegion(ScrewPosition screwPos, bool fillEmpty = true)
        {
            var medialBumpList = new List<Mesh>();
            foreach (var screw in this.GetAllScrews())
            {
                // Get trimmed medial bumps if they exist and are from the given screw region
                if (screw.ScrewAides.ContainsKey(ScrewAideType.MedialBump) && screwPos == screw.positioning)
                {
                    medialBumpList.Add(screw.MedialTrimmedBump);
                }
                // Add empty mesh for other screws
                else if (fillEmpty)
                {
                    medialBumpList.Add(new Mesh());
                }
            }
            return medialBumpList;
        }

        /// <summary>
        /// Gets all screws.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Screw> GetAllScrews()
        {
            var settings =
                new ObjectEnumeratorSettings
                {
                    NameFilter = BuildingBlocks.Blocks[IBB.Screw].Name,
                    HiddenObjects = true
                };
            var rhobjs = _document.Objects.FindByFilter(settings);
            var screws = rhobjs.OfType<Screw>().ToList();
            screws.Sort();
            return screws;
        }

        /// <summary>
        /// Gets all stud deletors.
        /// </summary>
        /// <returns></returns>
        private List<Mesh> GetAllStudDeletors()
        {
            return GetAllScrews().Select(screw => screw.StudSelector).ToList();
        }

        /// <summary>
        /// Gets all stud deletors union.
        /// </summary>
        /// <returns></returns>
        public Mesh GetAllStudDeletorsUnion()
        {
            // init
            var studDeletorUnion = new Mesh();

            // Unify
            var studDeletorList = GetAllStudDeletors();
            if (studDeletorList.Count > 0)
            {
                Booleans.PerformBooleanUnion(out studDeletorUnion, studDeletorList.ToArray());
            }

            // done
            return studDeletorUnion;
        }

        public void CreateMissingTrimmedLateralBumps()
        {
            foreach (var screw in GetAllScrews())
            {
                // Create lateral bump if it does not exist
                if (screw.ScrewAides.ContainsKey(ScrewAideType.LateralBump))
                {
                    screw.CreateTrimmedLateralBump();
                }
            }
        }

        public List<Mesh> GetTrimmedLateralBumps()
        {
            var lateralBumpList = new List<Mesh>();
            foreach (var screw in GetAllScrews())
            {
                // Create lateral bump if it does not exist
                lateralBumpList.Add(screw.ScrewAides.ContainsKey(ScrewAideType.LateralBump)
                    ? screw.lateralTrimmedBump
                    : new Mesh());
            }
            return lateralBumpList;
        }

        public List<Mesh> GetTrimmedMedialBumps()
        {
            var medialBumpList = new List<Mesh>();
            foreach (var screw in GetAllScrews())
            {
                // Get trimmed medial bumps if they exist
                medialBumpList.Add(screw.ScrewAides.ContainsKey(ScrewAideType.MedialBump)
                    ? screw.MedialTrimmedBump
                    : new Mesh());
            }
            return medialBumpList;
        }

        public void CreateMissingTrimmedMedialBumps()
        {
            foreach (var screw in GetAllScrews())
            {
                // Get trimmed medial bumps if they exist
                if (screw.ScrewAides.ContainsKey(ScrewAideType.MedialBump))
                {
                    screw.CreateTrimmedMedialBump();
                }
            }
        }

        public List<Mesh> GetLateralBumps()
        {
            var lateralBumpList = new List<Mesh>();
            foreach (var screw in this.GetAllScrews())
            {
                // Create medial bump for sunk screws
                lateralBumpList.Add(screw.positioning == ScrewPosition.Flange ? screw.LateralBump : new Mesh());
            }
            return lateralBumpList;
        }

        /// <summary>
        /// Gets all MBV hole subtractors.
        /// </summary>
        /// <returns></returns>
        private List<Mesh> GetAllMbvHoleSubtractors()
        {
            return GetAllScrews().Select(theScrew => theScrew.ScaffoldBoolean).ToList();
        }

        /// <summary>
        /// Gets all MBV hole subtractors union.
        /// </summary>
        /// <returns></returns>
        public Mesh GetAllMbvHoleSubtractorsUnion()
        {
            // init
            var mbvHoleUnion = new Mesh();

            // Unify
            var mbvHoleSubtractorList = GetAllMbvHoleSubtractors();
            if (mbvHoleSubtractorList.Count > 0)
            {
                Booleans.PerformBooleanUnion(out mbvHoleUnion, mbvHoleSubtractorList.ToArray());
            }

            // done
            return mbvHoleUnion;
        }

        /// <summary>
        /// Gets all medial bumps.
        /// </summary>
        /// <param name="fillEmpty">if set to <c>true</c> [fill empty].</param>
        /// <returns></returns>
        public List<Mesh> GetMedialBumps(bool fillEmpty = true)
        {
            var medialBumpList = new List<Mesh>();
            foreach (var screw in this.GetAllScrews())
            {
                // Create medial bump for sunk screws
                if (screw.screwAlignment == ScrewAlignment.Sunk)
                {
                    medialBumpList.Add(screw.MedialBump); // get
                }
                // Add empty mesh for other screws
                else if (fillEmpty)
                {
                    medialBumpList.Add(new Mesh());
                }
            }
            return medialBumpList;
        }

        /// <summary>
        /// Gets all screw hole subtractors.
        /// </summary>
        /// <returns></returns>
        private List<Mesh> GetAllScrewHoleSubtractors()
        {
            return GetAllScrews().Select(theScrew => theScrew.ScrewHoleSubtractor).ToList();
        }

        /// <summary>
        /// Gets all screw hole subtractors union.
        /// </summary>
        /// <returns></returns>
        public Mesh GetAllScrewHoleSubtractorsUnion()
        {
            // init
            var screwHoleUnion = new Mesh();

            // Unify
            var screwHoleSubtractorList = GetAllScrewHoleSubtractors();
            if (screwHoleSubtractorList.Count > 0)
            {
                var mergedMeshes = MeshUtilities.MergeMeshes(screwHoleSubtractorList.ToArray());
                screwHoleUnion = AutoFix.PerformUnify(mergedMeshes);
            }

            // done
            return screwHoleUnion;
        }
    }
}
