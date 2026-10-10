using System;
using System.IO;
using UnityEngine;

public static class SaveGame
{
    const int Version = 4;

    public static bool Exists()
    {
        return File.Exists(Path);
    }

    public static bool TryLoad(out SaveData data)
    {
        if (!TryRead(Path, out data))
        {
            if (File.Exists(Path))
                Delete();
            data = null;
            return false;
        }

        return true;
    }

    public static void Write(SaveData data)
    {
        if (data == null)
            return;

        data.version = Version;
        File.WriteAllText(Path, JsonUtility.ToJson(data));
    }

    public static void WritePrevious(SaveData data)
    {
        if (data == null)
            return;

        data.version = Version;
        File.WriteAllText(PreviousPath, JsonUtility.ToJson(data));
    }

    public static bool TryLoadPrevious(out SaveData data)
    {
        return TryRead(PreviousPath, out data);
    }

    public static void ClearPrevious()
    {
        if (File.Exists(PreviousPath))
            File.Delete(PreviousPath);
    }

    public static void Delete()
    {
        if (File.Exists(Path))
            File.Delete(Path);
        if (File.Exists(PreviousPath))
            File.Delete(PreviousPath);
    }

    static bool TryRead(string path, out SaveData data)
    {
        data = null;
        if (!File.Exists(path))
            return false;

        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception)
        {
            data = null;
        }

        if (data == null || data.version < 1 || data.version > Version)
        {
            data = null;
            return false;
        }

        if (data.fruits == null)
            data.fruits = new FruitSave[0];

        return true;
    }

    static string Path
    {
        get { return System.IO.Path.Combine(Application.persistentDataPath, "fruitmerge_save.json"); }
    }

    static string PreviousPath
    {
        get { return System.IO.Path.Combine(Application.persistentDataPath, "fruitmerge_save_prev.json"); }
    }
}

[Serializable]
public class SaveData
{
    public int version;
    public int mode;
    public int points;
    public int combo;
    public int highestUnlocked;
    public int nextFruit = -1;
    public bool nextJoker;
    public int nextKind;
    public int queuedFruit = -1;
    public bool queuedJoker;
    public int queuedKind;
    public int rerollLeft;
    public int shakeLeft;
    public int undoLeft;
    public FruitSave[] fruits;
}

[Serializable]
public class FruitSave
{
    public int type;
    public bool joker;
    public int bonus;
    public int fuse;
    public float x;
    public float y;
    public float z;
    public float angle;
    public bool flipX;
}
