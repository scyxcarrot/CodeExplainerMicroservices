using System;
using System.Threading;

namespace IDS.Core.Http
{
    public interface IUpdater
    {
        bool GetBuildInfo(string url);

        bool Download(string url, double timeout, string filePath, int bufferSize, 
            IProgress<double> progress = null, CancellationToken cancellationToken = default);
    }
}
