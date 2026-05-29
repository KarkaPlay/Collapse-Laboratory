using UnityEngine;

[CreateAssetMenu(fileName = "New Readable Data", menuName = "Collapse Lab/Readable Data")]
public class ReadableData : ScriptableObject
{
    [System.Serializable]
    public class Page
    {
        [TextArea(10, 20)]
        public string text;
        public Sprite pageImage; // Опционально, если на странице есть уникальная картинка
    }

    public string title;
    public Sprite mainImage; // Изображение самого носителя (книги, планшета)
    public Page[] pages;
}
