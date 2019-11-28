using Model;
using OCServer.Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Transfer;
using Util;

namespace OCServer
{
    public class Service
    {
        public PlayerServer Player;
        
        public ModelStatus Login(ModelLogin packet)
        {
            if (Player != null) return null;
            //Thread.Sleep(3000);

            if (packet.Login == "system") return null;
            //Loger.Log("1111 " + Repository.GetData.PlayersAll.Count);
            var player = Repository.GetData.PlayersAll
                .FirstOrDefault(p => p.Public.Login == packet.Login);
            if (player != null)
            {
                if (player.Pass != packet.Pass) //todo
                    player = null;
            }
            if (player == null)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "User or password incorrect"
                };
            }
            Player = player;
            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }

        public ModelStatus Registration(ModelLogin packet)
        {
            if (Player != null) return null;
            if (packet.Login.Trim().Length < 2
                || packet.Pass.Length < 5)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "Incorrect login or password"
                };
            }
            //Thread.Sleep(5000);
            packet.Login = packet.Login.Trim();
            var player = Repository.GetData.PlayersAll
                .FirstOrDefault(p => Repository.NormalizeLogin(p.Public.Login) == Repository.NormalizeLogin(packet.Login));
            if (player != null)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "This login already exists"
                };
            }
            player = new PlayerServer(packet.Login)
            {
                Pass = packet.Pass,
                IsAdmin = Repository.GetData.PlayersAll.Count == 1
            };
            Repository.GetData.PlayersAll.Add(player);
            Repository.Get.ChangeData = true;
            Player = player;
            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }
        
        public ModelInfo GetInfo(ModelInt packet)
        {
            if (Player == null) return null;

            lock (Player)
            {
                var saveData = Player.SaveDataPacket;
                if (packet.Value == 1)
                {
                    //полная информация fullInfo = true
                    var data = Repository.GetData;
                    var result = new ModelInfo()
                    {
                        My = Player.Public,
                        IsAdmin = Player.IsAdmin,
                        VersionInfo = MainHelper.VersionInfo,
                        VersionNum = MainHelper.VersionNum,
                        Seed = data.WorldSeed ?? "",
                        MapSize = data.WorldMapSize,
                        PlanetCoverage = data.WorldPlanetCoverage,
                        Difficulty = data.WorldDifficulty,
                        NeedCreateWorld = saveData == null,
                        ServerTime = DateTime.UtcNow,
                    };
                    return result;
                }
                else if (packet.Value == 3)
                {
                    //передача файла игры, для загрузки WorldLoad();
                    var result = new ModelInfo();
                    result.SaveFileData = saveData;
                    return result;
                }
                else
                {
                    //краткая (зарезервированно, пока не используется) fullInfo = false
                    var result = new ModelInfo();
                    return result;
                }
            }
        }
        
        public ModelStatus CreatingWorld(ModelCreateWorld packet)
        {
            if (Player == null) return null;

            lock (Player)
            {
                if (!Player.IsAdmin)
                {
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = "Access is denied"
                    };
                }
                if (!string.IsNullOrEmpty(Repository.GetData.WorldSeed))
                {
                    return new ModelStatus()
                    {
                        Status = 2,
                        Message = "The world is already created"
                    };
                }
                var data = Repository.GetData;
                data.WorldSeed = packet.Seed;
                data.WorldDifficulty = packet.Difficulty;
                data.WorldMapSize = packet.MapSize;
                data.WorldPlanetCoverage = packet.PlanetCoverage;
                data.WorldObjects = packet.WObjects ?? new List<WorldObjectEntry>();
                data.WorldObjectsDeleted = new List<WorldObjectEntry>();
                data.Orders = new List<OrderTrade>();
                Repository.Get.ChangeData = true;
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }

        public ModelPlayToClient PlayInfo(ModelPlayToServer packet)
        {
            if (Player == null) return null;

            lock (Player)
            {
                var data = Repository.GetData;
                var timeNow = DateTime.UtcNow;
                var toClient = new ModelPlayToClient();
                toClient.UpdateTime = timeNow;
                if (packet.GetPlayersInfo != null && packet.GetPlayersInfo.Count > 0)
                {
                    var pSee = PartyLoginSee();
                    var pGet = pSee.Where(s => packet.GetPlayersInfo.Any(p => s == p)).ToList();
                    toClient.PlayersInfo = Repository.GetData.PlayersAll
                        .Where(p => pGet.Any(g => g == p.Public.Login))
                        .Select(p => p.Public)
                        .ToList();
                    if (pGet.Any(p => p == null)) Loger.Log("nulllllll");
                }
                if (packet.SaveFileData != null && packet.SaveFileData.Length > 0)
                {
                    Player.SaveDataPacket = packet.SaveFileData;
                    Player.Public.LastSaveTime = timeNow;
                    Repository.Get.ChangeData = true;
                }
                Player.Public.LastTick = packet.LastTick;

                var pLogin = Player.Public.Login;
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
                        if (pDs[i].LoginOwner != Player.Public.Login) continue;
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
                        if (pWOs[i].LoginOwner != Player.Public.Login) continue; // <-на всякий случай
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

                    //завершили сбор информации клиенту
                    toClient.WObjects = outWO;
                    toClient.WObjectsToDelete = outWOD;
                    Player.LastUpdateTime = timeNow;
                }
                
                //прикрепляем письма
                toClient.Mails = Player.Mails;
                Player.Mails = new List<ModelMailTrade>();

                //флаг, что на клиента кто-то напал и он должен запросить подробности
                toClient.AreAttacking = Player.AttackData != null && Player.AttackData.Host == Player && Player.AttackData.State == 1;

                return toClient;
            }
        }

        public ModelStatus SendThings(ModelMailTrade packet)
        {
            if (Player == null) return null;

            PlayerServer toPlayer;

            lock (Player)
            {
                var data = Repository.GetData;

                toPlayer = data.PlayersAll.FirstOrDefault(p => p.Public.Login == packet.To.Login);
                if (toPlayer == null)
                {
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = "Destination not found"
                    };
                }

                packet.From = data.PlayersAll.Select(p => p.Public).FirstOrDefault(p => p.Login == packet.From.Login);
                packet.To = toPlayer.Public;
            }
            lock (toPlayer)
            {
                toPlayer.Mails.Add(packet);
            }   
            return new ModelStatus()
            {
                Status = 0,
                Message = "Load shipped"
            };
        }

        /// <summary>
        /// Ники тех кого мы видим
        /// </summary>
        /// <returns></returns>
        private List<string> PartyLoginSee()
        {
            var ps = Player.IsAdmin
                ? Repository.GetData.PlayersAll.Select(p => p.Public.Login).ToList()
                : Player.Chats[0].PartyLogin;
            return ps;
        }

        public bool CheckChat(DateTime time)
        {
            if (Player == null) return false;
            return Player.Chats.Any(ct => ct.Posts.Any(p => p.Time > time));
        }

        public ModelUpdateChat UpdateChat(ModelUpdateTime time)
        {
            if (Player == null) return null;

            lock (Player)
            {
                var res = new ModelUpdateChat()
                {
                    Time = DateTime.UtcNow
                };

                //Список игроков кого видим
                var ps = PartyLoginSee();
                /*
                string ttt = "";
                
                try ////////////////////////////////
                {
                    Loger.Log("Server test " + Repository.GetData.PlayersAll[1].Chats[0].Posts.Count + "=" + Player.Chats[0].Posts.Count);
                    Repository.GetData.PlayersAll[1].Chats[0].Posts.Add(new ChatPost());
                    Loger.Log("Server test " + Repository.GetData.PlayersAll[1].Chats[0].Posts.Count + "=" + Player.Chats[0].Posts.Count);
                }
                catch { }
                try
                {
                    ttt = Player.Chats[0].Posts[0].OnlyForPlayerLogin + " - " + Player.Chats[0].Posts[0].OwnerLogin;
                }
                catch { }
                
                Loger.Log("Server UpdateChat " + Player.Public.Login + " " + time.Time.Ticks + ": " + string.Join(",", ps.ToArray())
                    + " | " + ttt);
                */
                //Копируем чат без лишнего и отфильтровываем посты
                res.Chats = Player.Chats
                    .Select(ct => new Chat()
                    {
                        Id = ct.Id,
                        OwnerLogin = ct.OwnerLogin,
                        Name = ct.Name,
                        OwnerMaker = ct.OwnerMaker,
                        PartyLogin = ct.PartyLogin,
                        Posts = ct.Posts
                                .Where(p => p.Time > time.Time)
                                .Where(p => p.OnlyForPlayerLogin == null && ps.Any(pp => pp == p.OwnerLogin)
                                    || p.OnlyForPlayerLogin == ct.OwnerLogin)
                                .ToList()
                    })
                    .Where(ct => ct != null)
                    .ToList();

                //Loger.Log("Server " + res.Chats.Count);
                return res;
            }
        }

        public ModelStatus PostingChat(ModelPostingChat pc)
        {
            if (Player == null) return null;

            lock (Player)
            {
                var timeNow = DateTime.UtcNow;
                if (string.IsNullOrEmpty(pc.Message))
                    return new ModelStatus()
                    {
                        Status = 0,
                        Message = null
                    };

                var chat = Player.Chats.FirstOrDefault(ct => ct.Id == pc.ChatId);
                if (chat == null)
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = "Chat not available"
                    };

                if (pc.Message[0] == '/')
                {
                    var s = pc.Message.Split(new char[] { ' ' }, 2);
                    var command = s[0].Trim().ToLower();
                    var args = s.Length == 1 ? "" : s[1];
                    //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
                    var argsM = SplitBySpace(args);
                    
                    // обработка команд чата {
                    switch (command)
                    {
                        case "/help":
                            PostCommandPrivatPostActivChat(chat, ChatHelpText
                                + (Player.IsAdmin ? ChatHelpTextAdmin : "" ));
                            break;
                        case "/createchat":
                            Loger.Log("Server createChat");
                            if (argsM.Count < 1) PostCommandPrivatPostActivChat(chat, "No new channel name specified");
                            else
                            {
                                var nChat = new Chat()
                                {
                                    Name = argsM[0],
                                    OwnerLogin = Player.Public.Login,
                                    OwnerMaker = true,
                                    PartyLogin = new List<string>() { Player.Public.Login, "system" },
                                    Id = Repository.GetData.GetChatId(),
                                };
                                nChat.Posts.Add(new ChatPost()
                                {
                                    Time = DateTime.UtcNow,
                                    Message = "User " + Player.Public.Login + " created a channel " + argsM[0],
                                    OwnerLogin = "system"
                                });
                                Player.Chats.Add(nChat);

                                if (argsM.Count > 1)
                                    PostCommandAddPlayer(nChat, argsM[1]);
                                Repository.Get.ChangeData = true;
                            }
                            break;
                        case "/exitchat":
                            Loger.Log("Server exitChat OwnerMaker=" + (chat.OwnerMaker ? "1" : "0"));
                            if (!chat.OwnerMaker) PostCommandPrivatPostActivChat(chat, "From a shared channel, you can not leave");
                            else
                            {
                                chat.Posts.Add(new ChatPost()
                                {
                                    Time = DateTime.UtcNow,
                                    Message = "User " + Player.Public.Login + " left the channel.",
                                    OwnerLogin = "system"
                                });
                                chat.PartyLogin.Remove(Player.Public.Login);
                                var r = Player.Chats.Remove(chat);
                                Loger.Log("Server exitChat remove" + (r ? "1" : "0"));
                                Repository.Get.ChangeData = true;
                            }
                            break;
                        case "/renamechat":
                            if (!chat.OwnerMaker) PostCommandPrivatPostActivChat(chat, "You can not rename a shared channel");
                            if (argsM.Count < 1) PostCommandPrivatPostActivChat(chat, "No new name specified");
                            else if (chat.OwnerLogin != Player.Public.Login && !Player.IsAdmin)
                                PostCommandPrivatPostActivChat(chat, "Operation is not available to you");
                            else
                            {
                                chat.Posts.Add(new ChatPost()
                                {
                                    Time = DateTime.UtcNow,
                                    Message = "The channel was renamed to " + argsM[0],
                                    OwnerLogin = "system"
                                });
                                Loger.Log("Server renameChat " + chat.Name + " -> " + argsM[0]);
                                chat.Name = argsM[0];
                                Repository.Get.ChangeData = true;
                            }
                            break;
                        case "/addplayer":
                            Loger.Log("Server addPlayer");
                            if (argsM.Count < 1) PostCommandPrivatPostActivChat(chat, "Player name is empty");
                            else PostCommandAddPlayer(chat, argsM[0]);
                            break;
                        case "/killmyallplease":
                            Loger.Log("Server killmyallplease OwnerMaker=" + (chat.OwnerMaker ? "1" : "0"));
                            if (chat.OwnerMaker) PostCommandPrivatPostActivChat(chat, "Operation only for the shared channel");
                            else
                            {
                                chat.Posts.Add(new ChatPost()
                                {
                                    Time = DateTime.UtcNow,
                                    Message = "User " + Player.Public.Login + " deleted settlements.",
                                    OwnerLogin = "system"
                                });
                                var data = Repository.GetData;
                                lock (data)
                                {
                                    for (int i = 0; i < data.WorldObjects.Count; i++)
                                    {
                                        var item = data.WorldObjects[i];
                                        if (item.LoginOwner != Player.Public.Login) continue;
                                        //удаление из базы
                                        item.UpdateTime = timeNow;
                                        data.WorldObjects.Remove(item);
                                        data.WorldObjectsDeleted.Add(item);
                                    }
                                }
                                Player.SaveDataPacket = null;
                                Loger.Log("Server killmyallplease " + Player.Public.Login);
                                Player = null;
                                Repository.Get.ChangeData = true;
                            }
                            break;
                        case "/killhimplease":
                            Loger.Log("Server killhimplease OwnerMaker=" + (chat.OwnerMaker ? "1" : "0"));
                            if (!Player.IsAdmin) PostCommandPrivatPostActivChat(chat, "Command only for admin");
                            else
                            if (argsM.Count < 1) PostCommandPrivatPostActivChat(chat, "Player name is empty");
                            else
                            {
                                var killPlayer = Repository.GetData.PlayersAll
                                    .FirstOrDefault(p => p.Public.Login == argsM[0]);
                                if (killPlayer == null)
                                    PostCommandPrivatPostActivChat(chat, "User " + argsM[0] + " not found");
                                else
                                {
                                    chat.Posts.Add(new ChatPost()
                                    {
                                        Time = DateTime.UtcNow,
                                        Message = "User " + killPlayer.Public.Login + " deleted settlements.",
                                        OwnerLogin = "system"
                                    });
                                    var data = Repository.GetData;
                                    lock (data)
                                    {
                                        for (int i = 0; i < data.WorldObjects.Count; i++)
                                        {
                                            var item = data.WorldObjects[i];
                                            if (item.LoginOwner != killPlayer.Public.Login) continue;
                                            //удаление из базы
                                            item.UpdateTime = timeNow;
                                            data.WorldObjects.Remove(item);
                                            data.WorldObjectsDeleted.Add(item);
                                        }
                                    }
                                    killPlayer.SaveDataPacket = null;
                                    Repository.Get.ChangeData = true;
                                    Loger.Log("Server killhimplease " + killPlayer.Public.Login);
                                }
                            }
                            break;
                        default:
                            PostCommandPrivatPostActivChat(chat, "Command not found: " + command);
                            return new ModelStatus()
                            {
                                Status = 2,
                                Message = "Command not found: " + command
                            };
                    }
                    // } обработка команд чата

                }
                else
                {
                    Loger.Log("Server post " + Player.Public.Login /*+ " " + timeNow.Ticks*/ + ":" + pc.Message);
                    var mmsg = pc.Message;
                    if (mmsg.Length > 2048) mmsg = mmsg.Substring(0, 2048);
                    chat.Posts.Add(new ChatPost()
                    {
                        Time = timeNow,
                        Message = mmsg,
                        OwnerLogin = Player.Public.Login
                    });
                }

                return new ModelStatus()
                {
                    Status = 0,
                    Message = null
                };
            }
        }

        private void PostCommandPrivatPostActivChat(Chat chat, string msg)
        {
            chat.Posts.Add(new ChatPost()
            {
                Time = DateTime.UtcNow,
                Message = msg,
                OwnerLogin = "system",
                OnlyForPlayerLogin = Player.Public.Login
            });
        }

        private void PostCommandAddPlayer(Chat chat, string who)
        {
            if (chat.PartyLogin.Any(p => p == who))
                PostCommandPrivatPostActivChat(chat, "The player is already here");
            else
            {
                var newPlayer = Repository.GetData.PlayersAll
                    .FirstOrDefault(p => p.Public.Login == who);
                if (newPlayer == null)
                    PostCommandPrivatPostActivChat(chat, "User " + who + " not found");
                else if (!Player.PublicChat.PartyLogin.Any(p => p == who))
                    PostCommandPrivatPostActivChat(chat, "Can not access " + who + " player");
                else if (!chat.OwnerMaker)
                    PostCommandPrivatPostActivChat(chat, "People can not be added to a shared channel");
                else if (chat.OwnerLogin != Player.Public.Login && !Player.IsAdmin)
                    PostCommandPrivatPostActivChat(chat, "You can not add people");
                else
                {
                    chat.Posts.Add(new ChatPost()
                    {
                        Time = DateTime.UtcNow,
                        Message = "User " + newPlayer.Public.Login + " entered the channel.",
                        OwnerLogin = "system"
                    });
                    chat.PartyLogin.Add(newPlayer.Public.Login);
                    newPlayer.Chats.Add(chat);
                    Repository.Get.ChangeData = true;
                }
            }
        }
        
        public static List<string> SplitBySpace(string args)
        {
            int i = 0;
            var argsM = new List<string>();
            while (i + 1 < args.Length)
            {
                if (args[i] == '\'')
                {
                    int endK = i;
                    bool exit;
                    do //запускаем поиск след кавычки снова, если после найденной ещё одна
                    {
                        exit = true;
                        endK = args.IndexOf('\'', endK + 1);
                        if (endK >= 0 && endK + 1 < args.Length && args[endK + 1] == '\'')
                        {
                            //это двойная кавычка - пропускаем её
                            endK++;
                            exit = false;
                        }
                    }
                    while (!exit);

                    if (endK >= 0)
                    {
                        argsM.Add(args.Substring(i + 1, endK - i - 1).Replace("''", "'"));
                        i = endK + 1;
                        continue;
                    }
                }
                var ni = args.IndexOf(" ", i);
                if (ni >= 0)
                {
                    //условие недобавления для двойного пробела
                    if (ni > i) argsM.Add(args.Substring(i, ni - i));
                    i = ni + 1;
                    continue;
                }
                else break;
            }
            if (i < args.Length)
            {
                argsM.Add(args.Substring(i));
            }

            return argsM;
        }


        public ModelStatus ExchengeEdit(OrderTrade order)
        {
            if (Player == null) return null;
            lock (Player)
            {
                var timeNow = DateTime.UtcNow;

                var data = Repository.GetData;

                if (Player.Public.Login != order.Owner.Login)
                {
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = "Ошибка. Ордер другого игрока"
                    };
                }

                if (order.Id == 0)
                {
                    //создать новый

                    //актуализируем
                    order.Created = timeNow;

                    order.Owner = Player.Public;

                    if (order.PrivatPlayers == null) order.PrivatPlayers = new List<Player>();
                    order.PrivatPlayers = order.PrivatPlayers
                        .Select(pp => data.PlayersAll.FirstOrDefault(p => p.Public.Login == pp.Login)?.Public)
                        .ToList();
                    if (order.PrivatPlayers.Any(pp => pp == null))
                    {
                        return new ModelStatus()
                        {
                            Status = 2,
                            Message = "Ошибка. Указан несуществующий игрок"
                        };
                    }

                    order.Id = Repository.GetData.GetChatId();

                    lock (data)
                    {
                        data.Orders.Add(order);
                    }
                }
                else
                {
                    //проверяем на существование
                    lock (data)
                    {
                        var id = order.Id > 0 ? order.Id : -order.Id;
                        var dataOrder = data.Orders.FirstOrDefault(o => o.Id == id);
                        if (dataOrder == null
                            || Player.Public.Login != dataOrder.Owner.Login)
                        {
                            return new ModelStatus()
                            {
                                Status = 3,
                                Message = "Ошибка. Ордер не найден"
                            };
                        }

                        if (order.Id > 0)
                        {
                            //редактирование 

                            //актуализируем
                            order.Created = timeNow;

                            order.Owner = Player.Public;

                            if (order.PrivatPlayers == null) order.PrivatPlayers = new List<Player>();
                            order.PrivatPlayers = order.PrivatPlayers
                                .Select(pp => data.PlayersAll.FirstOrDefault(p => p.Public.Login == pp.Login)?.Public)
                                .ToList();
                            if (order.PrivatPlayers.Any(pp => pp == null))
                            {
                                return new ModelStatus()
                                {
                                    Status = 4,
                                    Message = "Ошибка. Указан несуществующий игрок"
                                };
                            }

                            Loger.Log("Server ExchengeEdit " + Player.Public.Login + " Edit Id = " + order.Id.ToString());
                            data.Orders[data.Orders.IndexOf(dataOrder)] = order;
                        }
                        else
                        {
                            //Удаление
                            Loger.Log("Server ExchengeEdit " + Player.Public.Login + " Delete Id = " + order.Id.ToString());
                            data.Orders.Remove(dataOrder);
                        }
                    }
                }
                Repository.Get.ChangeData = true;

                return new ModelStatus()
                {
                    Status = 0,
                    Message = null
                };
            }
        }

        public ModelStatus ExchengeBuy(ModelOrderBuy buy)
        {
            if (Player == null) return null;
            return null;
            ///проверям в одной ли точке находятся
            ///остальные проверки похоже бессмысленны
            ///формируем письмо в обе стороны
            ///когда приходит обратное письмо, то вещи изымаются и стартует автосейв
            ///todo: если при приеме письма возникли ошибки (красные надписи в логе, мои ошибки или для письма-изьятия нет вещей),
            /// то пытаемся удалить, то что уже успели добавить и в пиьме меняем адресата на противоположного, 
            /// чтобы вернуть вещи (особый статус без проверки tile)
            /* todo
            lock (Player)
            {
                var timeNow = DateTime.UtcNow;
                if (string.IsNullOrEmpty(pc.Message))
                    return new ModelStatus()
                    {
                        Status = 0,
                        Message = null
                    };
            }
            */
        }

        public ModelOrderLoad ExchengeLoad()
        {
            if (Player == null) return null;

            lock (Player)
            {
                var timeNow = DateTime.UtcNow;
                var res = new ModelOrderLoad()
                {
                    Status = 0,
                    Message = null
                };

                //Список игроков кого видим
                var ps = PartyLoginSee();

                var data = Repository.GetData;
                res.Orders = (data.Orders ?? new List<OrderTrade>())
                    .Where(o => Player.Public.Login == o.Owner.Login
                        || ps.Any(p => p == o.Owner.Login)
                            && (o.PrivatPlayers.Count == 0 || o.PrivatPlayers.Any(p => p.Login == Player.Public.Login)))
                    .ToList();

                            

                return res;
            }
        }

        public AttackInitiatorFromSrv AttackOnlineInitiator(AttackInitiatorToSrv fromClient)
        {
            if (Player == null) return null;

            lock (Player)
            {
                var timeNow = DateTime.UtcNow;
                var data = Repository.GetData;
                var res = new AttackInitiatorFromSrv()
                {
                };

                //инициализируем общий объект
                if (fromClient.StartHostPlayer != null)
                {
                    if (Player.AttackData != null)
                    {
                        res.ErrorText = "There is an active attack";
                        return res;
                    }
                    var hostPlayer = data.PlayersAll.FirstOrDefault(p => p.Public.Login == fromClient.StartHostPlayer);

                    if (hostPlayer == null)
                    {
                        res.ErrorText = "Player not found";
                        return res;
                    }
                    if (hostPlayer.AttackData != null)
                    {
                        res.ErrorText = "The player participates in the attack";
                        return res;
                    }

                    var att = new AttackServer();
                    var err = att.New(Player, hostPlayer, fromClient);
                    if (err != null)
                    {
                        res.ErrorText = err;
                        return res;
                    }
                }
                if (Player.AttackData == null)
                {
                    res.ErrorText = "Unexpected error, no data";
                    return res;
                }

                //передаем управление общему объекту
                res = Player.AttackData.RequestInitiator(fromClient);

                return res;
            }
        }

        public AttackHostFromSrv AttackOnlineHost(AttackHostToSrv fromClient)
        {
            if (Player == null) return null;

            lock (Player)
            {
                var timeNow = DateTime.UtcNow;
                var data = Repository.GetData;
                var res = new AttackHostFromSrv()
                {
                };

                if (Player.AttackData == null)
                {
                    res.ErrorText = "Unexpected error, no data";
                    return res;
                }

                //передаем управление общему объекту
                res = Player.AttackData.RequestHost(fromClient);

                return res;
            }
        }

        private const string ChatHelpText = @"Available commands: 
/help
/createChat
/exitChat
/addPlayer
/renameChat

";
        private const string ChatHelpTextAdmin = @"/killmyallplease
/killhimplease

";

    }
}
