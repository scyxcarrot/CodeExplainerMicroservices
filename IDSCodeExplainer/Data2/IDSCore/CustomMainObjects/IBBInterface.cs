using IDS.Core.ImplantDirector;
using Rhino.Collections;

namespace IDS.Core.ImplantBuildingBlocks
{
    /// <summary>
    /// Methods that all custom Building blocks must implement
    /// </summary>
    public interface IBBinterface<T> where T : IImplantDirector
    {
        /// <summary>
        /// Serialize member variables to user dictionary.
        /// </summary>
        void PrepareForArchiving();


        /// <summary>
        /// De-serialize member variables from archive.
        /// </summary>
        /// <param name="userDict">The user dictionary.</param>
        void DeArchive(ArchivableDictionary userDict);

        /// <summary>
        /// Gets or sets the director.
        /// </summary>
        /// <value>
        /// The director.
        /// </value>
        T Director
        {
            get;
            set;
        }
    }
}