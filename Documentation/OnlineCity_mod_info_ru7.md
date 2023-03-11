

# 7. Сервер быстро

Распакуйте архив с сервером в любую папку. 
Запустите сервер Server.exe и сразу закройте его, после первого запуска он создаст папку World и файл настроек Settings.json.
Если вы будете использовать стандартные настройки без синхронизации модов, то это всё: запускайте сервер Server.exe.

Первый человек, который зарегистрируется на сервере через игру, будет считаться администратором. Ему доступны специальные команды и режим разработчика, даже если он выключен настройками сервера. При первом входе/регистрации администратору будет предложено выбирать сиид мира, заполненость мира в процентах и сложность игры (просто/нормально/сложно). После этого, игре понадобиться несколько минут, после чего вас вернёт в главное меню. Всё готово! Можно заходить снова и как обычный игрок создавать поселение.

Чтобы убедиться, что серер работает как надо, и проблема не в нем, а в сетевом доступе запустите игре там же, где запущен сервер введя в адресе localhost

Для того, что бы другие люди могли подключится к нему, нужно открыть порт 19019 в брандмауэре и на роутере (или тот который указан в Port в Settings.json).
Если у вас не белый IP то для подключения друзей можно использовать Hamachi или RadminVPN или что-то подобное.

Если вам не нужны чертежи, сделанные для торговли на больших серверах, и вы хотите играть с неизмененным игровым балансом, удалите папку Patches из мода по пути OnlineCity -> 1.1 -> patches. Удалить нужно в модах игры и в папке с модами на сервере, если у вас включена синхронизация, как описано ниже. Чертежи можно перебалансить на своё усмотрение. Достаточно открыть в любом блокноте TechBlueprints.xml и знать названия технологий на английском

Читайте дальше, чтобы настроить синхронизацию модов, либо этого уже достаточно для начала игры с друзьями! Просто скопируйте игру и моды, чтобы они были примерно одинаковыми.

Скопируйте все моды которые используете в папку Mods игры (т.е. не используйте моды из папки стима). Удалите лишние моды, чтобы они не закачивались всем игрокам.

Для начала и после каждого изменения модпака нужно очистить папку с настройками игры
```
"c:\Users\UserName\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config" 
```
(KeyPrefs.xml и Prefs.xml можете не удалять, они не будут участвовать в синхронизации).
Далее запускаем игру и настраиваем моды. Желательно создать новую игру и немного поиграть, чтобы моды записали свои настройки по умолчанию.

После этого в папку с сервером копируем папку Mods из игры, и папку Config из настроек игры. Должно получится так, что в одной папке с Server.exe находятся подпапки World, Mods и Config.

Далее открываем файл Settings.json блокнотом. По умолчанию он будет иметь такой вид:
```
{
  "ServerName": "Another OnlineCity Server",
  "SaveInterval": 10000,
  "Port": 19019,
  "Description": null,
  "IsModsWhitelisted": false,
  "DisableDevMode": false,
  "MinutesIntervalBetweenPVP": 0,
  "GeneralSettings": {
    "StorytellerDef": "",
    "Difficulty": "",
    "EnablePVP": false,
    "DisableGameSettings": false,
    "IncidentEnable": true,
    "IncidentCountInOffline": 2,
    "IncidentMaxMult": 10,
    "IncidentTickDelayBetween": 60000,
    "IncidentCostPrecent": 100,
    "IncidentPowerPrecent": 100,
    "IncidentCoolDownPercent": 100,
    "IncidentAlarmInHours": 10,
    "EquableWorldObjects": false,
    "ExchengeEnable": true,
    "ScenarioAviable": true,
    "ExchengePrecentCommissionConvertToCashlessCurrency": 50,
    "ExchengeCostCargoDelivery": 1000,
    "ExchengeAddPrecentCostForFastCargoDelivery": 100,
    "StartGameYear": -1,
    "EntranceWarning": null,
    "EntranceWarningRussian": null
  },
  "EqualFiles": [
    {
      "FolderType": 2,
      "ServerPath": "Mods",
      "NeedReplace": true,
      "IgnoreTag": null,
      "XMLFileName": null,
      "IgnoreFile": [
        ".cs",
        ".csproj",
        ".sln",
        ".gitignore",
        ".gitattributes"
      ],
      "IgnoreFolder": [
        "bin",
        "obj",
        ".vs"
      ]
    },
    {
      "FolderType": 0,
      "ServerPath": "Config",
      "NeedReplace": true,
      "IgnoreTag": null,
      "XMLFileName": null,
      "IgnoreFile": [
        "KeyPrefs.xml",
        "Knowledge.xml",
        "LastPlayedVersion.txt",
        "Prefs.xml"
      ],
      "IgnoreFolder": null
    },
    {
      "FolderType": 0,
      "ServerPath": "Config",
      "NeedReplace": true,
      "IgnoreTag": [
        "OnlineCity"
      ],
      "XMLFileName": "..\\HugsLib\\ModSettings.xml",
      "IgnoreFile": null,
      "IgnoreFolder": null
    }
  ],
  "ProtectingNovice": false,
  "DeleteAbandonedSettlements": false,
  "ColonyScreenFolderMaxMb": 0
}
```
Заменяем весь блок "EqualFiles": [ ... ], на этот:
```
  "EqualFiles": [
    {
      "FolderType": 2,
      "ServerPath": "Mods",
      "NeedReplace": true,
      "IgnoreTag": null,
      "XMLFileName": null,
      "IgnoreFile": [
        ".cs",
        ".csproj",
        ".sln",
        ".gitignore",
        ".gitattributes"
        "resources.assets.resS",
        ".git",
        ".obj",
        "packages",
        ".vs"
      ],
      "IgnoreFolder": [
        "bin",
        "obj",
        ".vs", 
        "packages"
      ]
    },
    {
      "FolderType": 0,
      "ServerPath": "Config",
      "NeedReplace": true,
      "IgnoreTag": null,
      "XMLFileName": null,
      "IgnoreFile": [
		" Mod___LocalCopy_Performance Optimizer_-18-12_PerformanceOptimizerMod.xml",
		"Mod_2664723367_PerformanceOptimizerMod.xml",
		"Mod_Dubs Mint Menus_DubsMintMenusMod.xml",
		"Mod_Dubs Performance Analyzer_Modbase.xml",
		"Mod___LocalCopy_FrameRateControl_-26-12_FrameRateControlMod.xml",
		"Mod_Camera+_CameraPlusMain.xml",
		"Mod_FrameRateControl_FrameRateControlMod.xml",
		"Mod_RimThemes_RimThemes.xml",
        "Mod_performance_optimizer_PerformanceOptimizerMod.xml",
        "KeyPrefs.xml",
        "Knowledge.xml",
        "LastPlayedVersion.txt",
        "Prefs.xml"
      ],
      "IgnoreFolder": [
        "RimHUD"
      ]
    }
  ],
```
Потом изменяем отдельные настройки:

  "IsModsWhitelisted": true,

Наверняка вам захочется установить ещё какие-то настройки, в более полном описании сервера обратите внимане на эти настройки:

  "DisableDevMode": true,
  "DisableGameSettings": true,
  "ProtectingNovice": true,
  
При запуске сервера могут происходить разнообразные ошибки, возможен вариант, когда после обращения в #техподдержка вас попросят кинуть настройки какого-либо мода в исключения, в таком случае, вы открываете файл Settings.json и добавляете в него нужный файл из папки «Config».
Например, файл "Mod_НазваниеМода.xml":
```
	{
      "FolderType": 0,
      "ServerPath": "Config",
      "NeedReplace": true,
      "IgnoreTag": null,
      "XMLFileName": null,
      "IgnoreFile": [
        "Mod_НазваниеМода.xml",
        "KeyPrefs.xml",
        "Knowledge.xml",
        "LastPlayedVersion.txt",
        "Prefs.xml"
      ],
      "IgnoreFolder": null
    } 
```