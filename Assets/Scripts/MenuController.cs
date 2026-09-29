using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    const string SelectedLevelKey = "SelectedLevelIndex";

    public void StartGame()
    {
        StartLevel(0);
    }

    public void StartLevel(int levelIndex)
    {
        if (levelIndex < 0)
        {
            Debug.LogError(
                "MenuController: Level index cannot be negative."
            );

            return;
        }

        PlayerPrefs.SetInt(SelectedLevelKey, levelIndex);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Ants");
    }
}