using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
        
        public void ExitGame() => Application.Quit();
    }
}