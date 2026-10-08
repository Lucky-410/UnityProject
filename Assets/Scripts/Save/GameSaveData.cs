using System;
using UnityEngine;

namespace Relicfall.Save
{
    [Serializable]
    public sealed class SavedItem
    {
        public string id;
        public int count;
    }

    [Serializable]
    public sealed class SavedWorldItem
    {
        public string instanceId, itemId;
        public int count;
        public Vector3 position;
    }
    [Serializable]
    public sealed class SavedEnemy
    {
        public string id;
        public int health;
        public Vector3 position;
    }
    [Serializable]
    public sealed class SavedBoss
    {
        public int health;
        public bool started, phaseTwo, defeated;
        public Vector3 position;
    }
    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 2;
        public int version = CurrentVersion;
        public int levelRevision;
        public Vector3 position;
        public int health;
        public string weapon1;
        public string weapon2;
        public int activeWeapon;
        public string potion;
        public SavedItem[] items;
        public string[] collectedWorldItems;
        public SavedWorldItem[] worldItems;
        public SavedEnemy[] enemies;
        public SavedBoss boss;
    }
}
