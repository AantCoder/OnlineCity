using System;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public enum PackageType : byte
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
        ResponseUserInfo = 6,
        /// <summary>
        /// 7 - создать мир (всё, что нужно для начала работы сервера)
        /// </summary>
        RequestCreateWorld = 7,
        /// <summary>
        /// 8 - ответ на 7 (успешно, сообщение)
        /// </summary>
        ResponseWorldCreated = 8,
        /// <summary>
        /// 9 - создать поселение (запрос с данными о поселении нового игрока, всё что передается после создания карты поселения игроком)
        /// </summary>
        RequestCreateSettlement = 9,
        /// <summary>
        /// 10 - ответ на 9 (успешно, сообщение)
        /// </summary>
        ResponseSettlementCreated = 10,
        /// <summary>
        /// 11 - синхронизация мира (тип синхранезации, время последней синхронизации, все данные для сервера)
        /// </summary>
        Request11 = 11,
        /// <summary>
        /// 12 - ответ на 11 (время сервера, все данные мира которые изменились с указанного времени)
        /// </summary>
        Response12 = 12,
        /// <summary>
        /// 13 - создать игру (id лобби)
        /// </summary>
        Request13 = 13,
        /// <summary>
        /// 14 - ответ (сиид для создания мира, ?)
        /// </summary>
        Response14 = 14,
        /// <summary>
        /// 15 - отправка игрового действия (данные для обновления на сервере)
        /// </summary>
        Request15 = 15,
        /// <summary>
        /// 16 - ответ (успешно, сообщение)
        /// </summary>
        Response16 = 16,
        /// <summary>
        /// 17 - обновить чат (время после которого нужны данные)
        /// </summary>
        Request17 = 17,
        /// <summary>
        /// 18 - данные чата
        /// </summary>
        Response18 = 18,
        /// <summary>
        /// 19 - написать в чат (id канала, сообщение) //здесь же командами создать канал, добавить в канал и прочее
        /// </summary>
        Request19 = 19,
        /// <summary>
        /// 20 - ответ (успешно, сообщение)
        /// </summary>
        Response20 = 20,
        /// <summary>
        /// 21 - команды работы с биржей 
        /// </summary>
        Request21 = 21,
        /// <summary>
        /// 22 - ответ 
        /// </summary>
        Response22 = 22,
        /// <summary>
        /// 23 - команды работы с биржей
        /// </summary>
        Request23 = 23,
        /// <summary>
        /// 24 - ответ 
        /// </summary>
        Response24 = 24,
        /// <summary>
        /// 25 - команды работы с биржей
        /// </summary>
        Request25 = 25,
        /// <summary>
        /// 26 - ответ 
        /// </summary>
        Response26 = 26,
        /// <summary>
        /// 27 - атака онлайн
        /// </summary>
        Request27 = 27,
        /// <summary>
        /// 28 - ответ 
        /// </summary>
        Response28 = 28,
        /// <summary>
        /// 29 - атакуемый онлайн
        /// </summary>
        Request29 = 29,
        /// <summary>
        /// 30 - ответ 
        /// </summary>
        Response30 = 30,

        RequestPlayerByToken,
        ResponsePlayerByToken,
    }
}