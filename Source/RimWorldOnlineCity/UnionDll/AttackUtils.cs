using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCUnion
{
    public class AttackUtils
    {
        public static string CheckPossibilityAttack(IPlayerEx attacker, IPlayerEx host, long attackerWOServerId, long hostWOServerId)
        {
            try
            {
                var res = 
                    !attacker.Online ? "Attacker not online"
                    : !host.Online ? "Host not online"
                    : !attacker.Public.EnablePVP ? "Attacker not EnablePVP"
                    : !host.Public.EnablePVP ? "Host not EnablePVP"
                    //стоимость колонии больше стоимости каравана
                    : attacker.CostWorldObjects(attackerWOServerId).MarketValue > host.CostWorldObjects(hostWOServerId).MarketValue
                    ? "The cost of the attackers is higher than the cost of the colony, this is not fair"
                    //колонии больше 1 года
                    //todo  : host.Public.LastTick < 3600000 ? "You must not attack the game for less than a year"

                    //todo колонию атаковали недавно //attacker.LastOnlineTime
                    : null;
                /*
                if (res != null) Loger.Log("CheckPossibilityAttack: " + res
                    + " LastOnlineTime=" + attacker.Public.LastOnlineTime.ToString("o")
                    + " UtcNow=" + DateTime.UtcNow.ToString("o")
                    );
                */
                return res;
            }
            catch (Exception exp)
            {
                Loger.Log("CheckPossibilityAttack Exception" + exp.ToString());
                return "Error calc";
            }
        }
    }
}
