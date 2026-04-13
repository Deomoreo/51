using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class RawImageUVScroll : MonoBehaviour
{
    public Vector2 uvTiling = new Vector2(2.5f, 2.5f);
    public Vector2 speed = new Vector2(0.004f, 0.001f);
    public bool unscaledTime = true;

    RawImage img;

    void Awake()
    {
        img = GetComponent<RawImage>();
        var r = img.uvRect;      // texture coordinates rectangle [web:78]
        r.size = uvTiling;       // tiling dentro la UI (più alto = più ripetizioni)
        img.uvRect = r;
    }

    void Update()
    {
        float dt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        var r = img.uvRect;
        r.position += speed * dt; // offset UV
        img.uvRect = r;
    }
}
