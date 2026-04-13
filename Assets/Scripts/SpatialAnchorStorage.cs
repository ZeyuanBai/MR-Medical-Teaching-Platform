using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public static class SpatialAnchorStorage
{
    public const string NumUuidsPlayerPref = "numUuids";

    public static int Count => PlayerPrefs.GetInt(NumUuidsPlayerPref, 0);




    public static HashSet<Guid> Uuids
    {
        get => Enumerable
            .Range(0, Count)
            .Select(GetUuidKey)
            .Select(PlayerPrefs.GetString)
            .Select(str => Guid.TryParse(str, out var uuid) ? uuid : Guid.Empty)
            .Where(uuid => uuid != Guid.Empty)
            .ToHashSet();
        set
        {
            // 删除所有旧键（基于原始数量）
            int originalCount = PlayerPrefs.GetInt(NumUuidsPlayerPref, 0);
            for (int i = 0; i < originalCount; i++)
            {
                PlayerPrefs.DeleteKey(GetUuidKey(i));
            }

            // 写入新数据
            PlayerPrefs.SetInt(NumUuidsPlayerPref, value.Count);
            int index = 0;
            foreach (var uuid in value)
            {
                PlayerPrefs.SetString(GetUuidKey(index++), uuid.ToString());
            }

            // 强制保存并更新内存中的UUID集合
            PlayerPrefs.Save();
        }
        //set
        //{
        //    // Delete everything beyond the new count
        //    foreach (var key in Enumerable.Range(0, Count).Select(GetUuidKey))
        //    {
        //        PlayerPrefs.DeleteKey(key);
        //    }

        //    // Set the new count
        //    PlayerPrefs.SetInt(NumUuidsPlayerPref, value.Count);

        //    // Update all the uuids
        //    var index = 0;
        //    foreach (var uuid in value)
        //    {
        //        PlayerPrefs.SetString(GetUuidKey(index++), uuid.ToString());
        //    }
        //}
    }

    public static void Add(Guid uuid)
    {
        var uuids = Uuids;
        if (uuids.Add(uuid))
        {
            Uuids = uuids;
        }
    }

    //public static void Remove(Guid uuid)
    //{
    //    var uuids = Uuids;
    //    if (uuids.Remove(uuid))
    //    {
    //        Uuids = uuids;
    //    }
    //}

    // SpatialAnchorStorage.cs
    public static void Remove(Guid uuid)
    {
        // 1. 从当前存储中移除UUID
        var uuids = Uuids;
        if (!uuids.Remove(uuid)) return;

        // 2. 直接更新PlayerPrefs，避免覆盖错误
        int originalCount = PlayerPrefs.GetInt(NumUuidsPlayerPref, 0);
        int newIndex = 0;

        // 重新写入剩余UUID
        PlayerPrefs.DeleteKey(NumUuidsPlayerPref);
        foreach (var remainingUuid in uuids)
        {
            PlayerPrefs.SetString(GetUuidKey(newIndex), remainingUuid.ToString());
            newIndex++;
        }

        // 3. 更新总数并保存
        PlayerPrefs.SetInt(NumUuidsPlayerPref, newIndex);
        PlayerPrefs.Save();

        Debug.Log($"Successfully removed UUID: {uuid}");
    }

    public static void ClearAllUuids()
    {
        // 1. 获取当前存储的UUID数量
        int originalCount = PlayerPrefs.GetInt(NumUuidsPlayerPref, 0);
        Debug.Log($"Clearing {originalCount} UUIDs...");

        // 2. 删除所有uuidX键
        for (int i = 0; i < originalCount; i++)
        {
            string key = GetUuidKey(i);
            PlayerPrefs.DeleteKey(key);
            Debug.Log($"Deleted key: {key}");
        }

        // 3. 重置UUID计数器
        PlayerPrefs.DeleteKey(NumUuidsPlayerPref);
        Debug.Log("Reset UUID counter to 0");

        // 4. 强制保存更改
        PlayerPrefs.Save();
        Debug.Log("All UUIDs cleared successfully");
    }

    static string GetUuidKey(int index) => $"uuid{index}";
}
