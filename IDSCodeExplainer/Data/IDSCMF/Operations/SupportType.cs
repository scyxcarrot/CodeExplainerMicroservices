namespace IDS.CMF.Operations
{
    /// <summary>
    /// Represents the type of support used for implant creation
    /// </summary>
    public enum SupportType
    {
        None,
        ImplantSupport,
        PatchSupport,
        Both // When both types are present (conflicting state)
    }
}
