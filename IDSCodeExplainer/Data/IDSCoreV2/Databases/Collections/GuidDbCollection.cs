using IDS.Core.V2.DataModels;
using IDS.Core.V2.Reflections;
using IDS.Core.V2.TreeDb.Interface;
using LiteDB;

namespace IDS.Core.V2.Databases
{
    [DbCollectionName(DbCollectionNameConstants.GuidCollectionName)]
    [DbCollectionVersion(0, 0, 0)]
    public class GuidDbCollection : GenericLiteDbCollection<GuidValueData>
    {
        public GuidDbCollection(LiteDatabase database, string collectionName) :
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
                        [LiteDbKeyConstants.ColValueKey] = value.Value
                    };
                    SetIdAndParentsId(doc, value);
                    return doc;
                },
                deserialize: (bson) => GetIdAndParentsId(bson, out var id, out var parents) ? new GuidValueData(id, parents, bson[LiteDbKeyConstants.ColValueKey].AsGuid) : null
            );
        }
    }
}
