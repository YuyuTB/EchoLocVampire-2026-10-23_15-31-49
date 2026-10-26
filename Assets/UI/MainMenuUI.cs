using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

public class MainMenuUI : MonoBehaviour
{
    [Header("Configuration du Jeu")]
    [Tooltip("Glissez votre scène de jeu ici en Drag & Drop !")]
#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset gameSceneAsset;
#endif
    [SerializeField] private string gameSceneName;

    private VisualElement root;
    private VisualElement mainPanel;
    private VisualElement controlsPanel;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (gameSceneAsset != null)
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(gameSceneAsset);
            gameSceneName = System.IO.Path.GetFileNameWithoutExtension(path);
        }
#endif
    }

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        // Récupération des panneaux
        mainPanel = root.Q<VisualElement>("MainPanel");
        controlsPanel = root.Q<VisualElement>("ControlsPanel");

        // Boutons
        SetupButton("PlayButton", OnPlayClicked);
        SetupButton("QuitButton", OnQuitClicked);
        SetupButton("StartGameButton", OnStartGameClicked);
        SetupButton("BackButton", OnBackClicked);

        // État initial
        ShowMainPanel();

        // Sécurité curseur et temps
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetupButton(string buttonName, System.Action clickAction)
    {
        Button btn = root.Q<Button>(buttonName);
        if (btn != null)
        {
            btn.clicked += clickAction;
        }
    }

    private void OnPlayClicked()
    {
        if (mainPanel != null) mainPanel.style.display = DisplayStyle.None;
        if (controlsPanel != null) controlsPanel.style.display = DisplayStyle.Flex;
    }

    private void OnBackClicked()
    {
        if (mainPanel != null) mainPanel.style.display = DisplayStyle.Flex;
        if (controlsPanel != null) controlsPanel.style.display = DisplayStyle.None;
    }

    private void OnStartGameClicked()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Aucune scène de jeu n'est assignée dans l'inspecteur !");
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("Fermeture du jeu...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.style.display = DisplayStyle.Flex;
        if (controlsPanel != null) controlsPanel.style.display = DisplayStyle.None;
    }
}