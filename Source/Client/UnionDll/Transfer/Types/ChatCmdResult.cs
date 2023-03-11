using System.ComponentModel;

namespace OCUnion.Transfer.Types
{
    public enum ChatCmdResult : byte
    {
        Ok,
        // Command not found
        // Access deny
        AccessDeny,
        // Can not access %1 player ' what is it ?
        CantAccess,
        CommandNotFound,
        IncorrectSubCmd,
        // Operation only for the shared channel
        OnlyForPublicChannel,
        // only when try run chat Cmd from the Discord
        OwnLoginNotFound,
        // The player is already here
        PlayerHere,
        //Player name is empty
        PlayerNameEmpty,
        RoleNotFound,
        // Set Name Channel
        SetNameChannel,
        //User %1 does not have permission
        UserDoesNotHavePermission,
        UserNotFound,
    }
}