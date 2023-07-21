using OCUnion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Util;

namespace ServerOnlineCity
{
    public class RepositorySaveData
    {
        /// <summary>
        /// Длинна истории сохранений пользователя (макс кол-во файлов с колонией пользователя от 1)
        /// </summary>
        public int CountSaveDataPlayer { get; } = 3;

        private Repository MainRepository;

        public RepositorySaveData(Repository repository)
        {
            MainRepository = repository;
        }

        private string GetFileNameBase(string login)
        {
            return Path.Combine(MainRepository.SaveFolderDataPlayers, Repository.NormalizeLogin(login) + ".dat");
        }

        /// <summary>
        /// Получить данные по сохранению игры пользователя.
        /// </summary>
        /// <param name="login">Логин пользователя, но основании него получается имя файла</param>
        /// <param name="numberSave">Номер сохранения, от 1 самого последнего, до CountSaveDataPlayer самого старого. Если такого файла нет, будет дан самый старый существующий</param>
        /// <returns>Содержимое сейва игры или null если ни одного файла с данными нет.</returns>
        public byte[] LoadPlayerData(string login, int numberSave)
        {
            if (numberSave < 1 || numberSave > CountSaveDataPlayer) return null;

            var fileName = GetFileNameBase(login) + numberSave.ToString().NormalizePath();

            var info = new FileInfo(fileName);
            if (!info.Exists || info.Length < 10) return null;

            //читаем содержимое
            bool readAsXml;
            using (var file = File.OpenRead(fileName))
            {
                var buff = new byte[10];
                file.Read(buff, 0, 10);
                readAsXml = Encoding.ASCII.GetString(buff, 0, 10).Contains("<?xml");
            }
            //считываем текст как xml сейва или как сжатого zip'а
            var saveFileData = File.ReadAllBytes(fileName);
            if (readAsXml)
            {
                return saveFileData;
            }
            else
            {
                return GZip.UnzipByteByte(saveFileData);
            }
        }

        /// <summary>
        /// Сохраняем игровые данные игрока. Вся существующая истоия файлов переименовывается на номера +1. С номером больше CountSaveDataPlayer удаляется
        /// </summary>
        /// <param name="login">Логин пользователя, но основании него получается имя файла</param>
        /// <param name="data">Содержимое сейва игры</param>
        /// <param name="single">Если задано, то удаляется вся история оставляя только данный сейв и последний из истории с расширением bak (для возможности ручного восстановления администратором)</param>
        public void SavePlayerData(string login, byte[] data, bool single)
        {
            if (data == null || data.Length < 10) return;

            var fileNameBase = GetFileNameBase(login);
            var pFiles = GetListPlayerFiles(login);
            if (single)
            {
                if (pFiles.Count > 0)
                {
                    var bakupFileName = Path.Combine(Path.GetDirectoryName(pFiles[0]), Path.GetFileNameWithoutExtension(pFiles[0])) + ".bak";
                    if (File.Exists(bakupFileName)) DeleteFileAndBackup(bakupFileName);
                    File.Move(pFiles[0], bakupFileName);
                }
                for (int i = 1; i < pFiles.Count; i++) DeleteFileAndBackup(pFiles[i]);
            }
            else
            {
                //Делаем так, чтобы в pFiles[pFiles.Count - 1] было имя файла которого нет
                if (pFiles.Count == CountSaveDataPlayer)
                {
                    DeleteFileAndBackup(pFiles[pFiles.Count - 1]);
                }
                else
                {
                    pFiles.Add(fileNameBase + (pFiles.Count + 1).ToString());
                }
                for (int i = pFiles.Count - 2; i >= 0 ; i--)
                {
                    File.Move(pFiles[i], pFiles[i + 1]);
                }
            }

            var fileName = fileNameBase + "1";

            byte[] dataToSave;
            dataToSave = GZip.ZipByteByte(data);

            File.WriteAllBytes(fileName, dataToSave);
            Loger.Log("Server User " + Path.GetFileNameWithoutExtension(fileName) + " saved.");
        }
        /// <summary>
        /// Удаляем файл, но перед этим сохраняем его копию так, чтобы былка копия более 12 часов назад
        /// </summary>
        /// <param name="fileName"></param>
        private void DeleteFileAndBackup(string fileName)
        {
            var fi = new FileInfo(fileName);
            if (!fi.Exists) return;
            if ((DateTime.UtcNow - fi.LastWriteTimeUtc).TotalHours < 12)
            {
                fi.Delete();
                return;
            }
            var bakupFileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName));
            var d1 = new FileInfo(bakupFileName + ".day1");
            var d2 = new FileInfo(bakupFileName + ".day2");
            if (!d1.Exists || (fi.LastWriteTimeUtc - d1.LastWriteTimeUtc).TotalHours > 12)
            {
                //нужно записать файл fileName в fileName.day1
                if (d1.Exists)
                {
                    //разбираемся с существующим fileName.day1
                    if (!d2.Exists || (d1.LastWriteTimeUtc - d2.LastWriteTimeUtc).TotalHours > 12)
                    {
                        //нужно записать файл fileName.day1 в fileName.day2
                        if (d2.Exists)
                        {
                            //не разбираемся с существующим fileName.day2
                            d2.Delete();
                        }
                        File.Move(d1.FullName, d2.FullName);
                    }
                    else
                    {
                        d1.Delete();
                    }
                }
                File.Move(fi.FullName, d1.FullName);
            }
            else
            {
                fi.Delete();
            }
        }

        public void DeletePlayerData(string login)
        {
            var pFiles = GetListPlayerFiles(login);

            if (pFiles.Count > 0)
            {
                var bakupFileName = Path.Combine(Path.GetDirectoryName(pFiles[0]), Path.GetFileNameWithoutExtension(pFiles[0])) + ".bak";
                if (File.Exists(bakupFileName)) File.Delete(bakupFileName);
                File.Move(pFiles[0], bakupFileName);
            }
            for (int i = 1; i < pFiles.Count; i++) File.Delete(pFiles[i]);
        }

        /// <summary>
        /// Возвращает список доступной истории сохранений игрока. В каждой строке описания сохранения в виде даты сохранения по времени сервера.
        /// </summary>
        /// <param name="login">Описания сохранения игрока, так, что индекс 0 соответствует номеру 1, индекс 1 - номер 2 и т.д.</param>
        /// <returns></returns>
        public List<string> GetListPlayerDatas(string login)
        {
            return GetListPlayerFiles(login)
                .Select(fn => new FileInfo(fn).LastWriteTime.ToString("yyyy-MM-dd"))
                .ToList();
        }

        private List<string> GetListPlayerFiles(string login)
        {
            var result = new List<string>();
            var fileNameBase = GetFileNameBase(login);

            for (int num = 1; num <= CountSaveDataPlayer; num++)
            {
                if (!File.Exists(fileNameBase + num.ToString())) break;
                result.Add(fileNameBase + num.ToString());
            }
            if (result.Count == 0 && File.Exists(fileNameBase))
            {
                File.Move(fileNameBase, fileNameBase + "1");
                result.Add(fileNameBase + "1");
            }
            return result;
        }
    }
}
