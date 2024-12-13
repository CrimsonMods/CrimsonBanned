using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonBanned.Structs;
using UnityEngine;

namespace CrimsonBanned.Utilities;

public static class SQLConflict
{
    public static async Task ResolveOfflines(List<Ban> list)
    {
        List<Ban> negatives = list.FindAll(x => x.DatabaseId == -1);

        foreach (Ban ban in negatives)
        {
            int i = SQLlink.AddBan(ban, list);
            if (i >= 0)
            {
                ban.DatabaseId = i;
            }
            else
                await ResolveConflict(ban, list);
        }
    }

    public static async Task<int> ResolveConflict(Ban ban, List<Ban> list)
    {
        Ban banFromDB = SQLlink.GetBan(ban.PlayerID, list);

        // No conflict - why we here?
        if (banFromDB == null)
        {
            Plugin.LogInstance.LogError($"Could not find ban for {ban.PlayerID} in the database.");
            return -1000;
        }

        // Identical DatabaseIds - delete local
        if (ban.DatabaseId != -1 && banFromDB.DatabaseId != -1 && ban.DatabaseId == banFromDB.DatabaseId)
        {
            list.Remove(ban);
            return 4;
        }

        // Same duration - keep DB, delete local
        if (ban.TimeUntil == banFromDB.TimeUntil)
        {
            list.Remove(ban);
            Database.AddBan(banFromDB, list);

            if (TimeUtility.IsPermanent(banFromDB.TimeUntil) && TimeUtility.IsPermanent(ban.TimeUntil)) return 0;
            else return 4;
        }

        // DB perma - keep DB, delete local
        if(TimeUtility.IsPermanent(banFromDB.TimeUntil))
        {
            list.Remove(ban);
            Database.AddBan(banFromDB, list);
            return 0;
        }

        // Expired DB ban - replace with local
        if (IsExpiredBan(banFromDB))
        {
            return await ReplaceExistingBan(banFromDB, ban, list);
        }

        // DB temp vs Local perma = replace with local
        if (!TimeUtility.IsPermanent(banFromDB.TimeUntil) && TimeUtility.IsPermanent(ban.TimeUntil))
        {
            return await ReplaceExistingBan(banFromDB, ban, list);
        }

        // Compare durations - longer ban wins
        if(ban.TimeUntil > banFromDB.TimeUntil)
        {
            return await ReplaceExistingBan(banFromDB, ban, list);
        }
        else
        {
            list.Remove(ban);
            Database.AddBan(banFromDB, list);
            return 1;
        }
    }

    private static bool IsExpiredBan(Ban ban)
    {
        return ban.TimeUntil < DateTime.UtcNow && !TimeUtility.IsPermanent(ban.TimeUntil);
    }

    private static async Task<int> ReplaceExistingBan(Ban oldBan, Ban newBan, List<Ban> list)
    {
        SQLlink.DeleteBan(oldBan, list);
        int newId = SQLlink.AddBan(newBan, list);
        newBan.DatabaseId = newId;
        Database.AddBan(newBan, list);
        return 2;
    }
}