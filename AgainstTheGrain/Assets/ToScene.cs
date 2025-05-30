using UnityEngine;
using UnityEngine.SceneManagement;

public class ToScene : MonoBehaviour
{
    public int SceneNumber = 0;
    public void LoadScene()
    {
        SceneManager.LoadScene(SceneNumber);
    }
}