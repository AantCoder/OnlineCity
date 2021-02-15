using OCUnion.Transfer.Model;
using RimWorldOnlineCity.UI;
using Verse;

namespace RimWorldOnlineCity.ClientHashCheck
{
    internal class ClientFileCheckerFactory
    {
        public IClientFileChecker GetFileChecker(FolderType folderType)
        {
            switch (folderType)
            {
                case FolderType.ModsConfigPath:
                    {
                        return new ClientFileChecker(folderType, GenFilePaths.ConfigFolderPath)
                        {
                            OnChangeFolderAction = updateStatusWindow,
                        };
                    }
                case FolderType.ModsFolder:
                    return new ClientFileChecker(folderType, GenFilePaths.ModsFolderPath)
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
