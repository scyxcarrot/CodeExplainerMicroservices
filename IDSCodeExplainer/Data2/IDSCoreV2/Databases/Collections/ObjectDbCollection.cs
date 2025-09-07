using IDS.Core.V2.DataModels;
using IDS.Core.V2.Reflections;
using IDS.Core.V2.TreeDb.Interface;
using LiteDB;
using System.Linq;

namespace IDS.Core.V2.Databases
{
    [DbCollectionName(DbCollectionNameConstants.ObjectCollectionName)]
    [DbCollectionVersion(0, 0, 0)]
    public class ObjectDbCollection : GenericLiteDbCollection<ObjectValueData>
    {
        public const string ColAttributesKey = "Attributes";

        public ObjectDbCollection(LiteDatabase database, string collectionName) :
            base(database, collectionName)
        {
        }

        public override void MapObject(IVersion savedVersion)
        {
            BsonMapper.Global.RegisterType
            (
                serialize: (value) =>
                {
                    var doc = new BsonDocument
                    {
                        [ColAttributesKey] = new BsonDocument(value.Value.Attributes.ToDictionary(attr => attr.Key, attr => new BsonValue(attr.Value)))
                    };
                    SetIdAndParentsId(doc, value);
                    return doc;
                },
                deserialize: (bson) => GetIdAndParentsId(bson, out var id, out var parents) ? new ObjectValueData(id, parents, Deserialize(bson)) : null
            );
        }

        private ObjectValue Deserialize(BsonValue bson)
        {
            var document = bson[ColAttributesKey].AsDocument;

            return new ObjectValue
            {
                Attributes = document.ToDictionary(attr => attr.Key, attr => (object)attr.Value)
            };
        }
    }
}
