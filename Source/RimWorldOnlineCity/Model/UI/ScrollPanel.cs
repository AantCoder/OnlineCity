using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RimWorldOnlineCity.UI
{
    public class ScrollPanel : DialogControlBase
    {
        public Func<Rect, Rect> DrawInner { get; set; }
        public Rect AreaOuter { get; set; }

        private Rect AreaInner;

        private Vector2 ScrollPosition;
        
        public void Draw()
        {
            if (DrawInner == null) return;

            if (AreaInner.width <= 0)
            {
                AreaInner = new Rect(0, 0, AreaOuter.width - WidthScrollLine, AreaOuter.height);
            }
            if (AreaInner.width <= 0) return;

            ScrollPosition = GUI.BeginScrollView(
                AreaOuter
                , ScrollPosition
                , AreaInner);
            var rectDraw = DrawInner(AreaInner);
            GUI.EndScrollView();
            if (rectDraw.height > AreaInner.height) AreaInner.height = rectDraw.height;
            if (AreaOuter.height > AreaInner.height) AreaInner.height = AreaOuter.height;
        }
    }
}

///todo
///Пешка не узнает себя
///Крашится при отображении пешки
///Функция расстояния
///Добавить в консоль связи 2 кнопки Перевести в безнал, Обналичить
///Список местоположений, где у нас есть наши вещи (новую иконку?)
///Выбор местоположения, для торговли оттуда
///Заказ перевоза вещей
///Переработать интерфейс:
/// Добавить сводку текущих ордеров
/// Добавить текущий безналичный счет и логин на экран Onlie dev 123456$
/// Добавить таблицу с игроками
/// Добавить список мест с вещами/караванами/поселениями
/// Вкладка настройки (настройки отображения чата, удалить игру)