﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Sample.MediaInfo.Core;

namespace Sample.MediaInfo.ConsoleApp
{
    /// <summary>
    /// Performs an upload and a remote analysis of the sample media file.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main.
        /// </summary>
        /// <returns>.</returns>
        internal static async Task Main()
        {
            // Setup
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var uploadAndAnalyzeService = serviceProvider.GetService<IUploadAndAnalyze>();

            var sourcePath = "./media/bbb.mp4";
            var accountName = "youraccount";
            var container = "test";

            // Run
            JObject report = null;
            try
            {
                report = await uploadAndAnalyzeService.ExecuteAsync(sourcePath, accountName, container).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.Write(report);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            services.AddLogging(configure => configure.AddConsole());

            services.AddMediaInfoService();
            services.AddSingleton<IUploadAndAnalyze, UploadAndAnalyze>();
        }
    }
}
