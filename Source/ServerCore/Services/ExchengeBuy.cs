using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class ExchengeBuy : IGenerateResponseContainer
    {
        public int RequestTypePackage => 23;

        public int ResponseTypePackage => 24;

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = exchengeBuy((ModelOrderBuy)request.Packet, ref player);
            return result;
        }

        private ModelStatus exchengeBuy(ModelOrderBuy buy, ref PlayerServer player)
        {
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
    }
}
