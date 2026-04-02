using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility.Enum;
using Utility;
using Newtonsoft.Json.Linq;
using Environment = Utility.Enum.Environment;
using Utility.Readers;

namespace CoreLogic.Managers
{
    public class EnvironmentConfigurationManager
    {
        private static readonly string EnvironmentConfigurationFilePath = @"CoreLogic\FilePaths\Environments.json";

        public static string GetFilePath(Environment environment)
        {
            string fullConfigurationFilePath = Helper.GetFullPath(EnvironmentConfigurationFilePath);
            JObject environmentConfiguration = JsonReader.ReadJson(fullConfigurationFilePath);
            var environmentFilePathToken = environmentConfiguration.SelectToken(environment.ToString());

            if (environmentFilePathToken == null)
            {
                throw new ArgumentException($"File path is not available for environment: {environment}");
            }

            return Helper.GetFullPath(environmentFilePathToken.ToString());
        }
    }
}
