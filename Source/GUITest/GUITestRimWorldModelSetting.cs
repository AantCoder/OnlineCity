using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GUITest
{
    public class GUITestRimWorldModelSetting : ICloneable
    {
        /// <summary>
        /// Временная папка, после теста содержимое удалится
        /// </summary>
        [Description("Временная папка, после теста содержимое удалится")]
        [DisplayName("Временная папка")]
        public string TempFolder { get; set; } = @"C:\W\OnlineCity\Разное\Test\Temp";
        /// <summary>
        /// Папка с сервером и подпапкой World с сейвами и всеми настройками
        /// </summary>
        [Description("Папка с сервером и подпапкой World с сейвами и всеми настройками")]
        [DisplayName("Папка с сервером для теста")]
        public string ServerFolder { get; set; } = @"C:\W\OnlineCity\Разное\Test\Server";
        /// <summary>
        /// Копия папки Config игры, которая установится во время теста, после теста восстановится то, что было раньше
        /// </summary>
        [Description("Копия папки Config игры, которая установится во время теста, после теста восстановится то, что было раньше")]
        [DisplayName("Папка Config для теста")]
        public string TestConfigFolder { get; set; } = @"C:\W\OnlineCity\Разное\Test\Config";
        /// <summary>
        /// Папка Config игры
        /// </summary>
        [Description("Папка Config игры")]
        [DisplayName("Папка Config игры")]
        public string GameConfigFolder { get; set; } = @"C:\Users\Ant\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config";
        /// <summary>
        /// Папка с логами мода OnlineCity
        /// </summary>
        [Description("Папка с логами мода OnlineCity")]
        [DisplayName("Папка с логами мода OnlineCity")]
        public string ModLogFolder { get; set; } = @"C:\Users\Ant\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\OnlineCity";
        /// <summary>
        /// Файл с логами, который пишет сама игра
        /// </summary>
        [Description("Файл с логами, который пишет сама игра")]
        [DisplayName("Файл с логами, который пишет сама игра")]
        public string GameLogFile { get; set; } = @"C:\Users\Ant\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log";
        /// <summary>
        /// Имя исполняемого файла запуска игры
        /// </summary>
        [Description("Имя исполняемого файла запуска игры")]
        [DisplayName("Имя исполняемого файла запуска игры")]
        public string GameExec { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64.exe";
        /// <summary>
        /// Заголовок окна игры, для поиска запущенной игры
        /// </summary>
        [Description("Заголовок окна игры, для поиска запущенной игры")]
        [DisplayName("Заголовок окна игры")]
        public string WindowsTitle { get; set; } = "RimWorld by Ludeon Studios";



        public object Clone()
        {
            return MemberwiseClone();
        }
    }



}
