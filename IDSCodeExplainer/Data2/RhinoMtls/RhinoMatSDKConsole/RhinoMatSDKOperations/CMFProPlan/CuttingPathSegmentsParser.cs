using Materialise.SDK.MatSAX;
using System;
using System.Windows.Media.Media3D;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public class CuttingPathSegmentsParser : ISAXReadHandler
    {
            private IOsteotomy Osteotomy;
            public CuttingPathSegmentsParser(IOsteotomy osteotomy)
            {
                Osteotomy = osteotomy;
            }

            public bool HandleTag(string tag, MSAXReaderWrapper reader)
            {
                if (tag == "CoordinateSystem")
                {
                    int coordSysId;
                    reader.ReadValue(out coordSysId);
                    Osteotomy.CoordinateSysId = coordSysId;
                    return true;
                }

                if (tag == "Label")
                {
                    string label;
                    reader.ReadValue(out label);
                    Osteotomy.Label = label;
                    return true;
                }

                if (tag == "Depth")
                {
                    double depth;
                    reader.ReadValue(out depth);
                    Osteotomy.Depth = depth;
                    return true;
                }

                if (tag == "Thickness")
                {
                    double thickness;
                    reader.ReadValue(out thickness);
                    Osteotomy.Thickness = thickness;
                    return true;
                }

                if (tag == "ExtensionFront")
                {
                    double extensionFront;
                    reader.ReadValue(out extensionFront);
                    Osteotomy.ExtensionFront = extensionFront;
                    return true;
                }

                if (tag == "ExtensionBack")
                {
                    double extensionBack;
                    reader.ReadValue(out extensionBack);
                    Osteotomy.ExtensionBack = extensionBack;
                    return true;
                }

                if (tag == "Closed")
                {
                    string isClosed;
                    reader.ReadValue(out isClosed);
                    if (isClosed == "false")
                        Osteotomy.IsClosed = false;
                    else
                        Osteotomy.IsClosed = true;
                    return true;
                }

                if (tag == "Direction")
                {
                    var tempPoint = new Point3D();
                    reader.ReadValue(out tempPoint);
                    Osteotomy.Direction = new Vector3D(tempPoint.X, tempPoint.Y, tempPoint.Z);
                    return true;
                }

                if (tag == "ControlPoints")
                {
                    var controlPtsParser = new ControlPointsParser(Osteotomy.ControlPoints);
                    reader.PushTagHandler(controlPtsParser);
                    return true;
                }

                if (tag == "IsDefined")
                {
                    string isDefined;
                    reader.ReadValue(out isDefined);
                    if (isDefined == "false")
                        Osteotomy.IsDefined = false;
                    else
                        Osteotomy.IsDefined = true;
                    return true;
                }

                if (Osteotomy is OsteotomyFreeFormCut)
                {
                    if (tag == "ExtensionDistance")
                    {
                        double extensionDistance;
                        reader.ReadValue(out extensionDistance);
                        OsteotomyFreeFormCut osteotomyFreeFormCut = Osteotomy as OsteotomyFreeFormCut;
                        if (osteotomyFreeFormCut != null)
                        {
                            osteotomyFreeFormCut.ExtensionDistance = extensionDistance;
                        }
                    }
                }

                if (Osteotomy is OsteotomyPlanar)
                {
                    if (tag == "Width")
                    {
                        double width;
                        reader.ReadValue(out width);

                        OsteotomyPlanar osteotomyPlanar = Osteotomy as OsteotomyPlanar;
                        if (osteotomyPlanar != null)
                        {
                            osteotomyPlanar.Width = width;
                        }
                    }

                    if (tag == "Height")
                    {
                        double height;
                        reader.ReadValue(out height);

                        OsteotomyPlanar osteotomyPlanar = Osteotomy as OsteotomyPlanar;
                        if (osteotomyPlanar != null)
                        {
                            osteotomyPlanar.Height = height;
                        }
                    }
                }

                return false;
            }
            public void HandleEndTag(string tag)
            {
            }

            public void InitAfterLoading()
            {
            }
        }
    }
