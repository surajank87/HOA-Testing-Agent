using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Utility.DataModels;

namespace Utility.Readers
{
    public static class ConfigurationReader
    {
        //Josn file reader
        public static Configuration ReadConfiguration(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            },
                PropertyNameCaseInsensitive = true
            };
            Configuration? config;
            if (jsonString == null)
            {
                throw new NullReferenceException("Provided configuration file is null or empty");
            }
            config = JsonSerializer.Deserialize<Configuration>(jsonString, options);
            if (config == null)
            {
                throw new NullReferenceException("Common Configuration object is null/empty");
            }
            return config;
        }
    }
}
