using System;
using System.Collections.Generic;

namespace IDS.Testing.Data
{
    public class ManyToOneTestData
    {
        public DummyData A { get; }
        public DummyData B { get; }
        public DummyData C { get; }
        public DummyData D { get; }
        public DummyData E { get; }
        public DummyData F { get; }

        public List<DummyData> AllData =>
            new List<DummyData>()
            {
                A,
                B,
                C,
                D,
                E,
                F
            };

        /*
         *
         *             A
         *             |
         *      ----------------
         *      |      |       |
         *      B      C       D
         *       \    /  \    /
         *        \  /    \  /
         *          E       F
         *
         */
        public ManyToOneTestData()
        {

            A = new DummyData("A");
            B = new DummyData("B", new List<Guid>() { A.Id });
            C = new DummyData("C", new List<Guid>() { A.Id });
            D = new DummyData("D", new List<Guid>() { A.Id });
            E = new DummyData("E", new List<Guid>() { B.Id, C.Id });
            F = new DummyData("F", new List<Guid>() { C.Id, D.Id });
        }
    }
}
