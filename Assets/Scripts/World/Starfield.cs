using UnityEngine;

// Campo de estrelas de fundo que cintilam e derivam devagar.
public class Starfield : MonoBehaviour
{
    public Sprite[] sprites;
    public int count = 70;
    public Vector2 area = new Vector2(32f, 20f);
    public float drift = 0.15f;
    public int sortingOrder = -80;
    public int seed = 42;

    Transform[] stars;
    SpriteRenderer[] renderers;
    float[] phases;
    float[] speeds;

    void Awake()
    {
        var random = new System.Random(seed);
        stars = new Transform[count];
        renderers = new SpriteRenderer[count];
        phases = new float[count];
        speeds = new float[count];

        for (int i = 0; i < count; i++)
        {
            var star = new GameObject("Estrela de fundo");
            star.transform.SetParent(transform, false);
            star.transform.localPosition = new Vector3(
                ((float)random.NextDouble() - 0.5f) * area.x,
                ((float)random.NextDouble() - 0.5f) * area.y, 0f);
            float size = Mathf.Lerp(0.25f, 0.7f, (float)random.NextDouble());
            star.transform.localScale = Vector3.one * size;

            var r = star.AddComponent<SpriteRenderer>();
            r.sprite = sprites[random.Next(sprites.Length)];
            r.sortingOrder = sortingOrder;
            stars[i] = star.transform;
            renderers[i] = r;
            phases[i] = (float)random.NextDouble() * 10f;
            speeds[i] = Mathf.Lerp(0.5f, 1.5f, size);
        }
    }

    void Update()
    {
        for (int i = 0; i < count; i++)
        {
            float twinkle = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.time * 1.7f + phases[i]));
            renderers[i].color = new Color(1f, 1f, 1f, twinkle * 0.8f);

            var p = stars[i].localPosition;
            p.x -= drift * speeds[i] * Time.deltaTime;
            if (p.x < -area.x / 2f) p.x += area.x;
            stars[i].localPosition = p;
        }
    }
}
