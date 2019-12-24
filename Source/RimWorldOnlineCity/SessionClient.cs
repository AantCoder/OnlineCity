namespace RimWorldOnlineCity
{
    /// <summary>
    /// Специфический для игры класс SessionClient
    /// </summary>
    public class SessionClient : Transfer.SessionClient
    {
        private static SessionClient Single = new SessionClient();

        public static SessionClient Get => Single;
    }
}
