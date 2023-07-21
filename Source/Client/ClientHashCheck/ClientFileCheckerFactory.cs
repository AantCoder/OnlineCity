using OCUnion.Transfer.Model;
using RimWorldOnlineCity.UI;
using System.IO;
using Verse;

namespace RimWorldOnlineCity.ClientHashCheck
{
    internal class ClientFileCheckerFactory
    {
        public ClientFileChecker GetFileChecker(FolderType folderType)
        {
            var fc = new FolderCheck();
            fc.FolderType = folderType;
            switch (folderType)
            {
                case FolderType.ModsConfigPath:
                    {
                        return new ClientFileChecker(fc, GenFilePaths.ConfigFolderPath)
                        {
                            OnChangeFolderAction = updateStatusWindow,
                        };
                    }
                case FolderType.ModsFolder:
                    return new ClientFileChecker(fc, GenFilePaths.ModsFolderPath)
                    {
                        OnChangeFolderAction = updateStatusWindow,
                    };
                case FolderType.GamePath:
                    return new ClientFileChecker(fc, Path.GetDirectoryName(GenFilePaths.ModsFolderPath))
                    {
                        OnChangeFolderAction = updateStatusWindow,
                    };
            }

            return null;
        }

        private static void updateStatusWindow(string folderName, int folderIndex)
        {
            UpdateModsWindow.HashStatus = folderName;
        }
    }
}
