using LabBuilder.Data;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LabBuilder
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Lab Builder/Room Component")]
    public sealed class LabRoomComponent : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        public RoomData roomData = new();

        [SerializeField, HideInInspector]
        public List<ConnectionInfo> connections = new();

        public RoomData Data => roomData;
        public IReadOnlyList<ConnectionInfo> Connections => connections;
        public int DoorCount => roomData.doors.Count;

        public struct DoorWorldInfo
        {
            public Vector3 Position;
            public Vector3 Forward;
            public Quaternion Rotation;
            public float Width;
            public float Height;
            public WallSide Wall;
            public string DoorId;
        }

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
                Height = door.height,
                Wall = door.wall,
                DoorId = door.doorId
            };
        }

        public bool IsDoorConnected(int doorIndex)
        {
            foreach (var conn in connections)
            {
                if (conn.sourceDoorIndex == doorIndex && conn.connectedObject != null)
                    return true;
            }
            return false;
        }

        public ConnectionInfo GetConnection(int doorIndex)
        {
            return connections.Find(c => c.sourceDoorIndex == doorIndex);
        }

        public Bounds GetWorldBounds()
        {
            var center = transform.position + new Vector3(0f, roomData.height * 0.5f, 0f);
            var size = new Vector3(roomData.width, roomData.height, roomData.length);
            return new Bounds(center, size);
        }

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

        public static Vector3 GetWallOutwardNormal(WallSide wall) => wall switch
        {
            WallSide.North => Vector3.forward,
            WallSide.South => Vector3.back,
            WallSide.East => Vector3.right,
            WallSide.West => Vector3.left,
            _ => Vector3.forward
        };

        private void OnDrawGizmosSelected()
        {
            if (roomData == null) return;

            var settings = LabBuilderSettings.Instance;
            Gizmos.color = settings != null ? settings.DoorGizmoColor : new Color(0.2f, 0.9f, 0.3f, 0.6f);

            for (int i = 0; i < roomData.doors.Count; i++)
            {
                var info = GetDoorWorldInfo(i);
                var center = info.Position + Vector3.up * (info.Height * 0.5f);
                var size = new Vector3(info.Width, info.Height, 0.05f);

                Gizmos.matrix = Matrix4x4.TRS(center, info.Rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);

                if (IsDoorConnected(i))
                {
                    Gizmos.color = new Color(0.9f, 0.4f, 0.1f, 0.3f);
                    Gizmos.DrawCube(Vector3.zero, size);
                    Gizmos.color = settings != null ? settings.DoorGizmoColor : new Color(0.2f, 0.9f, 0.3f, 0.6f);
                }

                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawRay(info.Position + Vector3.up, info.Forward * 1.5f);

#if UNITY_EDITOR
                if (settings != null && settings.ShowDoorLabels)
                {
                    var labelPos = info.Position + Vector3.up * info.Height + info.Forward * 0.3f;
                    Handles.Label(labelPos, $"Door {i}\n{info.Wall}", new GUIStyle
                    {
                        normal = { textColor = Color.white },
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11
                    });
                }
#endif
            }

            // Визуализация путей коридоров
            Gizmos.color = new Color(0.9f, 0.6f, 0.1f, 0.8f);
            foreach (var conn in connections)
            {
                if (conn.corridorPath != null && conn.corridorPath.Count > 1)
                {
                    for (int i = 0; i < conn.corridorPath.Count - 1; i++)
                    {
                        Gizmos.DrawLine(conn.corridorPath[i] + Vector3.up, conn.corridorPath[i + 1] + Vector3.up);
                    }
                }
            }
        }
    }
}