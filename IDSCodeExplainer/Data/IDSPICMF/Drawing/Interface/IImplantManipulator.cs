using IDS.CMF.DataModel;

namespace IDS.PICMF.Drawing
{
    public interface IImplantManipulator
    {
        void SetBaseImplantData(ImplantDataModelBase dataModel);

        ImplantDataModelBase GetImplantDataModelResult();
    }
}
