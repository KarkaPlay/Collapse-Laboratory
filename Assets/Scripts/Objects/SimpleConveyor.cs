using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простой конвейер, двигает объекты слева направо и телепортирует их обратно.
/// </summary>
public class SimpleConveyor : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Скорость движения (м/с)")]
    public float speed = 2f;

    [Tooltip("Позиция X левой границы (появление)")]
    public float leftBoundary = -10f;

    [Tooltip("Позиция X правой границы (исчезновение)")]
    public float rightBoundary = 10f;

    [Tooltip("Список объектов на ленте")]
    public List<Transform> items = new();

    private void Update()
    {
        MoveItems();
        CheckBoundaries();
    }

    private void MoveItems()
    {
        float movement = speed * Time.deltaTime;

        foreach (var item in items)
        {
            if (item == null) continue;
            item.position += Vector3.right * movement;
        }
    }

    private void CheckBoundaries()
    {
        foreach (var item in items)
        {
            if (item == null) continue;

            // Если объект прошёл правую границу — телепортируем на левую
            if (item.position.x > rightBoundary)
            {
                Vector3 pos = item.position;
                pos.x = leftBoundary;
                item.position = pos;
            }
        }
    }

    /// <summary>
    /// Изменить скорость конвейера (для запутанности с Шестернями)
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    private void OnDrawGizmos()
    {
        // Левая граница (зелёная)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(leftBoundary, transform.position.y - 1, transform.position.z), new Vector3(leftBoundary, transform.position.y + 1, transform.position.z));

        // Правая граница (красная)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(rightBoundary, transform.position.y - 1, transform.position.z), new Vector3(rightBoundary, transform.position.y + 1, transform.position.z));

        // Линия конвейера (жёлтая)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(leftBoundary, transform.position.y, transform.position.z), new Vector3(rightBoundary, transform.position.y, transform.position.z));
    }
}