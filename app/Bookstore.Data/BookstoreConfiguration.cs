using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Data
{
    public sealed class BookstoreConfiguration
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, string> _overrides = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _connectionStringOverrides = new Dictionary<string, string>();

        public BookstoreConfiguration(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void AddSetting(string key, string value)
        {
            _overrides[key] = value;
        }

        public string GetSetting(string key)
        {
            // Check overrides first (preserves the AddSetting runtime-override behaviour),
            // then environment variables (mirrors the legacy env-var fallback),
            // then IConfiguration.
            if (_overrides.TryGetValue(key, out var overrideValue))
            {
                return overrideValue;
            }

            var envValue = Environment.GetEnvironmentVariable(key);
            if (envValue != null)
            {
                return envValue;
            }

            return _configuration[key] ?? throw new KeyNotFoundException($"Configuration key '{key}' was not found.");
        }

        public T GetSetting<T>(string key)
        {
            var value = GetSetting(key);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public void AddConnectionString(string key, string value)
        {
            _connectionStringOverrides[key] = value;
        }

        public string GetConnectionString(string key)
        {
            if (_connectionStringOverrides.TryGetValue(key, out var overrideValue))
            {
                return overrideValue;
            }

            return _configuration.GetConnectionString(key)
                ?? throw new KeyNotFoundException($"Connection string '{key}' was not found.");
        }
    }
}
