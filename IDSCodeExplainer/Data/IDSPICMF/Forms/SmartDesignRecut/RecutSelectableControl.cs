namespace IDS.PICMF.Forms
{
    public delegate void OnSelectPartDelegate(PartSelectionViewModel data);

    public interface ISelectableControl
    {
        OnSelectPartDelegate OnSelectPartEventHandler { get; set; }

        void CleanUp();
    }
}
