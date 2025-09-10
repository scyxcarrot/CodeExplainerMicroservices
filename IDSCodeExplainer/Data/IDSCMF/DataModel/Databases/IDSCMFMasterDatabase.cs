namespace IDS.CMF.DataModel
{
    // Currently this class is only use for save/load
    // TODO: Added undo/redo to make it
    // TODO: Added more type into IDSCoreV2 and IDSCMFV2
    // So it able to replace RhinoDoc and implant director in future
    public class IDSCMFMasterDatabase
    {
        public ImplantScrewQcDatabase ImplantScrewQcDatabase { get; set; } = new ImplantScrewQcDatabase();
    }
}
