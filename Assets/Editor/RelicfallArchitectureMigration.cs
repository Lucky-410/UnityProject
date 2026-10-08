using System;
using System.Linq;
using System.IO;
using Relicfall.Player;
using Relicfall.Items;
using Relicfall.Combat;
using Relicfall.Save;
using Relicfall.Boss;
using Relicfall.Enemies;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

namespace Relicfall.Editor
{
    public static class RelicfallArchitectureMigration
    {
        [MenuItem("Tools/Relicfall/配置角色状态与存档标识")]
        public static void Configure()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式。");
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Data" }))
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                item.ConfigureIdentity(guid, item.LegacyName);
                EditorUtility.SetDirty(item);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:WeaponData", new[] { "Assets/Data" }))
            {
                WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid));
                weapon.ConfigureIdentity(guid, weapon.LegacyName);
                EditorUtility.SetDirty(weapon);
            }
            GameObject prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
            try
            {
                if (!prefab.TryGetComponent<PlayerActionController>(out _)) prefab.AddComponent<PlayerActionController>();
                if (!prefab.TryGetComponent<PlayerPickupDetector>(out _)) prefab.AddComponent<PlayerPickupDetector>();
                if (!prefab.TryGetComponent<PlayerContext>(out _)) prefab.AddComponent<PlayerContext>();
                PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/Prefabs/Player.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            foreach (string path in new[] { "Assets/Prefabs/Enemies/DemonSwordsman.prefab", "Assets/Prefabs/Enemies/DemonGunner.prefab" })
            {
                GameObject enemy = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (!enemy.TryGetComponent<EnemyPerception>(out _)) enemy.AddComponent<EnemyPerception>();
                    PrefabUtility.SaveAsPrefabAsset(enemy, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(enemy); }
            }
            Directory.CreateDirectory("Assets/Data/Boss");
            AssetDatabase.Refresh();
            const string attackPath = "Assets/Data/Boss/DragonKnightAttacks.asset";
            BossAttackSettings attacks = AssetDatabase.LoadAssetAtPath<BossAttackSettings>(attackPath);
            if (attacks == null) { attacks = ScriptableObject.CreateInstance<BossAttackSettings>(); AssetDatabase.CreateAsset(attacks, attackPath); }
            GameObject bossPrefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Enemies/DragonKnightBoss.prefab");
            try
            {
                bossPrefab.GetComponent<DragonKnightBoss>().ConfigureAttacks(attacks);
                PrefabUtility.SaveAsPrefabAsset(bossPrefab, "Assets/Prefabs/Enemies/DragonKnightBoss.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(bossPrefab); }
            GroupAnimations();
            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/GameScene.unity");
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                foreach (PlayerMotor motor in roots.SelectMany(go => go.GetComponentsInChildren<PlayerMotor>(true)))
                {
                    if (!motor.TryGetComponent<PlayerActionController>(out _)) Undo.AddComponent<PlayerActionController>(motor.gameObject);
                    if (!motor.TryGetComponent<PlayerPickupDetector>(out _)) Undo.AddComponent<PlayerPickupDetector>(motor.gameObject);
                    if (!motor.TryGetComponent<PlayerContext>(out _)) Undo.AddComponent<PlayerContext>(motor.gameObject);
                }
                foreach (WorldItem world in roots.SelectMany(go => go.GetComponentsInChildren<WorldItem>(true)))
                {
                    var serialized = new SerializedObject(world);
                    if (!string.IsNullOrEmpty(serialized.FindProperty("saveId").stringValue)) continue;
                    Vector3 point = world.transform.position;
                    string legacy = world.Item.name + ":" + Mathf.RoundToInt(point.x * 1000) + ":" + Mathf.RoundToInt(point.y * 1000);
                    world.ConfigureIdentity(Guid.NewGuid().ToString("N"), legacy);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(world);
                    EditorUtility.SetDirty(world);
                }
                FixedEnemySpawner spawner = roots.SelectMany(go => go.GetComponentsInChildren<FixedEnemySpawner>(true)).Single();
                var placements = new SerializedObject(spawner);
                SerializedProperty array = placements.FindProperty("placements");
                for (int i = 0; i < array.arraySize; i++)
                {
                    SerializedProperty id = array.GetArrayElementAtIndex(i).FindPropertyRelative("StableId");
                    if (string.IsNullOrEmpty(id.stringValue)) id.stringValue = Guid.NewGuid().ToString("N");
                }
                placements.ApplyModifiedPropertiesWithoutUndo();
                GameSaveController save = roots.SelectMany(go => go.GetComponentsInChildren<GameSaveController>(true)).Single();
                save.ConfigureWorld(AssetDatabase.LoadAssetAtPath<WorldItem>("Assets/Prefabs/WorldItem.prefab"), spawner,
                    roots.SelectMany(go => go.GetComponentsInChildren<DragonKnightBoss>(true)).Single(),
                    roots.SelectMany(go => go.GetComponentsInChildren<BossEncounter>(true)).Single());
                EditorUtility.SetDirty(save);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
            Debug.Log("角色状态组件、Animator 子状态机及物品/关卡存档标识配置完成。");
        }

        private static void GroupAnimations()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Player/PlayerMovement.controller");
            AnimatorStateMachine root = controller.layers[0].stateMachine;
            if (root.stateMachines.Any(child => child.stateMachine.name == "Locomotion")) return;
            AnimatorStateMachine locomotion = root.AddStateMachine("Locomotion", new Vector3(250, 40));
            AnimatorStateMachine combat = root.AddStateMachine("Combat", new Vector3(250, 150));
            AnimatorStateMachine actions = root.AddStateMachine("Actions", new Vector3(250, 260));
            ChildAnimatorState[] all = root.states;
            locomotion.states = all.Where(child => new[] { "Idle", "Run", "Jump", "Fall" }.Contains(child.state.name)).ToArray();
            combat.states = all.Where(child => child.state.name.StartsWith("Attack")).ToArray();
            actions.states = all.Except(locomotion.states).Except(combat.states).ToArray();
            locomotion.defaultState = locomotion.states.Single(child => child.state.name == "Idle").state;
            combat.defaultState = combat.states.Single(child => child.state.name == "Attack1").state;
            actions.defaultState = actions.states.Single(child => child.state.name == "Dash").state;
            root.states = Array.Empty<ChildAnimatorState>();
            root.defaultState = null;
            root.AddEntryTransition(locomotion);
            foreach (AnimatorStateMachine machine in new[] { root, locomotion, combat, actions }) EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(controller);
        }
    }
}
