using System;
using System.IO;
using UnityEngine;

namespace Relicfall.Save
{
    public static class JsonSaveManager
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "relicfall-save.json");
        public static bool HasSave => File.Exists(SavePath) || File.Exists(SavePath + ".bak");

        public static bool Write(GameSaveData data)
            => WriteToPath(data, SavePath);

        public static bool WriteToPath(GameSaveData data, string path)
        {
            if (data == null) return false;
            try
            {
                string temporary = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                // 同目录原子替换，旧文件保留为备份；首次保存只需重命名。
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("保存游戏失败：" + exception.Message);
                return false;
            }
        }

        public static GameSaveData Read()
            => ReadFromPath(SavePath);

        public static GameSaveData ReadFromPath(string path)
        {
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    string json = File.ReadAllText(candidate);
                    if (!json.Contains("\"version\"")) continue;
                    GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                    if (data != null && data.version >= 1 && data.version <= GameSaveData.CurrentVersion &&
                        data.health > 0 && data.items != null && IsFinite(data.position.x) &&
                        IsFinite(data.position.y) && IsFinite(data.position.z)) return data;
                }
                catch (Exception exception) { Debug.LogWarning("读取存档失败，尝试备份：" + exception.Message); }
            }
            return null;
        }
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
