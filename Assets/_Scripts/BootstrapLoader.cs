using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BootstrapLoader : MonoBehaviour
{
    // —юда записываем индекс сцены, которую нужно загрузить
    public static int TargetSceneIndex = 1;

    [SerializeField] private float minWaitTime = 1.0f;
    private RectTransform _fillRect;

    private void Awake()
    {
        Time.timeScale = 1f;
        CreateBootstrapUI();
    }

    private void Start()
    {
        StartCoroutine(LoadSceneRoutine());
    }

    private void CreateBootstrapUI()
    {
        GameObject canvasGO = new GameObject("[BootstrapCanvas]");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject bgScreen = new GameObject("BG_Screen");
        bgScreen.transform.SetParent(canvasGO.transform, false);
        Image bgScreenImg = bgScreen.AddComponent<Image>();
        bgScreenImg.color = Color.black;
        RectTransform screenRect = bgScreen.GetComponent<RectTransform>();
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.sizeDelta = Vector2.zero;

        GameObject barBg = new GameObject("Bar_BG");
        barBg.transform.SetParent(canvasGO.transform, false);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform barBgRect = barBg.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        barBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        barBgRect.sizeDelta = new Vector2(500f, 30f);

        GameObject barFill = new GameObject("Bar_Fill");
        barFill.transform.SetParent(barBg.transform, false);
        Image barFillImg = barFill.AddComponent<Image>();
        barFillImg.color = new Color(0.2f, 0.8f, 0.3f, 1f);

        _fillRect = barFill.GetComponent<RectTransform>();
        _fillRect.anchorMin = new Vector2(0f, 0f);
        _fillRect.anchorMax = new Vector2(0f, 1f);
        _fillRect.pivot = new Vector2(0f, 0.5f);
        _fillRect.anchoredPosition = Vector2.zero;
        _fillRect.sizeDelta = new Vector2(0f, 0f);
    }

    private IEnumerator LoadSceneRoutine()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(TargetSceneIndex);
        if (op == null)
        {
            Debug.LogError($"[Bootstrap] —цена с индексом {TargetSceneIndex} не найдена!");
            yield break;
        }

        op.allowSceneActivation = false;
        float timer = 0f;

        while (timer < minWaitTime || op.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Min(timer / minWaitTime, op.progress / 0.9f);

            if (_fillRect != null)
            {
                _fillRect.sizeDelta = new Vector2(500f * progress, 0f);
            }

            yield return null;
        }

        if (_fillRect != null)
        {
            _fillRect.sizeDelta = new Vector2(500f, 0f);
        }

        op.allowSceneActivation = true;
    }
}