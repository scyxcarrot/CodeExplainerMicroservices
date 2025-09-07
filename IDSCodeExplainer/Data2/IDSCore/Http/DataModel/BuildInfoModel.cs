using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDSCore.Extension;
using Newtonsoft.Json;

namespace IDS.Core.Http
{
    public class BuildInfoModel : IUpdater
    {
        public string Version { get; set; }
        public string ChecksumSha256 { get; set; }

        private readonly string _keyVersion;
        private readonly string _keyChecksumSha256;
        private readonly string _token;

        public BuildInfoModel(string token, string KeyVersion, string KeyChecksumSha256)
        {
            _token = token;
            _keyVersion = KeyVersion;
            _keyChecksumSha256 = KeyChecksumSha256;
        }

        public bool Download(string url, double timeout, string filePath, int bufferSize, IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                    client.Timeout = TimeSpan.FromMinutes(timeout);
                    using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        client.Download(url, file, bufferSize, progress, cancellationToken);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to download due to: {ex.Message}");
                }
            }

            return false;
        }

        public bool GetBuildInfo(string url)
        {
            try
            {
                if (!ExecuteHttpClient(url, out var response))
                {
                    return false;
                }

                var propertiesMessage = response.Content.ReadAsStringAsync().Result;
                var properties = JsonConvert.DeserializeObject<PropertiesResponseModel>(propertiesMessage);

                if (properties.Properties[_keyVersion].Count != 1)
                {
                    throw new IDSException("Version in properties contains multiple value");
                }

                var version = (string)properties.Properties[_keyVersion][0];

                var sha256 = (properties.Properties.ContainsKey(_keyChecksumSha256) &&
                              properties.Properties[_keyChecksumSha256].Count == 1)
                    ? (string)properties.Properties[_keyChecksumSha256][0] : null;

                Version = version;
                ChecksumSha256 = sha256;

                return true;

            }
            catch (Exception ex)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Failed to get version due to: {ex.Message}");
            }

            return false;
        }

        public bool GetFilesList(string url, out FilesResponseModel fileList)
        {
            fileList = new FilesResponseModel();
            try
            {
                if (!ExecuteHttpClient(url, out var response))
                {
                    return false;
                }

                var responseResult = response.Content.ReadAsStringAsync().Result;
                fileList = JsonConvert.DeserializeObject<FilesResponseModel>(responseResult);

                return true;
            }
            catch (Exception ex)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Failed to get files due to: {ex.Message}");
            }

            return false;
        }


        private bool ExecuteHttpClient(string url, out HttpResponseMessage response)
        {
            response = new HttpResponseMessage();

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                    response = client.GetAsync(url).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Failed to get response from client: {ex.Message}");
                return false;
            }
        }
    }
}
