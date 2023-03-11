using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUITest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //В релизе запускается формочка для разработки новых тестов
            //В дебаге запускаются тесты с консоли на GUI с запуском игры для отладки или на сервере
//#if DEBUG
//            var t = new GUITestRimWorld();
//            //t.CreateDataFromGame();
//            var methods = t.GetType().GetMethods();
//            foreach (var m in methods.Where(x => x.IsPublic && x.CustomAttributes.Any(y => y.AttributeType == typeof(TestMethodAttribute))))
//            {
//                m.Invoke(t, null);
//            }
//#else
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UtilityGorTestForm(args?.Length > 0 ? args[0] : null));
//#endif
        }
    }
}
