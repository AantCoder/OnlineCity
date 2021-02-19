﻿using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Common;
using ServerOnlineCity.Model;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Services
{
    internal sealed class PlayInfo : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request11;

        public int ResponseTypePackage => (int)PackageType.Response12;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = playInfo((ModelPlayToServer)request.Packet, context);
            return result;
        }

        public ModelPlayToClient playInfo(ModelPlayToServer packet, ServiceContext context)
        {
            if (Repository.CheckIsBanIP(context.AddrIP))
            {
                context.Disconnect("New BanIP " + context.AddrIP);
                return null;
            }
            if (context.PossiblyIntruder)
            {
                context.Disconnect("Possibly intruder");
                return null;
            }
            lock (context.Player)
            {
                var data = Repository.GetData;
                var timeNow = DateTime.UtcNow;
                var toClient = new ModelPlayToClient();
                toClient.UpdateTime = timeNow;
                if (packet.GetPlayersInfo != null && packet.GetPlayersInfo.Count > 0)
                {
                    var pSee = StaticHelper.PartyLoginSee(context.Player);
                    var pGet = new HashSet<string>(packet.GetPlayersInfo);
                    pGet.IntersectWith(pSee);

                    toClient.PlayersInfo = pGet
                        .Where(l => Repository.GetData.PlayersAllDic.ContainsKey(l))
                        .Select(l => Repository.GetData.PlayersAllDic[l].Public)
                        .ToList();
                }
                if (packet.SaveFileData != null && packet.SaveFileData.Length > 0)
                {
                    Repository.GetSaveData.SavePlayerData(context.Player.Public.Login, packet.SaveFileData, packet.SingleSave);
                    context.Player.Public.LastSaveTime = timeNow;

                    //Действия при сохранении, оно происодит только здесь!
                    context.Player.MailsConfirmationSave = new List<ModelMail>();

                    Repository.Get.ChangeData = true;
                }
                if (context.Player.GetKeyReconnect())
                {
                    toClient.KeyReconnect = context.Player.KeyReconnect1;
                }

                var pLogin = context.Player.Public.Login;
                //packet.WObjects тут все объекты этого игрока, добавляем которых у нас нет
                var pWOs = packet.WObjects ?? new List<WorldObjectEntry>();
                //packet.WObjectsToDelete тут те объекты этого игрока, что нужно удалить
                var pDs = packet.WObjectsToDelete ?? new List<WorldObjectEntry>();
                //передаем назад объекты у которых не было ServerId, т.е. они новые для сервера + все с изменениями
                var outWO = new List<WorldObjectEntry>();
                var outWOD = new List<WorldObjectEntry>();
                //это первое обращение, если не прислали своих объектов
                var first = pWOs.Count == 0;
                lock (data)
                {
                    for (int i = 0; i < pDs.Count; i++)
                    {
                        if (pDs[i].LoginOwner != context.Player.Public.Login) continue;
                        var sid = pDs[i].ServerId;
                        var pD = data.WorldObjects.FirstOrDefault(p => p.ServerId == sid);
                        if (pD != null)
                        {
                            //удаление из базы
                            pD.UpdateTime = timeNow;
                            data.WorldObjects.Remove(pD);
                            data.WorldObjectsDeleted.Add(pD);
                        }
                    }

                    for (int i = 0; i < pWOs.Count; i++)
                    {
                        if (pWOs[i].LoginOwner != context.Player.Public.Login) continue; // <-на всякий случай
                        var sid = pWOs[i].ServerId;
                        if (sid == 0)
                        {
                            //добавление в базу
                            pWOs[i].UpdateTime = timeNow;
                            pWOs[i].ServerId = data.GetWorldObjectEntryId();
                            data.WorldObjects.Add(pWOs[i]);
                            outWO.Add(pWOs[i]);
                            continue;
                        }
                        var WO = data.WorldObjects.FirstOrDefault(p => p.ServerId == sid);
                        if (WO != null)
                        {
                            //данный объект уже есть в базу обновляем по нему информкацию
                            if (WO.Name != pWOs[i].Name)
                            {
                                WO.UpdateTime = timeNow;
                                WO.Name = pWOs[i].Name;
                            }
                            if (WO.FreeWeight != pWOs[i].FreeWeight)
                            {
                                WO.UpdateTime = timeNow;
                                WO.FreeWeight = pWOs[i].FreeWeight;
                            }
                            if (WO.MarketValue != pWOs[i].MarketValue)
                            {
                                WO.UpdateTime = timeNow;
                                WO.MarketValue = pWOs[i].MarketValue;
                            }
                            if (WO.MarketValuePawn != pWOs[i].MarketValuePawn)
                            {
                                WO.UpdateTime = timeNow;
                                WO.MarketValuePawn = pWOs[i].MarketValuePawn;
                            }
                            if (WO.Tile != pWOs[i].Tile)
                            {
                                WO.UpdateTime = timeNow;
                                WO.Tile = pWOs[i].Tile;
                            }
                        }
                        else
                        {
                            Loger.Log("PlayInfo find error add WO: " + pWOs[i].Name + " sid=" + sid);
                        }
                    }

                    //передаем объекты других игроков + при первом обращении first
                    for (int i = 0; i < data.WorldObjects.Count; i++)
                    {
                        if (data.WorldObjects[i].UpdateTime < packet.UpdateTime) continue;
                        if (!first && data.WorldObjects[i].LoginOwner == pLogin) continue;
                        outWO.Add(data.WorldObjects[i]);
                    }

                    //передаем удаленные объекты других игроков (не для первого запроса)
                    if (packet.UpdateTime > DateTime.MinValue)
                    {
                        for (int i = 0; i < data.WorldObjectsDeleted.Count; i++)
                        {
                            if (data.WorldObjectsDeleted[i].UpdateTime < packet.UpdateTime)
                            {
                                //Удаляем все записи сроком старше 2х минут (их нужно хранить время между тем как игрок у которого удалился караван зальёт это на сервер, и все другие онлайн игроки получат эту инфу, а обновление идет раз в 5 сек)
                                if ((timeNow - data.WorldObjectsDeleted[i].UpdateTime).TotalSeconds > 120000)
                                {
                                    data.WorldObjectsDeleted.RemoveAt(i--);
                                }
                                continue;
                            }
                            if (data.WorldObjectsDeleted[i].LoginOwner == pLogin) continue;
                            outWOD.Add(data.WorldObjectsDeleted[i]);
                        }
                    }

                    //World Object Online
                    if (ServerManager.ServerSettings.GeneralSettings.EquableWorldObjects)
                    {
                        //World Object Online
                        try
                        {
                            if (packet.WObjectOnlineToDelete != null && packet.WObjectOnlineToDelete.Count > 0)
                            {
                                data.WorldObjectOnlineList.RemoveAll(data => packet.WObjectOnlineToDelete.Any(pkt => ValidateWorldObject(pkt, data)));
                            }
                            if (packet.WObjectOnlineToAdd != null && packet.WObjectOnlineToAdd.Count > 0)
                            {
                                data.WorldObjectOnlineList.AddRange(packet.WObjectOnlineToAdd);
                            }
                            if (packet.WObjectOnlineList != null && packet.WObjectOnlineList.Count > 0)
                            {
                                if (data.WorldObjectOnlineList.Count == 0)
                                {
                                    data.WorldObjectOnlineList = packet.WObjectOnlineList;
                                }
                                else if (data.WorldObjectOnlineList != null && data.WorldObjectOnlineList.Count > 0)
                                {
                                    toClient.WObjectOnlineToDelete = packet.WObjectOnlineList.Where(pkt => !data.WorldObjectOnlineList.Any(data => ValidateWorldObject(pkt, data))).ToList();
                                    toClient.WObjectOnlineToAdd = data.WorldObjectOnlineList.Where(data => !packet.WObjectOnlineList.Any(pkt => ValidateWorldObject(pkt, data))).ToList();
                                }
                            }
                            toClient.WObjectOnlineList = data.WorldObjectOnlineList;
                        }
                        catch
                        {
                            Loger.Log("ERROR PLAYINFO World Object Online");
                        }

                        //Faction Online
                        try
                        {
                            if (packet.FactionOnlineToDelete != null && packet.FactionOnlineToDelete.Count > 0)
                            {
                                data.FactionOnlineList.RemoveAll(data => packet.FactionOnlineToDelete.Any(pkt => ValidateFaction(pkt, data)));
                            }
                            if (packet.FactionOnlineToAdd != null && packet.FactionOnlineToAdd.Count > 0)
                            {
                                data.FactionOnlineList.AddRange(packet.FactionOnlineToAdd);
                            }
                            if (packet.FactionOnlineList != null && packet.FactionOnlineList.Count > 0)
                            {
                                if (data.FactionOnlineList.Count == 0)
                                {
                                    data.FactionOnlineList = packet.FactionOnlineList;
                                }
                                else if (data.FactionOnlineList != null && data.FactionOnlineList.Count > 0)
                                {

                                    toClient.FactionOnlineToDelete = packet.FactionOnlineList.Where(pkt => !data.FactionOnlineList.Any(data => ValidateFaction(pkt, data))).ToList();
                                    toClient.FactionOnlineToAdd = data.FactionOnlineList.Where(data => !packet.FactionOnlineList.Any(pkt => ValidateFaction(pkt, data))).ToList();
                                }
                            }
                            toClient.FactionOnlineList = data.FactionOnlineList;
                        }
                        catch
                        {
                            Loger.Log("ERROR PLAYINFO Faction Online");
                        }
                    }

                    //завершили сбор информации клиенту
                    toClient.WObjects = outWO;
                    toClient.WObjectsToDelete = outWOD;

                    context.Player.WLastUpdateTime = timeNow;
                    context.Player.WLastTick = packet.LastTick;

                    //обновляем статистические поля
                    var costAll = context.Player.CostWorldObjects();
                    if (context.Player.StartMarketValuePawn == 0)
                    {
                        context.Player.StartMarketValue = costAll.MarketValue;
                        context.Player.StartMarketValuePawn = costAll.MarketValuePawn;

                        context.Player.DeltaMarketValue = 0;
                        context.Player.DeltaMarketValuePawn = 0;
                    }
                    else if (context.Player.LastUpdateIsGood && (costAll.MarketValue > 0 || costAll.MarketValuePawn > 0))
                    {
                        //считаем дельту
                        context.Player.DeltaMarketValue = (costAll.MarketValue - context.Player.LastMarketValue);
                        context.Player.DeltaMarketValuePawn = (costAll.MarketValuePawn - context.Player.LastMarketValuePawn);

                        context.Player.SumDeltaGameMarketValue += context.Player.DeltaMarketValue;
                        context.Player.SumDeltaGameMarketValuePawn += context.Player.DeltaMarketValuePawn;
                        context.Player.SumDeltaRealMarketValue += context.Player.DeltaMarketValue;
                        context.Player.SumDeltaRealMarketValuePawn += context.Player.DeltaMarketValuePawn;

                        if (packet.LastTick - context.Player.StatLastTick > 15 * 60000) // сбор раз в 15 дней
                        {
                            if (context.Player.StatMaxDeltaGameMarketValue < context.Player.SumDeltaGameMarketValue)
                                context.Player.StatMaxDeltaGameMarketValue = context.Player.SumDeltaGameMarketValue;
                            if (context.Player.StatMaxDeltaGameMarketValuePawn < context.Player.SumDeltaGameMarketValuePawn)
                                context.Player.StatMaxDeltaGameMarketValuePawn = context.Player.SumDeltaGameMarketValuePawn;

                            context.Player.SumDeltaGameMarketValue = 0;
                            context.Player.SumDeltaGameMarketValuePawn = 0;
                            context.Player.StatLastTick = packet.LastTick;
                        }

                        if (context.Player.SumDeltaRealSecond > 60 * 60) //сбор раз в час
                        {
                            if (context.Player.StatMaxDeltaRealMarketValue < context.Player.SumDeltaRealMarketValue)
                                context.Player.StatMaxDeltaRealMarketValue = context.Player.SumDeltaRealMarketValue;
                            if (context.Player.StatMaxDeltaRealMarketValuePawn < context.Player.SumDeltaRealMarketValuePawn)
                                context.Player.StatMaxDeltaRealMarketValuePawn = context.Player.SumDeltaRealMarketValuePawn;
                            if (context.Player.StatMaxDeltaRealTicks < context.Player.SumDeltaRealTicks)
                                context.Player.StatMaxDeltaRealTicks = context.Player.SumDeltaRealTicks;

                            context.Player.SumDeltaRealMarketValue = 0;
                            context.Player.SumDeltaRealMarketValuePawn = 0;
                            context.Player.SumDeltaRealTicks = 0;
                            context.Player.SumDeltaRealSecond = 0;
                        }
                    }
                    context.Player.LastUpdateIsGood = costAll.MarketValue > 0 || costAll.MarketValuePawn > 0;
                    if (context.Player.LastUpdateIsGood)
                    {
                        context.Player.LastMarketValue = costAll.MarketValue;
                        context.Player.LastMarketValuePawn = costAll.MarketValuePawn;
                    }
                    var dt = packet.LastTick - context.Player.Public.LastTick;
                    context.Player.SumDeltaRealTicks += dt;
                    if (dt > 0)
                    {
                        var ds = (long)(timeNow - context.Player.LastUpdateTime).TotalSeconds;
                        context.Player.SumDeltaRealSecond += ds;
                        context.Player.TotalRealSecond += ds;
                    }

                    context.Player.WLastUpdateTime = context.Player.LastUpdateTime;
                    context.Player.WLastTick = context.Player.Public.LastTick;
                    context.Player.LastUpdateTime = timeNow;
                    context.Player.Public.LastTick = packet.LastTick;
                }

                //обновляем состояние отложенной отправки писем
                if (context.Player.FunctionMails.Count > 0)
                {
                    for (int i = 0; i < context.Player.FunctionMails.Count; i++)
                    {
                        var needRemove = context.Player.FunctionMails[i].Run(context);
                        if (needRemove) context.Player.FunctionMails.RemoveAt(i--);
                    }
                }

                //прикрепляем письма
                //если есть команда на отключение без сохранения, то посылаем только одно это письмо
                var md = context.Player.Mails.FirstOrDefault(m => m is ModelMailAttackCancel);
                if (md == null)
                {
                    toClient.Mails = context.Player.Mails;
                    context.Player.MailsConfirmationSave.AddRange(context.Player.Mails.Where(m => m.NeedSaveGame).ToList());
                    context.Player.Mails = new List<ModelMail>();
                }
                else
                {
                    toClient.Mails = new List<ModelMail>() { md };
                    context.Player.Mails.Remove(md);
                }

                //команда выполнить сохранение и отключиться
                toClient.NeedSaveAndExit = !context.Player.IsAdmin && data.EverybodyLogoff;

                //флаг, что на клиента кто-то напал и он должен запросить подробности
                toClient.AreAttacking = context.Player.AttackData != null && context.Player.AttackData.Host == context.Player && context.Player.AttackData.State == 1;

                if (toClient.Mails.Count > 0)
                {
                    foreach (var mail in toClient.Mails)
                    {
                        Loger.Log($"DownloadMail {mail.GetType().Name} {mail.From.Login}->{mail.To.Login} {mail.ContentString()}");
                    }
                }

                return toClient;
            }
        }

        private static bool ValidateWorldObject(WorldObjectOnline pkt, WorldObjectOnline data)
        {
            if(pkt.Name == data.Name
                && pkt.Tile == data.Tile)
            {
                return true;
            }
            return false;
        }

         private static bool ValidateFaction(FactionOnline pkt, FactionOnline data)
        {
            if (pkt.DefName == data.DefName && 
                pkt.LabelCap == data.LabelCap &&
                pkt.loadID == data.loadID)
            {
                return true;
            }
            return false;
        }
    }
}