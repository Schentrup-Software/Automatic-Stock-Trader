using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace AutomaticStockTrader.Tests
{
    public static class LaunchSettingsFixture
    {
        private const string LAUNCH_SETTINGS_FILE_PATH = "Properties/launchSettings.json";

        public static void SetupEnvVars()
        {
            if (File.Exists(LAUNCH_SETTINGS_FILE_PATH))
            {
                using var file = File.OpenText(LAUNCH_SETTINGS_FILE_PATH);
                var reader = new JsonTextReader(file);
                var jObject = JObject.Load(reader);

                var variables = jObject
                    .GetValue("profiles")
                    .SelectMany(profiles => profiles.Children())
                    .SelectMany(profile => profile.Children<JProperty>())
                    .Where(prop => prop.Name == "environmentVariables")
                    .SelectMany(prop => prop.Value.Children<JProperty>())
                    .ToList();

                foreach (var variable in variables)
                {
                    Environment.SetEnvironmentVariable(variable.Name, variable.Value.ToString());
                }
            }
        }
    }
}
