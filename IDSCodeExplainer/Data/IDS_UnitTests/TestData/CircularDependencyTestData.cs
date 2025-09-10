using System;
using System.Collections.Generic;

namespace IDS.Testing.Data
{
    public class CircularDependencyTestData
    {
        public DummyData A { get; }
        public DummyData B { get; }
        public DummyData C { get; }
        public DummyData D { get; }

        public List<DummyData> AllData =>
            new List<DummyData>()
            {
                A,
                B,
                C,
                D
            };

        /*
         *               A
         *               |
         *           ---------
         *           |       |
         *      -----B       D
         *      |    |
         *      -----C
         */

        public CircularDependencyTestData()
        {
            A = new DummyData("A");
            B = new DummyData("B", new List<Guid>() { A.Id });
            C = new DummyData("C", new List<Guid>() { B.Id });
            B.AddParent(C.Id);
            D = new DummyData("D", new List<Guid>() { A.Id });
        }
    }
}
