using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Универсальный базовый класс для реализации синглтона на основе MonoBehaviour.
    /// </summary>
    /// <typeparam name="T">Тип класса, который будет синглтоном.</typeparam>
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        private static T _instance;
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    // Если объект все еще не найден (возможна ситуация, когда вызывается из другого Awake)
                    if (_instance == null)
                    {
                        Debug.LogError($"Синглтон типа {typeof(T).Name} не найден на сцене. Пожалуйста, добавьте его.");
                    }
                }
                return _instance;
            }
        }
        
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"Найден второй экземпляр синглтона {typeof(T).Name}. Текущий объект будет уничтожен.");
                Destroy(gameObject);
            }
            else if (_instance == null)
            {
                _instance = (T)this;
            
                // Опционально: предотвращаем уничтожение объекта при загрузке новых сцен
                // DontDestroyOnLoad(this.gameObject); 
            }
        }
    }
}