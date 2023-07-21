using ServerOnlineCity;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mechanics
{
    public class NameValidator
    {
        public BaseContainer Data;

        public NameValidator(BaseContainer data)
        {
            Data = data;
        }

        public string TextValidator(string name)
        {
            if (name.Contains("\r")
                || name.Contains("\n")
                || name.Contains("\t")
                || name.Contains(" ")
                || name.Contains("*")
                || name.Contains("@")
                || name.Contains("/")
                || name.Contains("\\")
                || name.Contains("\"")
                || name.Contains("<")
                || name.Contains(">"))
            {
                return "cannot contain characters: space * @ / \\ \" < > ";
            }

            if (name.Trim().Length <= 2)
            {
                return "must be longer than 3 characters";
            }

            if (name.Trim().Length > 20)
            {
                return "must be shorter than 20 characters";
            }

            return null;
        }

        public bool CheckFree(string name)
        {
            name = Repository.NormalizeLogin(name.Trim());

            var player = Repository.GetData.PlayersAll.FirstOrDefault(p => Repository.NormalizeLogin(p.Public.Login) == name);
            if (player != null) return false;

            var state = Repository.GetData.GetStates.FirstOrDefault(p => Repository.NormalizeLogin(p.Name) == name);
            if (state != null) return false;

            return true;
        }
    }
}
