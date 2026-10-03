using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using BobsBookstoreClassic.Data;
using Bookstore.Common;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Web
{
    public static class ConfigurationSetup
    {
        public static async Task ConfigureConfigurationAsync(IConfiguration configuration)
        {
            // Initialize BookstoreConfiguration from IConfiguration (appsettings.json)
            BookstoreConfiguration.Initialize(configuration);

            var rootPath = "/" + Constants.AppName;

            const string databasePath = "/Database";
            const string authenticationPath = "/Authentication";
            const string fileServicePath = "/Files";

            if (BookstoreConfiguration.GetSetting("Services/Database") == "aws")
            {
                using (var client = new AmazonSimpleSystemsManagementClient())
                {
                    var request = new GetParameterRequest { Name = $"{rootPath}{databasePath}/ConnectionStrings/BookstoreDatabaseConnection" };
                    var response = await client.GetParameterAsync(request);

                    BookstoreConfiguration.AddSetting(response.Parameter.Name.Replace($"{rootPath}{databasePath}/", string.Empty), response.Parameter.Value);
                }
            }

            if (BookstoreConfiguration.GetSetting("Services/Authentication") == "aws")
            {
                using (var client = new AmazonSimpleSystemsManagementClient())
                {
                    var request = new GetParametersByPathRequest { Path = $"{rootPath}{authenticationPath}/", Recursive = true };
                    var response = await client.GetParametersByPathAsync(request);

                    foreach (var parameter in response.Parameters)
                    {
                        BookstoreConfiguration.AddSetting(parameter.Name.Replace($"{rootPath}/", string.Empty), parameter.Value);
                    }
                }
            }

            if (BookstoreConfiguration.GetSetting("Services/FileService") == "aws")
            {
                using (var client = new AmazonSimpleSystemsManagementClient())
                {
                    var request = new GetParametersByPathRequest { Path = $"{rootPath}{fileServicePath}/", Recursive = true };
                    var response = await client.GetParametersByPathAsync(request);

                    foreach (var parameter in response.Parameters)
                    {
                        BookstoreConfiguration.AddSetting(parameter.Name.Replace($"{rootPath}/", string.Empty), parameter.Value);
                    }
                }
            }
        }
    }
}
