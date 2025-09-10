namespace IDS.Core.V2.TreeDb.Interface
{
    /// <summary>
    /// Command design pattern for support undo/redo for database operation
    /// </summary>
    public interface IDbCommand
    {
        /// <summary>
        /// Execute the command
        /// </summary>
        /// <returns>True if successfully execute</returns>
        bool Execute();

        /// <summary>
        /// Undo the command
        /// </summary>
        void Undo();
    }
}
