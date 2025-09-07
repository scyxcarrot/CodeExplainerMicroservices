using IDS.Core.V2.Geometries;
using IDS.Core.V2.Geometry;
using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Operations;
using IDS.EnlightCMFIntegration.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Loader;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.CMF.V2.Loader
{
    public class EnlightCMFLoader : IPreopLoader
    {
        private readonly IConsole _console;
        private EnlightCMFReader _reader;
        private List<StlProperties> _stls;
        private List<OsteotomyProperties> _osteotomies;
        private List<SplineProperties> _splines;

        public EnlightCMFLoader(IConsole console, string filePath)
        {
            _console = console;
            _reader = new EnlightCMFReader(filePath);
        }

        public List<IPreopLoadResult> PreLoadPreop()
        {
            if (_stls != null || _osteotomies != null || _splines != null)
            {
                return GetFilteredParts();
            }

            if (!LoadAllPreopProperties())
            {
                return null;
            }

            var filteredStls = EnlightCMFLoaderUtilities.FilterParts(_stls.ToList<IObjectProperties>());
            _stls.Clear();
            _stls.AddRange(filteredStls.Cast<StlProperties>());

            var filteredOsteotomies = EnlightCMFLoaderUtilities.FilterParts(_osteotomies.ToList<IObjectProperties>());
            _osteotomies.Clear();
            _osteotomies.AddRange(filteredOsteotomies.Cast<OsteotomyProperties>());

            var filteredSplines = EnlightCMFLoaderUtilities.FilterParts(_splines.ToList<IObjectProperties>());
            _splines.Clear();
            _splines.AddRange(filteredSplines.Cast<SplineProperties>());

            return GetFilteredParts();
        }

        public bool LoadAllPreopProperties()
        {
            if (!_reader.GetAllStlProperties(out List<StlProperties> stls))
            {
                _console.WriteErrorLine("Failed to get stls.");
                return false;
            }

            if (!_reader.GetAllOsteotomyProperties(out List<OsteotomyProperties> osteotomies))
            {
                _console.WriteErrorLine("Failed to get osteotomies.");
                return false;
            }

            if (!_reader.GetAllSplineProperties(out List<SplineProperties> splines))
            {
                _console.WriteErrorLine("Failed to get splines.");
                return false;
            }

            _stls = stls;
            _osteotomies = osteotomies;
            _splines = splines;

            return true;

        }

        public void CleanUp()
        {
            if (_reader != null)
            {
                _reader.Dispose();

                _reader = null;
            }
        }

        public List<IPreopLoadResult> ImportPreop()
        {
            return Import(out _);
        }

        public List<IPreopLoadResult> Import(out List<IObjectProperties> objectProperties)
        {
            objectProperties = null;

            if (_stls == null || _osteotomies == null || _splines == null)
            {
                _console.WriteErrorLine("Inputs are invalid!");
                return null;
            }

            var outputEnlight = new List<IPreopLoadResult>();

            var outputStls = GetStls(_stls);
            if (outputStls == null)
            {
                return null;
            }

            var outputOsteotomies = GetOsteotomies(_osteotomies);
            if (outputOsteotomies == null)
            {
                return null;
            }

            var outputSplines = GetSplines(_splines);
            if (outputSplines == null)
            {
                return null;
            }

            outputEnlight.AddRange(outputStls);
            outputEnlight.AddRange(outputOsteotomies);
            outputEnlight.AddRange(outputSplines);

            objectProperties = new List<IObjectProperties>();
            objectProperties.AddRange(_stls);
            objectProperties.AddRange(_osteotomies);
            objectProperties.AddRange(_splines);

            return outputEnlight;
        }

        public bool GetPlanes(out IPlane sagittalPlane, out IPlane axialPlane, out IPlane coronalPlane, out IPlane midSagittalPlane)
        {
            sagittalPlane = IDSPlane.Unset;
            axialPlane = IDSPlane.Unset;
            coronalPlane = IDSPlane.Unset;
            midSagittalPlane = IDSPlane.Unset;

            List<PlaneProperties> planes;
            if (!_reader.GetAllPlaneProperties(out planes))
            {
                return false;
            }

            foreach (var plane in planes)
            {
                if (plane.Name.Equals(NHPPlane.SagittalPlaneName))
                {
                    //TAKE NOTE
                    //divides the parts into left and right sections; it's normal is pointing from the RIGHT to the LEFT direction
                    //***normal needs to be flipped for IDS***
                    sagittalPlane = new IDSPlane(ToPoint3d(plane.Origin), -ToVector3d(plane.Normal));
                }
                else if (plane.Name.Equals(NHPPlane.MidSagittalPlaneName))
                {
                    midSagittalPlane = new IDSPlane(ToPoint3d(plane.Origin), -ToVector3d(plane.Normal));
                }
                else if (plane.Name.Equals(NHPPlane.CoronalPlaneName))
                {
                    //divides the parts into front and back sections; it's normal is pointing from the front to the back direction
                    coronalPlane = new IDSPlane(ToPoint3d(plane.Origin), ToVector3d(plane.Normal));
                }
                else if (plane.Name.Equals(NHPPlane.AxialPlaneName))
                {
                    //divides the parts into upper and lower sections; it's normal is pointing from the lower to the upper direction
                    axialPlane = new IDSPlane(ToPoint3d(plane.Origin), ToVector3d(plane.Normal));
                }
            }

            return !sagittalPlane.IsUnset() && !coronalPlane.IsUnset() && !axialPlane.IsUnset() && !midSagittalPlane.IsUnset();
        }

        public List<Tuple<string, bool>> GetPartInfos()
        {
            var parts = PreLoadPreop();
            return parts.GroupBy(part => part.Name.ToLower()).Select(group => group.LastOrDefault()).Select(part => new Tuple<string, bool>(part.Name, part.IsReferenceObject)).ToList();
        }

        public bool ExportPreopToStl(List<string> partNames, string outputDirectory)
        {
            if (_stls == null || _osteotomies == null || _splines == null)
            {
                _console.WriteErrorLine("PreLoadPreop not performed!");
                return false;
            }

            var loweredPartNames = partNames.Select(n => n.ToLower());
            var objectProperties = GetStls(_stls.Where(s => loweredPartNames.Contains(s.Name.ToLower())).ToList());
            objectProperties.AddRange(GetOsteotomies(_osteotomies.Where(s => loweredPartNames.Contains(s.Name.ToLower())).ToList()));
            objectProperties.AddRange(GetSplines(_splines.Where(s => loweredPartNames.Contains(s.Name.ToLower())).ToList()));

            foreach (var objectProp in objectProperties)
            {
                StlUtilitiesV2.IDSMeshToStlBinary(objectProp.Mesh, Path.Combine(outputDirectory, $"{objectProp.FilePath}.stl"));
            }

            return true;
        }

        private List<IPreopLoadResult> GetFilteredParts()
        {
            var filteredParts = new List<IObjectProperties>();
            filteredParts.AddRange(_stls);
            filteredParts.AddRange(_osteotomies);
            filteredParts.AddRange(_splines);

            var processed = new List<IPreopLoadResult>();

            foreach (var part in filteredParts)
            {
                processed.Add(new EnlightCMFLoadResult(part));
            }

            return processed;
        }

        private IDSPoint3D ToPoint3d(double[] pointArray)
        {
            return new IDSPoint3D(pointArray[0], pointArray[1], pointArray[2]);
        }

        private IDSVector3D ToVector3d(double[] vectorArray)
        {
            return new IDSVector3D(vectorArray[0], vectorArray[1], vectorArray[2]);
        }

        private List<IPreopLoadResult> GetStls(List<StlProperties> stlProperties)
        {
            var outputStls = new List<IPreopLoadResult>();

            if (!_reader.GetStlMeshesProperties(stlProperties))
            {
                _console.WriteErrorLine("Something went wrong while retrieving stl mesh properties");
                return null;
            }

            foreach (var stl in stlProperties)
            {
                if (stl.Triangles == null || stl.Vertices == null)
                {
                    _console.WriteErrorLine($"Failed to get stl mesh properties for {stl.Name}");
                    return null;
                }

                outputStls.Add(EnlightCMFLoadResult.Create(stl));
            }

            return outputStls;
        }

        private List<IPreopLoadResult> GetOsteotomies(List<OsteotomyProperties> osteotomyProperties)
        {
            var outputOsteotomies = new List<IPreopLoadResult>();

            if (!_reader.GetOstetotomyMeshesProperties(osteotomyProperties))
            {
                _console.WriteErrorLine("Something went wrong while retrieving osteotomy mesh properties");
                return null;
            }

            foreach (var osteotomy in osteotomyProperties)
            {
                if (osteotomy.Triangles == null || osteotomy.Vertices == null)
                {
                    _console.WriteErrorLine($"Failed to get osteotomy mesh properties for {osteotomy.Name}");
                    return null;
                }

                outputOsteotomies.Add(EnlightCMFLoadResult.Create(osteotomy));
            }

            return outputOsteotomies;
        }

        private List<IPreopLoadResult> GetSplines(List<SplineProperties> splineProperties)
        {
            var outputSplines = new List<IPreopLoadResult>();

            if (!_reader.GetSplineMeshesProperties(splineProperties))
            {
                _console.WriteErrorLine("Something went wrong while retrieving spline mesh properties");
                return null;
            }

            foreach (var spline in splineProperties)
            {
                if (spline.Triangles == null || spline.Vertices == null)
                {
                    _console.WriteErrorLine($"Failed to get spline mesh properties for { spline.Name}");
                    return null;
                }

                outputSplines.Add(EnlightCMFLoadResult.Create(spline));
            }

            return outputSplines;
        }

        public bool GetOsteotomyHandler(out List<IOsteotomyHandler> osteotomyHandler)
        {
            osteotomyHandler = new List<IOsteotomyHandler>();

            foreach (var osteotomy in _osteotomies)
            {
                if (osteotomy.HandlerCoordinates.GetLength(0) < 1)
                {
                    continue;
                }

                osteotomyHandler.Add(new EnlightCMFOsteotomyHandler(osteotomy));
            }

            return true;
        }
    }
}
