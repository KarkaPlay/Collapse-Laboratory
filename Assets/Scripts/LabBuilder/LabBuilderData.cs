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
        East = 1,  // +X
        South = 2,  // -Z
        West = 3   // -X
    }

    // ══════════════════════════════════════════════
    //  Данные дверного проёма
    // ══════════════════════════════════════════════

    /// <summary>
    /// Описывает один дверной проём на стене комнаты.
    /// Position — нормализованная позиция вдоль стены (0 = левый край, 1 = правый).
    /// </summary>
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

        /// <summary>Проверяет корректность двери относительно размеров стены.</summary>
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

            // Проверка: дверь не выходит за края стены
            float doorCenter = Mathf.Lerp(0f, wallLength, position);
            float halfDoor = width * 0.5f;

            if (doorCenter - halfDoor < 0.05f || doorCenter + halfDoor > wallLength - 0.05f)
            {
                error = "Дверь слишком близко к углу стены";
                return false;
            }

            return true;
        }
    }

    // ══════════════════════════════════════════════
    //  Данные комнаты
    // ══════════════════════════════════════════════

    /// <summary>
    /// Полное описание комнаты: размеры, двери, материалы.
    /// Комната центрирована в (0,0,0) по XZ, пол на y=0, потолок на y=height.
    /// Width — по оси X, Length — по оси Z.
    /// </summary>
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

        /// <summary>Валидация всех параметров комнаты.</summary>
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
    }

    // ══════════════════════════════════════════════
    //  Данные коридора
    // ══════════════════════════════════════════════

    /// <summary>
    /// Описание коридора. Строится вдоль +Z в локальных координатах.
    /// Вход коридора — при z=0, выход — при z=length.
    /// </summary>
    [Serializable]
    public sealed class CorridorData
    {
        [Header("Размеры коридора")]
        [Min(1.5f)] public float width = 2f;
        [Min(2.5f)] public float height = 3f;
        [Min(1f)] public float length = 4f;
        [Range(0.05f, 0.5f)]
        public float wallThickness = 0.15f;

        [Header("Материалы")]
        public Material floorMaterial;
        public Material ceilingMaterial;
        public Material wallMaterial;

        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            if (width < 1.5f) errors.Add("Минимальная ширина коридора — 1.5м");
            if (height < 2.5f) errors.Add("Минимальная высота коридора — 2.5м");
            if (length < 1f) errors.Add("Минимальная длина коридора — 1м");
            return errors.Count == 0;
        }
    }

    // ══════════════════════════════════════════════
    //  Информация о соединении
    // ══════════════════════════════════════════════

    /// <summary>Хранит связь между дверью комнаты и подключённым коридором.</summary>
    [Serializable]
    public sealed class ConnectionInfo
    {
        public int sourceDoorIndex;
        public GameObject connectedCorridor;
    }
}