using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace BobsBookstoreClassic.Data
{
    public sealed class BookstoreConfiguration
    {
        private static readonly Lazy<BookstoreConfiguration> Lazy = new Lazy<BookstoreConfiguration>(() => new BookstoreConfiguration());

        private static BookstoreConfiguration Instance => Lazy.Value;

        private readonly Dictionary<string, string> _appSettings = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _connectionStrings = new Dictionary<string, string>();

        private BookstoreConfiguration()
        {
        }

        /// <summary>
        /// Initializes settings from IConfiguration (replaces ConfigurationManager usage).
        /// Must be called once at startup before any GetSetting/GetConnectionString calls.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            // Load flat key/value pairs from appSettings-style sections
            // Map nested IConfiguration sections to slash-separated keys for backward compatibility
            LoadSection(configuration.GetSection("Services"), "Services");
            LoadSection(configuration.GetSection("Authentication"), "Authentication");
            LoadSection(configuration.GetSection("Files"), "Files");

            // Also check environment variable overrides
            foreach (var key in Instance._appSettings.Keys.ToArray())
            {
                var envValue = Environment.GetEnvironmentVariable(key);
                if (envValue != null)
                {
                    Instance._appSettings[key] = envValue;
                }
            }

            // Load connection strings
            var connectionStrings = configuration.GetSection("ConnectionStrings");
            if (connectionStrings.Exists())
            {
                foreach (var child in connectionStrings.GetChildren())
                {
                    Instance._connectionStrings[child.Key] = child.Value ?? string.Empty;
                }
            }
        }

        private static void LoadSection(IConfigurationSection section, string prefix)
        {
            if (!section.Exists()) return;

            foreach (var child in section.GetChildren())
            {
                var key = $"{prefix}/{child.Key}";
                if (child.GetChildren().Any())
                {
                    // Recurse into nested sections
                    LoadSection(child, key);
                }
                else
                {
                    Instance._appSettings[key] = child.Value ?? string.Empty;
                }
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
