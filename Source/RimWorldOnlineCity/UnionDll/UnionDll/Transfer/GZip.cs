using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.IO.Compression;

namespace Util
{
    public static partial class GZip
    {

        [ThreadStatic]
        private static BinaryFormatter formatter = null;

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static string Zip(string str)
        {
            return Convert.ToBase64String(ZipByte(str));
        }

        public static byte[] ZipByteByte(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
                return ZipStreamByte(msi);
        }
        public static byte[] ZipByte(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
                return ZipStreamByte(msi);
        }

        public static byte[] Serialize(object obj)
        {
            using (var msi = new MemoryStream())
            {
                if (formatter == null) formatter = new BinaryFormatter();
                formatter.Serialize(msi, obj);
                msi.Seek(0, SeekOrigin.Begin);
                return msi.ToArray();
            }
        }

        public static byte[] ZipObjByte(object obj)
        {
            using (var msi = new MemoryStream())
            {
                if (formatter == null) formatter = new BinaryFormatter();
                formatter.Serialize(msi, obj);
                msi.Seek(0, SeekOrigin.Begin);
                return ZipStreamByte(msi);
            }
        }

        public static byte[] ZipStreamByte(Stream msi)
        {
            using (var mso = CreateToStream(msi, "data"))
            {
                return mso.ToArray();
            }
        }

        public static string Unzip(string str)
        {
            return UnzipByte(Convert.FromBase64String(str));
        }

        public static byte[] UnzipByteByte(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            {
                byte[] bs = UnzipStreamByte(msi);

                return bs;
            }
        }
        public static string UnzipByte(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            {
                byte[] bs = UnzipStreamByte(msi);

                return Encoding.UTF8.GetString(bs);
            }
        }

        public static object Deserialize(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            {
                if (formatter == null) formatter = new BinaryFormatter();
                return formatter.Deserialize(msi);
            }
        }

        public static object UnzipObjByte(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = UnpackFromStream(msi))
            {
                mso.Seek(0, SeekOrigin.Begin);
                if (formatter == null) formatter = new BinaryFormatter();
                return formatter.Deserialize(mso);
            }
        }

        public static byte[] UnzipStreamByte(Stream msi)
        {
            using (var mso = UnpackFromStream(msi))
            {
                return mso.ToArray();
            }
        }
    }
}
