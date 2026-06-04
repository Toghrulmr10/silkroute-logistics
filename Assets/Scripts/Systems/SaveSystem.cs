using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Save(GameSaveData data)
    {
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        string tmp  = SavePath + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(SavePath)) File.Delete(SavePath);
        File.Move(tmp, SavePath);
        Debug.Log($"[Save] Saxlanıldı → {SavePath}");
    }

    public GameSaveData Load()
    {
        if (!File.Exists(SavePath)) return null;
        try
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        catch
        {
            Debug.LogWarning("[Save] Fayl oxunmadı, silinir.");
            DeleteSave();
            return null;
        }
    }

    public bool HasSave() => File.Exists(SavePath);

    public void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }
}
