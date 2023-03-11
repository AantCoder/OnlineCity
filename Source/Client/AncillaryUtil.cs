using ClientAncillary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorldOnlineCity
{
    public class AncillaryUtil
    {
        private static string ConsloeFileName;
        public AncillaryUtil()
        { 
        }

        private string GetConsloeFileName()
        {
            if (ConsloeFileName == null)
            {
                ConsloeFileName = "";
                var fs = Directory.GetFiles(GenFilePaths.ModsFolderPath, "ClientAncillary.exe", SearchOption.AllDirectories);
                if (fs.Length > 0) ConsloeFileName = fs[0];
            }

            if (string.IsNullOrEmpty(ConsloeFileName)) return null;

            return ConsloeFileName;
        }

        private byte[] Request(string argument = "")
        {
            if (string.IsNullOrEmpty(GetConsloeFileName())) return null;

            //
            int code = new Random().Next(ushort.MaxValue - 1) + 1;
            Process proc;
            var com = new CommunicationConsole();
            var data = com.ReceiveData(code, () =>
            {
                var pi = new ProcessStartInfo(ConsloeFileName, code.ToString() + " " + argument);
                pi.CreateNoWindow = true;
                pi.UseShellExecute = false;
                proc = Process.Start(pi);
                proc.WaitForExit();
            });

            return data;
        }

        public byte[] GetClipboardImageData()
        {
            return Request("0");
        }



    }
}
