using IDS.Interface.Loader;
using IDS.Interface.Tools;
using System;
using System.IO;

namespace IDS.CMF.V2.Loader
{
    public class PreopLoaderFactory
    {
        public IPreopLoader GetLoader(IConsole console, string filePath)
        {
            IPreopLoader loader;

            var extension = Path.GetExtension(filePath);
            switch (extension.ToLower())
            {
                case ".sppc":
                    loader = new ProplanLoader(console, filePath);
                    break;
                case ".mcs":
                    loader = new EnlightCMFLoader(console, filePath);
                    break;
                default:
                    throw new Exception("File not supported!");
            }

            return loader;
        }
    }
}
