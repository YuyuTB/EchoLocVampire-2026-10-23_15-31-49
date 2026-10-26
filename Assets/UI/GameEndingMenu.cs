using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Nécessaire pour l'EventSystem

public class GameEndingMenu : MonoBehaviour
{
    private void Start()
    {
        // Rendre le curseur visible et libre de bouger
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Méthode liée au bouton Return to Main Menu
    public void ReturnToMainMenu()
    {
        // Désélectionne le bouton pour éviter le bug interne UGUI (Selectable.OnDisable)
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        Time.timeScale = 1f; // Remet le temps à la normale
        SceneManager.LoadScene("MainMenu"); // Remplace par le nom exact de ta scène de menu principal
    }
}