using IDS.Core.V2.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace IDS.CMF.V2.ExternalTools
{
    public struct CoordinateSystemJson
    {
        public string Part { get; set; }
        public double[] Matrix { get; set; }
    }

    public class CoordinateSystemJsonParser
    {
        public List<CoordinateSystemJson> LoadCoordinateSystems(string filePath)
        {
            var jsonText = File.ReadAllText(filePath);

            var coordinateSystems = new List<CoordinateSystemJson>();

            var coords = JsonConvert.DeserializeObject<JObject>(jsonText);
            foreach (var coord in coords)
            {
                coordinateSystems.Add(new CoordinateSystemJson
                {
                    Part = coord.Key,
                    Matrix = ParserUtilities.GetMatrix(coord.Value.ToString())
                });
            }

            return coordinateSystems;
        }
    }
}
