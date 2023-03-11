using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OCUnion
{
    public class WorkTimer
    {
        private class WorkTimerData
        {
            public long Interval;
            public Action Act;
            public DateTime LastRun;
        }

        private List<WorkTimerData> Timers;
        private int Index;
        public bool IsStop { get; private set; } = false;
        public bool Pause { get; set; } = false;
        public Thread ThreadDo { get; private set; }
        public DateTime LastLoop { get; set; }

        public WorkTimer()
        {
            Timers = new List<WorkTimerData>();
            Index = 0;
            ThreadDo = new Thread(Do);
            ThreadDo.IsBackground = true;
            ThreadDo.Start();
        }

        /// <summary>
        /// После остановки невозможно продолжить работу, класс должен быть пересоздан
        /// </summary>
        public void Stop()
        {
            IsStop = true;
            Timers = new List<WorkTimerData>(); //без блокировки это может вызвать исключение, но это допустимо, т.к. останавливаем, а блокировка недопкустима
        }

        public object Add(long interval, Action action)
        {
            var item = new WorkTimerData()
            {
                Interval = interval,
                Act = action,
                LastRun = DateTime.UtcNow
            };
            lock (Timers)
            {
                Timers.Add(item);
            }
            return item;
        }

        public void Remove(object obj)
        {
            var item = obj as WorkTimerData;
            if (item == null) return;
            lock (Timers)
            {
                Timers.Remove(item);
            }
        }

        private void Do()
        {
            var needSleep = true;
            while (!IsStop)
            {
                if (Pause)
                {
                    Thread.Sleep(1);
                    continue;
                }
                if (needSleep) Thread.Sleep(1);
                needSleep = true;
                try
                {
                    lock (Timers)
                    {
                        if (Timers.Count == 0) continue;
                        var now = DateTime.UtcNow;
                        LastLoop = now;
                        var curIndex = Index;
                        while (true)
                        {
                            var item = Timers[curIndex++];
                            if (curIndex >= Timers.Count) curIndex = 0;
                            if (item.LastRun.AddMilliseconds(item.Interval) < now)
                            {
                                //выполнение
                                item.LastRun = now;
                                DoItem(item.Act);
                                //записываем индекс с которого начнем цикл в следующий раз
                                Index = curIndex;
                                needSleep = false;
                                break;
                            }
                            //если ничего не выполняли, то проверяем, не завешен ли цикл
                            if (IsStop || curIndex == Index) break;
                        }
                    }
                }
                catch
                {
                }
            }

        }

        private void DoItem(Action action)
        {
            try
            {
                action(); //выполняем всё в потоке таймера
            }
            catch (Exception e)
            {
                ExceptionUtil.ExceptionLog(e, "WorkTimer");
            }
        }

    }
}
