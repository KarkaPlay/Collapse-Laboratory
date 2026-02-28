using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Процедурный генератор низкополигонального мусора для пола.
/// Вешается на объект Plane. Генерирует мелкие обломки в пределах границ плоскости.
/// Unity 6, URP. Без префабов, без Update(), без физики.
/// </summary>
[DisallowMultipleComponent]
public class DebrisGenerator : MonoBehaviour
{
    // ───────────────────────────── Настройки ─────────────────────────────

    [Header("Генерация")]
    [Tooltip("Максимальное количество объектов мусора")]
    [SerializeField, Range(1, 500)]
    private int _count = 60;

    [Tooltip("Seed для воспроизводимой генерации (0 = случайный)")]
    [SerializeField]
    private int _seed = 0;

    [Header("Расположение")]
    [Tooltip("Минимальное расстояние между объектами")]
    [SerializeField, Range(0.05f, 2f)]
    private float _minDistance = 0.25f;

    [Tooltip("Отступ от краёв Plane (в долях, 0–0.4)")]
    [SerializeField, Range(0f, 0.4f)]
    private float _edgePadding = 0.05f;

    [Header("Трансформация")]
    [Tooltip("Диапазон случайного масштаба")]
    [SerializeField]
    private Vector2 _scaleRange = new Vector2(0.03f, 0.12f);

    [Tooltip("Случайный поворот по Y (0–360)")]
    [SerializeField, Range(0f, 360f)]
    private float _maxYRotation = 360f;

    [Tooltip("Небольшой случайный наклон (градусы)")]
    [SerializeField, Range(0f, 15f)]
    private float _maxTiltAngle = 5f;

    [Header("Внешний вид")]
    [Tooltip("Палитра цветов мусора")]
    [SerializeField]
    private Color[] _palette = new Color[]
    {
        new Color(0.35f, 0.32f, 0.28f), // Тёмно-серый бетон
        new Color(0.45f, 0.40f, 0.33f), // Пыльный камень
        new Color(0.30f, 0.28f, 0.25f), // Ржавый металл
        new Color(0.50f, 0.45f, 0.35f), // Грязный жёлтый
        new Color(0.25f, 0.22f, 0.20f), // Тёмный грунт
        new Color(0.40f, 0.35f, 0.30f), // Светлый бетон
    };

    [Tooltip("Генерировать при Start()")]
    [SerializeField]
    private bool _generateOnStart = true;

    // ─────────────────────── Приватные поля ───────────────────────

    private readonly List<GameObject> _spawnedDebris = new(128);
    private readonly List<Vector2> _placedPositions = new(128);
    private System.Random _rng;

    // Стандартный Unity Plane: 10×10 юнитов при scale (1,1,1)
    private const float PlaneUnitSize = 10f;

    // ─────────────────────── Жизненный цикл ───────────────────────

    private void Start()
    {
        if (_generateOnStart)
            Generate();
    }

    // ─────────────────────── Публичный API ───────────────────────

    [ContextMenu("▶ Сгенерировать мусор")]
    public void Generate()
    {
        ClearDebris();

        int seed = _seed != 0 ? _seed : System.Environment.TickCount;
        _rng = new System.Random(seed);

        Vector3 planeScale = transform.lossyScale;
        float halfX = (PlaneUnitSize * planeScale.x) * 0.5f * (1f - _edgePadding);
        float halfZ = (PlaneUnitSize * planeScale.z) * 0.5f * (1f - _edgePadding);

        _placedPositions.Clear();

        int maxAttempts = _count * 10;
        int placed = 0;

        for (int attempt = 0; attempt < maxAttempts && placed < _count; attempt++)
        {
            float lx = RngRange(-halfX, halfX);
            float lz = RngRange(-halfZ, halfZ);
            Vector2 candidate = new(lx, lz);

            if (!IsPositionValid(candidate))
                continue;

            _placedPositions.Add(candidate);

            // Переводим локальные координаты в мировые
            Vector3 worldPos = transform.TransformPoint(
                new Vector3(lx / planeScale.x, 0f, lz / planeScale.z)
            );

            GameObject debris = CreateDebrisPiece(placed);
            Transform t = debris.transform;

            // Масштаб
            float scale = RngRange(_scaleRange.x, _scaleRange.y);
            t.localScale = new Vector3(
                scale * RngRange(0.6f, 1.4f),
                scale * RngRange(0.3f, 1.0f),
                scale * RngRange(0.6f, 1.4f)
            );

            // Поворот: основной по Y + лёгкий наклон
            float yaw = RngRange(0f, _maxYRotation);
            float tiltX = RngRange(-_maxTiltAngle, _maxTiltAngle);
            float tiltZ = RngRange(-_maxTiltAngle, _maxTiltAngle);
            t.rotation = transform.rotation * Quaternion.Euler(tiltX, yaw, tiltZ);

            // Позиция (слегка приподнять, чтобы не z-fight с полом)
            t.position = worldPos + transform.up * (0.001f + scale * 0.15f);

            t.SetParent(transform, true);
            _spawnedDebris.Add(debris);
            placed++;
        }

        Debug.Log($"[DebrisGenerator] Создано {placed}/{_count} объектов (seed: {seed})");
    }

    [ContextMenu("✖ Очистить мусор")]
    public void ClearDebris()
    {
        for (int i = _spawnedDebris.Count - 1; i >= 0; i--)
        {
            if (_spawnedDebris[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(_spawnedDebris[i]);
                else
                    DestroyImmediate(_spawnedDebris[i]);
            }
        }
        _spawnedDebris.Clear();
        _placedPositions.Clear();
    }

    // ──────────────────── Проверка минимального расстояния ────────────────────

    private bool IsPositionValid(Vector2 candidate)
    {
        float sqrMin = _minDistance * _minDistance;

        for (int i = 0, count = _placedPositions.Count; i < count; i++)
        {
            float dx = candidate.x - _placedPositions[i].x;
            float dy = candidate.y - _placedPositions[i].y;
            if (dx * dx + dy * dy < sqrMin)
                return false;
        }
        return true;
    }

    // ──────────────────── Фабрика мусора ────────────────────

    private GameObject CreateDebrisPiece(int index)
    {
        int type = _rng.Next(0, 7);
        Mesh mesh = type switch
        {
            0 => CreateFlatShard(),
            1 => CreateCuboid(),
            2 => CreateWedge(),
            3 => CreateTetrahedron(),
            4 => CreateLShape(),
            5 => CreateCylinder(5),
            6 => CreateCuboid(),  // дублирование самых частых
            _ => CreateFlatShard()
        };

        mesh.name = $"Debris_{index}_{type}";
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject go = new(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial();
        go.isStatic = true;

        return go;
    }

    // Кешированные материалы — по одному на каждый цвет палитры
    private Material[] _materials;

    private Material GetMaterial()
    {
        if (_materials == null || _materials.Length != _palette.Length)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard"); // Fallback

            _materials = new Material[_palette.Length];
            for (int i = 0; i < _palette.Length; i++)
            {
                _materials[i] = new Material(shader)
                {
                    name = $"Debris_Mat_{i}",
                    color = _palette[i]
                };
                // Небольшая вариация roughness для разнообразия
                _materials[i].SetFloat("_Smoothness", 0.1f + i * 0.05f);
            }
        }
        return _materials[_rng.Next(0, _materials.Length)];
    }

    // ──────────────────── Генераторы мешей ────────────────────

    /// <summary>Плоский треугольный осколок (3–5 вершин)</summary>
    private Mesh CreateFlatShard()
    {
        float h = RngRange(0.02f, 0.08f);
        Vector3[] verts =
        {
            new(-0.5f, 0,  -0.3f),
            new( 0.5f, 0,  -0.2f),
            new( 0.2f, 0,   0.5f),
            new(-0.3f, 0,   0.4f),
            // нижние дубли
            new(-0.5f, -h, -0.3f),
            new( 0.5f, -h, -0.2f),
            new( 0.2f, -h,  0.5f),
            new(-0.3f, -h,  0.4f),
        };
        int[] tris =
        {
            // Верх
            0,1,2,  0,2,3,
            // Низ
            6,5,4,  7,6,4,
            // Стороны
            0,4,5, 0,5,1,
            1,5,6, 1,6,2,
            2,6,7, 2,7,3,
            3,7,4, 3,4,0,
        };
        return new Mesh { vertices = verts, triangles = tris };
    }

    /// <summary>Кубоид со случайными пропорциями</summary>
    private Mesh CreateCuboid()
    {
        float w = RngRange(0.3f, 1f);
        float h = RngRange(0.15f, 0.5f);
        float d = RngRange(0.3f, 1f);
        float hw = w * 0.5f, hh = h * 0.5f, hd = d * 0.5f;

        Vector3[] v =
        {
            // Front
            new(-hw,-hh, hd), new( hw,-hh, hd), new( hw, hh, hd), new(-hw, hh, hd),
            // Back
            new( hw,-hh,-hd), new(-hw,-hh,-hd), new(-hw, hh,-hd), new( hw, hh,-hd),
            // Top
            new(-hw, hh, hd), new( hw, hh, hd), new( hw, hh,-hd), new(-hw, hh,-hd),
            // Bottom
            new(-hw,-hh,-hd), new( hw,-hh,-hd), new( hw,-hh, hd), new(-hw,-hh, hd),
            // Left
            new(-hw,-hh,-hd), new(-hw,-hh, hd), new(-hw, hh, hd), new(-hw, hh,-hd),
            // Right
            new( hw,-hh, hd), new( hw,-hh,-hd), new( hw, hh,-hd), new( hw, hh, hd),
        };

        int[] t = new int[36];
        for (int face = 0; face < 6; face++)
        {
            int si = face * 6, vi = face * 4;
            t[si] = vi; t[si + 1] = vi + 2; t[si + 2] = vi + 1;
            t[si + 3] = vi; t[si + 4] = vi + 3; t[si + 5] = vi + 2;
        }
        return new Mesh { vertices = v, triangles = t };
    }

    /// <summary>Клин (треугольная призма)</summary>
    private Mesh CreateWedge()
    {
        float w = RngRange(0.4f, 0.8f);
        float h = RngRange(0.2f, 0.5f);
        float d = RngRange(0.4f, 0.8f);

        Vector3[] v =
        {
            new(-w*.5f, 0, -d*.5f),  // 0
            new( w*.5f, 0, -d*.5f),  // 1
            new( w*.5f, 0,  d*.5f),  // 2
            new(-w*.5f, 0,  d*.5f),  // 3
            new(-w*.5f, h, -d*.5f),  // 4
            new( w*.5f, h, -d*.5f),  // 5
        };
        int[] t =
        {
            // Дно
            0,2,1, 0,3,2,
            // Перед
            0,1,5, 0,5,4,
            // Скос
            4,5,2, 4,2,3,
            // Левый
            0,4,3,
            // Правый
            1,2,5,
        };
        return new Mesh { vertices = v, triangles = t };
    }

    /// <summary>Тетраэдр</summary>
    private Mesh CreateTetrahedron()
    {
        float s = RngRange(0.3f, 0.7f);
        Vector3[] v =
        {
            new( 0,    s, 0),
            new(-s*.5f, 0, s*.5f),
            new( s*.5f, 0, s*.5f),
            new( 0,    0,-s*.5f),
        };
        int[] t =
        {
            0,1,2,
            0,2,3,
            0,3,1,
            1,3,2,
        };
        return new Mesh { vertices = v, triangles = t };
    }

    /// <summary>L-образный кусок</summary>
    private Mesh CreateLShape()
    {
        float h = RngRange(0.1f, 0.3f);
        Vector3[] v =
        {
            // Верхняя грань (6 вершин L-формы)
            new(0,   h, 0),      // 0
            new(1f,  h, 0),      // 1
            new(1f,  h, 0.4f),   // 2
            new(.4f, h, 0.4f),   // 3
            new(.4f, h, 1f),     // 4
            new(0,   h, 1f),     // 5
            // Нижняя грань
            new(0,   0, 0),      // 6
            new(1f,  0, 0),      // 7
            new(1f,  0, 0.4f),   // 8
            new(.4f, 0, 0.4f),   // 9
            new(.4f, 0, 1f),     // 10
            new(0,   0, 1f),     // 11
        };
        int[] t =
        {
            // Верх
            0,1,2, 0,2,3, 0,3,5, 3,4,5,
            // Низ
            8,7,6, 9,8,6, 11,9,6, 11,10,9,
            // Стороны
            0,6,7,  0,7,1,
            1,7,8,  1,8,2,
            2,8,9,  2,9,3,
            3,9,10, 3,10,4,
            4,10,11,4,11,5,
            5,11,6, 5,6,0,
        };
        return new Mesh { vertices = v, triangles = t };
    }

    /// <summary>Низкополигональный цилиндр</summary>
    private Mesh CreateCylinder(int segments)
    {
        float radius = RngRange(0.2f, 0.5f);
        float height = RngRange(0.1f, 0.4f);

        int vertCount = segments * 2 + 2;
        Vector3[] verts = new Vector3[vertCount];
        List<int> tris = new(segments * 12);

        // Центры
        verts[0] = new Vector3(0, height * .5f, 0); // top center
        verts[1] = new Vector3(0, -height * .5f, 0); // bottom center

        for (int i = 0; i < segments; i++)
        {
            float angle = (2f * Mathf.PI * i) / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            verts[2 + i] = new Vector3(x, height * .5f, z);
            verts[2 + segments + i] = new Vector3(x, -height * .5f, z);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int t0 = 2 + i, t1 = 2 + next;
            int b0 = 2 + segments + i, b1 = 2 + segments + next;

            // Top
            tris.Add(0); tris.Add(t0); tris.Add(t1);
            // Bottom
            tris.Add(1); tris.Add(b1); tris.Add(b0);
            // Side
            tris.Add(t0); tris.Add(b0); tris.Add(b1);
            tris.Add(t0); tris.Add(b1); tris.Add(t1);
        }

        return new Mesh { vertices = verts, triangles = tris.ToArray() };
    }

    // ──────────────────── Утилиты ────────────────────

    private float RngRange(float min, float max)
    {
        return (float)(_rng.NextDouble() * (max - min) + min);
    }

    // ──────────────────── Gizmos ────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
        Vector3 s = transform.lossyScale;
        float sx = PlaneUnitSize * s.x * (1f - _edgePadding);
        float sz = PlaneUnitSize * s.z * (1f - _edgePadding);

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(sx, 0.01f, sz));
    }
#endif
}