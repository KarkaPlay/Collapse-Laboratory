#if UNITY_EDITOR
using LabBuilder.Data;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace LabBuilder.Builder
{
    /// <summary>
    /// Генерирует геометрию комнат и коридоров через ProBuilder API.
    /// Все методы предназначены только для Editor (обёрнуто в #if UNITY_EDITOR).
    /// Каждый элемент (пол, потолок, стена) — отдельный ProBuilderMesh.
    /// </summary>
    public static class LabGeometryBuilder
    {
        // ══════════════════════════════════════
        //  Room Building
        // ══════════════════════════════════════

        /// <summary>
        /// Создаёт комнату с полом, потолком, стенами и дверными проёмами.
        /// Возвращает корневой GameObject или null при ошибке валидации.
        /// </summary>
        public static GameObject BuildRoom(RoomData data, Vector3 position)
        {
            if (!data.Validate(out var errors))
            {
                foreach (var e in errors)
                    Debug.LogWarning($"[LabBuilder] Валидация: {e}");
                return null;
            }

            // Корневой объект
            var root = new GameObject($"LabRoom_{data.width:F0}x{data.length:F0}");
            root.transform.position = position;
            Undo.RegisterCreatedObjectUndo(root, "Create Lab Room");

            // Компонент с данными
            var comp = root.AddComponent<LabRoomComponent>();
            // Deep copy через JSON (избегаем shared references)
            comp.roomData = JsonUtility.FromJson<RoomData>(
                JsonUtility.ToJson(data)
            );

            // Строим геометрию
            BuildFloor(root.transform, data);
            BuildCeiling(root.transform, data);
            BuildWalls(root.transform, data);

            Selection.activeGameObject = root;
            return root;
        }

        // ══════════════════════════════════════
        //  Corridor Building
        // ══════════════════════════════════════

        /// <summary>
        /// Создаёт коридор: пол, потолок, две боковые стены.
        /// Коридор строится вдоль +Z в локальных координатах (от z=0 до z=length).
        /// </summary>
        public static GameObject BuildCorridor(
            CorridorData data, Vector3 position, Quaternion rotation)
        {
            if (!data.Validate(out var errors))
            {
                foreach (var e in errors)
                    Debug.LogWarning($"[LabBuilder] Валидация: {e}");
                return null;
            }

            var root = new GameObject($"LabCorridor_{data.length:F0}m");
            root.transform.position = position;
            root.transform.rotation = rotation;
            Undo.RegisterCreatedObjectUndo(root, "Create Lab Corridor");

            float hw = data.width * 0.5f;
            float wt = data.wallThickness;
            float halfLen = data.length * 0.5f;

            // Пол
            var floor = CreateBox("Floor",
                new Vector3(data.width, wt, data.length),
                new Vector3(0f, -wt * 0.5f, halfLen),
                root.transform);
            if (data.floorMaterial != null)
                ApplyMaterial(floor, data.floorMaterial);

            // Потолок
            var ceiling = CreateBox("Ceiling",
                new Vector3(data.width, wt, data.length),
                new Vector3(0f, data.height + wt * 0.5f, halfLen),
                root.transform);
            if (data.ceilingMaterial != null)
                ApplyMaterial(ceiling, data.ceilingMaterial);

            // Левая стена
            var leftWall = CreateBox("Wall_Left",
                new Vector3(wt, data.height, data.length),
                new Vector3(-hw - wt * 0.5f, data.height * 0.5f, halfLen),
                root.transform);
            if (data.wallMaterial != null)
                ApplyMaterial(leftWall, data.wallMaterial);

            // Правая стена
            var rightWall = CreateBox("Wall_Right",
                new Vector3(wt, data.height, data.length),
                new Vector3(hw + wt * 0.5f, data.height * 0.5f, halfLen),
                root.transform);
            if (data.wallMaterial != null)
                ApplyMaterial(rightWall, data.wallMaterial);

            Selection.activeGameObject = root;
            return root;
        }

        // ══════════════════════════════════════
        //  Connection: Room ↔ Corridor
        // ══════════════════════════════════════

        /// <summary>
        /// Строит коридор, автоматически выравнивая его по двери комнаты.
        /// Коридор начинается точно у проёма двери и уходит наружу.
        /// </summary>
        public static GameObject ConnectCorridorToRoom(
            LabRoomComponent room, int doorIndex, CorridorData corridorData)
        {
            if (room == null)
            {
                Debug.LogError("[LabBuilder] Комната не задана");
                return null;
            }

            if (doorIndex < 0 || doorIndex >= room.DoorCount)
            {
                Debug.LogError($"[LabBuilder] Индекс двери {doorIndex} вне диапазона");
                return null;
            }

            var doorInfo = room.GetDoorWorldInfo(doorIndex);

            // Строим коридор в точке двери, ориентированный наружу
            var corridor = BuildCorridor(corridorData, doorInfo.Position, doorInfo.Rotation);

            if (corridor != null)
            {
                // Регистрируем соединение
                Undo.RecordObject(room, "Connect Corridor");
                room.connections.Add(new ConnectionInfo
                {
                    sourceDoorIndex = doorIndex,
                    connectedCorridor = corridor
                });
                EditorUtility.SetDirty(room);
            }

            return corridor;
        }

        // ══════════════════════════════════════
        //  Overlap Validation
        // ══════════════════════════════════════

        /// <summary>Проверяет, пересекается ли новый AABB с существующими комнатами.</summary>
        public static bool CheckOverlap(Bounds newBounds, out LabRoomComponent overlapping)
        {
            overlapping = null;
            var rooms = Object.FindObjectsByType<LabRoomComponent>(
                FindObjectsSortMode.None);

            foreach (var room in rooms)
            {
                if (room.GetWorldBounds().Intersects(newBounds))
                {
                    overlapping = room;
                    return true;
                }
            }
            return false;
        }

        // ══════════════════════════════════════
        //  Floor & Ceiling
        // ══════════════════════════════════════

        private static void BuildFloor(Transform parent, RoomData data)
        {
            float wt = data.wallThickness;

            // Пол чуть больше комнаты — покрывает стыки стен
            var floor = CreateBox("Floor",
                new Vector3(data.width + wt * 2f, wt, data.length + wt * 2f),
                new Vector3(0f, -wt * 0.5f, 0f),
                parent);

            if (data.floorMaterial != null)
                ApplyMaterial(floor, data.floorMaterial);
        }

        private static void BuildCeiling(Transform parent, RoomData data)
        {
            float wt = data.wallThickness;

            var ceiling = CreateBox("Ceiling",
                new Vector3(data.width + wt * 2f, wt, data.length + wt * 2f),
                new Vector3(0f, data.height + wt * 0.5f, 0f),
                parent);

            if (data.ceilingMaterial != null)
                ApplyMaterial(ceiling, data.ceilingMaterial);
        }

        // ══════════════════════════════════════
        //  Walls
        // ══════════════════════════════════════

        private static void BuildWalls(Transform parent, RoomData data)
        {
            var wallRoot = new GameObject("Walls");
            wallRoot.transform.SetParent(parent, false);

            for (int i = 0; i < 4; i++)
            {
                var side = (WallSide)i;
                var doorsOnWall = data.doors.FindAll(d => d.wall == side);

                if (doorsOnWall.Count == 0)
                    BuildSolidWall(wallRoot.transform, data, side);
                else
                    BuildWallWithDoors(wallRoot.transform, data, side, doorsOnWall);
            }
        }

        /// <summary>Строит сплошную стену без проёмов.</summary>
        private static void BuildSolidWall(Transform parent, RoomData data, WallSide side)
        {
            ComputeSolidWallTransform(data, side, out var size, out var pos);

            var wall = CreateBox($"Wall_{side}", size, pos, parent);

            if (data.wallMaterial != null)
                ApplyMaterial(wall, data.wallMaterial);
        }

        /// <summary>
        /// Строит стену с одним или несколькими дверными проёмами.
        /// Стена разбивается на сегменты: между дверями, по бокам и над дверями.
        /// </summary>
        private static void BuildWallWithDoors(
            Transform parent, RoomData data, WallSide side, List<DoorData> doors)
        {
            // Сортируем двери по позиции слева направо
            doors.Sort((a, b) => a.position.CompareTo(b.position));

            float wallLen = side is WallSide.North or WallSide.South
                ? data.width : data.length;

            float cursor = 0f;

            for (int i = 0; i < doors.Count; i++)
            {
                float doorCenter = Mathf.Lerp(0f, wallLen, doors[i].position);
                float doorHW = doors[i].width * 0.5f;
                float doorLeft = Mathf.Max(0f, doorCenter - doorHW);
                float doorRight = Mathf.Min(wallLen, doorCenter + doorHW);

                // ── Сегмент СЛЕВА от двери (полная высота) ──
                float leftLen = doorLeft - cursor;
                if (leftLen > 0.01f)
                {
                    float segCenter = cursor + leftLen * 0.5f;
                    AddWallSegment(parent, data, side,
                        segCenter, leftLen, data.height, data.height * 0.5f,
                        $"Wall_{side}_S{i}L");
                }

                // ── Сегмент НАД дверью ──
                float topH = data.height - doors[i].height;
                if (topH > 0.01f)
                {
                    float segCenter = (doorLeft + doorRight) * 0.5f;
                    AddWallSegment(parent, data, side,
                        segCenter, doors[i].width, topH,
                        doors[i].height + topH * 0.5f,
                        $"Wall_{side}_S{i}T");
                }

                cursor = doorRight;
            }

            // ── Сегмент СПРАВА от последней двери ──
            float rightLen = wallLen - cursor;
            if (rightLen > 0.01f)
            {
                float segCenter = cursor + rightLen * 0.5f;
                AddWallSegment(parent, data, side,
                    segCenter, rightLen, data.height, data.height * 0.5f,
                    $"Wall_{side}_End");
            }
        }

        // ══════════════════════════════════════
        //  Wall Geometry Helpers
        // ══════════════════════════════════════

        /// <summary>Создаёт один прямоугольный сегмент стены.</summary>
        /// <param name="alongCenter">Центр сегмента вдоль стены (0..wallLen).</param>
        /// <param name="segWidth">Ширина сегмента вдоль стены.</param>
        /// <param name="segHeight">Высота сегмента.</param>
        /// <param name="yCenter">Y-координата центра сегмента.</param>
        private static void AddWallSegment(
            Transform parent, RoomData data, WallSide side,
            float alongCenter, float segWidth, float segHeight, float yCenter,
            string name)
        {
            float hw = data.width * 0.5f;
            float hl = data.length * 0.5f;
            float wt = data.wallThickness;

            Vector3 size, pos;

            switch (side)
            {
                case WallSide.North:
                    size = new Vector3(segWidth, segHeight, wt);
                    pos = new Vector3(alongCenter - hw, yCenter, hl + wt * 0.5f);
                    break;

                case WallSide.South:
                    size = new Vector3(segWidth, segHeight, wt);
                    pos = new Vector3(alongCenter - hw, yCenter, -hl - wt * 0.5f);
                    break;

                case WallSide.East:
                    size = new Vector3(wt, segHeight, segWidth);
                    pos = new Vector3(hw + wt * 0.5f, yCenter, alongCenter - hl);
                    break;

                case WallSide.West:
                    size = new Vector3(wt, segHeight, segWidth);
                    pos = new Vector3(-hw - wt * 0.5f, yCenter, alongCenter - hl);
                    break;

                default: return;
            }

            var seg = CreateBox(name, size, pos, parent);
            if (data.wallMaterial != null)
                ApplyMaterial(seg, data.wallMaterial);
        }

        /// <summary>Вычисляет размер и позицию сплошной стены (без дверей).</summary>
        private static void ComputeSolidWallTransform(
            RoomData data, WallSide side, out Vector3 size, out Vector3 pos)
        {
            float hw = data.width * 0.5f;
            float hl = data.length * 0.5f;
            float wt = data.wallThickness;
            float hh = data.height * 0.5f;

            switch (side)
            {
                case WallSide.North:
                    size = new Vector3(data.width, data.height, wt);
                    pos = new Vector3(0f, hh, hl + wt * 0.5f);
                    break;

                case WallSide.South:
                    size = new Vector3(data.width, data.height, wt);
                    pos = new Vector3(0f, hh, -hl - wt * 0.5f);
                    break;

                case WallSide.East:
                    // East/West стены длиннее — закрывают углы
                    size = new Vector3(wt, data.height, data.length + wt * 2f);
                    pos = new Vector3(hw + wt * 0.5f, hh, 0f);
                    break;

                case WallSide.West:
                    size = new Vector3(wt, data.height, data.length + wt * 2f);
                    pos = new Vector3(-hw - wt * 0.5f, hh, 0f);
                    break;

                default:
                    size = Vector3.one;
                    pos = Vector3.zero;
                    break;
            }
        }

        // ══════════════════════════════════════
        //  ProBuilder Primitives
        // ══════════════════════════════════════

        /// <summary>Создаёт ProBuilder-бокс с заданным размером и позицией.</summary>
        private static ProBuilderMesh CreateBox(
            string name, Vector3 size, Vector3 localPos, Transform parent)
        {
            var pb = ShapeGenerator.GenerateCube(PivotLocation.Center, size);

            pb.gameObject.name = name;
            pb.transform.SetParent(parent, false);
            pb.transform.localPosition = localPos;

            // Финализация меша
            pb.ToMesh();
            pb.Refresh();

            return pb;
        }

        /// <summary>Назначает материал на все грани ProBuilder-меша.</summary>
        private static void ApplyMaterial(ProBuilderMesh mesh, Material material)
        {
            var renderer = mesh.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }
    }
}
#endif