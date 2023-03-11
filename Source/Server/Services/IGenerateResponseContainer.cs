using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    public interface IGenerateResponseContainer
    {
        int RequestTypePackage { get; }
        int ResponseTypePackage { get; }
        ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context);
    }
}
