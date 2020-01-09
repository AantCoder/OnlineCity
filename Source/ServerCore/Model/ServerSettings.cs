using OCUnion.Transfer.Model;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json.Serialization;
using Transfer;

namespace ServerCore.Model
{
    public class ServerSettings : IValidatableObject
    {
        public string ServerName { get; set; } = "Another OnlineCity Server";
        public int SaveInterval { get; set; } = 10000;
        public int Port { get; set; } = SessionClient.DefaultPort;

        /// <summary>
        /// RPG: Legend of the server, Краткое описание сервера
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Check or ModList
        /// </summary>
        public bool IsModsWhitelisted { get; set; }

        /// <summary>
        /// Рабочая директория
        /// </summary>
        [JsonIgnore]
        public string WorkingDirectory { get; set; }

        [JsonIgnore]
        public static ModelModsFiles AppovedFolderAndConfig { get; set; }

        [JsonIgnore]
        public ModelModsFiles ModsDirConfig { get; set; }

        [JsonIgnore]
        public ModelModsFiles SteamDirConfig { get; set; }

        /// <summary>
        /// Директория где храняется моды
        /// </summary>
        public string ModsDirectory { get; set; } = "C:\\Games\\RimWorld\\Mods";

        /// <summary>
        /// For using steam workshop folder
        /// </summary>
        [JsonIgnore]
        public string SteamWorkShopModsDir { get; set; } 

        public ServerSettings() { }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> errors = new List<ValidationResult>();
            var obj = validationContext.ObjectInstance as ServerSettings;

            if (obj == null)
            {
                errors.Add(new ValidationResult("Setup setting before"));
                return errors;
            }

            if (obj.Port > 65535)
            {
                errors.Add(new ValidationResult("Port must be < 65535"));
            }

            if (obj.SaveInterval < 10000 || obj.SaveInterval > 100000)
            {
                errors.Add(new ValidationResult("Save interval must be betwen 10000 ms  and 100000"));
            }

            if (obj.IsModsWhitelisted)
            {
                if (string.IsNullOrEmpty(WorkingDirectory))
                {
                    errors.Add(new ValidationResult("Не задана рабочая директория в коде"));
                }
                else if (!File.Exists(Path.Combine(WorkingDirectory, "ModsConfig.xml")))
                {
                    errors.Add(new ValidationResult($"Copy ModsConfig.xml to {WorkingDirectory}"));
                }

                if (!Directory.Exists(obj.ModsDirectory))
                {
                    errors.Add(new ValidationResult($"Mods directory doesn't exist ModsDirectory={obj.ModsDirectory}"));
                }

                //if (!Directory.Exists(obj.SteamWorkShopModsDir))
                //{
                //    errors.Add(new ValidationResult($"Steam Mods directory doesn't exist SteamWorkShopModsDir={obj.SteamWorkShopModsDir}"));
                //}
            }

            return errors;
        }
    }
}