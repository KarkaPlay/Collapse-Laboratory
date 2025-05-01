using UnityEngine;

public class GameDebug : MonoBehaviour
{
    public static void Log(object message)
    {
#if UNITY_EDITOR
            Debug.Log(message);
#endif
    }

    public static void LogWarning(object message)
    {
#if UNITY_EDITOR
            Debug.LogWarning(message);
#endif
    }

    public static void LogError(object message)
    {
#if UNITY_EDITOR
            Debug.LogError(message);
#endif
    }

    public static void DrawLine(Vector3 start, Vector3 end)
    {
#if UNITY_EDITOR
            Debug.DrawLine(start, end);
#endif
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
#if UNITY_EDITOR
            Debug.DrawLine(start, end, color);
#endif
    }

    public static void DrawRay(Vector3 start, Vector3 direction)
    {
#if UNITY_EDITOR
            Debug.DrawRay(start, direction);
#endif
    }

    public static void DrawRay(Vector3 start, Vector3 direction, Color color)
    {
#if UNITY_EDITOR
            Debug.DrawRay(start, direction, color);
#endif
    }
}
