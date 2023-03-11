using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace OC.UnitTest
{
    class Program
    {
        static void Main(string[] args)
        {            
            // Для запуска тестов из консоли для отладки или на сервере
            var t = new BotRepositoryTests();
            var methods = t.GetType().GetMethods();
            foreach (var m in methods.Where(x => x.IsPublic && x.CustomAttributes.Any(y => y.AttributeType == typeof(TestMethodAttribute))))
            {
                m.Invoke(t, null);
            }
        }
    }
}