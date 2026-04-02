namespace Utility.Readers
{
    public static class RequestBodyReader
    {
        /// <summary>
        /// Reads all JSON request body files from the specified folder.
        /// Each file represents one test case.
        /// </summary>
        /// <returns>Dictionary where key = filename (without extension) and value = file content</returns>
        public static Dictionary<string, string> ReadRequestBodies(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException(
                    $"Request bodies folder not found: '{folderPath}'. " +
                    $"Please create the folder and add request body JSON files.");
            }

            var requestBodies = new Dictionary<string, string>();
            string[] files = Directory.GetFiles(folderPath, "*.json");

            if (files.Length == 0)
            {
                throw new FileNotFoundException(
                    $"No JSON request body files found in: '{folderPath}'.");
            }

            foreach (string filePath in files.OrderBy(f => f))
            {
                string testCaseName = Path.GetFileNameWithoutExtension(filePath);
                string content = File.ReadAllText(filePath);
                requestBodies.Add(testCaseName, content);
            }

            return requestBodies;
        }
    }
}
