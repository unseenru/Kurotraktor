using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private string gameSceneName = "2_Base";

    [Header("Replace these later")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite logoSprite;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.11f, 0.08f, 1f);
    [SerializeField] private Color panelColor = new Color(0.05f, 0.07f, 0.05f, 0.88f);
    [SerializeField] private Color buttonColor = new Color(0.22f, 0.38f, 0.20f, 1f);
    [SerializeField] private Color accentColor = new Color(0.91f, 0.72f, 0.25f, 1f);

    private GameObject mainPanel;
    private GameObject settingsPanel;
    private Font font;

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildMenu();
    }

    public void Play()
    {
        if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogError($"Scene '{gameSceneName}' is missing from Build Settings.", this);
    }

    public void OpenSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildMenu()
    {
        CreateEventSystem();

        GameObject canvasObject = new GameObject("Main Menu Canvas", typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = CreateImage("Background Image Slot", canvasObject.transform,
            backgroundSprite, backgroundColor);
        Stretch(background.rectTransform);

        GameObject content = CreateRect("Safe Area", canvasObject.transform);
        Stretch(content.GetComponent<RectTransform>(), 70f);

        mainPanel = CreatePanel("Main Panel", content.transform);
        RectTransform mainRect = mainPanel.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0f, 0.5f);
        mainRect.anchorMax = new Vector2(0f, 0.5f);
        mainRect.pivot = new Vector2(0f, 0.5f);
        mainRect.sizeDelta = new Vector2(540f, 760f);
        mainRect.anchoredPosition = new Vector2(60f, 0f);

        VerticalLayoutGroup layout = mainPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(55, 55, 50, 50);
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;

        CreateLogo(mainPanel.transform);
        CreateText("Title", mainPanel.transform, "KUROTRAKTOR", 54, accentColor, 90f);
        CreateText("Subtitle", mainPanel.transform, "ФЕРМЕРСКОЕ ПРИКЛЮЧЕНИЕ", 20,
            new Color(0.82f, 0.84f, 0.76f), 42f);
        CreateSpacer(mainPanel.transform, 24f);
        CreateButton("Play Button", mainPanel.transform, "ИГРАТЬ", Play);
        CreateButton("Settings Button", mainPanel.transform, "НАСТРОЙКИ", OpenSettings);
        CreateButton("Quit Button", mainPanel.transform, "ВЫХОД", Quit);
        CreateSpacer(mainPanel.transform, 12f);
        CreateText("Version", mainPanel.transform, $"v{Application.version}", 17,
            new Color(0.65f, 0.68f, 0.61f), 30f);

        BuildSettingsPanel(content.transform);
    }

    private void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = CreatePanel("Settings Panel", parent);
        RectTransform rect = settingsPanel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(620f, 560f);

        VerticalLayoutGroup layout = settingsPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(65, 65, 55, 55);
        layout.spacing = 28f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;

        CreateText("Settings Title", settingsPanel.transform, "НАСТРОЙКИ", 42, accentColor, 75f);
        CreateText("Volume Label", settingsPanel.transform, "ГРОМКОСТЬ", 21, Color.white, 38f);

        Slider slider = CreateSlider(settingsPanel.transform);
        slider.value = AudioListener.volume;
        slider.onValueChanged.AddListener(value => AudioListener.volume = value);

        CreateSpacer(settingsPanel.transform, 45f);
        CreateText("Settings Placeholder", settingsPanel.transform,
            "Здесь позже можно добавить графику, управление и другие параметры",
            18, new Color(0.72f, 0.75f, 0.69f), 70f);
        CreateButton("Back Button", settingsPanel.transform, "НАЗАД", CloseSettings);
        settingsPanel.SetActive(false);
    }

    private void CreateLogo(Transform parent)
    {
        Image logo = CreateImage("Logo Image Slot", parent, logoSprite, Color.white);
        LayoutElement element = logo.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = logoSprite == null ? 10f : 170f;
        logo.preserveAspect = true;
        logo.enabled = logoSprite != null;
    }

    private Button CreateButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateRect(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = buttonColor;
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = buttonColor * 1.22f;
        colors.pressedColor = buttonColor * 0.78f;
        button.colors = colors;
        button.onClick.AddListener(action);

        LayoutElement element = buttonObject.AddComponent<LayoutElement>();
        element.preferredHeight = 78f;
        CreateText("Label", buttonObject.transform, label, 27, Color.white, 78f);
        Stretch(buttonObject.transform.GetChild(0).GetComponent<RectTransform>());
        return button;
    }

    private Slider CreateSlider(Transform parent)
    {
        GameObject root = CreateRect("Volume Slider", parent);
        root.AddComponent<LayoutElement>().preferredHeight = 48f;
        Slider slider = root.AddComponent<Slider>();

        Image background = CreateImage("Background", root.transform, null, new Color(0.13f, 0.16f, 0.13f));
        RectTransform backgroundRect = background.rectTransform;
        Stretch(backgroundRect);
        backgroundRect.offsetMin = new Vector2(0f, 15f);
        backgroundRect.offsetMax = new Vector2(0f, -15f);

        GameObject fillArea = CreateRect("Fill Area", root.transform);
        Stretch(fillArea.GetComponent<RectTransform>(), 8f);
        Image fill = CreateImage("Fill", fillArea.transform, null, accentColor);
        Stretch(fill.rectTransform);

        Image handle = CreateImage("Handle", root.transform, null, accentColor);
        handle.rectTransform.sizeDelta = new Vector2(34f, 34f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        return slider;
    }

    private Text CreateText(string name, Transform parent, string value, int size, Color color, float height)
    {
        GameObject textObject = CreateRect(name, parent);
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        textObject.AddComponent<LayoutElement>().preferredHeight = height;
        return text;
    }

    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = CreateRect(name, parent);
        panel.AddComponent<Image>().color = panelColor;
        return panel;
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = CreateRect(name, parent);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = sprite != null;
        return image;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        CreateRect("Spacer", parent).AddComponent<LayoutElement>().preferredHeight = height;
    }

    private void CreateEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(transform, false);
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
