using UnityEngine;

public class RainbowColorCycle : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    [Header("Rainbow Settings")]
    [SerializeField] private float colorChangeInterval = 0.2f;

    private MaterialPropertyBlock propertyBlock;
    private float nextColorChange;
    private int colorIndex;

    private readonly Color[] rainbowColors =
    {
        Color.red,
        Color.yellow,
        Color.green,
        Color.cyan,
        Color.blue,
        Color.magenta
    };

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        ApplyColor();
        nextColorChange = Time.time + colorChangeInterval;
    }

    private void Update()
    {
        if (Time.time < nextColorChange)
            return;

        colorIndex = (colorIndex + 1) % rainbowColors.Length;

        ApplyColor();

        nextColorChange =
            Time.time + Mathf.Max(0.01f, colorChangeInterval);
    }

    private void ApplyColor()
    {
        if (targetRenderer == null)
            return;

        Color color = rainbowColors[colorIndex];

        targetRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);

        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}