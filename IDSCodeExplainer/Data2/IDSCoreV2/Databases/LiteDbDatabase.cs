using IDS.Core.V2.DataModels;
using IDS.Core.V2.TreeDb.Interface;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IDS.Core.V2.Databases
{
    public sealed class LiteDbDatabase: IDatabase
    {
        private bool _disposed;
        private readonly LiteDatabase _liteDb;
        private readonly LiteDbCollectionQuery _collectionQuery;
        private readonly Dictionary<Guid, Type> _cacheDataTypeMapper;
        private readonly string _filePath;
        private readonly MemoryStream _memoryStream;

        public event DataEventHandler OnDeleted;

        private LiteDbDatabase(LiteDatabase liteDb, Assembly[] assemblies)
        {
            _disposed = false;
            _liteDb = liteDb;
            _collectionQuery = new LiteDbCollectionQuery(_liteDb, assemblies);
            _cacheDataTypeMapper = new Dictionary<Guid, Type>();
        }

        public LiteDbDatabase(string databaseConnectionString, Assembly[] assemblies) : 
            this(new LiteDatabase(databaseConnectionString), assemblies)
        {
            var connectionString = new ConnectionString(databaseConnectionString);
            _filePath = connectionString.Filename;
        }

        public LiteDbDatabase(MemoryStream stream, Assembly[] assemblies) : 
            this(new LiteDatabase(stream), assemblies)
        {
            _memoryStream = stream;
        }

        ~LiteDbDatabase()
        {
            try
            {
                Dispose();
            }
            catch (ObjectDisposedException exception)
            {
                // During unit tests, there will be errors here after creating multiple instances of LiteDbDatabase without disposing it properly
            }
        }

        /// <summary>
        /// Save the data from the log file to the main file
        /// </summary>
        public void Save()
        {
            _liteDb.Checkpoint();
        }

        /// <summary>
        /// convert the database in memory stream or file into a byte array to be exported
        /// </summary>
        /// <returns>byte array which can be written into a file to export the database</returns>
        public byte[] GetBytes()
        {
            Save();
            Compression();

            // database in memory stream
            if (_memoryStream != null)
            {
                return _memoryStream.ToArray();
            }

            // database is in a file
            if (!string.IsNullOrEmpty(_filePath))
            {
                var tempPath = Path.GetTempPath();
                var databaseFileName = Path.GetFileName(_filePath);
                var exportFilePath = Path.Combine(
                    tempPath, $"{databaseFileName}_export.db");
                File.Copy(_filePath, exportFilePath);

                var databaseBytes = File.ReadAllBytes(exportFilePath);
                File.Delete(exportFilePath);
                return databaseBytes;
            }

            return null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _collectionQuery.SetMetadata();
                _liteDb.Dispose();
            }
            _disposed = true;
        }

        /// <summary>
        /// Create data in liteDb and store the data Id and type in _cacheDataTypeMapper
        /// </summary>
        /// <param name="data">Data to create in database</param>
        /// <returns>True if data have been successfully created</returns>
        public bool Create(IData data)
        {
            if (_collectionQuery.GetDbCollection(data, out var dbCollection) && dbCollection.Create(data))
            {
                _cacheDataTypeMapper.Add(data.Id, data.GetType());
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get a specific data from LiteDb
        /// </summary>
        /// <param name="id">Guid of the data you want to retrieve</param>
        /// <returns>IData type, cast the data type to extract the values if needed</returns>
        public IData Read(Guid id)
        {
            IData data = null;
            if (_cacheDataTypeMapper.TryGetValue(id, out var dataType) &&
                _collectionQuery.GetDbCollection(dataType, out var dbCollection))
            {
                data = dbCollection.Read(id);
            }
            return data;
        }

        /// <summary>
        /// Delete the data from liteDb and the _cacheDataTypeMapper
        /// </summary>
        /// <param name="id">Guid of the data you want to delete</param>
        /// <returns>IData type of the data deleted</returns>
        public IData Delete(Guid id)
        {
            IData data = null; 
            if (_cacheDataTypeMapper.TryGetValue(id, out var dataType) &&
                _collectionQuery.GetDbCollection(dataType, out var dbCollection))
            {
                data = dbCollection.Delete(id);

                if (OnDeleted != null)
                {
                    OnDeleted(data);
                }
            }
            _cacheDataTypeMapper.Remove(id);
            return data;
        }

        public IList<IData> ReadAll()
        {
            var allData = _collectionQuery.GetAllDbCollection()
                .SelectMany(c => c.ReadAll())
                .ToList();
            
            _cacheDataTypeMapper.Clear();
            allData.ForEach(d => _cacheDataTypeMapper.Add(d.Id, d.GetType()));
            return allData;
        }

        public void Compression()
        {
            _liteDb.Rebuild();
        }

        public IMetadata GetMetadata()
        {
            return _collectionQuery.GetMetadata();
        }

        public IVersion GetCurrentVersion(Guid id)
        {
            DbCollectionVersion version = null;
            if (_cacheDataTypeMapper.TryGetValue(id, out var dataType))
            {
                version = _collectionQuery.GetCurrentVersion(dataType);
            }
            return version;
        }

        public IVersion GetSavedVersion(Guid id)
        {
            DbCollectionVersion version = null;
            if (_cacheDataTypeMapper.TryGetValue(id, out var dataType))
            {
                version = _collectionQuery.GetSavedVersion(dataType);
            }
            return version;
        }
    }
}
