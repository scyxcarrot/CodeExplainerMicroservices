using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Interface.Implant;
using IDS.PICMF.Drawing;
using System.Linq;

namespace IDS.PICMF.DrawingAction
{
    public class AddDotAction : IUndoableAction
    {
        private int selectedIndex;
        private bool newDot;

        public IDot DotToAdd { get; set; }
        public IConnection AddedConnection { get; private set; }

        public bool Do(DrawImplantBaseState state)
        {
            selectedIndex = state.SelectedIndex;

            if (state.DataModelBase.DotList.Any())
            {
                AddedConnection = HandleIndicateConnectionCreation(state, DotToAdd);
                if (AddedConnection == null)
                {
                    return false;
                }
            }

            if (!state.DataModelBase.DotList.Contains(DotToAdd))
            {
                newDot = true;
                state.DataModelBase.DotList.Add(DotToAdd);
            }
            else
            {
                //This method will only return true when the dot to be added already exist.
                //Meaning, the intention of adding the dot is to add a connection to complete a cycle
                newDot = false;
                return true;
            }
            
            return false;
        }

        public bool Undo(DrawImplantBaseState state)
        {
            if (AddedConnection != null)
            {
                if (newDot)
                {
                    state.DataModelBase.DotList.Remove(DotToAdd);
                }
                state.DataModelBase.ConnectionList.Remove(AddedConnection);
                state.SelectedIndex = selectedIndex;
            }

            return true;
        }

        protected IConnection HandleIndicateConnectionCreation(DrawImplantBaseState state, IDot dot)
        {
            var dotToConnect = state.SelectedIndex != -1
                ? state.DataModelBase.DotList[selectedIndex]
                : state.DataModelBase.DotList.Last();

            if (dotToConnect.Location.EpsilonEquals(dot.Location, 0.0001))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Same Position!");
                return null;
            }

            var width = state.CreatePlate ? state.PlateWidth : state.LinkWidth;
            var conn = ImplantCreationUtilities.CreateConnection(dotToConnect, dot, state.ConnectionThickness, width, state.CreatePlate);

            //Prevent from creating on any existing connections
            if (!state.DataModelBase.ConnectionList.Any(x => DataModelUtilities.IsConnectionEquivalent(x, conn)))
            {
                state.DataModelBase.ConnectionList.Add(conn);
                state.SelectedIndex = -1;
                return conn;
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Same connection!");
                return null;
            }
        }
    }
}
