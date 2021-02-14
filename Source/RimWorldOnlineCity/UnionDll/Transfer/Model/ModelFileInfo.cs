using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OCUnion.Transfer.Model
{
    /// <summary>
    /// Содержит информацию по модам у меня в папке Mods ~15к файлов, даже если взять на каждый по 100 байт,
    /// </summary>
    [Serializable]
    public class ModelFileInfo
    {
        /// <summary>
        /// Полный путь от папки Mods вида OnlineCity\Defs\WorldObjectDefs\WorldObjects.xml
        /// </summary>
        public string FileName { get; set; }
        public byte[] Hash { get; set; }
        public long Size { get; set; }

        public override int GetHashCode()
        {
            if (Hash == null || Hash.Length < 8)
            {
                return 0;
            }

            // переводит первые 4 байта в int
            return BitConverter.ToInt32(Hash, 0);
        }

        public override bool Equals(object obj)
        {
            var modFilesInfo = obj as ModelFileInfo;
            if (modFilesInfo is null)
            {
                return false;
            }

            if (this.Hash is null)
            {
                if (modFilesInfo.Hash is null)
                {
                    return true;
                }

                return false;
            }

            return UnsafeByteArraysEquale(this.Hash, modFilesInfo.Hash) && this.FileName.Equals(modFilesInfo.FileName);
        }


        // Copyright (c) 2008-2013 Hafthor Stefansson
        // Distributed under the MIT/X11 software license
        // Ref: http://www.opensource.org/licenses/mit-license.php.
        // https://habr.com/ru/post/214841/
        public static unsafe bool UnsafeByteArraysEquale(byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null || a1.Length != a2.Length)
                return false;
            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                int l = a1.Length;
                for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                    if (*((long*)x1) != *((long*)x2))
                        return false;
                if ((l & 4) != 0)
                {
                    if (*((int*)x1) != *((int*)x2)) return false;
                    x1 += 4;
                    x2 += 4;
                }
                if ((l & 2) != 0)
                {
                    if (*((short*)x1) != *((short*)x2)) return false;
                    x1 += 2;
                    x2 += 2;
                }
                if ((l & 1) != 0)
                    if (*((byte*)x1) != *((byte*)x2))
                        return false;
                return true;
            }
        }
    }
}
