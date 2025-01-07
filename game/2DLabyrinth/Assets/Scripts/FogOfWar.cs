using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FogOfWar : MonoBehaviour
{
    [Header("Nebel-Einstellungen")]
    public int textureResolution = 512;
    [Range(0f, 1f)]
    public float centerTransparency = 0.0f;
    [Range(0f, 1f)]
    public float edgeTransparency = 0.8f;   
    public Color fogColor = Color.black;    
    public float fogRadius = 5f;             

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = CreateFogSprite();
        sr.sortingLayerName = "Fog"; 
        sr.sortingOrder = 100;      
        SetFogRadius(fogRadius);    
    }

    Sprite CreateFogSprite()
    {
        Texture2D tex = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(textureResolution / 2f, textureResolution / 2f);
        float maxDistance = Mathf.Min(textureResolution / 2f, textureResolution / 2f);

        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                Vector2 pixel = new Vector2(x, y);
                float distance = Vector2.Distance(pixel, center);
                float normalizedDistance = distance / maxDistance;

                float alpha = Mathf.Lerp(centerTransparency, edgeTransparency, normalizedDistance);
                alpha = Mathf.Clamp01(alpha);

                Color color = new Color(fogColor.r, fogColor.g, fogColor.b, alpha);
                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();

        Sprite fogSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return fogSprite;
    }

    void Update()
    {
    }
    
    public void SetFogRadius(float radius)
    {
        float diameter = radius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);
    }
}
