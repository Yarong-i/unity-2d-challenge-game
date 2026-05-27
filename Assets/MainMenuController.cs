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
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text fullscreenLabel;

    public static bool HasGameStarted { get; private set; }
    public static bool IsShowingMenu { get; private set; }
    public static bool BlocksPauseInput => IsShowingMenu && !HasGameStarted;

    private void Awake()
    {
        HasGameStarted = false;
        IsShowingMenu = true;
        GameSettingsController.Initialize();
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

        GameSettingsController.SyncControls(fullscreenToggle, volumeSlider, fullscreenLabel);
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
            fullscreenToggle = CreateToggle(settingsPanel.transform, "FullscreenToggle", "Fullscreen", new Vector2(0f, 48f));
            volumeSlider = CreateSlider(settingsPanel.transform, "VolumeSlider", "Volume", new Vector2(0f, -28f));
            CreateLabel(settingsPanel.transform, "FullscreenHintText", GameSettingsController.FullscreenHintText, 18f, new Vector2(0f, -78f), new Vector2(520f, 36f));
            backButton = CreateButton(settingsPanel.transform, "BackButton", "Back", new Vector2(0f, -138f));
        }

        CacheMainMenuButtons();
        CacheSettingsButtons();
        GameSettingsController.SyncControls(fullscreenToggle, volumeSlider, fullscreenLabel);
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

        if (fullscreenToggle == null)
            fullscreenToggle = FindToggle(settingsPanel.transform, "FullscreenToggle");

        if (fullscreenLabel == null)
            fullscreenLabel = FindText(settingsPanel.transform, "FullscreenToggle/Label");

        if (volumeSlider == null)
            volumeSlider = FindSlider(settingsPanel.transform, "VolumeSlider");
    }

    private Button FindButton(Transform parent, string buttonName)
    {
        Transform buttonTransform = parent.Find(buttonName);
        return buttonTransform == null ? null : buttonTransform.GetComponent<Button>();
    }

    private Toggle FindToggle(Transform parent, string toggleName)
    {
        Transform toggleTransform = parent.Find(toggleName);
        return toggleTransform == null ? null : toggleTransform.GetComponent<Toggle>();
    }

    private Slider FindSlider(Transform parent, string sliderName)
    {
        Transform sliderTransform = parent.Find(sliderName);
        return sliderTransform == null ? null : sliderTransform.GetComponent<Slider>();
    }

    private TMP_Text FindText(Transform parent, string textPath)
    {
        Transform textTransform = parent.Find(textPath);
        return textTransform == null ? null : textTransform.GetComponent<TMP_Text>();
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

    private Toggle CreateToggle(Transform parent, string objectName, string labelText, Vector2 anchoredPosition)
    {
        GameObject toggleObject = new GameObject(objectName, typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);

        var toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
        toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
        toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.anchoredPosition = anchoredPosition;
        toggleRect.sizeDelta = new Vector2(360f, 48f);

        GameObject boxObject = new GameObject("CheckmarkBox", typeof(RectTransform), typeof(Image));
        boxObject.transform.SetParent(toggleObject.transform, false);

        var boxRect = boxObject.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(36f, 36f);

        var boxImage = boxObject.GetComponent<Image>();
        boxImage.color = new Color(1f, 1f, 1f, 0.94f);

        GameObject checkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkObject.transform.SetParent(boxObject.transform, false);

        var checkRect = checkObject.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.anchoredPosition = Vector2.zero;
        checkRect.sizeDelta = new Vector2(22f, 22f);

        var checkImage = checkObject.GetComponent<Image>();
        checkImage.color = new Color(0.15f, 0.35f, 0.95f, 1f);

        fullscreenLabel = CreateSettingLabel(toggleObject.transform, "Label", GameSettingsController.FullscreenLabelText, new Vector2(62f, 0f), new Vector2(280f, 44f), TextAlignmentOptions.MidlineLeft);

        var toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = Screen.fullScreen;

        return toggle;
    }

    private Slider CreateSlider(Transform parent, string objectName, string labelText, Vector2 anchoredPosition)
    {
        GameObject containerObject = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
        containerObject.transform.SetParent(parent, false);

        var containerRect = containerObject.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = anchoredPosition;
        containerRect.sizeDelta = new Vector2(420f, 56f);

        CreateSettingLabel(containerObject.transform, "Label", labelText, new Vector2(0f, 0f), new Vector2(120f, 44f), TextAlignmentOptions.MidlineLeft);

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(containerObject.transform, false);

        var backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.offsetMin = new Vector2(128f, -6f);
        backgroundRect.offsetMax = new Vector2(0f, 6f);

        var backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(1f, 1f, 1f, 0.32f);

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObject.transform.SetParent(containerObject.transform, false);

        var fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = backgroundRect.anchorMin;
        fillAreaRect.anchorMax = backgroundRect.anchorMax;
        fillAreaRect.offsetMin = backgroundRect.offsetMin;
        fillAreaRect.offsetMax = backgroundRect.offsetMax;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(fillAreaObject.transform, false);

        var fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.15f, 0.55f, 0.95f, 1f);

        GameObject handleAreaObject = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaObject.transform.SetParent(containerObject.transform, false);

        var handleAreaRect = handleAreaObject.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = backgroundRect.anchorMin;
        handleAreaRect.anchorMax = backgroundRect.anchorMax;
        handleAreaRect.offsetMin = backgroundRect.offsetMin;
        handleAreaRect.offsetMax = backgroundRect.offsetMax;

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(handleAreaObject.transform, false);

        var handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24f, 24f);

        var handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.96f);

        var slider = containerObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = GameSettingsController.Volume;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private TMP_Text CreateSettingLabel(Transform parent, string objectName, string text, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = size;

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 26f;
        label.color = Color.white;
        label.alignment = alignment;
        label.raycastTarget = false;

        return label;
    }

    private void OnFullscreenChanged(bool fullscreen)
    {
        GameSettingsController.SetFullscreen(fullscreen);
        GameSettingsController.SyncControls(fullscreenToggle, volumeSlider, fullscreenLabel);
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

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(GameSettingsController.SetVolume);
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

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(GameSettingsController.SetVolume);
    }
}
