using IDS.CMF.DataModel;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace IDS.CMF.TestLib.Components
{
    public class LandmarkComponent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LandmarkType LandmarkType { get; set; }

        public IDSPoint3D Point { get; set; }
        
        public Guid Id { get; set; }

        public void SetLandmark(Landmark landmark)
        {
            LandmarkType = landmark.LandmarkType;
            Point = new IDSPoint3D(landmark.Point);
            Id = landmark.Id;
        }

        public Landmark GetLandmark()
        {
            return new Landmark()
            {
                Id = Id,
                LandmarkType = LandmarkType,
                Point = new IDSPoint3D(Point)
            };
        }
    }
}
