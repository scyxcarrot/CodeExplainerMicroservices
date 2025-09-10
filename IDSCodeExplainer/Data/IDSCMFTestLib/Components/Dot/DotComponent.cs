using IDS.CMF.DataModel;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace IDS.CMF.TestLib.Components
{
    public class DotComponent
    {
        public enum DotType
        {
            Pastille,
            ControlPoint
        }

        #region Common
        [JsonConverter(typeof(StringEnumConverter))]
        public DotType Type { get; set; }

        public IDSPoint3D Location { get; set; } = IDSPoint3D.Unset;

        public IDSVector3D Direction { get; set; } = IDSVector3D.Unset;
        #endregion

        #region Pastile Only
        public Guid Id { get; set; } = Guid.Empty;

        public double Diameter { get; set; } = Double.NaN;

        public double Thickness { get; set; } = Double.NaN;

        public string CreationAlgoMethod { get; set; } = null;

        public ScrewData Screw { get; set; } = null;

        public LandmarkComponent Landmark { get; set; } = null;
        #endregion

        private void SetDotControlPoint(DotControlPoint controlPoint)
        {
            Type = DotType.ControlPoint;
            Location = new IDSPoint3D(controlPoint.Location);
            Direction = new IDSVector3D(controlPoint.Direction);
            Id = controlPoint.Id;
        }

        private DotControlPoint GetDotControlPoint()
        {
            return new DotControlPoint()
            {
                Location = new IDSPoint3D(Location),
                Direction = new IDSVector3D(Direction),
                Id = Id
            };
        }

        private void SetDotPastille(DotPastille pastille)
        {
            Type = DotType.Pastille;
            Location = new IDSPoint3D(pastille.Location);
            Direction = new IDSVector3D(pastille.Direction);

            Id = pastille.Id;
            Diameter = pastille.Diameter;
            Thickness = pastille.Thickness;
            CreationAlgoMethod = pastille.CreationAlgoMethod;

            if (pastille.Screw != null)
            {
                Screw = new ScrewData()
                {
                    Id = pastille.Screw.Id
                };
            }

            if (pastille.Landmark != null)
            {
                Landmark = new LandmarkComponent();
                Landmark.SetLandmark(pastille.Landmark);
            }
        }

        private DotPastille GetDotPastille()
        {
            return new DotPastille()
            {
                Location = new IDSPoint3D(Location),
                Direction = new IDSVector3D(Direction),
                Diameter = Diameter,
                Thickness = Thickness,
                Screw = (IScrew)Screw?.Clone(),
                Landmark = Landmark?.GetLandmark(),
                CreationAlgoMethod = CreationAlgoMethod,
                Id = Id
            };
        }

        public void SetDot(IDot dot)
        {
            if (dot is DotPastille pastille)
            {
                SetDotPastille(pastille);
            }
            else if (dot is DotControlPoint controlPoint)
            {
                SetDotControlPoint(controlPoint);
            }
            else
            {
                throw new InvalidCastException($"dot Type {dot.GetType()} is not 'DotPastille' or 'DotControlPoint'");
            }
        }

        public IDot GetDot()
        {
            switch (Type)
            {
                case DotType.Pastille:
                    return GetDotPastille();
                case DotType.ControlPoint:
                    return GetDotControlPoint();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
