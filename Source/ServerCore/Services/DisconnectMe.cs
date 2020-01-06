using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    namespace ServerOnlineCity.Services
    {
        /// <summary>
        /// Корректная инициация дисконнекта c сообщением причины другой стороне.
        /// </summary>
        internal sealed class DisconnectMe : IGenerateResponseContainer
        {
            public int RequestTypePackage => (int)PackageType.Request39Disconnect;

            public int ResponseTypePackage => (int)PackageType.Response40Disconnect;

            public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
            {
                if (context.Player == null) return null;
                var result = new ModelContainer() { TypePacket = ResponseTypePackage };
                result.Packet = GetInfo((ModelInt)request.Packet, context);
                return result;
            }

            public ModelInt GetInfo(ModelInt packet, ServiceContext context)
            {
                var reason = (DisconnectReason)packet.Value;

                lock (context.Player)
                {
                    context.Player.ExitReason = reason;

                    var result = new ModelInt() { Value = (int)DisconnectReason.CloseConnection };
                    return result;
                }
            }
        }
    }
}
