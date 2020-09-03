using System;

namespace OCUnion.Transfer.Types
{
    /// <summary>
    /// Флаги проверки перед загрузкой мира клиенту
    /// </summary>
    [Serializable]
    [Flags]
    public enum ApproveLoadWorldReason : byte
    {
        /// <summary>
        /// Прошел аутентификацию (значение по умолчанию)
        /// </summary>
        LoginOk = 0,
        /// <summary>
        /// Папка Mods проверена
        /// </summary>
        ModsFilesFail = 1,
        /// <summary>
        /// Папка модов steamWorkShop проверена
        /// </summary>
        ModsSteamWorkShopFail = 2,
        /// <summary>
        /// Не все файлы есть у клиента, необходимо догрузить
        /// </summary>
        NotAllFilesOnClient = 4,
    }
}
