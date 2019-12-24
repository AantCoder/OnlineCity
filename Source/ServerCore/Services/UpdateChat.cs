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

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = updateChat((ModelUpdateTime)request.Packet, ref player);
            return result;
        }

        private ModelUpdateChat updateChat(ModelUpdateTime time, ref PlayerServer player)
        {
            lock (player)
            {
                var res = new ModelUpdateChat()
                {
                    Time = DateTime.UtcNow
                };

                //Список игроков кого видим
                var ps = StaticHelper.PartyLoginSee(player);

                //Копируем чат без лишнего и отфильтровываем посты
                res.Chats = player.Chats
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