using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCUnion
{
    public class AttackUtils
    {
        public static float MaxCostAttackerCaravan(float costTarget, bool isSettlement)
        {
            if (isSettlement)
            {
                //Очень грубо упрощенная средняя формула получения цены атакующих рейдов из исходника игры
                //  4 / (25/1000000 + 10/богатство колонии) + 2000 = богатство нападающих   (4 от сложности и коэф. времени, + 2000 на еду каравана)

                return 4f / (25f / 1000000f + 10f / costTarget) + 2000f;
            }
            else
            {
                //Если будет атака на караваны, то атакуемые могут быть сильнее на 15%
                return costTarget * 1.15f;
            }
        }

        public static string CheckPossibilityAttack(IPlayerEx attacker, IPlayerEx host, long attackerWOServerId, long hostWOServerId)
        {
            try
            {
                var res =
                    !attacker.Online ? "Attacker not online"
                    : !host.Online ? "Host not online"
                    : !attacker.Public.EnablePVP ? "Attacker not EnablePVP"
                    : !host.Public.EnablePVP ? "Host not EnablePVP"
                    : null
                    ;
                if (res != null) return res;

                var hostCosts = host.CostWorldObjects(hostWOServerId);
                var hostCost = MaxCostAttackerCaravan(hostCosts.MarketValue + hostCosts.MarketValuePawn, true);

                var attCosts = attacker.CostWorldObjects(attackerWOServerId);
                var attCost = attCosts.MarketValue + attCosts.MarketValuePawn;

                res =
                    //стоимость колонии больше стоимости каравана
                    attCost > hostCost
                    ? //"The cost of the attackers is higher than the cost of the colony, this is not fair"
                    "The cost of the attacker must be less than " + ((long)hostCost).ToString()
                    //колонии больше 1 года
                    //todo  : host.Public.LastTick < 3600000 ? "You must not attack the game for less than a year"

                    //колонию атаковали недавно 
                    : (DateTime.UtcNow - host.Public.LastPVPTime).TotalMinutes < host.MinutesIntervalBetweenPVP
                    ? "It was recently attacked. Wait to " + host.Public.LastPVPTime.ToGoodUtcString()
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
