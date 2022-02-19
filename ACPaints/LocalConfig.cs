using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ACPaints
{
    class LocalConfig
    {
        private static string configPath = Path.Combine(new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ACPaints.json" });
        
        public static LocalConfig instance;

        public static LocalConfig Instance
        {
            get 
            { 
                if (instance == null)
                {
                    instance = Load();
                }
                return instance;
            }
        }

        public bool Admin { get; set; }

        public bool Debug { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        private static LocalConfig Load()
        {
            if (!File.Exists(configPath))
            {
                return new LocalConfig();
            }

            LocalConfig config = null;

            try
            {
                var configString = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions();
                options.AllowTrailingCommas = true;
                config = JsonSerializer.Deserialize<LocalConfig>(configString, options);
            }
            catch (Exception)
            {
                Console.WriteLine("Error parsing local config file");
            }


            if (config == null)
            {
                config = new LocalConfig();
            }

            return config;
        }

        public static void Save()
        {
            var options = new JsonSerializerOptions() { AllowTrailingCommas = true, WriteIndented = true };
            string jsonConfig = JsonSerializer.Serialize(instance, options);
            File.WriteAllText(configPath, jsonConfig);
        }
    }
}
