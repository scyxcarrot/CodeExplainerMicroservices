using IDS.CMF.Factory;
using IDS.CMF.V2.DataModel;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using Rhino.Collections;
using System;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public static class DotSerializerHelper
    {
        public static ArchivableDictionary CreateArchive(IDot dot)
        {
            if (dot is DotPastille pastille)
            {
                return DotPastilleSerializer.Serialize(pastille);
            }
            else if (dot is DotControlPoint point)
            {
                return DotControlPointSerializer.Serialize(point);
            }

            return null;
        }
    }

    public static class ConnectionPlateSerializer
    {
        private const string SerializationLabel = "ConnectionPlate";
        private const string KeyA = "A";
        private const string KeyB = "B";
        private const string KeyId = "Id";
        private const string KeyThickness = "Thickness";
        private const string KeyWidth = "Width";
        private const string KeyIsSynchronizable = "IsSynchronizable";

        public static ArchivableDictionary Serialize(ConnectionPlate conn)
        {
            var success = true;
            var serializer = new ArchivableDictionary();
            success &= serializer.Set(IDS.CMF.Constants.Serialization.KeySerializationLabel, SerializationLabel);
            success &= serializer.Set(KeyA, DotSerializerHelper.CreateArchive(conn.A));
            success &= serializer.Set(KeyB, DotSerializerHelper.CreateArchive(conn.B));
            success &= serializer.Set(KeyId, conn.Id);
            success &= serializer.Set(KeyThickness, conn.Thickness);
            success &= serializer.Set(KeyWidth, conn.Width);
            success &= serializer.Set(KeyIsSynchronizable, conn.IsSynchronizable);
            success &= serializer.Set(KeyId, conn.Id);

            return success ? serializer : null;
        }

        public static ConnectionPlate DeSerialize(ArchivableDictionary serializer)
        {
            try
            {
                var conn = new ConnectionPlate
                {
                    SerializationLabel =
                        RhinoIOUtilities.GetStringValue(serializer, IDS.CMF.Constants.Serialization.KeySerializationLabel),
                    A = SerializationFactory.DeSerializeIDot((ArchivableDictionary)serializer[KeyA]),
                    B = SerializationFactory.DeSerializeIDot((ArchivableDictionary)serializer[KeyB]),
                    Id = serializer.GetGuid(KeyId, Guid.NewGuid()),
                    Thickness = serializer.GetDouble(KeyThickness),
                    Width = serializer.GetDouble(KeyWidth),

                    /** 
                        Added check for backward compatibility
                        Note: Cases which does not have this flag in the serializer would be set to true by default.
                        In other words, cases will have it's width synced to the case preference panel before 
                        overriding the individual plate width.
                    **/
                    IsSynchronizable = serializer.GetBool(KeyIsSynchronizable, true),
                };

                return conn;
            }
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                return null;
            }
        }
    }

    public static class ConnectionLinkSerializer
    {
        public static string SerializationLabel => "ConnectionLink";
        public static string KeyA => "A";
        public static string KeyB => "B";
        public static string KeyId => "Id";
        public static string KeyThickness => "Thickness";
        public static string KeyWidth => "Width";
        public static string KeyIsSynchronizable => "IsSynchronizable";

        public static ArchivableDictionary Serialize(ConnectionLink conn)
        {
            var success = true;
            var serializer = new ArchivableDictionary();
            success &= serializer.Set(IDS.CMF.Constants.Serialization.KeySerializationLabel, SerializationLabel);
            success &= serializer.Set(KeyA, DotSerializerHelper.CreateArchive(conn.A));
            success &= serializer.Set(KeyB, DotSerializerHelper.CreateArchive(conn.B));
            success &= serializer.Set(KeyId, conn.Id);
            success &= serializer.Set(KeyThickness, conn.Thickness);
            success &= serializer.Set(KeyWidth, conn.Width);
            success &= serializer.Set(KeyIsSynchronizable, conn.IsSynchronizable);
            success &= serializer.Set(KeyId, conn.Id);

            return success ? serializer : null;
        }

        public static ConnectionLink DeSerialize(ArchivableDictionary serializer)
        {
            try
            {
                var conn = new ConnectionLink
                {
                    SerializationLabel =
                        RhinoIOUtilities.GetStringValue(serializer, IDS.CMF.Constants.Serialization.KeySerializationLabel),
                    A = SerializationFactory.DeSerializeIDot((ArchivableDictionary)serializer[KeyA]),
                    B = SerializationFactory.DeSerializeIDot((ArchivableDictionary)serializer[KeyB]),
                    Id = serializer.GetGuid(KeyId, Guid.NewGuid()),
                    Thickness = serializer.GetDouble(KeyThickness),
                    Width = serializer.GetDouble(KeyWidth),

                    /** 
                        Added check for backward compatibility
                        Note: Cases which does not have this flag in the serializer would be set to true by default.
                        In other words, cases will have it's width synced to the case preference panel before 
                        overriding the individual plate width.
                    **/
                    IsSynchronizable = serializer.GetBool(KeyIsSynchronizable, true),
                };

                return conn;
            }
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                return null;
            }
        }
    }

    public static class DotPastilleSerializer
    {
        public const string SerializationLabel = "DotPastille";
        public const  string KeyLocation = "Location";
        public const string KeyDirection = "Direction";
        public const string KeyDiameter = "Diameter";
        public const string KeyThickness = "Thickness";
        public const string KeyScrew = "Screw";
        public const string KeyLandmark = "Landmark";
        public const string KeyCreationAlgoMethod = "CreationAlgoMethod";
        public const string KeyId = "Id";

        public static ArchivableDictionary Serialize(DotPastille dot)
        {
            var success = true;
            var serializer = new ArchivableDictionary();
            serializer.Set(IDS.CMF.Constants.Serialization.KeySerializationLabel, SerializationLabel);

            var locationArc = Point3DSerializer.Serialize((IDSPoint3D)dot.Location);
            success &= locationArc != null;
            success &= serializer.Set(KeyLocation, locationArc);

            var directionArc = Vector3DSerializer.Serialize((IDSVector3D)dot.Direction);
            success &= directionArc != null;
            success &= serializer.Set(KeyDirection, directionArc);

            serializer.Set(KeyDiameter, dot.Diameter);
            serializer.Set(KeyThickness, dot.Thickness);
            serializer.Set(KeyCreationAlgoMethod, dot.CreationAlgoMethod);
            serializer.Set(KeyId, dot.Id);

            //There can be there or not there
            if (dot.Screw != null)
            {
                var screw = ScrewDataSerializer.Serialize((ScrewData)dot.Screw);
                if (screw == null)
                {
                    success = false;
                }
                else
                {
                    serializer.Set(KeyScrew, screw);
                }
            }

            //There can be there or not there
            if (dot.Landmark != null)
            {
                
                var landmark = LandmarkSerializer.Serialize(dot.Landmark);
                if (landmark == null)
                {
                    success = false;
                }
                else
                {
                    serializer.Set(KeyLandmark, landmark);
                }
            }

            return success ? serializer : null;
        }

        public static DotPastille DeSerialize(ArchivableDictionary serializer)
        {
            try
            {
                var dot = new DotPastille
                {
                    SerializationLabel = RhinoIOUtilities.GetStringValue(serializer, IDS.CMF.Constants.Serialization.KeySerializationLabel)
                };

                var loc = SerializationFactory.DeSerializeIPoint3D((ArchivableDictionary)serializer[KeyLocation]);
                var dir = SerializationFactory.DeSerializeIVector3D((ArchivableDictionary)serializer[KeyDirection]);

                if (dir == null || loc == null)
                {
                    return null;
                }

                dot.Location = loc;
                dot.Direction = dir;

                dot.Diameter = serializer.GetDouble(KeyDiameter);
                dot.Thickness = serializer.GetDouble(KeyThickness);

                if (serializer.ContainsKey(KeyCreationAlgoMethod))
                {
                    dot.CreationAlgoMethod = serializer.GetString(KeyCreationAlgoMethod);
                }

                if (serializer.ContainsKey(KeyScrew))
                {
                    dot.Screw = SerializationFactory.DeSerializeScrew((ArchivableDictionary)serializer[KeyScrew]);
                }

                if (serializer.ContainsKey(KeyLandmark))
                {
                    dot.Landmark = SerializationFactory.DeSerializeLandmark((ArchivableDictionary)serializer[KeyLandmark]);
                }

                if (serializer.ContainsKey(KeyId))
                {
                    dot.Id = serializer.GetGuid(KeyId);
                }
                else
                {
                    dot.Id = Guid.NewGuid();
                }

                return dot;
            }
            catch
            {
                return null;
            }
        }
    }

    public static class DotControlPointSerializer
    {

        private const string SerializationLabel = "DotControlPoint";
        private const string KeyLocation = "Location";
        private const string KeyDirection = "Direction";
        public const string KeyId = "Id";

        public static ArchivableDictionary Serialize(DotControlPoint dot)
        {
            var success = true;
            var serializer = new ArchivableDictionary();
            success &= serializer.Set(IDS.CMF.Constants.Serialization.KeySerializationLabel, SerializationLabel);

            var locationArc = Point3DSerializer.Serialize((IDSPoint3D)dot.Location);
            success &= locationArc != null;
            success &= serializer.Set(KeyLocation, locationArc);


            var directionArc = Vector3DSerializer.Serialize((IDSVector3D)dot.Direction);
            success &= directionArc != null;
            success &= serializer.Set(KeyDirection, directionArc);

            serializer.Set(KeyId, dot.Id);

            return success ? serializer : null;
        }

        public static DotControlPoint DeSerialize(ArchivableDictionary serializer)
        {
            try
            {
                var dot = new DotControlPoint
                {
                    SerializationLabel = RhinoIOUtilities.GetStringValue(serializer, IDS.CMF.Constants.Serialization.KeySerializationLabel)
                };

                var loc = SerializationFactory.DeSerializeIPoint3D((ArchivableDictionary)serializer[KeyLocation]);
                var dir = SerializationFactory.DeSerializeIVector3D((ArchivableDictionary)serializer[KeyDirection]);

                if (dir == null || loc == null)
                {
                    return null;
                }

                dot.Location = loc;
                dot.Direction = dir;

                if (serializer.ContainsKey(KeyId))
                {
                    dot.Id = serializer.GetGuid(KeyId);
                }
                else
                {
                    dot.Id = Guid.NewGuid();
                }

                return dot;
            }
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                return null;
            }
        }
    }

    public static class Point3DSerializer
    {
        public static string SerializationLabel => "Point3D";

        private static string KeyX = "X";
        private static string KeyY = "Y";
        private static string KeyZ = "Z";

        public static ArchivableDictionary Serialize(IDSPoint3D pt)
        {
            var success = true;
            var serializer = new ArchivableDictionary();

            success &= serializer.Set(IDS.CMF.Constants.Serialization.KeySerializationLabel, SerializationLabel);
            success &= serializer.Set(KeyX, pt.X);
            success &= serializer.Set(KeyY, pt.Y);
            success &= serializer.Set(KeyZ, pt.Z);

            return success ? serializer : null;
        }

        public static IDSPoint3D DeSerialize(ArchivableDictionary serializer)
        {
            var pt = new IDSPoint3D
            {
                SerializationLabel = RhinoIOUtilities.GetStringValue(serializer, IDS.CMF.Constants.Serialization.KeySerializationLabel),
                X = serializer.GetDouble(KeyX),
                Y = serializer.GetDouble(KeyY),
                Z = serializer.GetDouble(KeyZ)
            };
            return pt;
        }
    }

    public static class Vector3DSerializer
    {
        public static string SerializationLabel => "Vector3D";

        private static string KeyX = "X";
        private static string KeyY = "Y";
        private static string KeyZ = "Z";

        public static ArchivableDictionary Serialize(IDSVector3D vec)
        {
            var success = true;
            var serializer = new ArchivableDictionary();

            success &= serializer.Set(IDS.CMF.Constants.Serialization.KeySerializationLabel, SerializationLabel);
            success &= serializer.Set(KeyX, vec.X);
            success &= serializer.Set(KeyY, vec.Y);
            success &= serializer.Set(KeyZ, vec.Z);

            return success ? serializer : null;
        }

        public static IDSVector3D Deserialize(ArchivableDictionary serializer)
        {
            var vec = new IDSVector3D
            {
                SerializationLabel = RhinoIOUtilities.GetStringValue(serializer, IDS.CMF.Constants.Serialization.KeySerializationLabel),
                X = serializer.GetDouble(KeyX),
                Y = serializer.GetDouble(KeyY),
                Z = serializer.GetDouble(KeyZ)
            };
            return vec;
        }
    }

    public static class LandmarkSerializer
    {
        private const string SerializationLabel = "Landmark";
        private const string KeyPoint = "Point";
        private const string KeyLandmarkType = "LandmarkType";
        private const string KeyId = "Id";

        public static ArchivableDictionary Serialize(Landmark lnd)
        {
            var success = true;
            var serializer = new ArchivableDictionary();

            success &= serializer.Set(IDS.CMF.Constants.Serialization.KeySerializationLabel, SerializationLabel);

            var locationArc = Point3DSerializer.Serialize((IDSPoint3D)lnd.Point);
            success &= locationArc != null;
            success &= serializer.Set(KeyPoint, locationArc);

            success &= serializer.SetEnumValue(KeyLandmarkType, lnd.LandmarkType);
            success &= serializer.Set(KeyId, lnd.Id);

            return success ? serializer : null;
        }

        public static Landmark Deserialize(ArchivableDictionary serializer)
        {
            var landmark = new Landmark();
            landmark.SerializationLabel = RhinoIOUtilities.GetStringValue(serializer, IDS.CMF.Constants.Serialization.KeySerializationLabel);

            var point = SerializationFactory.DeSerializeIPoint3D((ArchivableDictionary)serializer[KeyPoint]);
            if (point == null)
            {
                return null;
            }

            landmark.Point = point;
            landmark.LandmarkType = serializer.GetEnumValue<LandmarkType>(KeyLandmarkType);
            landmark.Id = serializer.GetGuid(KeyId);

            return landmark;
        }
    }

    public static class ScrewDataSerializer
    {
        private const string SerializationLabel = "ScrewData";
        private const string KeyId = "Id";

        public static ArchivableDictionary Serialize(ScrewData screw)
        {
            var success = true;
            var serializer = new ArchivableDictionary();

            success &= serializer.Set(IDS.CMF.Constants.Serialization.KeySerializationLabel, SerializationLabel);

            serializer.Set(KeyId, screw.Id);

            return success ? serializer : null;
        }

        public static ScrewData Deserialize(ArchivableDictionary serializer)
        {
            var screwData = new ScrewData();
            screwData.SerializationLabel = RhinoIOUtilities.GetStringValue(serializer, IDS.CMF.Constants.Serialization.KeySerializationLabel);

            screwData.Id = serializer.GetGuid(KeyId);

            return screwData;
        }
    }

}
