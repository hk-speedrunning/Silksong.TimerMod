using UnityEngine;

namespace TimerMod.Triggers;

public class SceneTrigger : Trigger
{
    private string targetSceneName;
    private string prevSceneName;

    public SceneTrigger(string scene)
    {
        targetSceneName = scene;
    }

    public bool active()
    {
        if (!GameManager.instance)
        {
            return false;
        }

        if (GameManager.instance.GetSceneNameString() == targetSceneName
                && targetSceneName != prevSceneName)
        {
            return true;
        }

        prevSceneName = GameManager.instance.GetSceneNameString();

        return false;
    }

    public void destroy() {}
}

