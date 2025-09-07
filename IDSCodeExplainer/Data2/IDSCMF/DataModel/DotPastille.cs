using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace IDS.CMF.DataModel
{
    public class DotPastille : IDot
    {
        public static readonly string[] CreationAlgoMethods = { "Primary", "Secondary" };

        public static string SerializationLabelConst => "DotPastille";
        public string SerializationLabel { get; set; }

        protected IPoint3D location;

        public IPoint3D Location
        {
            get { return location; }
            set
            {
                if (Landmark != null)
                {
                    //this does not transform the building block in the document. A call to invalidate the building block is required
                    var translateTransform =
                        Transform.Translation(RhinoPoint3dConverter.ToPoint3d(value) -
                                              RhinoPoint3dConverter.ToPoint3d(location));
                    var landmarkPoint = RhinoPoint3dConverter.ToPoint3d(Landmark.Point);
                    landmarkPoint.Transform(translateTransform);
                    Landmark.Point = RhinoPoint3dConverter.ToIPoint3D(landmarkPoint);
                }

                location = value;
            }
        }

        private IVector3D direction;

        public IVector3D Direction
        {
            get { return direction; }
            set
            {
                if (Landmark != null)
                {
                    //this does not transform the building block in the document. A call to invalidate the building block is required
                    var rotateTransform = Transform.Rotation(RhinoVector3dConverter.ToVector3d(Direction),
                        RhinoVector3dConverter.ToVector3d(value), RhinoPoint3dConverter.ToPoint3d(location));
                    var landmarkPoint = RhinoPoint3dConverter.ToPoint3d(Landmark.Point);
                    landmarkPoint.Transform(rotateTransform);
                    Landmark.Point = RhinoPoint3dConverter.ToIPoint3D(landmarkPoint);
                }

                direction = value;
            }
        }

        public double Diameter { get; set; }

        public double Thickness { get; set; }

        public string CreationAlgoMethod { get; set; }

        public IScrew Screw { get; set; }

        public Landmark Landmark { get; set; }

        public Guid Id { get; set; }

        public DotPastille()
        {
            SerializationLabel = SerializationLabelConst;
            CreationAlgoMethod = CreationAlgoMethods[0];
        }

        public DotPastille(Dictionary<string, object> dictionary, Guid id)
        {
            SerializationLabel = SerializationLabelConst;
            Location = new IDSPoint3D(dictionary["Location"].ToString());
            Direction = new IDSVector3D(dictionary["Direction"].ToString());
            Diameter = (double)dictionary["Diameter"];
            Thickness = (double)dictionary["Thickness"];
            CreationAlgoMethod = (string)dictionary["CreationAlgoMethod"];
            Id = id;
        }

        public object Clone()
        {
            return new DotPastille()
            {
                Location = Location,
                Direction = Direction,
                Diameter = Diameter,
                Thickness = Thickness,
                Screw = (IScrew)Screw?.Clone(),
                Landmark = (Landmark)Landmark?.Clone(),
                CreationAlgoMethod = CreationAlgoMethod,
                Id = Id
            };
        }

        public bool Equals(IDot other)
        {
            if (other is DotPastille pastille)
            {
                var res = true;
                res &= Location.EpsilonEquals(other.Location, 0.001);
                res &= Direction.EpsilonEquals(other.Direction, 0.001);
                res &= Diameter == pastille.Diameter;

                return res;
            }

            return false;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "Class", this.ToString() },
                { "Location", Location.ToString() },
                { "Direction", Direction.ToString() },
                { "Diameter", Diameter },
                { "Thickness", Thickness },
                { "CreationAlgoMethod", CreationAlgoMethod },
            };
        }
    }
}