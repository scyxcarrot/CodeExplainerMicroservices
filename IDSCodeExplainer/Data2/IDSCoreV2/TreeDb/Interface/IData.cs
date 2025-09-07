using System;
using System.Collections.Immutable;

namespace IDS.Core.V2.TreeDb.Interface
{
    /// <summary>
    /// A data should be immutable once it have been created,
    /// so if any update for the data, it should delete the old data, and trigger the cascading effect,
    /// then only add the new data
    /// </summary>
    public interface IData
    {
        /// <summary>
        /// The Id of the data, must be unique for identify the data
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Parents Id, it use for building trees for cascading effect
        /// </summary>
        ImmutableList<Guid> Parents { get; }
    }
}
