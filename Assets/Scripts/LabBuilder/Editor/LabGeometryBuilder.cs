#if UNITY_EDITOR
using LabBuilder.Data;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace LabBuilder.Builder
{
    public static class LabGeometryBuilder
    {
        private const float CORRIDOR_OFFSET = 0.01f;

        // ══════════════════════════════════════
        //  Room Building
        // ══════════════════════════════════════

        public static GameObject BuildRoom(RoomData data, Vector3 position, Quaternion rotation)
        {
            if (!data.Validate(out var errors))
            {
                foreach (var e in errors)
                    Debug.LogWarning($"[LabBuilder] Валидация: {e}");
                return null;
            }

            var root = new GameObject($"LabRoom_{data.width:F0}x{data.length:F0}");
            root.transform.position = position;
            root.transform.rotation = rotation;
            Undo.RegisterCreatedObjectUndo(root, "Create Lab Room");

            var comp = root.AddComponent<LabRoomComponent>();
            comp.roomData = data.Clone();

            BuildRoomGeometry(root.transform, data);

            Selection.activeGameObject = root;
            return root;
        }

        public static void RebuildRoom(LabRoomComponent room)
        {
            if (room == null) return;

            Undo.RecordObject(room.gameObject, "Rebuild Room");

            var children = new List<Transform>();
            foreach (Transform child in room.transform)
            {
                children.Add(child);
            }

            foreach (var child in children)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }

            BuildRoomGeometry(room.transform, room.Data);

            EditorUtility.SetDirty(room.gameObject);
        }

        private static void BuildRoomGeometry(Transform parent, RoomData data)
        {
            BuildFloor(parent, data);
            BuildCeiling(parent, data);
            BuildWalls(parent, data);
        }

        // ══════════════════════════════════════
        //  Connection: Room to Door
        // ══════════════════════════════════════

        public struct RoomPositionData
        {
            public Vector3 position;
            public Quaternion rotation;
        }

        /// <summary>
        /// Вычисляет позицию и поворот новой комнаты так, чтобы указанная дверь 
        /// выровнялась с исходной дверью через коридор.
        /// </summary>
        public static RoomPositionData CalculateRoomPositionFromDoor(
            LabRoomComponent.DoorWorldInfo sourceDoor,
            RoomData newRoomData,
            float corridorLength,
            int targetDoorIndex)
        {
            // Проверка наличия дверей
            if (newRoomData.doors.Count == 0)
            {
                // Если нет дверей, просто размещаем комнату прямо
                Vector3 position = sourceDoor.Position + sourceDoor.Forward * (corridorLength + newRoomData.length * 0.5f);
                Quaternion rotation = Quaternion.LookRotation(-sourceDoor.Forward);

                return new RoomPositionData
                {
                    position = position,
                    rotation = rotation
                };
            }

            // Проверка корректности индекса
            if (targetDoorIndex < 0 || targetDoorIndex >= newRoomData.doors.Count)
            {
                targetDoorIndex = 0;
            }

            var targetDoor = newRoomData.doors[targetDoorIndex];

            // 1. Базовая позиция: конец коридора
            Vector3 corridorEnd = sourceDoor.Position + sourceDoor.Forward * corridorLength;

            // 2. Поворот комнаты: дверь должна смотреть обратно
            Quaternion roomRotation = Quaternion.LookRotation(-sourceDoor.Forward);

            // 3. Локальная позиция двери в новой комнате
            Vector3 doorLocalPos = LabRoomComponent.ComputeDoorLocalPosition(targetDoor, newRoomData);

            // 4. Смещение комнаты
            Vector3 doorWorldOffset = roomRotation * doorLocalPos;
            Vector3 roomPosition = corridorEnd - doorWorldOffset;

            return new RoomPositionData
            {
                position = roomPosition,
                rotation = roomRotation
            };
        }

        /// <summary>
        /// Находит лучшую дверь для подключения в новой комнате.
        /// </summary>
        public static int FindBestDoorForConnection(RoomData newRoomData, LabRoomComponent.DoorWorldInfo sourceDoor)
        {
            if (newRoomData.doors.Count == 0)
                return -1;

            // Определяем направление исходной двери
            var sourceDir = sourceDoor.Forward;

            // Ищем дверь на противоположной стене
            // Если sourceDoor смотрит на North (+Z), ищем South (-Z) в новой комнате

            WallSide preferredWall = WallSide.South;

            // Определяем предпочтительную стену на основе направления
            if (Vector3.Dot(sourceDir, Vector3.forward) > 0.7f)
                preferredWall = WallSide.South;
            else if (Vector3.Dot(sourceDir, Vector3.back) > 0.7f)
                preferredWall = WallSide.North;
            else if (Vector3.Dot(sourceDir, Vector3.right) > 0.7f)
                preferredWall = WallSide.West;
            else if (Vector3.Dot(sourceDir, Vector3.left) > 0.7f)
                preferredWall = WallSide.East;

            // Ищем дверь на предпочтительной стене
            int doorIndex = newRoomData.doors.FindIndex(d => d.wall == preferredWall);

            if (doorIndex >= 0)
                return doorIndex;

            // Если не найдено, берём первую дверь
            return 0;
        }

        /// <summary>
        /// Создаёт комнату и прямой коридор от указанной двери.
        /// </summary>
        public static GameObject ConnectNewRoomToDoor(
            LabRoomComponent sourceRoom,
            int sourceDoorIndex,
            RoomData newRoomData,
            float corridorLength)
        {
            if (sourceRoom == null || sourceDoorIndex >= sourceRoom.DoorCount)
            {
                Debug.LogError("[LabBuilder] Некорректная исходная дверь");
                return null;
            }

            var sourceDoor = sourceRoom.GetDoorWorldInfo(sourceDoorIndex);

            // Находим лучшую дверь в новой комнате
            int targetDoorIndex = FindBestDoorForConnection(newRoomData, sourceDoor);

            if (targetDoorIndex < 0)
            {
                Debug.LogWarning("[LabBuilder] В новой комнате нет дверей!");
                return null;
            }

            // Вычисляем позицию и поворот новой комнаты
            var roomPlacement = CalculateRoomPositionFromDoor(
                sourceDoor,
                newRoomData,
                corridorLength,
                targetDoorIndex
            );

            // Создаём новую комнату
            var newRoom = BuildRoom(newRoomData, roomPlacement.position, roomPlacement.rotation);

            if (newRoom == null) return null;

            var newRoomComp = newRoom.GetComponent<LabRoomComponent>();

            // Создаём прямой коридор
            var corridor = CreateStraightCorridor(
                sourceDoor.Position,
                sourceDoor.Forward,
                corridorLength,
                sourceDoor.Width,
                sourceDoor.Height,
                sourceRoom.Data
            );

            if (corridor != null)
            {
                corridor.transform.SetParent(sourceRoom.transform);
                corridor.name = $"Corridor_to_{newRoom.name}";
            }

            // Регистрируем соединение
            Undo.RecordObject(sourceRoom, "Connect Room");
            sourceRoom.connections.Add(new ConnectionInfo
            {
                sourceDoorIndex = sourceDoorIndex,
                sourceDoorId = sourceDoor.DoorId,
                connectedObject = newRoom,
                connectionType = ConnectionType.DirectRoom,
                targetDoorIndex = targetDoorIndex,
                connectionLength = corridorLength
            });

            EditorUtility.SetDirty(sourceRoom);

            Selection.activeGameObject = newRoom;

            var targetDoor = newRoomData.doors[targetDoorIndex];
            Debug.Log($"[LabBuilder] Комната подключена. Коридор: {corridorLength:F1}м. " +
                     $"Дверь {sourceDoorIndex} ({sourceDoor.Wall}) → Дверь {targetDoorIndex} ({targetDoor.wall})");

            return newRoom;
        }

        // ══════════════════════════════════════
        //  Straight Corridor
        // ══════════════════════════════════════

        private static GameObject CreateStraightCorridor(
            Vector3 startPos,
            Vector3 direction,
            float length,
            float width,
            float height,
            RoomData sourceData)
        {
            var corridor = new GameObject($"Corridor_{length:F1}m");
            corridor.transform.position = startPos;
            corridor.transform.rotation = Quaternion.LookRotation(direction);

            Undo.RegisterCreatedObjectUndo(corridor, "Create Corridor");

            float hw = width * 0.5f;
            float wt = sourceData.wallThickness;

            float adjustedLength = length - (CORRIDOR_OFFSET * 2f);
            float startOffset = CORRIDOR_OFFSET;

            // Пол
            var floor = CreateBox("Floor",
                new Vector3(width, wt, adjustedLength),
                new Vector3(0f, -wt * 0.5f, startOffset + adjustedLength * 0.5f),
                corridor.transform
            );
            ApplyMaterial(floor, sourceData.floorMaterial);
            AddCollider(floor);

            // Потолок
            var ceiling = CreateBox("Ceiling",
                new Vector3(width, wt, adjustedLength),
                new Vector3(0f, height + wt * 0.5f, startOffset + adjustedLength * 0.5f),
                corridor.transform
            );
            ApplyMaterial(ceiling, sourceData.ceilingMaterial);
            AddCollider(ceiling);

            // Левая стена
            var leftWall = CreateBox("Wall_Left",
                new Vector3(wt, height, adjustedLength),
                new Vector3(-hw - wt * 0.5f, height * 0.5f, startOffset + adjustedLength * 0.5f),
                corridor.transform
            );
            ApplyMaterial(leftWall, sourceData.wallMaterial);
            AddCollider(leftWall);

            // Правая стена
            var rightWall = CreateBox("Wall_Right",
                new Vector3(wt, height, adjustedLength),
                new Vector3(hw + wt * 0.5f, height * 0.5f, startOffset + adjustedLength * 0.5f),
                corridor.transform
            );
            ApplyMaterial(rightWall, sourceData.wallMaterial);
            AddCollider(rightWall);

            return corridor;
        }

        /// <summary>
        /// Публичный метод для создания коридора (используется из LabBuilderWindow).
        /// </summary>
        public static GameObject CreateStraightCorridorPublic(
            Vector3 startPos,
            Vector3 direction,
            float length,
            float width,
            float height,
            RoomData sourceData)
        {
            return CreateStraightCorridor(startPos, direction, length, width, height, sourceData);
        }

        // ══════════════════════════════════════
        //  Overlap Check
        // ══════════════════════════════════════

        public static bool CheckOverlap(Bounds newBounds, out LabRoomComponent overlapping)
        {
            overlapping = null;
            var rooms = Object.FindObjectsByType<LabRoomComponent>(FindObjectsSortMode.None);

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
            var floor = CreateBox("Floor",
                new Vector3(data.width + wt * 2f, wt, data.length + wt * 2f),
                new Vector3(0f, -wt * 0.5f, 0f),
                parent
            );
            if (data.floorMaterial != null)
                ApplyMaterial(floor, data.floorMaterial);

            AddCollider(floor);
        }

        private static void BuildCeiling(Transform parent, RoomData data)
        {
            float wt = data.wallThickness;
            var ceiling = CreateBox("Ceiling",
                new Vector3(data.width + wt * 2f, wt, data.length + wt * 2f),
                new Vector3(0f, data.height + wt * 0.5f, 0f),
                parent
            );
            if (data.ceilingMaterial != null)
                ApplyMaterial(ceiling, data.ceilingMaterial);

            AddCollider(ceiling);
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

        private static void BuildSolidWall(Transform parent, RoomData data, WallSide side)
        {
            ComputeSolidWallTransform(data, side, out var size, out var pos);
            var wall = CreateBox($"Wall_{side}", size, pos, parent);
            if (data.wallMaterial != null)
                ApplyMaterial(wall, data.wallMaterial);

            AddCollider(wall);
        }

        private static void BuildWallWithDoors(
            Transform parent, RoomData data, WallSide side, List<DoorData> doors)
        {
            doors.Sort((a, b) => a.position.CompareTo(b.position));
            float wallLen = side is WallSide.North or WallSide.South ? data.width : data.length;
            float cursor = 0f;

            for (int i = 0; i < doors.Count; i++)
            {
                float doorCenter = Mathf.Lerp(0f, wallLen, doors[i].position);
                float doorHW = doors[i].width * 0.5f;
                float doorLeft = Mathf.Max(0f, doorCenter - doorHW);
                float doorRight = Mathf.Min(wallLen, doorCenter + doorHW);

                float leftLen = doorLeft - cursor;
                if (leftLen > 0.01f)
                {
                    float segCenter = cursor + leftLen * 0.5f;
                    var leftSeg = AddWallSegment(parent, data, side, segCenter, leftLen, data.height, data.height * 0.5f, $"Wall_{side}_S{i}L");
                    AddCollider(leftSeg);
                }

                float topH = data.height - doors[i].height;
                if (topH > 0.01f)
                {
                    float segCenter = (doorLeft + doorRight) * 0.5f;
                    var topSeg = AddWallSegment(parent, data, side, segCenter, doors[i].width, topH, doors[i].height + topH * 0.5f, $"Wall_{side}_S{i}T");
                    AddCollider(topSeg);
                }

                cursor = doorRight;
            }

            float rightLen = wallLen - cursor;
            if (rightLen > 0.01f)
            {
                float segCenter = cursor + rightLen * 0.5f;
                var rightSeg = AddWallSegment(parent, data, side, segCenter, rightLen, data.height, data.height * 0.5f, $"Wall_{side}_End");
                AddCollider(rightSeg);
            }
        }

        private static ProBuilderMesh AddWallSegment(
            Transform parent, RoomData data, WallSide side,
            float alongCenter, float segWidth, float segHeight, float yCenter, string name)
        {
            float hw = data.width * 0.5f;
            float hl = data.length * 0.5f;
            float wt = data.wallThickness;

            Vector3 size, pos;

            switch (side)
            {
                case WallSide.North: // +Z
                    size = new Vector3(segWidth, segHeight, wt);
                    pos = new Vector3(alongCenter - hw, yCenter, hl + wt * 0.5f);
                    break;
                case WallSide.South: // -Z
                    size = new Vector3(segWidth, segHeight, wt);
                    pos = new Vector3(alongCenter - hw, yCenter, -hl - wt * 0.5f);
                    break;
                case WallSide.East: // +X
                    size = new Vector3(wt, segHeight, segWidth);
                    pos = new Vector3(hw + wt * 0.5f, yCenter, alongCenter - hl);
                    break;
                case WallSide.West: // -X
                    size = new Vector3(wt, segHeight, segWidth);
                    pos = new Vector3(-hw - wt * 0.5f, yCenter, alongCenter - hl);
                    break;
                default:
                    size = Vector3.one;
                    pos = Vector3.zero;
                    break;
            }

            var seg = CreateBox(name, size, pos, parent);
            if (data.wallMaterial != null)
                ApplyMaterial(seg, data.wallMaterial);

            return seg;
        }

        private static void ComputeSolidWallTransform(
            RoomData data, WallSide side, out Vector3 size, out Vector3 pos)
        {
            float hw = data.width * 0.5f;
            float hl = data.length * 0.5f;
            float wt = data.wallThickness;
            float hh = data.height * 0.5f;

            switch (side)
            {
                case WallSide.North: // +Z
                    size = new Vector3(data.width, data.height, wt);
                    pos = new Vector3(0f, hh, hl + wt * 0.5f);
                    break;
                case WallSide.South: // -Z
                    size = new Vector3(data.width, data.height, wt);
                    pos = new Vector3(0f, hh, -hl - wt * 0.5f);
                    break;
                case WallSide.East: // +X
                    size = new Vector3(wt, data.height, data.length + wt * 2f);
                    pos = new Vector3(hw + wt * 0.5f, hh, 0f);
                    break;
                case WallSide.West: // -X
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

        private static ProBuilderMesh CreateBox(
            string name, Vector3 size, Vector3 localPos, Transform parent)
        {
            var pb = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            pb.gameObject.name = name;
            pb.transform.SetParent(parent, false);
            pb.transform.localPosition = localPos;
            pb.ToMesh();
            pb.Refresh();
            return pb;
        }

        private static void ApplyMaterial(ProBuilderMesh mesh, Material material)
        {
            var renderer = mesh.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        private static void AddCollider(ProBuilderMesh mesh)
        {
            if (mesh == null) return;

            var meshCollider = mesh.gameObject.AddComponent<MeshCollider>();

            // Ensure ProBuilder has pushed geometry into a Unity Mesh
            mesh.ToMesh();
            mesh.Refresh();

            var mf = mesh.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                meshCollider.sharedMesh = mf.sharedMesh;
            }
            else
            {
                Debug.LogWarning("[LabBuilder] Failed to get generated Mesh for MeshCollider.");
            }

            meshCollider.convex = false;
        }
    }
}
#endif