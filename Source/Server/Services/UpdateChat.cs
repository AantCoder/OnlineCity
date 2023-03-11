using System;
using System.Linq;
using Transfer;
using Model;
using ServerOnlineCity.Model;
using ServerOnlineCity.Common;
using System.Collections.Generic;
using OCUnion.Transfer.Model;

namespace ServerOnlineCity.Services
{
    public sealed class UpdateChat : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request17;

        public int ResponseTypePackage => (int)PackageType.Response18;

        private readonly ChatManager _chatManager = ChatManager.Instance;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = updateChat((ModelUpdateTime)request.Packet, context);
            return result;
        }

        private ModelUpdateChat updateChat(ModelUpdateTime time, ServiceContext context)
        {
            lock (context.Player)
            {
                var res = new ModelUpdateChat()
                {
                    Time = DateTime.UtcNow,
                    Chats = new List<Chat>(),
                };
                bool fullRequest = time.Time == DateTime.MinValue;

                var myLogin = context.Player.Public.Login;

                //Список игроков кого видим, а видим мы пока не построили консоль связи всех кто рядом в 10 клетках)
                // ( ну или мы админ админ, модератор or discord)
                var ps = StaticHelper.PartyLoginSee(context.Player);
                //Копируем чат без лишнего и отфильтровываем посты   

                foreach (var chatPair in context.Player.Chats)
                {
                    var ct = chatPair.Key;
                    var resChat = new Chat()
                    {
                        Id = ct.Id,
                        OwnerLogin = ct.OwnerLogin,
                        Name = ct.Name,
                        OwnerMaker = ct.OwnerMaker,
                        Posts = new List<ChatPost>(),
                        LastChanged = ct.LastChanged,
                    };

                    //Копируем чат без лишнего и отфильтровываем посты          
                    var ix = chatPair.Value;
                    var countOfPosts = ct.Posts.Count;
                    var fullRequestMinCountPosts = 20;
                    if (fullRequest && countOfPosts - ((int)ix.Value + 1) < fullRequestMinCountPosts)
                    {
                        ix.Value = countOfPosts - fullRequestMinCountPosts - 1;
                        if (ix.Value < 0) ix.Value = 0;
                    }

                    for (var i = (int)ix.Value + 1; i < countOfPosts; i++)
                    {
                        var post = ct.Posts[i];
                        if (post.OnlyForPlayerLogin == null && ps.Any(pp => pp == post.OwnerLogin) || post.OnlyForPlayerLogin == myLogin)
                        {
                            resChat.Posts.Add(post);
                        }
                    }

                    ix.Value = countOfPosts - 1;

                    // Если с с момента последнего изменения изменился список логинов ( добавили или удалили, обновляем список)                    
                    if (fullRequest || ct.LastChanged > ix.Time)
                    {
                        resChat.PartyLogin = ct.PartyLogin;
                        ix.Time = ct.LastChanged;
                    }

                    res.Chats.Add(resChat);
                }

                return res;
            }
        }
    }
}