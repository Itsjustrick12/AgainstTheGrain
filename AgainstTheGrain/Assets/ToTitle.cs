using UnityEngine;
using UnityEngine.SceneManagement;

public class ToTitleScreen : MonoBehaviour
{
    public void LoadTitleScreen()
    {
        SceneManager.LoadScene(0);
    }
}