using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.CMF.Factory
{
    public class PlanningImplantBrepFactory
    {
        public Brep CreateImplant(ImplantDataModel dataModel)
        {
            var breps = new List<Brep>();

            var pastilleBrepFactory = new PastilleBrepFactory();
            foreach (var dot in dataModel.DotList)
            {
                var pastille = dot as DotPastille;
                if (pastille == null)
                {
                    continue;
                }
                var direction = DataModelUtilities.GetAverageDirection(dataModel.ConnectionList, pastille);
                breps.Add(pastilleBrepFactory.CreatePastille(pastille, direction));
            }

            foreach (var line in dataModel.ConnectionList)
            {
                breps.Add(ConnectionBrepFactory.CreateConnection(line));
            }
            
            return BrepUtilities.Append(breps.ToArray());
        }

        public Brep CreateImplantRoiDefinition(ImplantDataModel dataModel, double dotDimension, double connectionDimension)
        {
            var breps = new List<Brep>();

            var pastilleBrepFactory = new PastilleBrepFactory();
            foreach (var dot in dataModel.DotList)
            {
                var pastille = dot as DotPastille;
                if (pastille == null)
                {
                    continue;
                }
                var direction = DataModelUtilities.GetAverageDirection(dataModel.ConnectionList, pastille);
                var adjustedLocation = Point3d.Add(RhinoPoint3dConverter.ToPoint3d(pastille.Location), Vector3d.Multiply(direction, -dotDimension / 2));
                breps.Add(pastilleBrepFactory.CreatePastille(pastille, direction, adjustedLocation, dotDimension, dotDimension));
            }

            foreach (var line in dataModel.ConnectionList)
            {
                var connectionWidth = line.Width > connectionDimension ? line.Width : connectionDimension;
                var connectionTubeDiameter = connectionWidth + Constants.ImplantCreation.RoIAreaRadiusOffsetModifier;
                breps.Add(ConnectionBrepFactory.CreateConnection(line, connectionDimension, connectionTubeDiameter, true));
            }

            return BrepUtilities.Append(breps.ToArray());
        }
    }
}
