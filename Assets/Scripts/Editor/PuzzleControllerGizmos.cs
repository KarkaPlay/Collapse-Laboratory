#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    /// <summary>
    /// Gizmos для PuzzleController — показывает условия головоломки.
    /// </summary>
    [InitializeOnLoad]
    public static class PuzzleControllerGizmos
    {
        static PuzzleControllerGizmos()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            var allPuzzles = Object.FindObjectsByType<PuzzleController>(FindObjectsSortMode.None);

            foreach (var puzzle in allPuzzles)
            {
                if (puzzle == null) continue;

                DrawPuzzleGizmo(puzzle);
            }
        }

        private static void DrawPuzzleGizmo(PuzzleController puzzle)
        {
            var position = puzzle.transform.position;
            bool isSelected = Selection.activeGameObject == puzzle.gameObject;

            // Цвет по статусу решения
            Color puzzleColor = puzzle.IsSolved
                ? new Color(0.2f, 1f, 0.3f, 0.8f)
                : new Color(1f, 0.8f, 0.2f, 0.8f);

            Handles.color = puzzleColor;

            // Иконка головоломки (сфера)
            Handles.DrawWireCube(position, Vector3.one * 0.6f);

            // Линии к условиям
            if (puzzle.conditions != null)
            {
                foreach (var condition in puzzle.conditions)
                {
                    if (condition.target == null) continue;

                    Color lineColor = condition.IsSatisfied
                        ? new Color(0.2f, 1f, 0.3f, 0.6f)
                        : new Color(1f, 0.3f, 0.3f, 0.6f);

                    Handles.color = lineColor;

                    if (isSelected)
                    {
                        Handles.DrawDottedLine(position, condition.target.transform.position, 3f);

                        // Метка у цели
                        string label = condition.IsSatisfied ? "✓" : $"Need: {condition.requiredState}";
                        GUIStyle style = new GUIStyle
                        {
                            normal = { textColor = lineColor },
                            fontSize = 11,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter
                        };

                        Handles.Label(
                            condition.target.transform.position + Vector3.up * 0.8f,
                            label, style);
                    }
                    else
                    {
                        Handles.DrawDottedLine(position, condition.target.transform.position, 1.5f);
                    }
                }
            }

            // Подпись
            if (isSelected)
            {
                GUIStyle nameStyle = new GUIStyle
                {
                    normal = { textColor = puzzleColor },
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                int satisfied = puzzle.conditions?.Count(c => c.IsSatisfied) ?? 0;
                int total = puzzle.conditions?.Count ?? 0;

                Handles.Label(
                    position + Vector3.up * 0.9f,
                    $"🧩 {puzzle.puzzleName}\n{satisfied}/{total}",
                    nameStyle);
            }
        }
    }
}
#endif