using IDS.Core.V2.DataModels;
using IDS.Core.V2.Reflections;
using IDS.Core.V2.TreeDb.Interface;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IDS.Core.V2.Databases
{
    public class LiteDbCollectionQuery
    {
        private class DbCollectionAndInfo
        {
            public IDbCollection DbCollection { get; }

            public DbCollectionVersion Version { get; }

            public DbCollectionVersion SavedVersion { get; }

            internal DbCollectionAndInfo(IDbCollection dbCollection, DbCollectionVersion version, DbCollectionVersion savedVersion)
            {
                DbCollection = dbCollection;
                Version = version;
                SavedVersion = savedVersion;
            }
        }

        private readonly Dictionary<Type, DbCollectionAndInfo> _queryMapper;

        private readonly ILiteCollection<DbComponentMetadata> _metadataCollection;

        public LiteDbCollectionQuery(LiteDatabase liteDb, Assembly[] assemblies)
        {
            _queryMapper = new Dictionary<Type, DbCollectionAndInfo>();
            _metadataCollection = liteDb.GetCollection<DbComponentMetadata>(DbCollectionNameConstants.MetadataCollectionName);
            var metadata = GetMetadata();

            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (CollectionAttributeReader.Read(type, out var collectionVersion, out var collectionName) &&
                            type.GetInterfaces().Contains(typeof(IDbCollection)))
                        {
                            var dbCollection = (IDbCollection)Activator.CreateInstance(type, liteDb, collectionName);

                            var savedVersion = collectionVersion;

                            if (metadata != null && metadata.KeyValuePairs.ContainsKey(collectionName))
                            {
                                if (!DbCollectionVersion.TryParse(metadata.KeyValuePairs[collectionName], out savedVersion))
                                {
                                    savedVersion = collectionVersion;
                                }
                            }
                            
                            _queryMapper.Add(dbCollection.DataType,
                                new DbCollectionAndInfo(dbCollection, collectionVersion, savedVersion));

                            dbCollection.MapObject(savedVersion);
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Skip this exception since it is normal for some dependencies references in C#
                }
            }

            if (_metadataCollection.Count() == 0)
            {
                SetMetadata();
            }
        }

        public bool GetDbCollection(IData data, out IDbCollection dbCollection)
        {
            var dataType = data.GetType();
            return GetDbCollection(dataType, out dbCollection);
        }

        public bool GetDbCollection(Type dataType, out IDbCollection dbCollection)
        {
            var success = _queryMapper.TryGetValue(dataType, out var dbCollectionInfo);
            dbCollection = success? dbCollectionInfo.DbCollection : null;
            return success;
        }

        public IEnumerable<IDbCollection> GetAllDbCollection()
        {
            return _queryMapper.Values.Select(i => i.DbCollection);
        }

        public IMetadata GetMetadata()
        {
            return _metadataCollection.FindAll().FirstOrDefault();
        }

        public void SetMetadata()
        {
            _metadataCollection.DeleteAll();

            var keyValuePairs = new Dictionary<string, string>();

            if (CollectionAttributeReader.Read(typeof(DbComponentMetadata), out var collectionVersion, out _))
            {
                keyValuePairs.Add("IDS", collectionVersion.ToString());
            }
            else
            {
                throw new Exception("Unable to retrieve IDS version");
            }

            foreach (var collectionAndInfo in _queryMapper.Values)
            {
                keyValuePairs.Add(collectionAndInfo.DbCollection.Name, collectionAndInfo.Version.ToString());
            }

            _metadataCollection.Insert(new DbComponentMetadata
            {
                KeyValuePairs = keyValuePairs
            });
        }

        public DbCollectionVersion GetCurrentVersion(Type dataType)
        {
            var success = _queryMapper.TryGetValue(dataType, out var dbCollectionInfo);
            return success ? dbCollectionInfo.Version : null;
        }

        public DbCollectionVersion GetSavedVersion(Type dataType)
        {
            var success = _queryMapper.TryGetValue(dataType, out var dbCollectionInfo);
            return success ? dbCollectionInfo.SavedVersion : null;
        }
    }
}
