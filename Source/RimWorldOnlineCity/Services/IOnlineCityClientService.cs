using OCUnion.Transfer.Model;

namespace RimWorldOnlineCity.Services
{
    interface IOnlineCityClientService <T>        
    {
        PackageType RequestTypePackage { get; }
        PackageType ResponseTypePackage { get; }
        T GenerateRequestAndDoJob(object context);
    }
}
