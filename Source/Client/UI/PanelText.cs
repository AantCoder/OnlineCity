using Model;
using OCUnion;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public class PanelText : DialogControlBase
    {
        /*
            Функции отрисовки GUI.* очень ограничены. Есть функция вывода красивого текста Rich Text (GUILayout.Label()), 
        но в нем не хватает двух важных компонентов: вывода изображений и реакцию на клик.
            Если встречается наш собственный тэг, то распознаем его. Это или картинка (самозакрытый тэг <img name="" />) 
        или тэг с событием клика (<btn act="">...</btn>). Параметр только один у обоих тэгов, определяется так: 
        значение между = (или с начала) и > (или />), потом Trim, потом убирать " если они с двух сторон.
            Отдельно обрабатывается тэг <l></l> внутри которой фраза для локализации игрой. Также можно локализовать def'ы, 
        например: <l>Ocean.label</l> будет "океан", <l>Ocean.description</l> будет "Открытый океан. Подходящее место для рыб, но не для вас."

            Принимаемые данные: область Rect, строка текста с тэгами string, и два словаря имя-объект 
        с данными по картинке, и имя-событие клика(в событие можно передать имя, порядковый номер тэга, 
        возможно содержимое тэга, что под руку подвернется, можно ничего не передавать).

            В объекте картинки будет сама текстура для вывода, и размер. Думаю при использовании такой готовый словарь иконок 
        можно загрузить при старте и передавать в функцию из какого-нибудь статика всегда одинаковый набор.

            По реализации: Этот компонент в заданной области отрисовывает текст с прямым контролем переносов. 
        Или, другими словами мы берем текст, который принимает GUILayout.Label дробим его на слова
        и отдельно вызываем эту функцию для каждого слова пока строка не кончится, после чего переходим на новую строку и продолжаем, 
        пока полностью не выйдем за область печати.
            Есть методы возвращающие размер, который займет текст: GUIStyle.CalcSize (можно ещё посмотреть ещё такой интересный класс как Verse.Text). 
        Измеряем очередное слово, если помещается в строку рисуем, изменяем наше смещение, продолжаем.
            Два варианта вывода, либо с коэф.: Text.CalcSize и Widgets.Label
            Либо базовый: GUI.skin.textField.CalcSize и GUI.Label
         */

        public string PrintText { get; set; }

        public static Dictionary<string, TagBtn> GlobalBtns { get; set; } = new Dictionary<string, TagBtn>();
        public Dictionary<string, TagBtn> Btns { get; set; } = new Dictionary<string, TagBtn>();
        public static Dictionary<string, Texture2D> GlobalImgs { get; set; } = new Dictionary<string, Texture2D>();
        public Dictionary<string, Texture2D> Imgs { get; set; } = new Dictionary<string, Texture2D>();

        public static ConcurrentDictionary<string, string> LanguageInjections { get; set; } = new ConcurrentDictionary<string, string>();

        private static ConcurrentDictionary<string, Tuple<ActionTree, float>> Optimization = new ConcurrentDictionary<string, Tuple<ActionTree, float>>();
        private static DateTime OptimizationTime;

        private DateTime FirstCalcDrow = DateTime.MinValue;
        private int FirstReady = 0;

        /// <summary>
        /// Отрисовка компонента
        /// </summary>
        /// <param name="inRect"></param>
        /// <param name="dynamicHeight">Более приоритетная высота, до которой будет отрисовано</param>
        /// <returns>Высота, которой достаточно для контента</returns>
        public float Drow(Rect inRect, float dynamicHeight = 0)
        {
            if (PrintText == null
                || PrintText.Length < 1
                || inRect.width < 1f || inRect.height < 1f)
                return 0f;

            var key = inRect.ToString() + dynamicHeight.ToString() + PrintText.GetHashCode();

            Tuple<ActionTree, float> res;

            //if (FirstReady < 2 && FirstCalcDrow > DateTime.MinValue && FirstCalcDrow.AddSeconds(2) < DateTime.UtcNow)
            //{
            //    Log.Message("Recalc " + FirstReady);
            //    //принудительно обновляем через 2 и через 6 секунд после первого отображения, для ожидания загрузки возможных картинок
            //    FirstReady++; 
            //    FirstCalcDrow = FirstCalcDrow.AddSeconds(6 - 2);

            //    res = CalcDrow(inRect, dynamicHeight);
            //    Optimization[key] = res;
            //}
            //else
            {
                if ((DateTime.UtcNow - OptimizationTime).TotalSeconds > 60
                    || Optimization.Count > 100)
                {
                    OptimizationTime = DateTime.UtcNow;
                    Optimization = new ConcurrentDictionary<string, Tuple<ActionTree, float>>();
                }
                res = Optimization.GetOrAdd(key, k => CalcDrow(inRect, dynamicHeight));
            }

            var act = res.Item1 as ActionTree;
            do act.Act();
            while ((act = act.Next) != null);

            return res.Item2;
        }

        private Tuple<ActionTree, float> CalcDrow(Rect inRect, float dynamicHeight = 0)
        {
            if (FirstCalcDrow == DateTime.MinValue) FirstCalcDrow = DateTime.UtcNow;

            //Text.Font = GameFont.Small;
            //GUI.skin.textField.wordWrap = false;
            ActionTree startAction = new ATStart();
            ActionTree currentAction = startAction;

            var log = inRect.ToString() + Environment.NewLine;

            var iconHeightDefault = Text.CalcSize("H").y;

            var text = PrintText.Replace("\r", "");
            var width = inRect.width;
            var height = dynamicHeight <= 0 ? inRect.height : dynamicHeight;

            //информация по открытому тэгу l
            //var tagLStartX = 0f; //относительная координата в строке начала действия

            //информация по открытому тэгу btn
            TagBtn tagBtnAct = null; //название в словаре, по которому определяется действие (и визуальное и событие)
            string tagBtnArg = null; //если указан атрибут arg=
            var tagBtnStartX = 0f; //относительная координата в строке начала действия

            var totalChars = 0;
            var currentWord = "";
            Vector2 currentWordSize = new Vector2();
            var curY = 0f;
            var curX = 0f; //позиция вывода до собираемого текста currentWorld
            var curHeight = 0f;
            Action printCurrent = () =>
            {
                if (currentWord.Length > 0)
                {
                    //Widgets.Label(new Rect(inRect.x + curX, inRect.y + curY, currentWordSize.x, currentWordSize.y), currentWord.Replace("\n", ""));
                    currentAction.Next = new ATLabel()
                    {
                        rect = new Rect(inRect.x + curX, inRect.y + curY, currentWordSize.x, currentWordSize.y),
                        label = currentWord.Replace("\n", ""),
                    };
                    currentAction = currentAction.Next;

                    curX += currentWordSize.x;
                    if (curHeight < currentWordSize.y) curHeight = currentWordSize.y; 
                    log += $" B({curX})print({currentWord.Length}) " + currentWord.Replace("\n", "");
                    currentWord = "";
                    currentWordSize = new Vector2();
                }
            };
            Action printBtnAct = () =>
            {
                if (tagBtnAct != null && tagBtnStartX != curX && curHeight > 0)
                {
                    log += $" btn({tagBtnStartX}-{curX}, {tagBtnArg})";
                    var tagRect = new Rect(inRect.x + tagBtnStartX, inRect.y + curY, curX - tagBtnStartX, curHeight);
                    //if (Mouse.IsOver(tagRect))
                    //{
                    //    if (tagBtnAct.HighlightIsOver) Widgets.DrawHighlight(tagRect);
                    //    if (tagBtnAct.ActionIsOver != null) tagBtnAct.ActionIsOver(tagBtnArg);
                    //}
                    //if (Widgets.ButtonInvisible(tagRect))
                    //{
                    //    if (tagBtnAct.ActionClick != null) tagBtnAct.ActionClick(tagBtnArg);
                    //}
                    //if (!string.IsNullOrEmpty(tagBtnAct.Tooltip))
                    //    TooltipHandler.TipRegion(tagRect, tagBtnAct.Tooltip);
                    currentAction.Next = new ATBtnAct()
                    {
                        tagRect = tagRect, tagBtnArg = tagBtnArg, tagBtnAct = tagBtnAct
                    };
                    currentAction = currentAction.Next;
                }
            };
            //перед нормальным анализом применяем локализацию отдельно обрабатываем тэги <l>
            while (true)
            {
                var posB = text.IndexOf("<l>");
                if (posB < 0) break;
                var posE = text.IndexOf("</l>", posB);
                if (posE < 0) break;
                string tr;

                var sub = text.Substring(posB + 3, posE - posB - 3);
                tr = ChatController.ServerCharTranslate(sub, true);

                //если перевод не сработал, то переводим через свойства дефов
                if (tr.Contains("."))
                {
                    tr = LanguageInjections.GetOrAdd(tr, (k) =>
                            LanguageDatabase.activeLanguage.defInjections
                                .Select(di => di.injections.FirstOrDefault(dii => dii.Value.path.ToLower() == tr.ToLower()).Value?.injection)
                                .FirstOrDefault(dii => dii != null)
                        ) ?? tr;
                }

                tr = tr.TranslateCache();

                text = text.Substring(0, posB) + tr + text.Substring(posE + 4);
            }
            try
            {
                foreach (var word in ParceText(text))
                {
                    log += Environment.NewLine + $"{curY} {curX} {currentWord.Length}. \"{word}\"";

                    totalChars += word.Length;
                    var lastLoop = totalChars == text.Length;

                    //проверяем нужно ли обрабатывать этот тэг, или пропускаем дальше как текст
                    if (word[0] == '<' && word.Length > 1)
                    {
                        /*if (word.ToLower().StartsWith("<l"))
                        {
                            printCurrent();
                            tagLStartX = curX;
                        }
                        else if (word.ToLower().StartsWith("</l"))
                        {
                            word = ChatController.ServerCharTranslate(currentWord)
                                + " " + currentWord.TranslateCache()
                                + " " + currentWord;
                            Loger.Log("test:" + currentWord);
                            currentWord = "";
                            currentWordSize = new Vector2();
                        }
                        else*/
                        if (word.ToLower().StartsWith("<btn "))
                        {
                            printCurrent();
                            tagBtnStartX = curX;
                            tagBtnAct = null;
                            tagBtnArg = null;
                            string name = null;
                            var className = "";
                            var d = "";
                            foreach (var arg in ParceAttributes(word))
                            {
                                if (arg.Second == "")
                                {
                                    name = arg.First;
                                }
                                else if (arg.First.ToLower() == "name")
                                {
                                    name = arg.Second;
                                }
                                else if (arg.First.ToLower() == "act")
                                {
                                    name = arg.Second;
                                }
                                else if (arg.First.ToLower() == "arg")
                                {
                                    tagBtnArg = arg.Second;
                                }
                                else if (arg.First.ToLower() == "class")
                                {
                                    className = arg.Second;
                                }
                                else if (arg.First.ToLower() == "d")
                                {
                                    d = arg.Second;
                                }
                            }
                            if (!Btns.TryGetValue(name, out tagBtnAct))
                            {
                                if (!GlobalBtns.TryGetValue(name, out tagBtnAct))
                                {
                                    if (string.IsNullOrEmpty(className))
                                        tagBtnAct = null;
                                    else
                                    {
                                        //создаем объект по данным в тэгах class и d
                                        tagBtnAct = TagBtn.GetByClass(className, d, tagBtnArg);
                                        Btns.Add(name, tagBtnAct);
                                    }
                                }
                            }

                            continue;
                        }
                        else if (word.ToLower().StartsWith("</btn"))
                        {
                            printCurrent();
                            printBtnAct();
                            tagBtnAct = null;
                            tagBtnArg = null;
                            continue;
                        }
                        else if (word.ToLower().StartsWith("<img "))
                        {
                            printCurrent();

                            Func<Texture2D> getIcon = null;
                            Texture2D icon = null;
                            var name = "";
                            var h = 0;
                            var w = 0;
                            foreach (var agr in ParceAttributes(word))
                            {
                                if (agr.Second == "")
                                {
                                    name = agr.First;
                                }
                                else if (agr.First.ToLower() == "name")
                                {
                                    name = agr.Second;
                                }
                                else if (agr.First.ToLower() == "defName".ToLower())
                                {
                                    var defName = agr.Second;
                                    icon = GeneralTexture.Get.GetDefTexture(defName);
                                }
                                else if (agr.First.ToLower() == "height")
                                {
                                    if (int.TryParse(agr.Second, out int nvi)) h = nvi;
                                }
                                else if (agr.First.ToLower() == "width")
                                {
                                    if (int.TryParse(agr.Second, out int nvi)) w = nvi;
                                }
                            }

                            if (icon == null)
                            {
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                if (name.StartsWith("pl_") && name.Length > 4)
                                {
                                    getIcon = () => GeneralTexture.Get.ByName(name);
                                    icon = getIcon();
                                }
                                else if (!Imgs.TryGetValue(name, out icon))
                                {
                                    if (!GlobalImgs.TryGetValue(name, out icon))
                                    {
                                        try
                                        {
                                            icon = ContentFinder<Texture2D>.Get(name, false);
                                        }
                                        catch
                                        {
                                            icon = null;
                                        }
                                        if (icon != null) GlobalImgs.Add(name, icon);
                                    }
                                }
                            }
                            if (icon == null) continue;


                            var iconHeight = h > 0 ? h : iconHeightDefault;
                            var iconWidth = w > 0 ? w : icon.width * iconHeight / icon.height;

                            log += $" img({w}, {h})->({iconWidth}, {iconHeight}) " + word;

                            //перевод на новую строку, если текущая не пустая и в неё не умещаемся
                            if (curX > 0 && curX + iconWidth > width)
                            {
                                printBtnAct();

                                tagBtnStartX = 0;
                                curX = 0;
                                curY += curHeight;
                                curHeight = 0f;
                                //нужно ли завершить вывод
                                if (curY >= height)
                                {
                                    log += " {return0} " + curY;
                                    return new Tuple<ActionTree, float>(startAction, curY);
                                }
                            }

                            //GUI.DrawTexture(new Rect(inRect.x + curX, inRect.y + curY, iconWidth, iconHeight), icon);
                            currentAction.Next = new ATDrawTexture()
                            {
                                position = new Rect(inRect.x + curX, inRect.y + curY, iconWidth, iconHeight),
                                image = getIcon ?? (() => icon),
                            };
                            currentAction = currentAction.Next;

                            curX += iconWidth;
                            if (curHeight < iconHeight) curHeight = iconHeight;

                            if (lastLoop)
                            {
                                printBtnAct();

                                tagBtnStartX = 0;
                                curX = 0;
                                curY += curHeight;
                                curHeight = 0f;
                                log += " {lastLoop img} ";
                            }
                            continue;
                        }
                    }

                    //определяем нужно ли завершить строку до слова
                    //var size = GUI.skin.textField.CalcSize(new GUIContent((currentWorld + word).Replace("\n", "")));
                    var size = Text.CalcSize((currentWord + word).Replace("\n", ""));
                    //нужно ли присоединить новую часть до печати (всегда, только если влазиет по длинне)
                    var concat = curX + size.x <= width || currentWord == "" && curX == 0;
                    if (concat)
                    {
                        currentWord += word;
                        currentWordSize = size;
                    }
                    //нужно ли напечатать собранное
                    var newLine = curX + size.x > width || currentWord[currentWord.Length - 1] == '\n' || lastLoop;
                    if (newLine)
                    {
                        log += " {newLine} ";
                        printCurrent();
                    }
                    //нужно ли завершить строку
                    if (newLine)
                    {
                        printBtnAct();

                        tagBtnStartX = 0;
                        curX = 0;
                        curY += curHeight;
                        curHeight = 0f;
                        if (lastLoop)
                        {
                            printCurrent();
                            printBtnAct();
                        }
                        //нужно ли завершить вывод
                        if (curY >= height)
                        {
                            log += " {return1} " + curY;
                            return new Tuple<ActionTree, float>(startAction, curY);
                        }
                    }
                    //если мы не присоединяли строку до вывода, то присоединяем сейчас
                    if (!concat)
                    {
                        log += " {!concat} ";
                        currentWord += word;
                        //currentWorldSize = GUI.skin.textField.CalcSize(new GUIContent(currentWorld.Replace("\n", "")));
                        currentWordSize = Text.CalcSize(currentWord.Replace("\n", ""));

                        //повторно проверяем нужно ли переносить после этого добавления
                        newLine = currentWord[currentWord.Length - 1] == '\n';
                        if (newLine) printCurrent();
                        //копия блока выше: нужно ли завершить строку
                        if (newLine)
                        {
                            printBtnAct();

                            tagBtnStartX = 0;
                            curX = 0;
                            curY += curHeight;
                            curHeight = 0f;
                            if (lastLoop)
                            {
                                printCurrent();
                                printBtnAct();
                            }
                            //нужно ли завершить вывод
                            if (curY >= height)
                            {
                                log += " {return2} " + curY;
                                return new Tuple<ActionTree, float>(startAction, curY);
                            }
                        }
                    }
                }
            }
            finally
            {
                //Text.Font = GameFont.Tiny;
                currentAction.Next = new ATFinish();
                currentAction = currentAction.Next;

                inRect.y += 100;
                //Widgets.Label(inRect, log);
                //Loger.Log("PanelText " + log);
            }
            //Loger.Log(" {return3} " + curY);
            return new Tuple<ActionTree, float>(startAction, curY);
        }

        private abstract class ActionTree
        {
            public ActionTree Next = null;
            public abstract void Act();
        }
        private class ATStart : ActionTree
        {
            public override void Act()
            {
                Text.Font = GameFont.Small;
                GUI.skin.textField.wordWrap = false;
            }
        }
        private class ATLabel : ActionTree
        {
            public Rect rect;
            public string label;
            public override void Act()
            {
                Widgets.Label(rect, label);
            }
        }
        private class ATBtnAct : ActionTree
        {
            public Rect tagRect;
            public string tagBtnArg;
            public TagBtn tagBtnAct;
            public override void Act()
            {
                if (Mouse.IsOver(tagRect))
                {
                    if (tagBtnAct.HighlightIsOver) Widgets.DrawHighlight(tagRect);
                    if (tagBtnAct.ActionIsOver != null) tagBtnAct.ActionIsOver(tagBtnArg);
                }
                if (Widgets.ButtonInvisible(tagRect))
                {
                    //Loger.Log("TestTagBtn 3 ButtonInvisible " + tagRect);
                    if (tagBtnAct.ActionClick != null) tagBtnAct.ActionClick(tagBtnArg);
                }
                if (!string.IsNullOrEmpty(tagBtnAct.Tooltip))
                    TooltipHandler.TipRegion(tagRect, tagBtnAct.Tooltip);
            }
        }
        private class ATDrawTexture : ActionTree
        {
            public Rect position;
            public Func<Texture2D> image;
            public override void Act()
            {
                GUI.DrawTexture(position, image());
            }
        }
        private class ATFinish : ActionTree
        {
            public override void Act()
            {
                Text.Font = GameFont.Tiny;
            }
        }

        private IEnumerable<Pair<string, string>> ParceAttributes(string fullTag)
        {
            var si = fullTag.IndexOf(" ");
            if (si < 0) yield break;

            var args = fullTag.Substring(si, fullTag.Length - si - 1);
            if (args[args.Length - 1] == '/') args = args.Remove(args.Length - 1, 1);
            args = args.Trim();

            if (!args.Contains("="))
            {
                if (!string.IsNullOrEmpty(args)) yield return new Pair<string, string>(args, "");
                yield break;
            }

            foreach (var arg in args.Split(' '))
            {
                var nv = arg.Split(new char[] { '=' }, 2);
                if (nv.Length < 2)
                    yield return new Pair<string, string>(arg, "");
                else
                    yield return new Pair<string, string>(nv[0], nv[1]);
            }
        }

        /// <summary>
        /// Разбиваем текст на слова. Возвращаемые части строк точно соответствуют исходной.
        /// Слово заканчивается, если следующий символ пробел, \n или '<'
        /// При этом пробел и \n добавляются к слову, а '<' нет
        /// Получается, что пробел и \n могут быть в начале только если слово из 1 символа, а
        /// '<' всегда может быть только в начале, в этом случае: слово заканчивается на символе '>' (включитльно),
        /// либо оно длинной 1 символ, если символа '>' нет дальше в строке.
        /// Результат не может быть меньше 1 символа
        /// </summary>
        private IEnumerable<string> ParceText(string text)
        {
            //text = (text ?? "").Replace("\r", "");
            var index = 0;
            int iNewLine = -1;
            int iSpace = -1;
            int iTag = -1;
            while (index < text.Length)
            {
                //если текущий символ начало тэга, то ищим его конец
                if (text[index] == '<')
                {
                    var p1 = text.IndexOf('>', index);
                    if (p1 < 0)
                    {
                        //нет окончания тэга, печатаем как отдельный символ
                        yield return "<";
                        index++;
                        continue;
                    }

                    yield return text.Substring(index, p1 - index + 1);
                    index = p1 + 1;
                    continue;
                }

                //определяем чем кончается текущее слово (начинающиеся с позиции index)
                if (iNewLine < index) iNewLine = text.IndexOf('\n', index);
                if (iSpace < index) iSpace = text.IndexOf(' ', index);
                if (iTag < index) iTag = text.IndexOf('<', index);

                int iNext; //позиция символа после текущего слова
                if (iNewLine >= 0 && (uint)iNewLine <= (uint)iSpace && (uint)iNewLine <= (uint)iTag)
                {
                    iNext = iNewLine + 1;
                }
                else if (iSpace >= 0 && (uint)iSpace <= (uint)iNewLine && (uint)iSpace <= (uint)iTag)
                {
                    iNext = iSpace + 1;
                }
                else if (iTag >= 0)
                {
                    iNext = iTag;
                }
                else
                {
                    iNext = text.Length;
                }

                yield return text.Substring(index, iNext - index);
                index = iNext;
            }
        }

    }
}
