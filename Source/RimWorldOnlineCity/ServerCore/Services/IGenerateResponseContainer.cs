using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    public interface IGenerateResponseContainer
    {
        byte RequestTypePackage { get; }
        byte ResponseTypePackage { get; }
        ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player);
    }
}
