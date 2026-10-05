using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Data
{
    public sealed class BookstoreConfiguration
    {
        private static BookstoreConfiguration _instance;
        private static BookstoreConfiguration Instance => _instance ?? throw new InvalidOperationException(
            "BookstoreConfiguration has not been initialized. Call Initialize(IConfiguration) at startup.");

        private readonly Dictionary<string, string> _appSettings = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _connectionStrings = new Dictionary<string, string>();

        /// <summary>
        /// Initializes the singleton from the ASP.NET Core IConfiguration.
        /// Call once at startup (e.g. in Program.cs) after building the configuration.
        /// </summary>
        // PORT-TODO
        // {"note": "Call BookstoreConfiguration.Initialize(builder.Configuration) in Program.cs at startup before any code reads settings. Also ensure appsettings.json has an 'AppSettings' section with the keys formerly in App.config/Web.config <appSettings>.", "files": ["../Bookstore.Web/Program.cs"]}
        public static void Initialize(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            _instance = new BookstoreConfiguration(configuration);
        }

        private BookstoreConfiguration(IConfiguration configuration)
        {
            // Load AppSettings section (equivalent to ConfigurationManager.AppSettings)
            var appSettingsSection = configuration.GetSection("AppSettings");
            foreach (var child in appSettingsSection.GetChildren())
            {
                _appSettings[child.Key] = child.Value;
            }

            // Environment variables override app settings (preserving legacy behavior)
            foreach (var key in _appSettings.Keys.ToList())
            {
                var envValue = Environment.GetEnvironmentVariable(key);
                if (envValue != null)
                {
                    _appSettings[key] = envValue;
                }
            }

            // Load ConnectionStrings section (equivalent to ConfigurationManager.ConnectionStrings)
            var connectionStringsSection = configuration.GetSection("ConnectionStrings");
            foreach (var child in connectionStringsSection.GetChildren())
            {
                _connectionStrings[child.Key] = child.Value;
            }
        }

        public static void AddSetting(string key, string value)
        {
            Instance._appSettings[key] = value;
        }

        public static string GetSetting(string key)
        {
            return Instance._appSettings[key];
        }

        public static T GetSetting<T>(string key)
        {
            var value = Instance._appSettings[key];
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static void AddConnectionString(string key, string value)
        {
            Instance._connectionStrings[key] = value;
        }

        public static string GetConnectionString(string key)
        {
            return Instance._connectionStrings[key];
        }
    }
}