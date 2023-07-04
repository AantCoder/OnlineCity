using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        /// Disable DevMode
        /// </summary>
        public bool DisableDevMode { get; set; }

        /// <summary>
        /// Authorization key for discord bot. 
        /// Ключ авторизации для Дискорд бота
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        ///  При регистрации требуется подтверждение пользователя
        /// </summary>
        public bool PlayerNeedApprove { get; set; }

        /// <summary>
        ///  Включено подтверждение при помощит дискорда через API, должно быть включено PlayerNeedApprove
        /// </summary>
        public bool PlayerNeedApproveInDiscord { get; set; }

        public int MinutesIntervalBetweenPVP { get; set; }

        public ServerGeneralSettings GeneralSettings { get; set; }

        /// <summary>
        /// Рабочая директория
        /// </summary>
        [JsonIgnore]
        public string WorkingDirectory { get; set; }

        public List<FolderCheck> EqualFiles { get; set; } = new List<FolderCheck>()
        {
            new FolderCheck()
            {
                ServerPath = "Mods",
                FolderType = FolderType.ModsFolder,
                IgnoreFile = FileChecker.IgnoredModFiles,
                IgnoreFolder = FileChecker.IgnoredModFolders,
                NeedReplace = true
            },
            new FolderCheck()
            {
                ServerPath = "Config",
                FolderType = FolderType.ModsConfigPath,
                IgnoreFile = FileChecker.IgnoredConfigFiles,
                NeedReplace = true
            },
            new FolderCheck()
            {
                ServerPath = "Config",
                FolderType = FolderType.ModsConfigPath,
                XMLFileName = "..\\HugsLib\\ModSettings.xml",
                IgnoreTag = new List<string>() { "OnlineCity" },
                NeedReplace = true
            },
        };

        ///// <summary>
        ///// Директория где храняется моды
        ///// </summary>
        //public string ModsDirectory { get; set; } = "C:\\Games\\RimWorld\\Mods";

        /// <summary>
        /// Защита начинающих игроков от нападения, а также запрет передавать товары от новых поселений
        /// Protection of novice players from attack, as well as a ban on transferring goods from new settlements
        /// </summary>
        public bool ProtectingNovice { get; set; }

        /// <summary>
        /// Сервер будет автоматически удалять не развитые поселения за которые давно не играют
        /// The server will automatically delete undeveloped settlements that have not been played for a long time
        /// </summary>
        public bool DeleteAbandonedSettlements { get; set; }

        /// <summary>
        /// Максимальный размер папки со скриншотами колоний, которого нужно стараться придерживаться.
        /// Maximum folder size with screenshots of colonies.
        /// </summary>
        public int ColonyScreenFolderMaxMb { get; set; } = 1024;

        ///// <summary>
        ///// Default Path to %AppData%\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config, but copy only files that must be replaced OnClient
        ///// </summary>
        //public string ModsConfigsDirectoryPath { get; set; } = @"Copy files from %AppData%\..\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\ and set folder";

        ///// <summary>
        /////  Attention: not all files must be checked and replaced for example Prefs.xml - contains local settings
        ///// </summary>
        //public string[] IgnoredLocalConfigFiles { get; set; } = FileChecker.IgnoredConfigFiles;

        //public string[] IgnoredLocalModFiles { get; set; } = FileChecker.IgnoredModFiles;

        ///// <summary>
        ///// Contains config files
        ///// </summary>
        //[JsonIgnore]
        //public string ModsConfigFiles { get; set; }

        public ServerSettings()
        {
            GeneralSettings = GeneralSettings.SetDefault();
        }

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

            //if (obj.IsModsWhitelisted)
            //{
            //    if (string.IsNullOrEmpty(WorkingDirectory))
            //    {
            //        errors.Add(new ValidationResult("Не задана рабочая директория в коде"));
            //    }
            //    else if (!Directory.Exists(ModsConfigsDirectoryPath))
            //    {
            //        errors.Add(new ValidationResult($"Directory doesn't exist ModsConfigFilePath={obj.ModsConfigsDirectoryPath}"));
            //    }

            //    if (!Directory.Exists(obj.ModsDirectory))
            //    {
            //        errors.Add(new ValidationResult($"Mods directory doesn't exist ModsDirectory={obj.ModsDirectory}"));
            //    }
            //}

            return errors;
        }
    }
}