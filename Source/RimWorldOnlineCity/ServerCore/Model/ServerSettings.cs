using System;
using System.ComponentModel.DataAnnotations;

namespace ServerCore.Model
{ 
    // all properties marked as [DisplayAttribute] show in Discord by a command srvinfo or in the Game
    public class ServerSettings
    {
        [Display]
        public string HostingLocation = "Moscow";
        [Display]
        public string Language = "Russian";
        [Display]
        public string ServerName = "Another OnlineCity Server";        
        public int SaveInterval = 10000;
        [Display]
        public int Port = 8888;
    }
}