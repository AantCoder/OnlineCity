using GuideTestGUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sidekick.Sidekick.Model;
using System;
using System.Threading;

namespace GUITest
{
    [TestClass]
    public class UnitTestGuideTestGUI
    {

        [TestMethod]
        public void TestGuideUI()
        {
            //Перед запуском должна стоять английская раскладка
            using (var guide = new GuideUI())
            {
                var test = guide.Test();

                Assert.IsTrue(test);
            }
        }

        [TestMethod]
        public void TestGraphics()
        {
            Thread.Sleep(1000);

            var graphics = new SKProcessCanvas(IntPtr.Zero, null);
            //Тут в конце должно отобразится исходное изображение и обработанное. Нужно нажать любую клавишу
            graphics.Test(@"c:\W\OnlineCity\Разное\SikuliX\SikuliXCSharp\SikuliXCSharp\1644782905899.png");

            Assert.IsTrue(true);
        }
    }
}
