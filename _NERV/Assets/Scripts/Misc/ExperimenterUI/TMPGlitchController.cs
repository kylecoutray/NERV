using UnityEngine;
using TMPro;

public class TMPGlitchController : MonoBehaviour
{
    [Header("Glitch Settings")]
    public float maxOffset = 0.01f;    // in UV units
    public float glitchChance = 0.1f;  // chance per character per frame

    private Material mat;
    private int idOffsetR, idOffsetG, idOffsetB;

    void Awake()
    {
        var tmp = GetComponent<TextMeshProUGUI>();
        mat = Instantiate(tmp.fontMaterial);
        tmp.fontMaterial = mat;

        idOffsetR = Shader.PropertyToID("_OffsetR");
        idOffsetG = Shader.PropertyToID("_OffsetG");
        idOffsetB = Shader.PropertyToID("_OffsetB");
    }

    void Update()
    {
        // randomly decide if we glitch this frame
        if (Random.value < glitchChance)
        {
            // pick random small shifts
            mat.SetVector(idOffsetR, new Vector4(Random.Range(-maxOffset, maxOffset), Random.Range(-maxOffset, maxOffset), 0, 0));
            mat.SetVector(idOffsetG, new Vector4(Random.Range(-maxOffset, maxOffset), Random.Range(-maxOffset, maxOffset), 0, 0));
            mat.SetVector(idOffsetB, new Vector4(Random.Range(-maxOffset, maxOffset), Random.Range(-maxOffset, maxOffset), 0, 0));
        }
        else
        {
            // reset
            mat.SetVector(idOffsetR, Vector4.zero);
            mat.SetVector(idOffsetG, Vector4.zero);
            mat.SetVector(idOffsetB, Vector4.zero);
        }
    }
}
