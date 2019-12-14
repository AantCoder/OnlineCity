using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace OC.UnitTest
{
    class Program
    {
        static void Main(string[] args)
        {            
            // return; 
            // Просто убери комментарий выше, если тесты сейчас не нужны
            // для запуска: надо поставить Multiply startup project  и выбрать последним этот проект 
            // если есть идеи как запустить автоматически, я только за
            var t = new BotRepositoryTests();
            var methods = t.GetType().GetMethods();
            foreach (var m in methods.Where(x => x.IsPublic && x.CustomAttributes.Any(y => y.AttributeType == typeof(TestMethodAttribute))))
            {
                m.Invoke(t, null);
            }
        }
    }
}