using IDS.Core.V2.TreeDb.Interface;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Core.V2.Databases
{
    public abstract class GenericLiteDbCollection<TData> : 
        IDbCollection where TData : IData
    {
        private readonly ILiteCollection<TData> _collection;

        public string Name { get; }

        public Type DataType { get; }

        protected GenericLiteDbCollection(LiteDatabase database, string collectionName)
        {
            Name = collectionName;
            DataType = typeof(TData);
            _collection = database.GetCollection<TData>(Name);
        }

        public bool Create(IData data)
        {
            var id = _collection.Insert((TData)data);
            return id.IsGuid && data.Id == id.AsGuid;
        }

        public IData Read(Guid id)
        {
            return _collection.FindOne(x => x.Id == id);
        }

        public IData Delete(Guid id)
        {
            var data = Read(id);
            if (data != null && 
                _collection.Delete(id))
            {
                return data;
            }

            return null;
        }

        public IList<IData> ReadAll()
        {
            return _collection.FindAll().Cast<IData>().ToList();
        }

        protected bool GetIdAndParentsId(BsonValue document, out Guid id, 
            out ImmutableList<Guid> parents)
        {
            id = document[LiteDbKeyConstants.ColIdKey].AsGuid;
            parents = document[LiteDbKeyConstants.ColParentsKey].AsArray
                ?.Select(v => v.AsGuid)
                .ToImmutableList();
            return !(id == Guid.Empty || parents == null);
        }

        protected void SetIdAndParentsId(BsonDocument document, TData data)
        {
            document[LiteDbKeyConstants.ColIdKey] = data.Id;
            document[LiteDbKeyConstants.ColParentsKey] = new BsonArray(
                data.Parents.Select(id =>new BsonValue(id)));
        }

        public abstract void MapObject(IVersion savedVersion);
    }
}
