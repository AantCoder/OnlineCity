using System;
using System.Reflection;
using System.Runtime.Serialization;

//https://habr.com/ru/post/430646/
//https://habr.com/ru/post/159855/
namespace ServerOnlineCity
{
    public class ServerCoreSerializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (assemblyName.Contains("OCServer"))
            {
                var bindToType = Type.GetType(typeName.Replace("OCServer", "ServerOnlineCity"));
                return bindToType;
            }
            else
            {
                var bindToType = LoadTypeFromAssembly(assemblyName, typeName.Replace("OCServer", "ServerOnlineCity"));
                return bindToType;
            }
        }

        private Type LoadTypeFromAssembly(string assemblyName, string typeName)
        {
            if (string.IsNullOrEmpty(assemblyName) ||
                string.IsNullOrEmpty(typeName))
                return null;
            var assembly = Assembly.Load(assemblyName);
            return FormatterServices.GetTypeFromAssembly(assembly, typeName);
        }
    }
}