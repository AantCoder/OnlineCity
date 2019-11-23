﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCUnion.Transfer.Model
{
    public enum PakageType : byte
    {
        /// <summary>
        /// 1 - регистрация (логин, пароль) EN: register (login, password)
        /// </summary>
        RequestRegister = 1,
        /// <summary>
        /// 2 - ответ регистрации (успешно, сообщение) EN: answer ( sucess, message)
        /// </summary>
        ResponseRegister = 2,
        /// <summary>
        /// 3 - вход (логин, пароль)
        /// </summary>
        RequestLogin = 3,
        /// <summary>
        /// 4 - ответ на вход (успешно, сообщение)
        /// </summary>
        ResponseLogin = 4,
        /// <summary>
        /// 5 - запрос информации
        /// </summary>
        RequestUserInfo = 5,
        /// <summary>
        /// 6 - информация о самом пользователе
        /// </summary>
        ResponseUserInfo = 6


        /*
7 - создать мир (всё, что нужно для начала работы сервера)
8 - ответ на 7 (успешно, сообщение)
9 - созадть поселение (запрос с данными о поселении нового игрока, всё что передается после создания карты поселения игроком)
10 - ответ на 9 (успешно, сообщение)
11 - синхронизация мира (тип синхранезации, время последней синхронизации, все данные для сервера)
12 - ответ на 11 (время сервера, все данные мира которые изменились с указанного времени)
13 - создать игру (id лобби)
14 - ответ (сиид для создания мира, ?)
15 - отправка игрового действия (данные для обновления на сервере)
16 - ответ (успешно, сообщение)
17 - обновить чат (время после которого нужны данные)
18 - данные чата
19 - написать в чат (id канала, сообщение) //здесь же командами создать канал, добавить в канал и прочее
20 - ответ (успешно, сообщение)
21 - команды работы с биржей 
22 - ответ 
23 - команды работы с биржей
24 - ответ 
25 - команды работы с биржей
26 - ответ 
27 - атака онлайн
28 - ответ  
29 - атакуемый онлайн
30 - ответ 
    */
    }
}