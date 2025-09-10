using IDS.CMF.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.DataModel
{
    public class BarrelAideDataModel
    {
        private Dictionary<string, string> _importPathDictionary { get; set; }
        public BarrelAideDataModel(string screwType, string barrelType)
        {
            _importPathDictionary =
                BarrelEntityImportPathUtility.GetBarrelAidesBrepImportPaths3dm(screwType, barrelType);
        }
        
        public Dictionary<string, GeometryBase> GenerateBarrelAideDictionary()
        {
            return new Dictionary<string, GeometryBase>
            {
                { Constants.BarrelAide.Barrel, ScrewBarrel },
                { Constants.BarrelAide.BarrelShape, ScrewBarrelShape },
                { Constants.BarrelAide.BarrelSubtractor, ScrewBarrelSubtractor },
                { Constants.BarrelAide.BarrelRef, ScrewBarrelRef },
            };
        }

        public void Update()
        {
            ScrewBarrel = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.BarrelAide.Barrel]).FirstOrDefault();
            ScrewBarrelShape = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.BarrelAide.BarrelShape]).FirstOrDefault();
            ScrewBarrelSubtractor = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.BarrelAide.BarrelSubtractor]).FirstOrDefault();
            ScrewBarrelRef = (Curve)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.BarrelAide.BarrelRef]).FirstOrDefault();
        }

        private Brep _screwBarrel;
        public Brep ScrewBarrel
        {
            get
            {
                if (_screwBarrel == null)
                {
                    _screwBarrel = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.BarrelAide.Barrel]).FirstOrDefault();
                }
                return _screwBarrel;
            }
            protected set
            {
                _screwBarrel = value;
            }
        }

        private Brep _screwBarrelShape;
        public Brep ScrewBarrelShape
        {
            get
            {
                if (_screwBarrelShape == null)
                {
                    _screwBarrelShape = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.BarrelAide.BarrelShape]).FirstOrDefault();
                }
                return _screwBarrelShape;
            }
            protected set
            {
                _screwBarrelShape = value;
            }
        }

        private Brep _screwBarrelSubtractor;
        public Brep ScrewBarrelSubtractor
        {
            get
            {
                if (_screwBarrelSubtractor == null)
                {
                    _screwBarrelSubtractor = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.BarrelAide.BarrelSubtractor]).FirstOrDefault();
                }
                return _screwBarrelSubtractor;
            }
            protected set
            {
                _screwBarrelSubtractor = value;
            }
        }

        private Curve _screwBarrelRef;
        public Curve ScrewBarrelRef
        {
            get
            {
                if (_screwBarrelRef == null)
                {
                    _screwBarrelRef = (Curve)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.BarrelAide.BarrelRef]).FirstOrDefault();
                }
                return _screwBarrelRef;
            }
            protected set
            {
                _screwBarrelRef = value;
            }
        }
    }
}
