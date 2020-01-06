namespace OCUnion.Transfer
{
    public enum ServerInfoType : byte
    {
        Full = 1,
        Short = 2,
        SendSave = 3,
        /// <summary>
        /// Полное с подробным текстовым описанием
        /// </summary>
        FullWithDescription
    }
}