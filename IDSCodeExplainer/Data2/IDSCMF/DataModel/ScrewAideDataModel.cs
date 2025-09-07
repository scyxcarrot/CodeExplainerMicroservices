using IDS.CMF.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.DataModel
{
    public class ScrewAideDataModel
    {
        private Dictionary<string, string> _importPathDictionary { get; set; }
        public ScrewAideDataModel(string screwType)
        {
            _importPathDictionary = ScrewEntityImportPathHelper.GetScrewAidesBrepImportPaths3dm(screwType);
        }
        
        public Dictionary<string, GeometryBase> GenerateScrewAideDictionary()
        {
            return new Dictionary<string, GeometryBase>
	        {
	            { Constants.ScrewAide.Head, ScrewHead },
	            { Constants.ScrewAide.HeadRef, ScrewHeadRef},
	            { Constants.ScrewAide.Eye, ScrewEye},
	            { Constants.ScrewAide.Container, ScrewContainer},
	            { Constants.ScrewAide.Stamp, ScrewStamp},
	            { Constants.ScrewAide.EyeShape, ScrewEyeShape},
	            { Constants.ScrewAide.EyeSubtractor, ScrewEyeSubtractor},
	            { Constants.ScrewAide.EyeLabelTag, ScrewLabelTag},
	            { Constants.ScrewAide.EyeLabelTagShape, ScrewLabelTagShape},
	            { Constants.ScrewAide.EyeRef, ScrewEyeRef},
	            { Constants.ScrewAide.EyeLabelTagRef, ScrewEyeLabelTagRef},
	        };
        }

        public void Update()
        {
            ScrewHead = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.Head]).FirstOrDefault();
            ScrewContainer = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.Container]).FirstOrDefault();
            ScrewEye = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.Eye]).FirstOrDefault();
            ScrewStamp = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.Stamp]).FirstOrDefault();
            ScrewHeadRef = (Curve)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.HeadRef]).FirstOrDefault();
            ScrewEyeShape = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeShape]).FirstOrDefault();
            ScrewEyeSubtractor = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeSubtractor]).FirstOrDefault();
            ScrewLabelTag = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeLabelTag]).FirstOrDefault();
            ScrewLabelTagShape = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeLabelTagShape]).FirstOrDefault();
            ScrewEyeRef = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeRef]).FirstOrDefault();
            ScrewEyeLabelTagRef = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeLabelTagRef]).FirstOrDefault();
        }

        private Brep _screwHead;
        public Brep ScrewHead
        {
            get
            {
                if (_screwHead == null)
                {
                    _screwHead = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.Head]).FirstOrDefault();
                }
                return _screwHead;
            }

            protected set
            {
                _screwHead = value;
            }
        }

        private Brep _screwContainer;
        public Brep ScrewContainer
        {
            get
            {
                if (_screwContainer == null)
                {
                    _screwContainer = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.Container]).FirstOrDefault();
                }
                return _screwContainer;
            }
            protected set
            {
                _screwContainer = value;
            }
        }

        private Brep _screwEye;
        public Brep ScrewEye
        {
            get
            {
                if (_screwEye == null)
                {
                    _screwEye = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.Eye]).FirstOrDefault();
                }
                return _screwEye;
            }
            protected set
            {
                _screwEye = value;
            }
        }

        private Brep _screwStamp;
        public Brep ScrewStamp
        {
            get
            {
                if (_screwStamp == null)
                {
                    _screwStamp = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.Stamp]).FirstOrDefault();
                }
                return _screwStamp;
            }
            protected set
            {
                _screwStamp = value;
            }
        }

        private Curve _screwHeadRef;
        public Curve ScrewHeadRef
        {
            get
            {
                if (_screwHeadRef == null)
                {
                    _screwHeadRef = (Curve)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.HeadRef]).FirstOrDefault();
                }
                return _screwHeadRef;
            }
            protected set
            {
                _screwHeadRef = value;
            }
        }

        private Brep _screwEyeRef;
        public Brep ScrewEyeRef
        {
            get
            {
                if (_screwEyeRef == null)
                {
                    _screwEyeRef = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeRef]).FirstOrDefault();
                }
                return _screwEyeRef;
            }
            protected set
            {
                _screwEyeRef = value;
            }
        }

        private Brep _screwEyeLabelTagRef;
        public Brep ScrewEyeLabelTagRef
        {
            get
            {
                if (_screwEyeLabelTagRef == null)
                {
                    _screwEyeLabelTagRef = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeLabelTagRef]).FirstOrDefault();
                }
                return _screwEyeLabelTagRef;
            }
            protected set
            {
                _screwEyeLabelTagRef = value;
            }
        }

        private Brep _screwEyeShape;
        public Brep ScrewEyeShape
        {
            get
            {
                if (_screwEyeShape == null)
                {
                    _screwEyeShape = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeShape]).FirstOrDefault();
                }
                return _screwEyeShape;

            }
            protected set
            {
                _screwEyeShape = value;
            }
        }

        private Brep _screwEyeSubtractor;
        public Brep ScrewEyeSubtractor
        {
            get
            {
                if (_screwEyeSubtractor == null)
                {
                    _screwEyeSubtractor = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeSubtractor]).FirstOrDefault();
                }
                return _screwEyeSubtractor;

            }
            protected set
            {
                _screwEyeSubtractor = value;
            }
        }

        private Brep _screwLabelTag;
        public Brep ScrewLabelTag
        {
            get
            {
                if (_screwLabelTag == null)
                {
                    _screwLabelTag = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeLabelTag]).FirstOrDefault();
                }
                return _screwLabelTag;
            }
            protected set
            {
                _screwLabelTag = value;
            }
        }

        private Brep _screwLabelTagShape;
        public Brep ScrewLabelTagShape
        {
            get
            {
                if (_screwLabelTagShape == null)
                {
                    _screwLabelTagShape = (Brep)StepsFileIOUtilities.ImportObjectVia3dm(_importPathDictionary[Constants.ScrewAide.EyeLabelTagShape]).FirstOrDefault();
                }
                return _screwLabelTagShape;
            }
            protected set
            {
                _screwLabelTagShape = value;
            }
        }

    }
}
