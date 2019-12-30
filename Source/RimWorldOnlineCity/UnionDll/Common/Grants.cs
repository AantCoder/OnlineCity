using System;

namespace OCUnion
{
    [Serializable]
    [Flags]
    public enum Grants
    {
        UsualUser = 0,
        SuperAdmin = 1, 
        Moderator = 2,  // for example: Can Kick users
        GameMaster = 4, // Can Create GameEvents
        Developer = 8,  // Can use some inside Features for testing
        // FractionLider = 16, :-)
        // InvisiableOrden =32 :-)
        // Cheater = -1 For punishment Cheaters  :-)  
        // CanTrade 
        // CanAttak e t.c
        // 
    }
}
