using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldOnlineCity.Services
{
    public class AnyLoadTask
    {
        public long Hash { get; set; }
        public string Data { get; set; }
        public DateTime LoadDate { get; set; }
    }

    public class AnyLoad
    {
        private static Thread Downloader = null;

        private static List<AnyLoad> Tasks = new List<AnyLoad>();

        private static Dictionary<long, AnyLoadTask> Database = new Dictionary<long, AnyLoadTask>();

        public List<AnyLoadTask> ListLoad { get; set; }
        public Action<AnyLoad, int> TaskProgress { get; set; }
        public Action<AnyLoad> TaskFinish { get; set; }
        public Action<AnyLoad, string> TaskError { get; set; }

        /// <summary>
        /// Создает задачу загрузки и помещает в очередь на выполнение.
        /// При завершении будет вызвано либо taskFinish, либо taskError при ошибке, кроме случая вызова Cancel()
        /// </summary>
        public AnyLoad(List<AnyLoadTask> listLoad, Action<AnyLoad> taskFinish, Action<AnyLoad, int> taskProgress, Action<AnyLoad, string> taskError)
        {
            ListLoad = listLoad;
            TaskFinish = taskFinish;
            TaskProgress = taskProgress;
            TaskError = taskError;
            lock (Tasks)
            {
                Tasks.Add(this);
                Start();
            }
        }

        public void Cancel()
        {
            Loger.Log("AnyLoadDownloadThread Cancel");
            lock (Tasks) Tasks.Remove(this);
            TaskFinish = null;
            TaskProgress = null;
            TaskError = null;
        }

        private void Start()
        {
            if (Downloader == null)
            {
                Downloader = new Thread(DownloadThread);
                Downloader.IsBackground = true;
                Downloader.Start();
            }
        }

        private static void DownloadThread()
        {
            try
            {
                while (Tasks.Count > 0)
                {
                    if (!DownloadCheckConnect()) 
                    {
                        Error("Load: error connect");
                        break;
                    }
                    //берем задачу
                    AnyLoad that;
                    lock (Tasks)
                    {
                        if (Tasks.Count == 0) break;
                        that = Tasks[0];
                    }
                    if (that.ListLoad.Count == 0)
                    {
                        Loger.Log("AnyLoadDownloadThread TasksRemove 1");
                        TasksRemove(that);
                        continue;
                    }

                    //качаем блоками по 10 штук с проверкой, что сама задача ещё в списке
                    List<AnyLoadTask> download = new List<AnyLoadTask>();
                    int exist = 0;
                    for (int i = 0; i < that.ListLoad.Count && download.Count < 10; i++)
                    {
                        AnyLoadTask db;
                        if (Database.TryGetValue(that.ListLoad[i].Hash, out db))
                        {
                            that.ListLoad[i].LoadDate = db.LoadDate = DateTime.UtcNow;
                            that.ListLoad[i].Data = db.Data;
                            exist++;
                        }
                        else
                        {
                            download.Add(that.ListLoad[i]);
                        }
                    }
                    if (download.Count > 0)
                    {
                        if (!DownloadCheckConnect())
                        {
                            Error("Load: error connect.");
                            break;
                        }
                        DownloadList(download);
                        exist += download.Count;
                    }

                    //вызываем события окончания загрузки
                    lock (Tasks)
                    {
                        if (!Tasks.Contains(that))
                        {
                            Loger.Log($"Client AnyLoad drop task {exist}/{that.ListLoad.Count} {(int)(100 * exist / that.ListLoad.Count)}%");
                            continue;
                        }
                    }
                    Loger.Log($"Client AnyLoad TaskProgress {exist}/{that.ListLoad.Count} {(int)(100 * exist / that.ListLoad.Count)}%");
                    if (that.TaskProgress != null) that.TaskProgress(that, (int)(100 * exist / that.ListLoad.Count));
                    if (that.ListLoad.Count == exist)
                    {
                        Loger.Log("AnyLoadDownloadThread TasksRemove 2");
                        TasksRemove(that);
                    }
                }
            }
            catch(Exception exp)
            {
                Error("Load error: " + exp.ToString());
            }
            //lock (Tasks)
            {
                Downloader = null;
            }
        }

        private static void TasksRemove(AnyLoad that)
        {
            lock (Tasks) Tasks.Remove(that);
            Loger.Log($"Client AnyLoad TaskFinish cnt={that.ListLoad.Count}");
            if (that.TaskFinish != null) that.TaskFinish(that);
        }

        private static bool DownloadCheckConnect()
        {
            while (SessionClient.Get.IsLogined && SessionClient.IsRelogin) Thread.Sleep(10);
            return SessionClient.Get.IsLogined;
        }

        private static void DownloadList(List<AnyLoadTask> download)
        {
            List<string> datas = null;
            SessionClientController.Command((connect) =>
            {
                datas = connect.AnyLoad(download.Select(d => d.Hash).ToList());
            });
            if (datas == null || datas.Count != download.Count) throw new ApplicationException("bad request");

            for (int i = 0; i < download.Count; i++)
            {
                download[i].Data = datas[i];
                download[i].LoadDate = DateTime.UtcNow;
                Database[download[i].Hash] = download[i];
            }
        }

        private static void Error(string error)
        {
            Loger.Log("Client AnyLoad error: " + error, Loger.LogLevel.ERROR);
            lock (Tasks)
            {
                foreach (var task in Tasks)
                {
                    task.TaskError(task, error);
                }
                Tasks.Clear();
                Downloader = null;
            } 
        }

    }
}
