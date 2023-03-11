using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace Sidekick.Sidekick.Model
{
    public class SKKeybord : SKProcess
    {
        private bool KeybordStateControl = false;
        private bool KeybordStateShift = false;
        private bool KeybordStateAlt = false;

        private Thread BGWorker = null;
        private bool BGWorkerNeedExit = false;

        private Dictionary<long, string> SendKeys = new Dictionary<long, string>();
        private long KeysIndexMax = 0;
        private long KeysIndexReady = 0;

        private InputSimulator Simulator = new InputSimulator();

        public SKKeybord(IntPtr proc)
            : base(proc)
        { 
        }

        /// <summary>
        /// Отправить нажатие клавиш приложению. Можно запускать из разных потоков.
        /// SHIFT +, CTRL ^, ALT %, Enter ~ или {ENTER}
        /// Регистр не имеет значения.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="wait"></param>
        public void Send(string keys, bool wait = true)
        { 
            Start();
            long index;
            lock(SendKeys)
            {
                SendKeys.Add(index = ++KeysIndexMax, keys);
            }
            if (wait)
                while (index > KeysIndexReady)
                {
                    Thread.Sleep(5);
                }
        }

        /// <summary>
        /// Запускает процесс, если требуется
        /// </summary>
        private void Start()
        {
            BGWorkerNeedExit = false;
            if (BGWorker != null) return;
            BGWorker = new Thread(BGWorker_DoWork);
            BGWorker.IsBackground = true;
            BGWorker.Start();
        }

        /// <summary>
        /// Останавливает поток, когда известно, что новых задач не будет вообще или в ближайшее время
        /// </summary>
        public void Stop()
        {
            BGWorkerNeedExit = true;
        }

        private void BGWorker_DoWork()
        {
            while (!BGWorkerNeedExit)
            {
                Thread.Sleep(0);
                bool existWork = false;

                long index = KeysIndexReady + 1;
                string keys;
                lock (SendKeys)
                {
                    if (SendKeys.ContainsKey(index))
                    {
                        existWork = true;
                        keys = SendKeys[index];
                    }
                    else keys = null;
                }
                if (!string.IsNullOrEmpty(keys))
                {
                    keys = keys.ToLower();

                    //парсим строку по правилам SendKeys.Send https://msdn.microsoft.com/ru-ru/library/system.windows.forms.sendkeys.send(v=vs.110).aspx

                    bool needControl = false;
                    bool needShift = false;
                    bool needAlt = false;
                    //внутри скобок - не обнуляем режимы need*
                    bool brackets = false;

                    while (keys.Length > 0)
                    {
                        if (BGWorkerNeedExit) break;
                        //если не null, значит это команда эмуляции символа
                        char? toSend = null;
                        //количество этих символов
                        int countSend = 1;
                        //непечатный символ (без события WM_CHAR и без ToUp)
                        bool notCharSend = false;
                        //сколько знаков удалить из keys до след итерации
                        int countDelChar = 1;
                        //парсим
                        switch (keys[0])
                        {
                            case '(': brackets = true;
                                break;
                            case ')': brackets = false;
                                break;
                            case '+': needShift = true;
                                break;
                            case '^': needControl = true;
                                break;
                            case '%': needAlt = true;
                                break;
                            case '~': toSend = (char)Keys.Enter;//'\r';
                                break;
                            case '.': toSend = (char)Keys.OemPeriod;
                                break;
                            case ',': toSend = (char)Keys.Oemcomma;
                                break;
                            case '{':
                                {
                                    int pos = keys.IndexOf('}');
                                    if (pos < 0) break;
                                    countDelChar = pos + 1;
                                    string subi = keys.Substring(1, pos - 1);
                                    //точный символ
                                    if (subi.Length == 1)
                                    {
                                        toSend = subi[0];
                                        break;
                                    }
                                    //находим строку после пробела
                                    pos = subi.LastIndexOf(' ');
                                    string sub1;
                                    if (pos > 0)
                                    {
                                        sub1 = subi.Substring(0, pos - 1);
                                        if (!int.TryParse(subi.Substring(pos), out countSend)) break;
                                    }
                                    else
                                        sub1 = subi;

                                    //определяем клавишу
                                    switch (sub1)
                                    {
                                        #region
                                        case "~": toSend = (char)Keys.Enter;// '\r';
                                            break;
                                        case "backspace":
                                        case "bs":
                                        case "bksp": toSend = (char)Keys.Back; notCharSend = true;
                                            break;
                                        case "break": toSend = (char)Keys.Back; notCharSend = true;
                                            break;
                                        case "capslock": toSend = (char)Keys.CapsLock; notCharSend = true;
                                            break;
                                        case "delete":
                                        case "del": toSend = (char)Keys.Delete; notCharSend = true;
                                            break;
                                        case "down": toSend = (char)Keys.Down; notCharSend = true;
                                            break;
                                        case "end": toSend = (char)Keys.End; notCharSend = true;
                                            break;
                                        case "enter": toSend = (char)Keys.Enter;
                                            break;
                                        case "esc": toSend = (char)Keys.Escape; notCharSend = true;
                                            break;
                                        case "help": toSend = (char)Keys.Help; notCharSend = true;
                                            break;
                                        case "home": toSend = (char)Keys.Home; notCharSend = true;
                                            break;
                                        case "insert":
                                        case "ins": toSend = (char)Keys.Insert; notCharSend = true;
                                            break;
                                        case "left": toSend = (char)Keys.Left; notCharSend = true;
                                            break;
                                        case "numlock": toSend = (char)Keys.NumLock; notCharSend = true;
                                            break;
                                        case "pgdn": toSend = (char)Keys.PageDown; notCharSend = true;
                                            break;
                                        case "pgup": toSend = (char)Keys.PageUp; notCharSend = true;
                                            break;
                                        case "prtsc": toSend = (char)Keys.PrintScreen; notCharSend = true;
                                            break;
                                        case "right": toSend = (char)Keys.Right; notCharSend = true;
                                            break;

                                        case "scrolllock": toSend = (char)Keys.Scroll; notCharSend = true;
                                            break;
                                        case "tab": toSend = (char)Keys.Tab;
                                            break;
                                        case "up": toSend = (char)Keys.Up; notCharSend = true;
                                            break;
                                        case "f1": toSend = (char)Keys.F1; notCharSend = true;
                                            break;
                                        case "f2": toSend = (char)Keys.F2; notCharSend = true;
                                            break;
                                        case "f3": toSend = (char)Keys.F3; notCharSend = true;
                                            break;
                                        case "f4": toSend = (char)Keys.F4; notCharSend = true;
                                            break;
                                        case "f5": toSend = (char)Keys.F5; notCharSend = true;
                                            break;
                                        case "f6": toSend = (char)Keys.F6; notCharSend = true;
                                            break;
                                        case "f7": toSend = (char)Keys.F7; notCharSend = true;
                                            break;
                                        case "f8": toSend = (char)Keys.F8; notCharSend = true;
                                            break;
                                        case "f9": toSend = (char)Keys.F9; notCharSend = true;
                                            break;
                                        case "f10": toSend = (char)Keys.F10; notCharSend = true;
                                            break;
                                        case "f11": toSend = (char)Keys.F11; notCharSend = true;
                                            break;
                                        case "f12": toSend = (char)Keys.F12; notCharSend = true;
                                            break;
                                        case "f13": toSend = (char)Keys.F13; notCharSend = true;
                                            break;
                                        case "f14": toSend = (char)Keys.F14; notCharSend = true;
                                            break;
                                        case "f15": toSend = (char)Keys.F15; notCharSend = true;
                                            break;
                                        case "f16": toSend = (char)Keys.F16; notCharSend = true;
                                            break;
                                        case "add": toSend = (char)Keys.Add;
                                            break;
                                        case "subtract": toSend = (char)Keys.Subtract;
                                            break;
                                        case "multiply": toSend = (char)Keys.Multiply;
                                            break;
                                        case "divide": toSend = (char)Keys.Divide;
                                            break;
                                        default: if (sub1.Length == 1) toSend = sub1[0];
                                            break;
                                        #endregion
                                    }
                                }
                                break;

                            default: toSend = keys[0];
                                break;
                        }

                        //эмулируем
                        if (toSend != null)
                        {
                            SetControl(needControl, needShift, needAlt);
                            while (countSend-- > 0)
                            {
                                if (BGWorkerNeedExit) break;
                                Wait(true, true);
                                var skk = notCharSend ? (int)toSend.Value : (int)char.ToUpper(toSend.Value);
                                SendKey(true, skk);
                                Wait(null, true);
                                SendKey(false, skk);
                                Wait(false, true);
                            }
                            if (!brackets)
                            {
                                needControl = false;
                                needShift = false;
                                needAlt = false;
                            }
                        }

                        //подготовка к следующиму символу
                        keys = keys.Substring(countDelChar);
                    }

                }

                if (existWork)
                {
                    SetControl(false, false, false);
                    SendKeys.Remove(index);
                    KeysIndexReady = index;
                } 
                else
                    Thread.Sleep(5);
            }
            BGWorker = null;
        }

        private void SetControl(bool needControl, bool needShift, bool needAlt)
        {
            ActControl(Keys.ControlKey, needControl);
            ActControl(Keys.ShiftKey, needShift);
            ActControl(Keys.Menu, needAlt);
        }

        /// <summary>
        /// Обеспечивает вход или выход из режима
        /// </summary>
        /// <param name="VKKey"></param>
        /// <param name="down"></param>
        private bool ActControl(Keys VKKey, bool down)
        {
            if (VKKey == Keys.ControlKey && (KeybordStateControl && down || !KeybordStateControl && !down)
                || VKKey == Keys.Menu && (KeybordStateAlt && down || !KeybordStateAlt && !down)
                || VKKey == Keys.ShiftKey && (KeybordStateShift && down || !KeybordStateShift && !down)) return false;
            Wait(true, true);
            SendKey(down, (int)VKKey);
            Wait(false, true);
            if (VKKey == Keys.ControlKey) KeybordStateControl = down;
            if (VKKey == Keys.Menu) KeybordStateAlt = down;
            if (VKKey == Keys.ShiftKey) KeybordStateShift = down;
            return true;

        }

        private void SendKey(bool down, int vkKey)
        {
            //CheckForegroundWindow();
            if (down) 
                Simulator.Keyboard.KeyDown((VirtualKeyCode)vkKey);
            else
                Simulator.Keyboard.KeyUp((VirtualKeyCode)vkKey);
        }

        /// <summary>
        /// Случайное ожидание до и после действия события клавиши.
        /// Переключение режимов (controlKey) (со своим одижанием до и после) происходит после задержки печати целевой клавиши.
        /// </summary>
        /// <param name="beforeAct">До или после события; null - во время</param>
        /// <param name="controlKey">Это управляющая клавиша (shift, alt, control)</param>
        /// <returns></returns>
        private int Wait(bool? beforeAct, bool controlKey)
        {
            int wait = beforeAct == null ? Rnd.Next(80, 150) /*№1*/
                : beforeAct.Value
                    ? controlKey ? Rnd.Next(5, 10) /*№2*/ : Rnd.Next(30, 70) /*№3*/
                    : controlKey ? Rnd.Next(30, 50) /*№4*/ : Rnd.Next(150, 300) /*№5*/;
            //Например, при вводе аББа будут следующие задержки:
            // №3 {a down} №1 {a up} №5 
            // №3 №2 {shift down} №4 {б down} №1 {б up} №5
            // №3 {б down} №1 {б up} №5 
            // №3 №2 {shift up} №4 {a down} №1 {a up} №5
            // Т.е.:
            // 50 {a down} 115 {a up} 242 {shift down} 40 {б down} 115 {б up} 235 {б down} 115 {б up} 242 {shift up} 40 {a down} 115 {a up} 185
            Thread.Sleep(wait);
            return wait;
        }

        public string GetClipboard() => GetClipboardText();
        public void SetClipboard(string text) => SetClipboardText(text);

        public static string GetClipboardText()
        {
            var text = "";
            Thread thread = new Thread(() => text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty);
            thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            thread.Start();
            thread.Join();
            return text;
        }
        public static void SetClipboardText(string text)
        {
            Thread thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            thread.Start();
            thread.Join();
        }

    }
}
