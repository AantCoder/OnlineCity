using System;

namespace OCUnion.Transfer
{
    /// <summary>
    /// Correctly disconnect reason
    /// </summary>
   [Serializable]
    public enum DisconnectReason : byte
    {
        ///
        /// All good Всё хорошо продолжаем работать
        /// 
        AllGood,
        /// <summary>
        /// Close game 
        /// </summary>
        CloseConnection,
        /// <summary>
        /// Connection Time Out
        /// </summary>
        ConnectionTimeOut,
        /// <summary>
        ///  
        /// </summary>
        FilesMods,
    }
}
