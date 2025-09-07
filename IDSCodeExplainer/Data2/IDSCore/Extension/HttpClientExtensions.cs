using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace IDSCore.Extension
{
    public static class HttpClientExtensions
    {
        public static void Download(this HttpClient client, string requestUri, Stream destination, int bufferSize,
            IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            using (var response = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).Result)
            {
                var contentLength = response.Content.Headers.ContentLength;

                using (var download = response.Content.ReadAsStreamAsync().Result)
                {
                    if (!contentLength.HasValue)
                    {
                        download.CopyTo(destination);
                        return;
                    }

                    download.CopyTo(destination, bufferSize, contentLength.Value, progress, cancellationToken);
                }
            }
        }
    }
}
