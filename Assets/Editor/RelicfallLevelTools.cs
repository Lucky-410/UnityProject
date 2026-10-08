using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Relicfall.Editor
{
    public static class RelicfallLevelTools
    {
        // 只迁移确认过的单层悬空台阶；多层承重地形、洞顶、墙体和坡面仍留在 Ground_Tilemap。
        private static readonly RectInt[] OneWayRanges =
        {
            new(60, 9, 4, 1), new(66, 13, 4, 1), new(72, 17, 4, 1),
            new(85, 25, 4, 1), new(91, 29, 4, 1),
            new(100, -9, 4, 1), new(104, -5, 4, 1),
            new(113, 17, 8, 1), new(123, 13, 6, 1),
            new(132, 9, 24, 1), new(158, 13, 16, 1),
            new(176, 9, 6, 1), new(184, 5, 6, 1)
        };

        [MenuItem("Tools/Relicfall/配置单向悬空平台")]
        public static void ConfigureOneWayPlatforms()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式。");
            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/GameScene.unity");
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Additive);
            try
            {
                Tilemap ground = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Tilemap>(true))
                    .Single(map => map.name == "Ground_Tilemap");
                Transform existing = ground.transform.parent.Find("OneWayPlatforms_Tilemap");
                Tilemap target = existing != null ? existing.GetComponent<Tilemap>() : null;
                // 先验证，避免地图改变后只迁移一部分。
                foreach (RectInt range in OneWayRanges)
                    foreach (Vector2Int cell in range.allPositionsWithin)
                    {
                        Vector3Int position = new(cell.x, cell.y, 0);
                        if (!ground.HasTile(position) && (target == null || !target.HasTile(position)))
                            throw new InvalidOperationException("平台瓦片缺失，请先核对地图：" + position);
                    }
                if (target == null)
                {
                    var go = new GameObject("OneWayPlatforms_Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
                    Undo.RegisterCreatedObjectUndo(go, "配置单向平台");
                    go.transform.SetParent(ground.transform.parent, false);
                    go.transform.localPosition = ground.transform.localPosition;
                    go.transform.localRotation = ground.transform.localRotation;
                    go.transform.localScale = ground.transform.localScale;
                    go.layer = ground.gameObject.layer;
                    target = go.GetComponent<Tilemap>();
                }
                Undo.RegisterCompleteObjectUndo(ground, "拆分单向平台瓦片");
                Undo.RegisterCompleteObjectUndo(target, "迁移单向平台瓦片");
                target.tileAnchor = ground.tileAnchor;
                target.orientation = ground.orientation;
                target.orientationMatrix = ground.orientationMatrix;
                target.color = ground.color;
                TilemapRenderer sourceRenderer = ground.GetComponent<TilemapRenderer>();
                TilemapRenderer renderer = target.GetComponent<TilemapRenderer>();
                renderer.sharedMaterial = sourceRenderer.sharedMaterial;
                renderer.sortingLayerID = sourceRenderer.sortingLayerID;
                renderer.sortingOrder = sourceRenderer.sortingOrder;
                renderer.mode = sourceRenderer.mode;
                foreach (RectInt range in OneWayRanges)
                    foreach (Vector2Int cell in range.allPositionsWithin)
                    {
                        Vector3Int position = new(cell.x, cell.y, 0);
                        TileBase tile = ground.GetTile(position);
                        if (tile == null) continue; // 已迁移时重复执行不改变现有平台。
                        Color color = ground.GetColor(position);
                        Matrix4x4 matrix = ground.GetTransformMatrix(position);
                        TileFlags flags = ground.GetTileFlags(position);
                        target.SetTile(position, tile);
                        target.SetTileFlags(position, TileFlags.None);
                        target.SetColor(position, color);
                        target.SetTransformMatrix(position, matrix);
                        target.SetTileFlags(position, flags);
                        ground.SetTile(position, null);
                    }
                Rigidbody2D body = GetOrAdd<Rigidbody2D>(target.gameObject);
                body.bodyType = RigidbodyType2D.Static;
                CompositeCollider2D composite = GetOrAdd<CompositeCollider2D>(target.gameObject);
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                composite.sharedMaterial = ground.GetComponent<CompositeCollider2D>().sharedMaterial;
                composite.usedByEffector = true;
                TilemapCollider2D collider = GetOrAdd<TilemapCollider2D>(target.gameObject);
                collider.compositeOperation = Collider2D.CompositeOperation.Merge;
                collider.extrusionFactor = 0.001f;
                PlatformEffector2D effector = GetOrAdd<PlatformEffector2D>(target.gameObject);
                effector.useOneWay = true;
                effector.useOneWayGrouping = true;
                effector.surfaceArc = 160; // 排除水平侧面法线，避免台阶边缘挡住角色。
                effector.useSideFriction = false;
                effector.useSideBounce = false;
                effector.useColliderMask = false;
                ground.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
                collider.ProcessTilemapChanges();
                target.CompressBounds();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("已配置 13 组单向悬空平台，共 94 个瓦片；实体地形保留原碰撞。");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T component) ? component : Undo.AddComponent<T>(go);
    }
}
