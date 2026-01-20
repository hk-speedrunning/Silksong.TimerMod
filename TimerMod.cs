using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;

using GlobalEnums;
using System;

namespace TimerMod;

public class Keybinds
{
    public ConfigEntry<KeyboardShortcut> SetStart;
    public ConfigEntry<KeyboardShortcut> SetEnd;
    public ConfigEntry<KeyboardShortcut> ToggleTriggerMethod;
    public ConfigEntry<KeyboardShortcut> CancelTimer;
    public ConfigEntry<KeyboardShortcut> StartTimer;
    public ConfigEntry<KeyboardShortcut> EndTimer;
    public ConfigEntry<KeyboardShortcut> ResetPb;
    public ConfigEntry<KeyboardShortcut> ToggleTimerVisibility;

    public Keybinds(ConfigFile Config)
    {
        SetStart = Config.Bind("Shortcuts", "SetStart", new KeyboardShortcut(KeyCode.F8), "");
        SetEnd = Config.Bind("Shortcuts", "SetEnd", new KeyboardShortcut(KeyCode.F9), "");
        ToggleTriggerMethod = Config.Bind("Shortcuts", "ToggleTriggerMethod", new KeyboardShortcut(KeyCode.F10), "");
        CancelTimer = Config.Bind("Shortcuts", "CancelTimer", new KeyboardShortcut(KeyCode.None), "Cancel the timer (does not affect pb)");
        StartTimer = Config.Bind("Shortcuts", "StartTimer", new KeyboardShortcut(KeyCode.None), "");
        EndTimer = Config.Bind("Shortcuts", "EndTimer", new KeyboardShortcut(KeyCode.None), "");
        ResetPb = Config.Bind("Shortcuts", "ResetPb", new KeyboardShortcut(KeyCode.None), "");
        ToggleTimerVisibility = Config.Bind("Shortcuts", "ToggleTimerVisibility", new KeyboardShortcut(KeyCode.None), "");
    }
}

[BepInDependency(DependencyGUID: "org.silksong-modding.modlist")]
[BepInAutoPlugin(id: "io.github.hk-speedrunning.timermod")]
public partial class TimerMod : BaseUnityPlugin
{
    public static ConfigFile config;

    private TimerDisplay timerDisplay;

    private const string MENU_TITLE = "Menu_Title";
    private const string QUIT_TO_MENU = "Quit_To_Menu";

    private Triggers.Trigger startTrigger = new Triggers.SceneTrigger("");
    private Triggers.Trigger endTrigger = new Triggers.SceneTrigger("");

    private Keybinds keybinds;
    private bool usingSceneTriggers = false;
    private double time = 0.0;

    private bool timerPaused = true;

    private GameState prevGameState = GameState.PLAYING;
    private bool lookForTele = false;

    private double[] history = new double[5];
    private int history_num = 0;
    private double pb = 0;

    private int funSceneCount = 0;

    private ConfigEntry<bool> showSpeed;
    private Vector2 startPos;

    private bool ShouldTickTimer()
    {
        if (timerPaused)
        {
            return false;
        }

        UIState ui_state = GameManager.instance.ui.uiState;
        string scene_name = GameManager.instance.GetSceneNameString();
        string next_scene = GameManager.instance.nextSceneName;

        bool loading_menu = (scene_name != MENU_TITLE && next_scene == "")
            || (scene_name != MENU_TITLE && next_scene == MENU_TITLE || scene_name == QUIT_TO_MENU);

        GameState game_state = GameManager.instance.GameState;


        if (game_state == GameState.PLAYING && prevGameState == GameState.MAIN_MENU)
        {
            lookForTele = true;
        }

        if (lookForTele && (game_state != GameState.PLAYING && game_state != GameState.ENTERING_LEVEL))
        {
            lookForTele = false;
        }

        bool accepting_input = GameManager.instance.inputHandler.acceptingInput;
        HeroTransitionState hero_transition_state;
        try
        {
            hero_transition_state = GameManager.instance.hero_ctrl.transitionState;
        }
        catch (Exception e)
        {
            hero_transition_state = HeroTransitionState.WAITING_TO_TRANSITION;
        }

        bool scene_load_activation_allowed = false;
        if (GameManager.instance.sceneLoad != null)
        {
            scene_load_activation_allowed = GameManager.instance.sceneLoad.IsActivationAllowed;
        }

        // big thing
        bool r0 = (lookForTele);
        bool r1 = ((game_state == GameState.PLAYING || game_state == GameState.ENTERING_LEVEL)
                    && ui_state != UIState.PLAYING);
        bool r2 = (game_state != GameState.PLAYING && game_state != GameState.CUTSCENE && !accepting_input);
        bool r3 = ((game_state == GameState.EXITING_LEVEL && scene_load_activation_allowed)
                    || game_state == GameState.LOADING);
        bool r4 = (hero_transition_state == HeroTransitionState.WAITING_TO_ENTER_LEVEL);
        bool r5 = (ui_state != UIState.PLAYING
                    && (loading_menu
                        || (ui_state != UIState.PAUSED && ui_state != UIState.CUTSCENE && !(next_scene == "")))
                    && next_scene != scene_name);

        bool is_game_time_paused = r0 || r1 || r2 || r3 || r4 || r5;
        if (is_game_time_paused)
        {
            // Logger.LogInfo($"{r0}{r1}{r2}{r3}{r4}{r5} {next_scene}");
        }

        prevGameState = game_state;
        return !is_game_time_paused;
    }

    private void resetPb()
    {
        pb = 0;
        timerDisplay.setPbTime(getTimeText(pb));
    }

    private void endTimer()
    {
        Vector2 endPos = HeroController.instance.gameObject.transform.position;
        double speedx = Math.Abs(startPos.x - endPos.x) / time;
        double speedy = Math.Abs(startPos.y - endPos.y) / time;

        history[history_num] = time;
        history_num += 1;
        history_num %= 5;

        // Logger.LogInfo($"Timed: {getTimeText(time)}");
        if (pb == 0 || time < pb)
        {
            pb = time;
            // Logger.LogInfo($"Got a pb: {getTimeText(time)}");

            timerDisplay.setPbTime(getTimeText(pb));
            if (showSpeed.Value)
                timerDisplay.setPbTime($"{getTimeText(pb)}\n{speedx:0.00} u/s, {speedy:0.00} u/s");
        }
        timerPaused = true;
    }

    private void startTimer()
    {
        time = 0;
        timerPaused = false;

        startPos = HeroController.instance.gameObject.transform.position;

        Logger.LogInfo("Started timer");
    }

    private void LateUpdate()
    {
        if (startTrigger.active() && timerPaused)
        {
            startTimer();
        }

        if (endTrigger.active() && !timerPaused)
        {
            endTimer();
        }

        if (keybinds.SetStart.Value.IsDown())
        {
            startTrigger.destroy();
            resetPb();

            if (usingSceneTriggers)
            {
                startTrigger = new Triggers.SceneTrigger(SceneManager.GetActiveScene().name);
                Logger.LogInfo("Set start scene");
            }
            else
            {
                startTrigger = new Triggers.CollisionTrigger(GameManager.instance.hero_ctrl.transform.position,
                        new Vector2(0.35f, 0.35f), new Color(0.1f, 0.4f, 0.1f));
                Logger.LogInfo("Set start pos");
            }
        }
        if (keybinds.SetEnd.Value.IsDown())
        {
            endTrigger.destroy();
            resetPb();

            if (usingSceneTriggers)
            {
                endTrigger = new Triggers.SceneTrigger(SceneManager.GetActiveScene().name);
                Logger.LogInfo("Set end scene");
            }
            else
            {
                endTrigger = new Triggers.CollisionTrigger(GameManager.instance.hero_ctrl.transform.position,
                        new Vector2(0.35f, 0.35f), new Color(0.4f, 0.1f, 0.1f));
                Logger.LogInfo("Set end pos");
            }
        }

        if (keybinds.ToggleTriggerMethod.Value.IsDown())
        {
            usingSceneTriggers = !usingSceneTriggers;
        }
        if (keybinds.CancelTimer.Value.IsDown())
        {
            Logger.LogInfo("Canceled");
            time = 0;
            timerPaused = true;
        }
        if (keybinds.ResetPb.Value.IsDown())
            resetPb();
        if (keybinds.StartTimer.Value.IsDown())
            startTimer();
        if (keybinds.EndTimer.Value.IsDown())
            endTimer();
        if (keybinds.ToggleTimerVisibility.Value.IsDown())
            timerDisplay.toggleVisibility();

        if (ShouldTickTimer())
        {
            time += Time.unscaledDeltaTime;
            // Logger.LogInfo(getTimeText(time));

            timerDisplay.setTime(getTimeText(time));
        }
    }

    public void onActiveSceneChanged(Scene from, Scene to)
    {
        if (funSceneCount == 3)
            timerDisplay = new TimerDisplay();

        funSceneCount++;
    }

    private string getTimeText(double t)
    {
        int milis = (int)(t * 100) % 100;
        int seconds = (int)(t) % 60;
        int minutes = (int)(t) / 60;

        return $"{minutes}:{seconds:00}.{milis:00}";
    }

    private void Awake()
    {
        SceneManager.activeSceneChanged += onActiveSceneChanged;
        keybinds = new Keybinds(Config);
        config = Config;

        showSpeed = Config.Bind("UI", "Show Speed", false, "");
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }
}

