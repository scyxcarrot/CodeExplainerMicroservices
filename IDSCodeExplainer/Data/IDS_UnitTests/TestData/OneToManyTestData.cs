using System;
using System.Collections.Generic;

namespace IDS.Testing.Data
{
    public class OneToManyTestData
    {
        public DummyData A { get; }
        public DummyData B { get; }
        public DummyData C { get; }
        public DummyData D { get; }
        public DummyData E { get; }
        public DummyData F { get; }
        public DummyData G { get; }
        public DummyData H { get; }
        public DummyData I { get; }

        public List<DummyData> AllData =>
            new List<DummyData>()
            {
                A,
                B,
                C,
                D,
                E,
                F,
                G,
                H,
                I,
            };

        /*
         *                            A
         *                            |
         *                -------------------------
         *                |                       |
         *                B                       C
         *                |                       |
         *      -----------------------       ---------
         *      |      |      |       |       |       |
         *      D      E      F       G       H       I
         *
         */
        public OneToManyTestData()
        {
            A = new DummyData("A");
            B = new DummyData("B", new List<Guid>() { A.Id });
            C = new DummyData("C", new List<Guid>() { A.Id });
            D = new DummyData("D", new List<Guid>() { B.Id });
            E = new DummyData("E", new List<Guid>() { B.Id });
            F = new DummyData("F", new List<Guid>() { B.Id });
            G = new DummyData("G", new List<Guid>() { B.Id });
            H = new DummyData("H", new List<Guid>() { C.Id });
            I = new DummyData("I", new List<Guid>() { C.Id });
        }
    }
}
