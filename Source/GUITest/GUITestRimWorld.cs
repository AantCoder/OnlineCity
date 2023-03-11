using GuideTestGUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sidekick.Sidekick.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUITest
{
    [TestClass]
    public class GUITestRimWorld
    {
        public GUITestRimWorldModelSetting Setting = new GUITestRimWorldModelSetting();

        public string ResourceFolder = "Resource";
        public string ResultFolder = "TestResult";


        [TestMethod]
        public void TestRimWorldStart()
        {
            return;
            //Запускаем тестовое окружение, сервер и игру
            var result = StartRimWorld((game, server, clientModLog) =>
            {
                //После запуска игры выполняются команды ниже:
                //Перемещаем мышку по центру игрового окна (для теста лучше настроить в окне)
                game.Mouse.Move(game.Width / 2, game.Height / 2);
                //ждем 1 сек
                Thread.Sleep(1000);
                //выходим и передаем, что тест прошел успешно

                /*

                var i = 0; //С помощью var объявляем переменную
                while (i < 5) //цикл пока условие верно, выполниться 5 раз
                {
                    Log("Цикл " + i.ToString()); //Записать в результат строку
                    i++; //то же что и i = i + 1;
                }

                if (5 * 5 == 25)
                {
                    Log("25"); //если условие выше истинно, то команды в операторных скобках выполняются
                }
                else
                    Log("???"); //вместо {...} можно сразу написать команду, если она одна

                Thread.Sleep(1000); //задержка в 1 сек

                //цикл ожидания указанное время, например 60 секунд:
                var ok = game.Wait(60000, () =>
                {
                    Log("Повторяем любые команды");
                    //Но в блоке нужно вызвать одно из:
                    return false; //возврат с ложью, для продолжения пока не кончится время
                    return true; //возврат с истиной, если нужно досрочно завершить и записать врезультат (тут это ok) значение истины
                });
                if (!ok) Log("Мы так и не дождались return истины"); // ! перед выражением работает как НЕ

                //Берем изображение из игры для анализа, пока не вызовем повторно изображение не изменится
                game.Graphics.UpdateScreenshot();

                //Находим кнопку Сетевая игра (имя файла "ButtonOnlineCity.png")
                var pos = game.Graphics.Find("ButtonOnlineCity");

                //в переменной pos координаты, если кнопка найдена
                if (pos.IsNull()) return LogText + "Нет кнопки :(("; //если не найдена, прерываем тест выводя текущий лог и фразу с ошибкой
                
                //можно искать в цикле пока кнопка не появится, максимум, например 60 секунд (символ _ только для удобства):
                pos = game.Graphics.FindWait("ButtonOnlineCity", 60_000); // (pos уже был объявлен, поэтому только устанавливаем значение)
                //выражение pos.IsNull() будет истино, если время истекло, а изображение не было найдено
                
                //можно объявить набор изображений
                var imgs = game.Graphics.ImageList("ButtonOnlineCity", "DebugLog");
                var pos = game.Graphics.FindWait(imgs, 60_000);

                //координаты можно использовать, чтобы передвинуть на них мышку
                game.Mouse.Move(pos);
                //можно сместить координаты на нужное количество пикселей относительно координат
                game.Mouse.Move(pos.Add(75, 30));

                var pp = new Point(10, 5); //можно создать координаты указав вручную
                Log(pp.ToString()); //вывести в текст результата, или так:
                Log("(" + pp.X.ToString() + ", " + pp.Y.ToString() + ")");

                //вот так можно задать прямоугольную область и искать в ней:
                var rect = new Rectangle(0, 0, game.Width, game.Height / 2); //это верхняя половина экрана
                var pos = game.Graphics.Find("WinXClose", rect: rect);

                game.Mouse.Move(0, 0); //переместить мышку в верхний левый угол окна
                game.Mouse.Move(game.Width / 2, game.Height / 2).Click(); //потом переметить в центр окна и кликнуть
                game.Mouse.Click(doubleClick: true); //двойной клик в текущей позиции
                game.Mouse.Click(leftButton: false); //кликнуть правой кнопкой (по умолчанию с true левой)
                game.Mouse.Click(down: true); //down: true -нажать и удерживать, пока не будет команда с down: false
                game.Mouse.Click(leftButton: false, doubleClick: true); //подобные параметры можно коминировать

                game.Keybord.Send("q"); //эмуляция нажатия кнопки q, не ввод текста, а именно как будто нажали на эту кнопку (с текущей раскладкой клавиатуры)
                //Можно комбинировать добавляя перед кнопкой символы: SHIFT +, CTRL ^, ALT %
                //Некоторые спец кнопки указываются в {}, например {ENTER} (Enter это также ~)
                game.Keybord.Send("^A127.0.0.1{tab}"); //будет выделен весь текст Ctrl+A введено 127.0.0.1 и нажата табуляция
                //Регистр не имеет значения. Эмуляция работает почти также как в функции SendKeys.Send https://msdn.microsoft.com/ru-ru/library/system.windows.forms.sendkeys.send(v=vs.110).aspx

                //Вот так можно объявить процедуру и потом вызывать её:
                Action proc = () =>
                {
                    Log("Вызов");
                };
                proc();
                proc();

                //Ниже до конца идет пример скрипта входа на локальный сервер под логином 111, паролем 111
                //Скрипт ждет все загрузки и закрывает консоль с ошибками после старта игры, если она будет
                //После загрузки игры, открывает-закрывает окно Онлайн и выходит из игры через меню

                //ждем пока не появиться кнопка Сетевая игра или консоль ошибок
                var imgs = game.Graphics.ImageList("ButtonOnlineCity", "DebugLog");
                var pos = game.Graphics.FindWait(imgs, 60_000);
                if (pos.IsNull()) return LogText + "Ошибка: кнопка Сетевая игра не появилась";

                //если это консоль ошибок
                pos = game.Graphics.Find("DebugLog");
                if (!pos.IsNull())
                {
                    //то ищем крестик, который относится к этому окну и закрываем консоль ошибок
                    //для этого ищем в узкой области правее найденной надписи
                    var rect = new Rectangle(pos.X, pos.Y - 17, game.Width - pos.X, 25);
                    pos = game.Graphics.Find("WinXClose", rect: rect);
                    if (pos.IsNull()) return LogText + "Ошибка: не получается закрыть консоль ошибок";

                    //закрываем окно по кремтику и анализируем после закрытия
                    game.Mouse.Move(pos).Click();
                    Thread.Sleep(500);
                    game.Graphics.UpdateScreenshot();
                }

                //Находим кнопку Сетевая игра
                var posButtonOnlineCity = game.Graphics.Find("ButtonOnlineCity");
                if (posButtonOnlineCity.IsNull()) return LogText + "Ошибка: нет кнопки Сетевая игра";

                //Цикл одижания минуту, завершается при return true;
                var ok = game.Wait(60_000, () =>
                {
                    //жмем кнопку Сетевая игра, обновляем экран
                    game.Mouse.Move(posButtonOnlineCity).Click();
                    game.Graphics.UpdateScreenshot();

                    //если в заголовке текста с "..." нет, то это не окно загрузки модов
                    pos = game.Graphics.Find("TitleWithDots");
                    if (pos.IsNull()) return true;

                    //жмем закрыть окно по крестику
                    pos = game.Graphics.Find("WinXClose");
                    game.Mouse.Move(pos).Click();

                    //дополнительно ждем секунду, чтобы не нажимать слишком часто
                    Thread.Sleep(1000);
                    return false;
                });
                if (!ok) return LogText + "Ошибка: не дождались окончания хеширования";

                //открываем окно и ищем опорную точку с иконкой диалога
                game.Mouse.Move(posButtonOnlineCity).Click();
                game.Graphics.UpdateScreenshot();
                var posLoginForm = game.Graphics.Find("IconBalloon");
                if (posLoginForm.IsNull()) return LogText + "Ошибка: нет опорной иконки с облачком диалога в окне входа";

                //жмем по полю с адресом сервера
                game.Mouse.Move(posLoginForm.Add(255, 88)).Click();
                //выделяем всё (чтобы удалилось) и вводим данные
                game.Keybord.Send("^A127.0.0.1");//localhost  

                //жмем по полю с логином
                game.Mouse.Move(posLoginForm.Add(255, 134)).Click();
                game.Keybord.Send("^A111");

                //жмем по полю с паролем
                game.Mouse.Move(posLoginForm.Add(255, 179)).Click();
                game.Keybord.Send("^A111");

                //жмем кнопку Вход
                game.Mouse.Move(posLoginForm.Add(100, 275)).Click();

                //ждем загрузку игры - появится меню в углу
                var posMenuInGame = game.Graphics.FindWait("MenuInGame", 60_000);
                if (posMenuInGame.IsNull()) return LogText + "Ошибка: не дождались загрузки сейва на сервере (нет иконки меню в игре)";

                //должна быть кнопка Онлайн
                pos = game.Graphics.Find("ButtonOnlineInGame");
                if (pos.IsNull()) return LogText + "Ошибка: после загрузки нет кнопки Онлайн";

                //кликаем два раза
                game.Mouse.Move(pos).Click();
                Thread.Sleep(500);
                game.Mouse.Click();

                //нажимаем на кнопку меню
                game.Mouse.Move(posMenuInGame).Click();

                //нажимаем кнопку выйти из игры
                game.Mouse.Move(posMenuInGame.Add(-313, -213)).Click();
                */
                return true;
            });
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestRimWorldLogin()
        {
            //Запускаем тестовое окружение, сервер и игру
            var result = StartRimWorld((game, server, clientModLog) =>
            {
                //После запуска игры выполняются команды ниже:
                //ждем пока не появиться кнопка Сетевая игра или консоль ошибок
                var imgs = game.Graphics.ImageList("ButtonOnlineCity", "DebugLog");
                var pos = game.Graphics.FindWait(imgs, 60_000);
                if (pos.IsNull()) return false;

                //если это консоль ошибок
                pos = game.Graphics.Find("DebugLog");
                if (!pos.IsNull())
                {
                    //то ищем крестик, который относится к этому окну и закрываем консоль ошибок
                    //для этого ищем в узкой области правее найденной надписи
                    var rect = new Rectangle(pos.X, pos.Y - 17, game.Width - pos.X, 25);
                    pos = game.Graphics.Find("WinXClose", rect: rect);
                    if (pos.IsNull()) return false;

                    //закрываем окно по кремтику и анализируем после закрытия
                    game.Mouse.Move(pos).Click();
                    Thread.Sleep(500);
                    game.Graphics.UpdateScreenshot();
                }

                //Находим кнопку Сетевая игра
                var posButtonOnlineCity = game.Graphics.Find("ButtonOnlineCity");
                if (posButtonOnlineCity.IsNull()) return false;

                //Цикл одижания минуту, завершается при return true;
                var ok = game.Wait(60_000, () =>
                {
                    //жмем кнопку Сетевая игра, обновляем экран
                    game.Mouse.Move(posButtonOnlineCity).Click();
                    game.Graphics.UpdateScreenshot();

                    //если в заголовке текста с "..." нет, то это не окно загрузки модов
                    pos = game.Graphics.Find("TitleWithDots");
                    if (pos.IsNull()) return true;

                    //жмем закрыть окно по крестику
                    pos = game.Graphics.Find("WinXClose");
                    game.Mouse.Move(pos).Click();

                    //дополнительно ждем секунду, чтобы не нажимать слишком часто
                    Thread.Sleep(1000);
                    return false;
                });
                if (!ok) return false;

                //открываем окно и ищем опорную точку с иконкой диалога
                game.Mouse.Move(posButtonOnlineCity).Click();
                game.Graphics.UpdateScreenshot();
                var posLoginForm = game.Graphics.Find("IconBalloon");
                if (posLoginForm.IsNull()) return false;

                //жмем по полю с адресом сервера
                game.Mouse.Move(posLoginForm.Add(255, 88)).Click();
                //выделяем всё (чтобы удалилось) и вводим данные
                game.Keybord.Send("^A127.0.0.1");//localhost  

                //жмем по полю с логином
                game.Mouse.Move(posLoginForm.Add(255, 134)).Click();
                game.Keybord.Send("^A111");

                //жмем по полю с паролем
                game.Mouse.Move(posLoginForm.Add(255, 179)).Click();
                game.Keybord.Send("^A111");

                //жмем кнопку Вход
                game.Mouse.Move(posLoginForm.Add(100, 275)).Click();

                //ждем загрузку игры - появится меню в углу
                var posMenuInGame = game.Graphics.FindWait("MenuInGame", 60_000);
                if (posMenuInGame.IsNull()) return false;

                //должна быть кнопка Онлайн
                pos = game.Graphics.Find("ButtonOnlineInGame");
                if (pos.IsNull()) return false;

                //кликаем два раза
                game.Mouse.Move(pos).Click();
                Thread.Sleep(500);
                game.Mouse.Click();

                //нажимаем на кнопку меню
                game.Mouse.Move(posMenuInGame).Click();

                //нажимаем кнопку выйти из игры
                game.Mouse.Move(posMenuInGame.Add(-313, -213)).Click();

                //координаты панели с пешками (627, 59), размер окна (1338, 923)
                return true;
            });
            Assert.IsTrue(result);
        }

        public T StartRimWorld<T>(Func<GuideUI, GuideUI, GuideUI, T> test)
        {
            if (Directory.Exists(ResultFolder)) Directory.Delete(ResultFolder, true);
            Directory.CreateDirectory(ResultFolder);

            T result = default;
            using (var loaderImage = new LoaderImage(ResourceFolder + @"\{0}.png"))
            using (var server = new GuideUI())
            {
                server.LoaderImage = loaderImage;
                server.EnvironmentSourceFolderName = Setting.ServerFolder;
                server.EnvironmentWorkFolderName = Setting.TempFolder + @"\Server";
                server.EnvironmentBackupFolderName = null;
                server.ResultTestLogFileName = Path.Combine(ResultFolder, "serverLog.txt");
                server.NeedRecoveryWorkFolder = false;
                server.StartEnvironment();
                server.StartProcess(server.EnvironmentWorkFolderName + @"\Server.exe", false);
                server.SetLogFileFromFolder(server.EnvironmentWorkFolderName + @"\World");

                using (var game = new GuideUI())
                {
                    game.LoaderImage = loaderImage;
                    game.EnvironmentSourceFolderName = Setting.TestConfigFolder;
                    game.EnvironmentWorkFolderName = Setting.GameConfigFolder;
                    game.EnvironmentBackupFolderName = Setting.TempFolder + @"\BackupConfig";
                    game.ResultTestLogFileName = Path.Combine(ResultFolder, "gameLog.txt");
                    game.NeedRecoveryWorkFolder = true;
                    game.StartEnvironment();
                    game.StartProcess(Setting.GameExec);
                    //game.ConnectProcess(Setting.WindowsTitle);
                    game.LogFileName = Setting.GameLogFile;

                    Thread.Sleep(5000);
                    var clientModLog = new GuideUI(); //сокращенный запуск только для логов
                    clientModLog.ResultTestLogFileName = Path.Combine(ResultFolder, "clientModLog.txt");
                    clientModLog.SetLogFileFromFolder(Setting.ModLogFolder);

                    result = test(game, server, clientModLog);

                    clientModLog.GetLogNewText();
                    game.GetLogNewText();
                    server.GetLogNewText();
                }
            }
            return result;
        }

        /// <summary>
        /// Запустить как-нибудь руками.
        /// Сохраняет из игровой папки GameConfigFolder в тестовую TestConfigFolder (а во время тестов наоборот устанавливает).
        /// В принципе проще руками скопировать :)
        /// </summary>
        public void CreateDataFromGame()
        {
            using (var game = new GuideUI())
            {
                game.EnvironmentSourceFolderName = Setting.TestConfigFolder;
                game.EnvironmentWorkFolderName = Setting.GameConfigFolder;
                game.EnvironmentBackupFolderName = Setting.TempFolder + @"\BackupConfig";
                game.NeedRecoveryWorkFolder = true;
                game.ModeCreateEnvironmentSource = true; //<- отличия в этом
                game.StartEnvironment();
            }
        }

    }
}
