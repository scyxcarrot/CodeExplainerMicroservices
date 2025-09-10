using IDS.Interface.Implant;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.DataModel
{
    public class ImplantDataModel : ImplantDataModelBase, ICloneable
    {
        public new List<IDot> DotList
        {
            get
            {
                return ConnectionList.Select(line => line.A).Union(ConnectionList.Select(line => line.B)).Distinct().ToList();
            }
        }

        public ImplantDataModel()
        {

        }

        public ImplantDataModel(IEnumerable<IConnection> lineList)
        {
            ConnectionList = lineList.ToList();
        }

        public bool IsHasConstruction()
        {
            return ConnectionList.Any();
        }

        public void Update(List<IConnection> lineList)
        {
            ConnectionList = lineList;
        }

        public object Clone()
        {
            var clonedConnectionList = new List<IConnection>();

            //For any same location dots is actually the same dot.
            ConnectionList.ForEach(x =>
            {
                var cloned = (IConnection) x.Clone();

                clonedConnectionList.ForEach(y =>
                {
                    if (cloned.A.Location.EpsilonEquals(y.A.Location, 0.0001))
                    {
                        cloned.A = y.A;
                    }

                    if (cloned.A.Location.EpsilonEquals(y.B.Location, 0.0001))
                    {
                        cloned.A = y.B;
                    }

                    if (cloned.B.Location.EpsilonEquals(y.A.Location, 0.0001))
                    {
                        cloned.B = y.A;
                    }

                    if (cloned.B.Location.EpsilonEquals(y.B.Location, 0.0001))
                    {
                        cloned.B = y.B;
                    }
                });

                clonedConnectionList.Add(cloned);
            });

            return new ImplantDataModel(clonedConnectionList);
        }
    }
}
