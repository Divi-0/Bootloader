using Bootloader.Domain;
using Bootloader.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Web;

namespace Bootloader
{
    internal class Program
    {
        static AppSettings? _appSettings;

        static async Task Main(string[] args)
        {
            ReadAppSettings();

            Version currentVersion = GetCurrentVersion();

            byte[] appData = new byte[0];

            try
            {
                appData = await CheckForNewVersion(currentVersion);

                SaveNewVersion(appData);
            }
            catch (AlreadyUpToDateException)
            {
                Console.WriteLine("App is already up to date, ending update process");
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected error: {exception}", e.ToString());
                throw;
            }

            Console.WriteLine("Updated");

            //Run app
        }

        static void ReadAppSettings()
        {
            string jsonContent = File.ReadAllText(@".\appsettings.json");

            AppSettings appSettings = JsonSerializer.Deserialize<AppSettings>(jsonContent)!;

            if (string.IsNullOrWhiteSpace(appSettings.UpdateServiceUrl))
            {
                throw new ArgumentNullException(nameof(appSettings.UpdateServiceUrl));
            }

            if (string.IsNullOrWhiteSpace(appSettings.ApplicationPath))
            {
                throw new ArgumentNullException(nameof(appSettings.ApplicationPath));
            }

            _appSettings = appSettings;
        }

        static Version GetCurrentVersion()
        {
            FileVersionInfo fileVersionInfo;

            try
            {
                fileVersionInfo = FileVersionInfo.GetVersionInfo(_appSettings!.ApplicationPath!);
            }
            catch (FileNotFoundException)
            {
                return new Version();
            }

            return new Version(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart);
        }

        static async Task<byte[]> CheckForNewVersion(Version version)
        {
            ServiceProvider serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();

            IHttpClientFactory httpClientFactory = serviceProvider.GetService<IHttpClientFactory>()!;

            HttpClient httpClient = httpClientFactory!.CreateClient();

            UriBuilder uriBuilder = new UriBuilder(new Uri(_appSettings!.UpdateServiceUrl!, UriKind.Absolute));

            NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["major"] = version.Major.ToString();
            query["minor"] = version.Minor.ToString();
            query["bugfix"] = version.Build >= 0 ? version.Build.ToString() : "0";

            uriBuilder.Query = query.ToString();

            HttpResponseMessage response = await httpClient.GetAsync(uriBuilder.ToString());

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new AlreadyUpToDateException();
            }

            response.EnsureSuccessStatusCode();

            NewVersion newVersion = JsonSerializer.Deserialize<NewVersion>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

            return newVersion.Data;
        }

        static void SaveNewVersion(byte[] appData)
        {
            File.WriteAllBytes(_appSettings!.ApplicationPath!, appData);
        }

    }
}