using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Materialise.SDK.MatSAX;

namespace MatSDKOperationConsole
{
    public class MatSaxHeaderReaderHandler
    {
        private string[] CommandArguments { get; }
        public MatSaxHeaderReaderHandler(string[] args)
        {
            CommandArguments = args;
        }

        public bool Run()
        {
            if (CommandArguments.Length != 3)
            {
                return false;
            }

            var sppcPath = CommandArguments[1];
            var headerExportPath = CommandArguments[2];

            return MSAXReaderWrapper.ExtractXMLHeader(sppcPath, headerExportPath);
        }

    }
}
