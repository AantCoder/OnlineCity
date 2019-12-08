using HugsLib.Utils;
using OCUnion;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorldOnlineCity
{
    public static class GameStarterSelectPawns
    {

        /// <summary>
        /// Перед запуском удалить всех колонистов игрока
        /// </summary>
        /// <param name="pawnsCount"></param>
        /// <param name="result"></param>
        public static void ShowDialog(int pawnsCount, Action<List<Pawn>> result)
        {
            List<Pawn> oldPawns = null;
            //запоминаем текущее
            var gameIsNull = Current.Game == null || Current.Game.InitData == null;
            if (gameIsNull)
            {
                GameUtils.ShortSetupForQuickTestPlay();
            }
            else
            {
                oldPawns = Current.Game.InitData.startingAndOptionalPawns.ToList();
            }

            //тяп-ляп и новые пешки
            Current.Game.InitData.startingAndOptionalPawns.Clear();
            for (int i = 0; i < pawnsCount; i++)
            {
                Current.Game.InitData.startingAndOptionalPawns.Add(PawnGenerateRandom());
            }
            //if (MainHelper.DebugMode) File.WriteAllText(Loger.PathLog + @"SrvTest.txt", DevelopTest.TextObj(Current.Game.InitData.startingPawns, true), Encoding.UTF8);
            for (int i = 0; i < pawnsCount; i++)
            {
                StartingPawnUtility.RandomizeInPlace(Current.Game.InitData.startingAndOptionalPawns[i]);
            }
            
            //запускаем форму редактирования
            var form = new Page_ConfigureStartingPawns();
            form.nextAct = () =>
            {
                var pawns = Current.Game.InitData.startingAndOptionalPawns.ToList();

                //восстанавливаем значеня
                if (gameIsNull) Current.Game = null;
                else
                {
                    Current.Game.InitData.startingAndOptionalPawns.Clear();
                    for (int i = 0; i < oldPawns.Count; i++)
                    {
                        Current.Game.InitData.startingAndOptionalPawns.Add(oldPawns[i]);
                    }
                }

                result(pawns);
            };
            Find.WindowStack.Add(form);
        }


        public static Pawn PawnGenerateRandom()
        {
            Faction faction;
            /* к сожалению бессмысленно, т.к. в FactionGenerator.NewGeneratedFaction используется Find.World
            if (Find.World == null || Find.World.factionManager == null)
            {
                var facDef = DefDatabase<FactionDef>.GetNamed("PlayerColony");
                faction = FactionGenerator.NewGeneratedFaction(facDef);
            }
            else
                */
            faction = Faction.OfPlayer;

            PawnKindDef random = DefDatabase<PawnKindDef>.GetRandom();
            //Faction faction = Faction.OfPlayer;//FactionUtility.DefaultFactionFrom(FactionDef.Named("PlayerColony")); // FactionUtility.DefaultFactionFrom(random.defaultFactionType);
            Pawn pawn = PawnGenerator.GeneratePawn(random, faction);
            return pawn;
        }
    }
}
