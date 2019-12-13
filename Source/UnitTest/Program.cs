using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace OC.UnitTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = new BotRepositoryTests();
            var methods = t.GetType().GetMethods();
            foreach (var m in methods.Where(x => x.IsPublic && x.CustomAttributes.Any(y => y.AttributeType == typeof(TestMethodAttribute))))
            {
                m.Invoke(t, null);
            }
        }
    }
}