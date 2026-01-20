using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace TimerMod.Triggers;

public class CollisionTrigger : Trigger
{
    private bool shouldActivate = false;
    private string scene;

    private GameObject? triggerObject;
    private Vector2 pos;
    private Vector2 size;
    Color colour;

    public CollisionTrigger(Vector2 pos, Vector2 size, Color colour)
    {
        this.pos = pos;
        this.size = size;
        this.colour = colour;

        scene = GameManager.instance.GetSceneNameString();

        createSelf();
    }

    private void onActiveSceneChanged(Scene from, Scene to)
    {
        if (to.name == scene)
        {
            destroy();
            createSelf();
        }
    }

    private void createSelf()
    {
        SceneManager.activeSceneChanged += onActiveSceneChanged;

        Vector3[] vertices = new Vector3[] {
            new Vector3(pos.x, pos.y),
            new Vector3(pos.x, pos.y + size.y),
            new Vector3(pos.x + size.x, pos.y),
            new Vector3(pos.x + size.x, pos.y + size.y)
        };

        Mesh objectMesh = new Mesh();
        objectMesh.vertices = vertices;
        objectMesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        objectMesh.triangles = new int[] {0, 1, 2, 2, 1, 3};
        objectMesh.RecalculateNormals();

        triggerObject = new GameObject("TimerModTrigger");
        triggerObject.AddComponent<MeshFilter>().mesh = objectMesh;

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, colour);
        texture.Apply();

        MeshRenderer meshRenderer = triggerObject.AddComponent<MeshRenderer>();
        // meshRenderer.material.shader = Shader.Find("Particles/Multiply");
        Console.WriteLine(meshRenderer.material.shader);
        meshRenderer.material.mainTexture = texture;
        meshRenderer.material.color = colour;

        TriggerCollider c = triggerObject.AddComponent<TriggerCollider>();
        c.callback = setPlayerIn;

        triggerObject.AddComponent<BoxCollider2D>().isTrigger = true;
        triggerObject.SetActive(true);
    }

    public void setPlayerIn()
    {
        shouldActivate = true;
    }

    public bool active()
    {
        if (shouldActivate)
        {
            shouldActivate = false;
            return true;
        }

        return false;
    }

    public void destroy()
    {
        GameObject.Destroy(triggerObject);
        SceneManager.activeSceneChanged -= onActiveSceneChanged;
    }

}

class TriggerCollider : MonoBehaviour
{
    public Action? callback;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == HeroController.instance.gameObject)
        {
            if (callback != null)
            {
                callback();
            }
        }
    }
}
