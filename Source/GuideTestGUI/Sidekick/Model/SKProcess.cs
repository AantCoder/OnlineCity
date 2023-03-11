using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Sidekick.Sidekick.Model
{
    public class SKProcess
    {
        protected IntPtr Proc;
        protected IntPtr ProcParent;
        protected Random Rnd = new Random();
        protected Rectangle ScreenSize;
        public Rectangle ProcSize;
        private DateTime UpdateProcSizeTime;

        public SKProcess(IntPtr proc)
        { 
            Proc = proc;
            ProcParent = (IntPtr)0;
            ScreenSize = Screen.PrimaryScreen.Bounds;

            ProcSize = CaptureWindow.GetWindowRect(Proc);
        }


        protected void CheckProcSize()
        {
            if ((DateTime.UtcNow - UpdateProcSizeTime).TotalMilliseconds > 5000) UpdateProcSize();
        }

        protected void UpdateProcSize()
        {
            UpdateProcSizeTime = DateTime.UtcNow;
            ProcSize = CaptureWindow.GetWindowRect(Proc);
        }

        public bool CheckForegroundWindow()
        {
            UpdateProcSize();
            //работает немного некорректно: может активировать дочерний элемент, а не естественная активация формы элемента
            var fw = User32.GetForegroundWindow();
            if (fw == Proc || fw == ProcParent) return true;
            Thread.Sleep(Rnd.Next(150, 300));
            User32.SetForegroundWindow(Proc);
            Thread.Sleep(Rnd.Next(1500, 1600));
            var fw1 = User32.GetForegroundWindow();
            if (fw1 != Proc && fw1 != ProcParent)
            {
                Thread.Sleep(1000);
                var fw2 = User32.GetForegroundWindow();
                if (fw2 == fw1 && fw2 == fw) //если ничего не поменялось, запоминаем, что так нормально (активен один из родителей)
                {
                    ProcParent = fw2;
                    return true;
                }
            }
            return false;
        }

        #region пересчет позиции мыши

        protected Point GetMousePos()
        {
            return Cursor.Position;
        }

        protected Point PosToRelativePos(Point point)
        {
            return new Point(
                point.X * 65535 / ScreenSize.Width + 1 + Rnd.Next(65535 / ScreenSize.Width - 1)
                , point.Y * 65535 / ScreenSize.Height + 1 + Rnd.Next(65535 / ScreenSize.Height - 1));
        }

        protected Point PosFromRelativePos(Point relativePos)
        {
            return new Point(
                relativePos.X * ScreenSize.Width / 65535
                , relativePos.Y * ScreenSize.Height / 65535);
        }

        public Point GetMousePosProc()
        {
            var pos = GetMousePos();
            pos.X -= ProcSize.Left;
            pos.Y -= ProcSize.Top;
            return pos;
        }

        protected Point PosProcToRelativePos(Point point)
        {
            var pos = point;
            pos.X += ProcSize.Left;
            pos.Y += ProcSize.Top;
            return PosToRelativePos(pos);
        }

        protected Point PosProcFromRelativePos(Point relativePos)
        {
            var pos = PosFromRelativePos(relativePos);
            pos.X -= ProcSize.Left;
            pos.Y -= ProcSize.Top;
            return pos;
        }

        #endregion 

    }
}
