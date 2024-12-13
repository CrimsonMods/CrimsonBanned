using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CrimsonBanned.Commands;
using CrimsonBanned.Structs;
using static CrimsonBanned.Services.PlayerService;

namespace CrimsonBanned.Utilities;

public static class SQLUtility
{
    private static bool IsSyncing = false;

    public static bool TrySQL()
    {
        return Database.SQL != null && Settings.UseSQL.Value && SQLlink.Connect();
    }

    public static async void SyncDB()
    {
        if (!TrySQL()) return;
        if (!SQLlink.Connect()) return;

        if (IsSyncing) return;
        IsSyncing = true;

        foreach (var delete in Database.Deletes)
        {
            SQLlink.DeleteBan(delete.ID, delete.TableName);
        }

        Database.Deletes.Clear();
        if (File.Exists(Database.DeleteFile)) File.Delete(Database.DeleteFile);

        await SyncTable(Database.ChatBans, "ChatBans");
        await SyncTable(Database.VoiceBans, "VoiceBans");
        await SyncTable(Database.Banned, "ServerBans");

        Database.SaveDatabases();

        IsSyncing = false;
    }

    private static async Task SyncTable(List<Ban> list, string tableName)
    {
        if (!ValidateSync(list, tableName)) return;

        await SQLConflict.ResolveOfflines(list);

        var sortedTable = GetSortedTableFromDatabase(tableName);
        HandleRemovedBans(list, sortedTable);
        RemoveExpiredBans(sortedTable, tableName);
        SyncMissingBans(list, sortedTable);
    }

    private static DataTable GetSortedTableFromDatabase(string tableName)
    {
        DataTable table = Database.SQL.Select(tableName);
        DataView view = table.DefaultView;
        view.Sort = "Id ASC";
        return view.ToTable();
    }

    private static void HandleRemovedBans(List<Ban> list, DataTable sortedTable)
    {
        var bansByID = new HashSet<int>(
            sortedTable.Rows.Cast<DataRow>()
            .Select(row => Convert.ToInt32(row["Id"]))
        );

        list.RemoveAll(ban => !bansByID.Contains(ban.DatabaseId) && ban.DatabaseId != -1);

        if (list == Database.Banned)
        {
            var removedBans = list.Where(ban => !bansByID.Contains(ban.DatabaseId) && ban.DatabaseId != -1).ToList();
            Database.BanListFix(removedBans);
        }
    }

    private static void RemoveExpiredBans(DataTable sortedTable, string tableName)
    {
        foreach (DataRow row in sortedTable.Rows)
        {
            DateTime time = Convert.ToDateTime(row["TimeUntil"]);
            if (time < DateTime.UtcNow && !TimeUtility.IsPermanent(time))
            {
                SQLlink.DeleteBan(Convert.ToInt32(row["Id"]), tableName);
            }
        }
    }

    private static void SyncMissingBans(List<Ban> list, DataTable sortedTable)
    {
        foreach (DataRow row in sortedTable.Rows)
        {
            var ban = CreateBanFromRow(row);

            // Skip if we already have this exact DatabaseId
            if (list.Any(x => x.DatabaseId == ban.DatabaseId))
                continue;

            if (IsPlayerAlreadyBanned(ban, list, out Ban existing))
            {
                if (TimeUtility.IsPermanent(ban.TimeUntil))
                {
                    list.Remove(existing);
                    AddBanToList(list, ban);
                }
                else if (TimeUtility.IsPermanent(existing.TimeUntil))
                {
                    SQLlink.DeleteBan(ban, list);
                    AddBanToList(list, existing);
                    return;
                }
                else if (ban.TimeUntil > existing.TimeUntil)
                {
                    list.Remove(existing);
                    AddBanToList(list, ban);
                }
                else
                {
                    SQLlink.DeleteBan(ban, list);
                    AddBanToList(list, existing);
                }
            }
            else
            {
                AddBanToList(list, ban);
            }
        }
    }

    private static bool IsPlayerAlreadyBanned(Ban ban, List<Ban> list, out Ban existingBan)
    {
        existingBan = list.Find(x =>
            x.PlayerID == ban.PlayerID ||
            (x.DatabaseId != -1 && x.DatabaseId == ban.DatabaseId)
        );

        return existingBan != null;
    }

    private static Ban CreateBanFromRow(DataRow row)
    {
        Ban ban = new Ban(
            row["PlayerName"].ToString(),
            Convert.ToUInt64(row["PlayerID"]),
            Convert.ToDateTime(row["TimeUntil"]),
            row["Reason"].ToString(),
            row["IssuedBy"].ToString()
        );

        ban.Issued = Convert.ToDateTime(row["Issued"]);
        ban.DatabaseId = Convert.ToInt32(row["Id"]);
        return ban;
    }

    private static void AddBanToList(List<Ban> list, Ban ban)
    {
        list.Add(ban);

        if (list == Database.Banned)
        {
            HandleServerBan(ban);
        }
    }

    private static void HandleServerBan(Ban ban)
    {
        if (Extensions.TryGetPlayerInfo(ban.PlayerID, out PlayerInfo player))
        {
            if (player.User.IsConnected)
            {
                Core.StartCoroutine(BanCommands.DelayKick(player));
            }
        }

        if (File.Exists(Settings.BanFilePath.Value))
        {
            UpdateBanFile(ban);
        }
    }

    private static void UpdateBanFile(Ban ban)
    {
        var existingBans = File.ReadAllLines(Settings.BanFilePath.Value);
        if (!existingBans.Contains(ban.PlayerID.ToString()))
        {
            File.AppendAllText(Settings.BanFilePath.Value, ban.PlayerID + Environment.NewLine);
        }
    }

    private static bool ValidateSync(List<Ban> list, string tableName)
    {
        if (Database.SQL == null) return false;
        if (!SQLlink.Connect()) return false;
        if (list == null) return false;
        if (string.IsNullOrEmpty(tableName)) return false;

        return true;
    }
}