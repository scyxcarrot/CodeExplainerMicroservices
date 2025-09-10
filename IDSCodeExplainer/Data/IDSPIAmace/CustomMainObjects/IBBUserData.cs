using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;

namespace IDS.Common.ImplantBuildingBlocks
{
    /// <summary>
    /// Companion UserData object to attach to each RhinoObject that represents
    /// an Implant Building Block(IBB) in the document.
    /// Used to detect transformations of the object and report
    /// them to our other classes.
    /// </summary>
    /// <seealso cref="Rhino.DocObjects.Custom.UserData" />
    public class IbbUserData : Rhino.DocObjects.Custom.UserData
    {
        /// <summary>
        /// The version major
        /// </summary>
        private const int VersionMajor = 1;

        /// <summary>
        /// The version minor
        /// </summary>
        private const int VersionMinor = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="IbbUserData"/> class.
        /// </summary>
        /// <param name="blockType">Type of the block.</param>
        /// <param name="geom">The geom.</param>
        public IbbUserData(ImplantBuildingBlock blockType, GeometryBase geom)
        {
            this.BlockType = blockType;
            this.CharacteristicValue = ComputeCharacteristicValue(geom);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IbbUserData"/> class.
        /// </summary>
        public IbbUserData()
        {
            // empty
        }

        /// <summary>
        /// Gets the type of the block.
        /// </summary>
        /// <value>
        /// The type of the block.
        /// </value>
        public ImplantBuildingBlock BlockType
        {
            get;
            private set;
        }

        /// <summary>
        /// A value derived from the owner's geometry that is suited
        /// for characterizing it & checking if it has changed.
        /// Currently this is the CRC of the object's area/centroid/
        /// length/degree
        /// </summary>
        /// <value>
        /// The characteristic value.
        /// </value>
        public int CharacteristicValue
        {
            get;
            private set;
        }

        /// <summary>
        /// Descriptive name of the user data.
        /// </summary>
        public override string Description =>
            $"IDS Custom UserData object for RhinoObject representing Implant Building Block of type {BlockType.Name}";

        /// <summary>
        /// If you want to save this user data in a 3dm file, override
        /// ShouldWrite and return true.  If you do support serialization,
        /// you must also override the Read and Write functions.
        /// </summary>
        public override bool ShouldWrite => true;

        /// <summary>
        /// Computes the characteristic value.
        /// </summary>
        /// <param name="geom">The geom.</param>
        /// <returns></returns>
        private static int ComputeCharacteristicValue(GeometryBase geom)
        {
            if (geom is Mesh)
            {
                AreaMassProperties am = AreaMassProperties.Compute((Mesh)geom);
                uint crc = RhinoMath.CRC32((uint)am.Area, am.Centroid.X);
                crc = RhinoMath.CRC32(crc, am.Centroid.Y);
                crc = RhinoMath.CRC32(crc, am.Centroid.Z);
                return (int)crc;
            }

            if (geom is Brep)
            {
                AreaMassProperties am = AreaMassProperties.Compute((Brep)geom);
                uint crc = RhinoMath.CRC32((uint)am.Area, am.Centroid.X);
                crc = RhinoMath.CRC32(crc, am.Centroid.Y);
                crc = RhinoMath.CRC32(crc, am.Centroid.Z);
                return (int)crc;
            }

            if (geom is Surface)
            {
                AreaMassProperties am = AreaMassProperties.Compute((Surface)geom);
                uint crc = RhinoMath.CRC32((uint)am.Area, am.Centroid.X);
                crc = RhinoMath.CRC32(crc, am.Centroid.Y);
                crc = RhinoMath.CRC32(crc, am.Centroid.Z);
                return (int)crc;
            }

            if (geom is Curve)
            {
                Curve curve = (Curve)geom;
                Point3d centroid = curve.ComputeCentroid(0.5);
                uint crc = RhinoMath.CRC32((uint)curve.SpanCount, curve.Degree);
                crc = RhinoMath.CRC32(crc, curve.GetLength());
                crc = RhinoMath.CRC32(crc, centroid.X);
                crc = RhinoMath.CRC32(crc, centroid.Y);
                crc = RhinoMath.CRC32(crc, centroid.Z);
                return (int)crc;
            }

            return -1;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Description;
        }

        /// <summary>
        /// Is called when the object is being duplicated.
        /// </summary>
        /// <param name="source">The source data.</param>
        protected override void OnDuplicate(Rhino.DocObjects.Custom.UserData source)
        {
            var src = source as IbbUserData;
            if (null != src)
            {
                // Copy member properties
                BlockType = src.BlockType;
                CharacteristicValue = src.CharacteristicValue;
            }
        }

        /// <summary>
        /// Reads the content of this data from a stream archive.
        /// </summary>
        /// <param name="archive">An archive.</param>
        /// <returns>
        /// true if the data was successfully written. The default implementation always returns false.
        /// </returns>
        protected override bool Read(Rhino.FileIO.BinaryArchiveReader archive)
        {
            int major;
            int minor;
            archive.Read3dmChunkVersion(out major, out minor);
            if (major == VersionMajor && minor == VersionMinor)
            {
                var blockType = archive.ReadInt();
                BlockType = new ImplantBuildingBlock
                                {
                                    ID = blockType
                                };
                CharacteristicValue = archive.ReadInt();
            }
            return !archive.ReadErrorOccured;
        }

        /// <summary>
        /// Writes the content of this data to a stream archive.
        /// </summary>
        /// <param name="archive">An archive.</param>
        /// <returns>
        /// true if the data was successfully written. The default implementation always returns false.
        /// </returns>
        protected override bool Write(Rhino.FileIO.BinaryArchiveWriter archive)
        {
            archive.Write3dmChunkVersion(VersionMajor, VersionMinor); // version of the chunk containing this object's serialized
            archive.WriteInt(this.BlockType.ID);
            archive.WriteInt(this.CharacteristicValue);
            return !archive.WriteErrorOccured;
        }
    }
}