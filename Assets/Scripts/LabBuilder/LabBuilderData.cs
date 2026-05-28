using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabBuilder.Data
{
    // ══════════════════════════════════════════════
    //  Перечисление сторон стены
    // ══════════════════════════════════════════════

    public enum WallSide
    {
        North = 0,  // +Z
        East = 1,   // +X
        South = 2,  // -Z
        West = 3    // -X
    }

    // ══════════════════════════════════════════════
    //  Данные дверного проёма
    // ══════════════════════════════════════════════

    [Serializable]
    public sealed class DoorData
    {
        [Tooltip("Стена, на которой расположена дверь")]
        public WallSide wall = WallSide.North;

        [Tooltip("Позиция по длине стены (0 = левый край, 1 = правый край)")]
        [Range(0.15f, 0.85f)]
        public float position = 0.5f;

        [Tooltip("Ширина дверного проёма (м)")]
        [Min(0.7f)]
        public float width = 1.2f;

        [Tooltip("Высота дверного проёма (м)")]
        [Min(1.5f)]
        public float height = 2.4f;

        [HideInInspector]
        public string doorId = Guid.NewGuid().ToString();

        public bool Validate(float wallLength, float roomHeight, out string error)
        {
            error = null;

            if (width >= wallLength - 0.4f)
            {
                error = $"Ширина двери ({width:F1}м) слишком велика для стены ({wallLength:F1}м)";
                return false;
            }

            if (height >= roomHeight - 0.05f)
            {
                error = $"Высота двери ({height:F1}м) >= высота комнаты ({roomHeight:F1}м)";
                return false;
            }

            float doorCenter = Mathf.Lerp(0f, wallLength, position);
            float halfDoor = width * 0.5f;

            if (doorCenter - halfDoor < 0.05f || doorCenter + halfDoor > wallLength - 0.05f)
            {
                error = "Дверь слишком близко к углу стены";
                return false;
            }

            return true;
        }

        public DoorData Clone()
        {
            return new DoorData
            {
                wall = wall,
                position = position,
                width = width,
                height = height,
                doorId = Guid.NewGuid().ToString()
            };
        }
    }

    // ══════════════════════════════════════════════
    //  Данные комнаты
    // ══════════════════════════════════════════════

    [Serializable]
    public sealed class RoomData
    {
        [Header("Размеры комнаты")]
        [Min(2f)] public float width = 5f;
        [Min(2f)] public float length = 5f;
        [Min(2.5f)] public float height = 3f;
        [Range(0.05f, 0.5f)]
        public float wallThickness = 0.15f;

        [Header("Двери")]
        public List<DoorData> doors = new();

        [Header("Материалы")]
        public Material floorMaterial;
        public Material ceilingMaterial;
        public Material wallMaterial;

        public void ApplyDefaultMaterials()
        {
            var settings = LabBuilderSettings.Instance;
            if (settings == null) return;

            if (floorMaterial == null)
                floorMaterial = settings.DefaultFloorMaterial;
            if (ceilingMaterial == null)
                ceilingMaterial = settings.DefaultCeilingMaterial;
            if (wallMaterial == null)
                wallMaterial = settings.DefaultWallMaterial;
        }

        public void ApplyDefaultDimensions()
        {
            var settings = LabBuilderSettings.Instance;
            if (settings == null) return;

            width = settings.DefaultRoomWidth;
            length = settings.DefaultRoomLength;
            height = settings.DefaultRoomHeight;
            wallThickness = settings.DefaultWallThickness;
        }

        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (width < 2f)
                errors.Add("Минимальная ширина комнаты — 2м");
            if (length < 2f)
                errors.Add("Минимальная длина комнаты — 2м");
            if (height < 2.5f)
                errors.Add("Минимальная высота комнаты — 2.5м");

            for (int i = 0; i < doors.Count; i++)
            {
                float wallLen = doors[i].wall is WallSide.North or WallSide.South
                    ? width
                    : length;

                if (!doors[i].Validate(wallLen, height, out var err))
                    errors.Add($"Дверь [{i}] ({doors[i].wall}): {err}");
            }

            return errors.Count == 0;
        }

        public RoomData Clone()
        {
            var clone = new RoomData
            {
                width = width,
                length = length,
                height = height,
                wallThickness = wallThickness,
                floorMaterial = floorMaterial,
                ceilingMaterial = ceilingMaterial,
                wallMaterial = wallMaterial
            };

            foreach (var door in doors)
            {
                clone.doors.Add(door.Clone());
            }

            return clone;
        }

        /// <summary>Копирует параметры из другой RoomData.</summary>
        public void CopyFrom(RoomData other)
        {
            width = other.width;
            length = other.length;
            height = other.height;
            wallThickness = other.wallThickness;
            floorMaterial = other.floorMaterial;
            ceilingMaterial = other.ceilingMaterial;
            wallMaterial = other.wallMaterial;

            doors.Clear();
            foreach (var door in other.doors)
            {
                doors.Add(door.Clone());
            }
        }
    }

    // ══════════════════════════════════════════════
    //  Информация о соединении
    // ══════════════════════════════════════════════

    [Serializable]
    public sealed class ConnectionInfo
    {
        public int sourceDoorIndex;
        public string sourceDoorId;
        public GameObject connectedObject;
        public ConnectionType connectionType;
        public int targetDoorIndex = -1;
        public float connectionLength;

        /// <summary>Путь коридора (для изогнутых коридоров).</summary>
        public List<Vector3> corridorPath = new();
    }

    public enum ConnectionType
    {
        DirectRoom,
        Connector,
        Corridor
    }

    // ══════════════════════════════════════════════
    //  Режим подключения
    // ══════════════════════════════════════════════

    public enum ConnectionMode
    {
        None,              // Создать отдельно стоящую комнату
        ConnectToExisting  // Подключить к существующей двери
    }
}