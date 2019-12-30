using System;
using System.Linq;
using Transfer;
using Model;
using ServerOnlineCity.Model;
using ServerOnlineCity.Common;

namespace ServerOnlineCity.Services
{
    public sealed class UpdateChat : IGenerateResponseContainer
    {
        public int RequestTypePackage => 17;

        public int ResponseTypePackage => 18;

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
                    Time = DateTime.UtcNow
                };

                //Список игроков кого видим
                var ps = StaticHelper.PartyLoginSee(context.Player);

                //Копируем чат без лишнего и отфильтровываем посты
                res.Chats = context.Player.Chats
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

                return res;
            }
        }
    }
}