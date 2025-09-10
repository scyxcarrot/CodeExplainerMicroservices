using System;
using IDS.Interface.Geometry;

namespace IDS.Core.V2.Geometries
{
    public class IDSTransform : ITransform
    {
        public double M00 { get; set; }
        public double M01 { get; set; }
        public double M02 { get; set; }
        public double M03 { get; set; }
        public double M10 { get; set; }
        public double M11 { get; set; }
        public double M12 { get; set; }
        public double M13 { get; set; }
        public double M20 { get; set; }
        public double M21 { get; set; }
        public double M22 { get; set; }
        public double M23 { get; set; }
        public double M30 { get; set; }
        public double M31 { get; set; }
        public double M32 { get; set; }
        public double M33 { get; set; }

        public IDSTransform()
        {
        }

        public IDSTransform(ITransform source)
        {
            M00 = source.M00;
            M01 = source.M01;
            M02 = source.M02;
            M03 = source.M03;
            M10 = source.M10;
            M11 = source.M11;
            M12 = source.M12;
            M13 = source.M13;
            M20 = source.M20;
            M21 = source.M21;
            M22 = source.M22;
            M23 = source.M23;
            M30 = source.M30;
            M31 = source.M31;
            M32 = source.M32;
            M33 = source.M33;
        }

        public static ITransform Unset() => new IDSTransform()
        {
            M00 = -1.23432101234321E+308,
            M01 = -1.23432101234321E+308,
            M02 = -1.23432101234321E+308,
            M03 = -1.23432101234321E+308,
            M10 = -1.23432101234321E+308,
            M11 = -1.23432101234321E+308,
            M12 = -1.23432101234321E+308,
            M13 = -1.23432101234321E+308,
            M20 = -1.23432101234321E+308,
            M21 = -1.23432101234321E+308,
            M22 = -1.23432101234321E+308,
            M23 = -1.23432101234321E+308,
            M30 = -1.23432101234321E+308,
            M31 = -1.23432101234321E+308,
            M32 = -1.23432101234321E+308,
            M33 = -1.23432101234321E+308
        };

        public static IDSTransform Identity => new IDSTransform()
        {
            M00 = 1.0,
            M11 = 1.0,
            M22 = 1.0,
            M33 = 1.0
        };

        public double this[int row, int column]
        {
            get
            {
                #region GetTransformWithRowAndColumn

                if (row < 0)
                    throw new IndexOutOfRangeException("Negative row indices are not allowed when accessing a Transform matrix");
                if (row > 3)
                    throw new IndexOutOfRangeException("Row indices higher than 3 are not allowed when accessing a Transform matrix");
                if (column < 0)
                    throw new IndexOutOfRangeException("Negative column indices are not allowed when accessing a Transform matrix");
                if (column > 3)
                    throw new IndexOutOfRangeException("Column indices higher than 3 are not allowed when accessing a Transform matrix");

                switch (row)
                {
                    case 0:
                        switch (column)
                        {
                            case 0:
                                return this.M00;
                            case 1:
                                return this.M01;
                            case 2:
                                return this.M02;
                            case 3:
                                return this.M03;
                        }
                        break;
                    case 1:
                        switch (column)
                        {
                            case 0:
                                return this.M10;
                            case 1:
                                return this.M11;
                            case 2:
                                return this.M12;
                            case 3:
                                return this.M13;
                        }
                        break;
                    case 2:
                        switch (column)
                        {
                            case 0:
                                return this.M20;
                            case 1:
                                return this.M21;
                            case 2:
                                return this.M22;
                            case 3:
                                return this.M23;
                        }
                        break;
                    case 3:
                        switch (column)
                        {
                            case 0:
                                return this.M30;
                            case 1:
                                return this.M31;
                            case 2:
                                return this.M32;
                            case 3:
                                return this.M33;
                        }
                        break;
                }
                throw new IndexOutOfRangeException("One of the cross beams has gone out askew on the treadle.");

                #endregion
            }
            set
            {
                #region SetTransformWithRowAndColumn

                if (row < 0)
                    throw new IndexOutOfRangeException("Negative row indices are not allowed when accessing a Transform matrix");
                if (row > 3)
                    throw new IndexOutOfRangeException("Row indices higher than 3 are not allowed when accessing a Transform matrix");
                if (column < 0)
                    throw new IndexOutOfRangeException("Negative column indices are not allowed when accessing a Transform matrix");
                if (column > 3)
                    throw new IndexOutOfRangeException("Column indices higher than 3 are not allowed when accessing a Transform matrix");
                switch (row)
                {
                    case 0:
                        switch (column)
                        {
                            case 0:
                                this.M00 = value;
                                return;
                            case 1:
                                this.M01 = value;
                                return;
                            case 2:
                                this.M02 = value;
                                return;
                            case 3:
                                this.M03 = value;
                                return;
                            default:
                                return;
                        }
                    case 1:
                        switch (column)
                        {
                            case 0:
                                this.M10 = value;
                                return;
                            case 1:
                                this.M11 = value;
                                return;
                            case 2:
                                this.M12 = value;
                                return;
                            case 3:
                                this.M13 = value;
                                return;
                            default:
                                return;
                        }
                    case 2:
                        switch (column)
                        {
                            case 0:
                                this.M20 = value;
                                return;
                            case 1:
                                this.M21 = value;
                                return;
                            case 2:
                                this.M22 = value;
                                return;
                            case 3:
                                this.M23 = value;
                                return;
                            default:
                                return;
                        }
                    case 3:
                        switch (column)
                        {
                            case 0:
                                this.M30 = value;
                                return;
                            case 1:
                                this.M31 = value;
                                return;
                            case 2:
                                this.M32 = value;
                                return;
                            case 3:
                                this.M33 = value;
                                return;
                            default:
                                return;
                        }
                }

                #endregion
            }
        }
    }
}
