using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;

    public static bool HasGameStarted { get; private set; }
    public static bool IsShowingMenu { get; private set; }
    public static bool BlocksPauseInput => IsShowingMenu && !HasGameStarted;

    private void Awake()
    {
        HasGameStarted = false;
        IsShowingMenu = true;
        EnsureMenuUI();
    }

    private void Start()
    {
        ShowMainMenu();
        Time.timeScale = 0f;
    }

    private void OnEnable()
    {
        AddButtonListeners();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();

        if (IsShowingMenu && !HasGameStarted)
            Time.timeScale = 1f;
    }

    public void StartGame()
    {
        RunTimerUI timer = FindFirstObjectByType<RunTimerUI>();
        if (timer != null)
        {
            timer.ResetTimer();
            timer.StartTimer();
        }

        Time.timeScale = 1f;
        HasGameStarted = true;
        IsShowingMenu = false;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        IsShowingMenu = true;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        IsShowingMenu = true;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void EnsureMenuUI()
    {
        if (mainMenuPanel == null)
            mainMenuPanel = GameObject.Find("MainMenuPanel");

        if (settingsPanel == null)
            settingsPanel = GameObject.Find("SettingsPanel");

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            canvas = CreateRuntimeCanvas();

        EnsureEventSystem();

        if (mainMenuPanel == null)
        {
            mainMenuPanel = CreateFullScreenPanel(canvas.transform, "MainMenuPanel", new Color(0.04f, 0.05f, 0.06f, 0.88f));
            CreateLabel(mainMenuPanel.transform, "TitleText", "CLIMB CHALLENGE", 52f, new Vector2(0f, 150f), new Vector2(620f, 84f));
            startButton = CreateButton(mainMenuPanel.transform, "StartButton", "Start", new Vector2(0f, 42f));
            settingsButton = CreateButton(mainMenuPanel.transform, "SettingsButton", "Settings", new Vector2(0f, -38f));
            quitButton = CreateButton(mainMenuPanel.transform, "QuitButton", "Quit", new Vector2(0f, -118f));
        }

        if (settingsPanel == null)
        {
            settingsPanel = CreateFullScreenPanel(canvas.transform, "SettingsPanel", new Color(0.04f, 0.05f, 0.06f, 0.88f));
            CreateLabel(settingsPanel.transform, "SettingsTitleText", "SETTINGS", 46f, new Vector2(0f, 140f), new Vector2(420f, 76f));
            CreateLabel(settingsPanel.transform, "SettingsPlaceholderText", "Settings options will be added later.", 26f, new Vector2(0f, 42f), new Vector2(620f, 56f));
            backButton = CreateButton(settingsPanel.transform, "BackButton", "Back", new Vector2(0f, -88f));
        }

        CacheMainMenuButtons();
        CacheSettingsButtons();
    }

    private void CacheMainMenuButtons()
    {
        if (startButton == null)
            startButton = FindButton(mainMenuPanel.transform, "StartButton");

        if (settingsButton == null)
            settingsButton = FindButton(mainMenuPanel.transform, "SettingsButton");

        if (quitButton == null)
            quitButton = FindButton(mainMenuPanel.transform, "QuitButton");
    }

    private void CacheSettingsButtons()
    {
        if (settingsPanel == null)
            return;

        if (backButton == null)
            backButton = FindButton(settingsPanel.transform, "BackButton");
    }

    private Button FindButton(Transform parent, string buttonName)
    {
        Transform buttonTransform = parent.Find(buttonName);
        return buttonTransform == null ? null : buttonTransform.GetComponent<Button>();
    }

    private Canvas CreateRuntimeCanvas()
    {
        GameObject canvasObject = new GameObject(
            "RuntimeUI",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private GameObject CreateFullScreenPanel(Transform parent, string objectName, Color color)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panelObject.GetComponent<Image>();
        panelImage.color = color;

        return panelObject;
    }

    private void CreateLabel(Transform parent, string objectName, string text, float fontSize, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = size;

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private Button CreateButton(Transform parent, string objectName, string buttonText, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(260f, 58f);

        var buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.94f);

        GameObject labelObject = new GameObject("Text", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = buttonText;
        label.fontSize = 28f;
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return buttonObject.GetComponent<Button>();
    }

    private void AddButtonListeners()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(ShowSettings);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (backButton != null)
            backButton.onClick.AddListener(ShowMainMenu);
    }

    private void RemoveButtonListeners()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(ShowSettings);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);

        if (backButton != null)
            backButton.onClick.RemoveListener(ShowMainMenu);
    }
}
