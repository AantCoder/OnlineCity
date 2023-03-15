using System;

namespace OCUnion
{
    [Serializable]
    [Flags]
    public enum Grants
    {
        NoPermissions = 0,
        UsualUser = 1,
        SuperAdmin = 2,
        Moderator = 4,  // for example: Can Kick users
        GameMaster = 8, // Can Create GameEvents
        Developer = 16,  // Can use some inside Features for testing
        DiscordBot = 32,// Mark commands that approved run from Discrord bot
        // FractionLider = 16, :-)
        // InvisiableOrden =32 :-)
        // Cheater = -1 For punishment Cheaters  :-)  
        // CanTrade 
        // CanAttak e t.c
        //         
    }
}
