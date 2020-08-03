using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Sample.MediaInfo.ConsoleApp
{
    public interface IUploadAndAnalyze
    {
        /// <summary>
        /// Upload a media file into AzureStorage and analyze it with MediaInfo.
        /// </summary>
        /// <param name="sourcePath">Path to local file.</param>
        /// <param name="accountName">Target storage account name for upload.</param>
        /// <param name="container">Upload container name.</param>
        /// <returns>The report on the file.</returns>
        Task<JObject> ExecuteAsync(string sourcePath, string accountName, string container);
    }
}
