using OCUnion;
using OCUnion.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorldOnlineCity
{
    internal static class CacheResource
    {
        private static string CachePath;
        private static ConcurrentDictionary<string, byte[]> Cache = new ConcurrentDictionary<string, byte[]>();
        private static ConcurrentDictionary<string, string> Hash = new ConcurrentDictionary<string, string>();

        public static void Init()
        {
            CachePath = Path.Combine(Path.GetDirectoryName(GenFilePaths.ConfigFolderPath), "OnlineCityCache").NormalizePath();
            if (!Directory.Exists(CachePath))
            {
                Directory.CreateDirectory(CachePath);
            }

            //удаляем старше 7 дней
            var now = DateTime.UtcNow;
            foreach (var old in Directory.GetFiles(CachePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                var info = new FileInfo(old);
                //Loger.Log("Client Delete File " + old + " " + (now - info.CreationTimeUtc).TotalDays);
                if (info.Exists && (now - info.CreationTimeUtc).TotalDays > 7d) info.Delete();
            }
        }

        private static string GetFileName(string name) => Path.Combine(CachePath, name.NormalizeUniqueFileNameChars());

        public static byte[] GetData(string name)
        {
            if (Cache.ContainsKey(name)) return Cache[name];
            try
            {
                var fileName = GetFileName(name);
                if (!File.Exists(fileName)) return null;
                var data = File.ReadAllBytes(fileName);
                Cache[name] = data;
                Hash.TryRemove(name, out _);
                return data;
            }
            catch
            {
                return null;
            }
        }
        public static void SetData(string name, byte[] content)
        {
            try
            {
                var fileName = GetFileName(name);
                if (File.Exists(fileName)) File.Delete(fileName);
                File.WriteAllBytes(fileName, content);
            }
            catch
            {
            }
            Cache[name] = content;
            Hash.TryRemove(name, out _);
        }
        public static string GetHash(string name)
        {
            if (Hash.ContainsKey(name)) return Hash[name];
            var data = GetData(name);
            if (data == null) return null;
            var hash = FileChecker.GetCheckSum(data);
            Hash[name] = hash;
            return hash;
        }
    }
}
