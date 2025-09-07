using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Utilities;
using Materialise.MtlsMimicsRW.Mimics;
using System;
using System.Collections.Generic;

namespace IDS.EnlightCMFIntegration.Operations
{
    public class EnlightCMFReader : IDisposable
    {
        private readonly string _filePath;
        private MimicsFile _mimicsFile;

        public EnlightCMFReader(string filePath)
        {
            _filePath = filePath;
        }

        public bool GetAllStlProperties(out List<StlProperties> stls)
        {
            stls = new List<StlProperties>();

            try
            {
                LoadFile();

                var getter = new StlPropertiesGetter(MtlsGlobals.SharedContext, _mimicsFile);
                var result = getter.GetAllStlProperties(out stls);

                CloseFile();

                return result;
            }
            catch
            {
                return false;
            }
        }

        public bool GetAllSplineProperties(out List<SplineProperties> splines)
        {
            splines = new List<SplineProperties>();

            try
            {
                LoadFile();

                var getter = new SplinePropertiesGetter(MtlsGlobals.SharedContext, _mimicsFile);
                var result = getter.GetAllSplineProperties(out splines);

                CloseFile();

                return result;
            }
            catch
            {
                return false;
            }
        }

        public bool GetAllOsteotomyProperties(out List<OsteotomyProperties> osteotomies)
        {
            osteotomies = new List<OsteotomyProperties>();

            try
            {
                LoadFile();

                var getter = new OsteotomyPropertiesGetter(MtlsGlobals.SharedContext, _mimicsFile);
                var result = getter.GetAllOsteotomyProperties(out osteotomies);

                CloseFile();

                return result;
            }
            catch
            {
                return false;
            }
        }

        public bool GetAllPlaneProperties(out List<PlaneProperties> planes)
        {
            planes = new List<PlaneProperties>();

            try
            {
                LoadFile();

                var getter = new PlanePropertiesGetter(MtlsGlobals.SharedContext, _mimicsFile);
                var result = getter.GetAllPlaneProperties(out planes);

                CloseFile();

                return result;
            }
            catch
            {
                return false;
            }
        }

        public bool GetStlMeshesProperties(List<StlProperties> stlsProperties)
        {
            try
            {
                LoadFile();

                foreach (var stlProperties in stlsProperties)
                {
                    var getter = new StlMeshPropertiesGetter(MtlsGlobals.SharedContext, _mimicsFile);
                    getter.GetStlMeshProperties(stlProperties);
                }

                CloseFile();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool GetOstetotomyMeshesProperties(List<OsteotomyProperties> osteotomiesProperties)
        {
            try
            {
                LoadFile();

                foreach (var osteotomyProperties in osteotomiesProperties)
                {
                    var getter = new OstetotomyMeshPropertiesGetter(MtlsGlobals.SharedContext, _mimicsFile);
                    getter.GetOstetotomyMeshProperties(osteotomyProperties);
                }

                CloseFile();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool GetSplineMeshesProperties(List<SplineProperties> splinesProperties)
        {
            try
            {
                foreach (var splineProperties in splinesProperties)
                {
                    var getter = new SplineMeshPropertiesGetter();
                    getter.GetSplineMeshProperties(splineProperties);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LoadFile()
        {
            var fileLoader = new LoadMimicsFile
            {
                FilePath = _filePath
            };

            var result = fileLoader.Operate(MtlsGlobals.SharedContext);

            _mimicsFile = result.MimicsFile;
        }

        public void Dispose()
        {
            if (_mimicsFile != null && _mimicsFile.IsValid)
            {
                CloseFile();
            }
        }

        private void CloseFile()
        {
            var fileCloser = new CloseMimicsFile
            {
                MimicsFile = _mimicsFile
            };

            fileCloser.Operate(MtlsGlobals.SharedContext);

            _mimicsFile.Dispose();
        }
    }
}
