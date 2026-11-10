
using System;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PlayerColor : NetworkBehaviour
{
    [Header("Visual References")]
    [SerializeField] private Renderer capsuleRenderer;

    // Couleur attribuée uniquement par le serveur.
    public readonly NetworkVariable<int> ColorIndex =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public event Action ColorChanged;

    // J1 rouge, J2 vert, J3 bleu, J4 rose.
    private static readonly Color32[] Colors =
    {
        new Color32(229, 72, 77, 255),
        new Color32(56, 179, 109, 255),
        new Color32(55, 139, 250, 255),
        new Color32(239, 128, 187, 255)
    };

    private static readonly string[] HtmlColors =
    {
        "#E5484D",
        "#38B36D",
        "#378BFA",
        "#EF80BB"
    };

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        if (capsuleRenderer == null)
            capsuleRenderer = GetComponentInChildren<Renderer>();

        propertyBlock = new MaterialPropertyBlock();
    }

    public override void OnNetworkSpawn()
    {
        ColorIndex.OnValueChanged += OnColorIndexChanged;

        ApplyColor();
    }

    // Appelée uniquement par le serveur.
    public void SetColorIndex(int index)
    {
        if (!IsSpawned || !IsServer)
            return;

        ColorIndex.Value = Mathf.Clamp(
            index, 0, Colors.Length - 1
        );

        ApplyColor();
        ColorChanged?.Invoke();
    }

    private void OnColorIndexChanged(
        int previous,
        int current)
    {
        ApplyColor();

        // Prévenir les interfaces.
        ColorChanged?.Invoke();
    }

    private void ApplyColor()
    {
        if (capsuleRenderer == null)
            return;

        int index = Mathf.Clamp(
            ColorIndex.Value, 0, Colors.Length - 1
        );

        capsuleRenderer.GetPropertyBlock(propertyBlock);

        // URP Lit et shaders classiques.
        propertyBlock.SetColor(
            BaseColorId, Colors[index]
        );

        propertyBlock.SetColor(
            ColorId, Colors[index]
        );

        capsuleRenderer.SetPropertyBlock(propertyBlock);
    }

    public static string GetHtmlColor(int index)
    {
        if (index < 0 || index >= HtmlColors.Length)
            return "#FFFFFF";

        return HtmlColors[index];
    }

    public override void OnNetworkDespawn()
    {
        ColorIndex.OnValueChanged -= OnColorIndexChanged;
    }
}