using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using WindowsInput;

namespace Sidekick.Sidekick.Model
{
    public class SKMouse : SKProcess
    {
        private InputSimulator Simulator = new InputSimulator();

        public SKMouse(IntPtr proc)
            : base(proc)
        { 
        }

        public SKMouse Scroll(int scrollAmountInClicks)
        {
            Simulator.Mouse.VerticalScroll(scrollAmountInClicks);
            return this;
        }

        /// <summary>
        /// Отправить нажатие клавиш приложению. Можно запускать из разных потоков.
        /// </summary>
        /// <param name="leftButton">true - левая кнопка мыши, false - правая</param>
        /// <param name="down">null для клика, true - down, false - up</param>
        /// <param name="doubleClick">Даблклик, только если down == null</param>
        public SKMouse Click(bool leftButton = true, bool? down = null, bool doubleClick = false)
        {
            CheckForegroundWindow();
            if (down == null)
            {
                Thread.Sleep(Rnd.Next(50, 200));
                if (leftButton)
                    Simulator.Mouse.LeftButtonDown();
                else
                    Simulator.Mouse.RightButtonDown();
                //Thread.Sleep(Rnd.Next(150, 300));
                Thread.Sleep(Rnd.Next(50, 100));
                if (leftButton)
                    Simulator.Mouse.LeftButtonUp();
                else
                    Simulator.Mouse.RightButtonUp();
                if (doubleClick)
                {
                    Thread.Sleep(Rnd.Next(50, 70));
                    if (leftButton)
                        Simulator.Mouse.LeftButtonDown();
                    else
                        Simulator.Mouse.RightButtonDown();
                    //Thread.Sleep(Rnd.Next(150, 300));
                    Thread.Sleep(Rnd.Next(50, 70));
                    if (leftButton)
                        Simulator.Mouse.LeftButtonUp();
                    else
                        Simulator.Mouse.RightButtonUp();
                }
                Thread.Sleep(Rnd.Next(200, 400));
            }
            else
            {
                Thread.Sleep(Rnd.Next(100, 200));
                if (down.Value)
                    if (leftButton)
                        Simulator.Mouse.LeftButtonDown();
                    else
                        Simulator.Mouse.RightButtonDown();
                else
                    if (leftButton)
                        Simulator.Mouse.LeftButtonUp();
                    else
                        Simulator.Mouse.RightButtonUp();
                Thread.Sleep(Rnd.Next(50, 100));
            }
            return this;
        }

        public void Test(int x, int y)
        {
            CheckForegroundWindow();
            Simulator.Mouse.LeftButtonDown();
            Move(x, y);
            Simulator.Mouse.LeftButtonUp();
        }

        public SKMouse Move(Point pos) => Move(pos.X, pos.Y);

        public SKMouse Move(int x, int y)
        {
            CheckProcSize();
            var target = PosProcToRelativePos(new Point(x, y));
            double step = Rnd.Next(65535 / 50, 65535 / 20); // примерно от 20 до 50 пикселей за 3-7 ms крейсерская скорость мыши
            Point targetTemp = target, targetTemp2 = target; //временная цель на которую двигается мышь
            double lengthTemp = 65535 * 4;
            double acc = 4;
            double div = 1;

            Thread.Sleep(Rnd.Next(10, 30));
            bool exit = false;
            while (!exit)
            {
                Thread.Sleep(Rnd.Next(3, 7));
                var mouse = PosToRelativePos(GetMousePos());
                var length = Math.Sqrt((double)(target.X - mouse.X) * (double)(target.X - mouse.X) + (double)(target.Y - mouse.Y) * (double)(target.Y - mouse.Y));

                if (length < 65535 / 5)
                {
                    if (length < 65535 / 10)
                        div = 4;
                    else
                        div = 2;
                }

                if (length < step / acc / div)
                {
                    Simulator.Mouse.MoveMouseTo(target.X, target.Y);
                    break;
                }

                if (length < lengthTemp / 2)
                {
                    lengthTemp = length;
                    targetTemp = new Point(target.X + Rnd.Next((int)-length / 5, (int)length / 5), target.Y + Rnd.Next((int)-length / 5, (int)length / 5));
                    targetTemp2 = new Point(target.X + Rnd.Next((int)-length / 5, (int)length / 5), target.Y + Rnd.Next((int)-length / 5, (int)length / 5));
                }

                //определем относительную длинну на которую передвинимся
                var delta = step / acc / div / lengthTemp;
                //определяем влияние отклонения targetTemp или targetTemp2
                var temp2 = (length - (lengthTemp / 2)) / (lengthTemp / 2);
                int xx = mouse.X + (int)((targetTemp.X - mouse.X) * delta * temp2 + (targetTemp2.X - mouse.X) * delta * (1 - temp2));
                int yy = mouse.Y + (int)((targetTemp.Y - mouse.Y) * delta * temp2 + (targetTemp2.Y - mouse.Y) * delta * (1 - temp2));

                Simulator.Mouse.MoveMouseTo(xx, yy);

                if (acc > 1) acc--;
            }
            Thread.Sleep(Rnd.Next(10, 30));

            return this;
        }

        /// <summary>
        /// Случайное ожидание до и после действия события клавиши.
        /// Переключение режимов (controlKey) (со своим одижанием до и после) происходит после задержки печати целевой клавиши.
        /// </summary>
        /// <param name="beforeAct">До или после события; null - во время</param>
        /// <param name="controlKey">Это управляющая клавиша (shift, alt, control)</param>
        /// <returns></returns>
        private int Wait(bool? beforeAct, bool controlKey)
        {
            int wait = beforeAct == null ? Rnd.Next(80, 150) /*№1*/
                : beforeAct.Value
                    ? controlKey ? Rnd.Next(5, 10) /*№2*/ : Rnd.Next(30, 70) /*№3*/
                    : controlKey ? Rnd.Next(30, 50) /*№4*/ : Rnd.Next(150, 300) /*№5*/;
            //Например, при вводе аББа будут следующие задержки:
            // №3 {a down} №1 {a up} №5 
            // №3 №2 {shift down} №4 {б down} №1 {б up} №5
            // №3 {б down} №1 {б up} №5 
            // №3 №2 {shift up} №4 {a down} №1 {a up} №5
            // Т.е.:
            // 50 {a down} 115 {a up} 242 {shift down} 40 {б down} 115 {б up} 235 {б down} 115 {б up} 242 {shift up} 40 {a down} 115 {a up} 185
            Thread.Sleep(Rnd.Next(5, 10));
            return wait;
        }


    }
}
