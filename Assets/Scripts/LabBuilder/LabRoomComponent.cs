using LabBuilder.Data;
using System.Collections.Generic;
using UnityEngine;

namespace LabBuilder
{
    /// <summary>
    /// MonoBehaviour-компонент, который автоматически добавляется
    /// на каждый сгенерированный объект комнаты.
    /// Хранит копию RoomData и список соединений.
    /// Предоставляет API для запроса мировых координат дверей.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Lab Builder/Room Component")]
    public sealed class LabRoomComponent : MonoBehaviour
    {
        // ──────────────────────────────────────
        //  Serialized Data
        // ──────────────────────────────────────

        [SerializeField, HideInInspector]
        public RoomData roomData = new();

        [SerializeField, HideInInspector]
        public List<ConnectionInfo> connections = new();

        // ──────────────────────────────────────
        //  Public Properties
        // ──────────────────────────────────────

        public RoomData Data => roomData;
        public IReadOnlyList<ConnectionInfo> Connections => connections;
        public int DoorCount => roomData.doors.Count;

        // ──────────────────────────────────────
        //  Door World Info
        // ──────────────────────────────────────

        /// <summary>Результат запроса мировых координат двери.</summary>
        public struct DoorWorldInfo
        {
            public Vector3 Position;  // Мировая позиция центра двери (на уровне пола)
            public Vector3 Forward;   // Направление наружу от стены
            public Quaternion Rotation;  // Ротация для выравнивания коридора
            public float Width;
            public float Height;
        }

        /// <summary>
        /// Возвращает мировые координаты и ориентацию двери.
        /// Используется для автоматического выравнивания коридоров.
        /// </summary>
        public DoorWorldInfo GetDoorWorldInfo(int doorIndex)
        {
            if (doorIndex < 0 || doorIndex >= roomData.doors.Count)
            {
                Debug.LogError($"[LabBuilder] Индекс двери {doorIndex} вне диапазона");
                return default;
            }

            var door = roomData.doors[doorIndex];
            var t = transform;

            Vector3 localPos = ComputeDoorLocalPosition(door, roomData);
            Vector3 localNormal = GetWallOutwardNormal(door.wall);

            return new DoorWorldInfo
            {
                Position = t.TransformPoint(localPos),
                Forward = t.TransformDirection(localNormal),
                Rotation = t.rotation * Quaternion.LookRotation(localNormal),
                Width = door.width,
                Height = door.height
            };
        }

        // ──────────────────────────────────────
        //  Bounds
        // ──────────────────────────────────────

        /// <summary>Возвращает мировой AABB комнаты (без учёта толщины стен).</summary>
        public Bounds GetWorldBounds()
        {
            var center = transform.position + new Vector3(0f, roomData.height * 0.5f, 0f);
            var size = new Vector3(roomData.width, roomData.height, roomData.length);
            return new Bounds(center, size);
        }

        // ──────────────────────────────────────
        //  Static Helpers
        // ──────────────────────────────────────

        /// <summary>
        /// Вычисляет локальную позицию центра двери (на уровне пола).
        /// Комната центрирована в (0,0,0): x от -width/2 до +width/2, z от -length/2 до +length/2.
        /// </summary>
        public static Vector3 ComputeDoorLocalPosition(DoorData door, RoomData room)
        {
            float hw = room.width * 0.5f;
            float hl = room.length * 0.5f;

            return door.wall switch
            {
                WallSide.North => new Vector3(Mathf.Lerp(-hw, hw, door.position), 0f, hl),
                WallSide.South => new Vector3(Mathf.Lerp(-hw, hw, door.position), 0f, -hl),
                WallSide.East => new Vector3(hw, 0f, Mathf.Lerp(-hl, hl, door.position)),
                WallSide.West => new Vector3(-hw, 0f, Mathf.Lerp(-hl, hl, door.position)),
                _ => Vector3.zero
            };
        }

        /// <summary>Возвращает направление «наружу» для указанной стены.</summary>
        public static Vector3 GetWallOutwardNormal(WallSide wall) => wall switch
        {
            WallSide.North => Vector3.forward,   //  (0, 0, +1)
            WallSide.South => Vector3.back,       //  (0, 0, -1)
            WallSide.East => Vector3.right,      //  (+1, 0, 0)
            WallSide.West => Vector3.left,       //  (-1, 0, 0)
            _ => Vector3.forward
        };

        // ──────────────────────────────────────
        //  Gizmos (для визуализации в Scene)
        // ──────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (roomData == null) return;

            // Отрисовка дверей
            Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.6f);

            for (int i = 0; i < roomData.doors.Count; i++)
            {
                var info = GetDoorWorldInfo(i);
                var center = info.Position + Vector3.up * (info.Height * 0.5f);
                var size = new Vector3(info.Width, info.Height, 0.05f);

                Gizmos.matrix = Matrix4x4.TRS(center, info.Rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;

                // Стрелка направления
                Gizmos.DrawRay(info.Position + Vector3.up, info.Forward * 1.5f);
            }
        }
    }
}