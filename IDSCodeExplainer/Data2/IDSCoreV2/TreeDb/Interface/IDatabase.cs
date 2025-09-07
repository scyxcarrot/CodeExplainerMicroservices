using System;
using System.Collections.Generic;

namespace IDS.Core.V2.TreeDb.Interface
{
    /// <summary>
    /// Interface for lose couple for database, so it can be easier to migrate to new database
    /// </summary>
    public interface IDatabase : IDisposable
    {
        #region CRUD

        /// <summary>
        /// Create data in database, and it able to undo/redo
        /// </summary>
        /// <param name="data">Data to create in database</param>
        /// <returns>True if data have been successfully created</returns>
        bool Create(IData data);

        /// <summary>
        /// Read specific type of data from database
        /// </summary>
        /// <param name="id">Targeted data ID</param>
        /// <returns>Targeted data</returns>
        IData Read(Guid id);

        // Update = Delete + Create

        /// <summary>
        /// Delete the data from the database, and it able to undo/redo
        /// </summary>
        /// <param name="id">Targeted data ID</param>
        /// <returns>Targeted deleted data</returns>

        IData Delete(Guid id);
        #endregion

        /// <summary>
        /// Read all data in database to build the tree when initial the software
        /// </summary>
        /// <returns>All data in database</returns>
        IList<IData> ReadAll();

        /// <summary>
        /// Compress the database(Most database will not compress data by default due to performance)
        /// </summary>
        void Compression();

        /// <summary>
        /// Get the metadata from database
        /// </summary>
        /// <returns>Metadata of data stored in database</returns>
        IMetadata GetMetadata();

        /// <summary>
        /// Get current version of data
        /// </summary>
        /// <returns>IVersion of data</returns>
        IVersion GetCurrentVersion(Guid id);

        /// <summary>
        /// Get saved version of data
        /// </summary>
        /// <returns>IVersion of data</returns>
        IVersion GetSavedVersion(Guid id);

        void Save();

        byte[] GetBytes();

        event DataEventHandler OnDeleted;
    }

    public delegate void DataEventHandler(IData data);
}
