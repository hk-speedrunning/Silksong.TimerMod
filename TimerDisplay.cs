using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

namespace TimerMod;

public class TimerDisplay
{
    private GameObject canvas;

    private Text? text;
    private Text? pbText;
    private Font? theFont;

    private bool timerVisible = true;

    private ConfigEntry<int> timerTextSize;
    private ConfigEntry<int> pbTextSize;

    private ConfigEntry<Vector2> timerTextPos;
    private ConfigEntry<Vector2> pbTextPos;

    public TimerDisplay()
    {
        ConfigFile config = TimerMod.config;
        timerTextSize = config.Bind("UI", "Timer Text Size", 50, "");
        pbTextSize = config.Bind("UI", "Pb Text Size", 25, "");
        timerTextPos = config.Bind("UI", "Timer Text Position", new Vector2(0.03f, 0.04f), "");
        pbTextPos = config.Bind("UI", "Pb Text Position", new Vector2(0.03f, 0.09f), "");

        timerTextSize.SettingChanged += (_, _) => {setAllConfigs();};
        pbTextSize.SettingChanged += (_, _) => {setAllConfigs();};
        timerTextPos.SettingChanged += (_, _) => {setAllConfigs();};
        pbTextPos.SettingChanged += (_, _) => {setAllConfigs();};

        canvas = new GameObject("TimerModCanvas");
        UnityEngine.Object.DontDestroyOnLoad(canvas);
    }

    public void setup()
    {
        var allFonts = Resources.FindObjectsOfTypeAll<Font>();
        theFont = allFonts.FirstOrDefault(x => x.name == "TrajanPro-Regular");

        setupCanvas();

        text = createText("TimerText", "0:00.00", timerTextSize.Value, timerTextPos.Value);
        pbText = createText("TimerPBText", "PB: 0:00.00", pbTextSize.Value, pbTextPos.Value);
    }

    private void setAllConfigs()
    {
        if (text == null || pbText == null)
            return;

        text.fontSize = timerTextSize.Value;
        pbText.fontSize = pbTextSize.Value;

        updateTextPos(text, timerTextPos.Value);
        updateTextPos(pbText, pbTextPos.Value);
    }


    private void setupCanvas()
    {
        var canvasC = canvas.AddComponent<UnityEngine.Canvas>();
        canvasC.renderMode = RenderMode.ScreenSpaceOverlay;
        // canvasC.worldCamera = GameCameras.SilentInstance.hudCamera;
        canvasC.planeDistance = 20;
        canvas.layer = LayerMask.NameToLayer("UI");

        CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvas.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = new Vector2(0, 0);
        canvasRect.anchorMax = new Vector2(1, 1);
        canvasRect.sizeDelta = new Vector2(0, 0);
    }

    public Text createText(string name, string defaultText, int fontSize, Vector2 position)
    {
        GameObject textObject = new GameObject(name);
        textObject.layer = LayerMask.NameToLayer("UI");
        textObject.AddComponent<CanvasRenderer>();
        RectTransform textTransform = textObject.AddComponent<RectTransform>();
        textTransform.localPosition = Vector3.zero;

        CanvasGroup group = textObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        Text text = textObject.AddComponent<Text>();
        text.text = defaultText;
        text.font = theFont;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Normal;

        textObject.transform.SetParent(canvas.transform, false);

        updateTextPos(text, position);

        return text;
    }

    private void updateTextPos(Text t, Vector2 position)
    {
        RectTransform transform = t.gameObject.GetComponent<RectTransform>();


        // 4 combinations, there probably exists a better way but it is alright
        if (position.y > 0.5f)
        {
            if (position.x > 0.5f)
            {
                t.alignment = TextAnchor.UpperRight;
                transform.anchorMin = new Vector2(0f, 0f);
                transform.anchorMax = position;
            }
            else
            {
                t.alignment = TextAnchor.UpperLeft;
                transform.anchorMin = new Vector2(position.x, 0f);
                transform.anchorMax = new Vector2(1f, position.y);
            }
        }
        else
        {
            if (position.x > 0.5f)
            {
                t.alignment = TextAnchor.LowerRight;
                transform.anchorMin = new Vector2(0f, position.y);
                transform.anchorMax = new Vector2(position.x, 1f);
            }
            else
            {
                t.alignment = TextAnchor.LowerLeft;
                transform.anchorMin = position;
                transform.anchorMax = new Vector2(1f, 1f);
            }
        }
    }

    public void toggleVisibility()
    {
        canvas.SetActive(timerVisible);

        timerVisible = !timerVisible;
    }

    public void setTime(string time)
    {
        if (text == null)
            return;

        text.text = time;
    }

    public void setPbTime(string time)
    {
        if (pbText == null)
            return;

        pbText.text = "PB: " + time;
    }
}

