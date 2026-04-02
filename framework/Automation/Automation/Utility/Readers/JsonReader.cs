using Newtonsoft.Json.Linq;

namespace Utility.Readers
{
    public static class JsonReader
    {
        public static JObject ReadJson(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JObject.Parse(json);
        }

        public static string GetValue(string filePath, string valuePath)
        {
            JObject jsonBody = ReadJson(filePath);
            JToken? token = jsonBody.SelectToken(valuePath);

            if (token == null)
            {
                throw new Exception($"Value path '{valuePath}' not found in JSON file '{filePath}'");
            }

            string value = token.ToString();

            if (string.IsNullOrEmpty(value))
            {
                throw new Exception($"Value at path '{valuePath}' in JSON file '{filePath}' is null or empty");
            }

            return value;
        }
    }
}
