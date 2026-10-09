using System;
using System.IO;
using UnityEngine;

public static class SaveGame
{
    const int Version = 2;

    public static bool Exists()
    {
        return File.Exists(Path);
    }

    public static bool TryLoad(out SaveData data)
    {
        data = null;
        if (!File.Exists(Path))
            return false;

        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));
        }
        catch (Exception)
        {
            data = null;
        }

        if (data == null || data.version < 1 || data.version > Version)
        {
            Delete();
            data = null;
            return false;
        }

        if (data.fruits == null)
            data.fruits = new FruitSave[0];

        return true;
    }

    public static void Write(SaveData data)
    {
        if (data == null)
            return;

        data.version = Version;
        File.WriteAllText(Path, JsonUtility.ToJson(data));
    }

    public static void Delete()
    {
        if (File.Exists(Path))
            File.Delete(Path);
    }

    static string Path
    {
        get { return System.IO.Path.Combine(Application.persistentDataPath, "fruitmerge_save.json"); }
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
    public int rerollLeft;
    public int shakeLeft;
    public FruitSave[] fruits;
}

[Serializable]
public class FruitSave
{
    public int type;
    public float x;
    public float y;
    public float z;
    public float angle;
    public bool flipX;
}
