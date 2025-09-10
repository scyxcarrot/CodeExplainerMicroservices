using IDS.CMF.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.TestLib.Components
{
    public class ImplantDataModelComponent
    {
        public Guid CaseGuid { get; set; } = Guid.Empty;

        public List<DotComponent> DotList { get; set; } = new List<DotComponent>();

        public List<ConnectionComponent> ConnectionList { get; set; } = new List<ConnectionComponent>();

        public void SetImplantDataModel(Guid caseGuid, ImplantDataModel implantDataModel)
        {
            CaseGuid = caseGuid;

            var dotList = implantDataModel.DotList;
            var connectionList = implantDataModel.ConnectionList;

            DotList.Clear();
            ConnectionList.Clear();

            foreach (var dot in dotList)
            {
                var dotComponent = new DotComponent();
                dotComponent.SetDot(dot);
                DotList.Add(dotComponent);
            }

            foreach (var connection in connectionList)
            {
                var connectionComponent = new ConnectionComponent();
                connectionComponent.SetConnection(connection, dotList);
                ConnectionList.Add(connectionComponent);
            }
        }

        public ImplantDataModel GetImplantDataModel()
        {
            var dotList =  DotList.Select(d => d.GetDot()).ToList();
            var connection = ConnectionList.Select(c => c.GetConnection(dotList)).ToList();
            return new ImplantDataModel(connection);
        }

    }
}
