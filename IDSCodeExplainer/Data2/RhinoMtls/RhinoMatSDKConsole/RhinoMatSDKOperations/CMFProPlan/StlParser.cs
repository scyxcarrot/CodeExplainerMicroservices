using Materialise.SDK.MatSAX;
using Materialise.SDK.MDCK.Model.Objects;
using Materialise.SDK.MDCK.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Media.Media3D;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public class StlParser : ISAXReadHandler
    {
        private Model stlModel;
        private string Label;

        public StlParser(Model model)
        {
            stlModel = model;
        }
        public bool HandleTag(string tag, MSAXReaderWrapper reader)
        {
            if (tag == "Label")
            {
                reader.ReadValue(out Label);
                return true;
            }

            if (tag == "Mesh")
            {
                var parseOperator = new ModelParseBySAXReader
                {
                    Model = stlModel,
                    SAXReader = new SAXReader(reader)
                };
                try
                {
                    parseOperator.Operate();
                }
                catch (ModelParseBySAXReader.Exception ex)
                {
                    throw new IOException("Could not read mesh from file: " + ex.Message);
                }
                finally
                {
                    parseOperator.Dispose();
                }

                return true;
            }
            return false;
        }

        public void HandleEndTag(string tag)
        {
            if(tag == "Mesh")
            {
                stlModel.Name = Label;
            }
        }

        public void InitAfterLoading()
        {
        }
    }
}
