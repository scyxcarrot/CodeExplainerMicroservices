namespace IDS.Core.V2.TreeDb.Interface
{
    /// <summary>
    /// Version contains Major, Minor, Patch information
    /// </summary>
    public interface IVersion
    {
        uint Major { get; }

        uint Minor { get; }

        uint Patch { get; }

        bool NeedBackwardCompatible(IVersion savedVersion);
    }
}
