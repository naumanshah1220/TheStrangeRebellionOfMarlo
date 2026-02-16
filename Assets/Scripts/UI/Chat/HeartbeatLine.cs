using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Custom MaskableGraphic that draws a scrolling ECG waveform.
/// Participates in Canvas batching, respects RectMask2D, no textures needed.
/// </summary>
public class HeartbeatLine : MaskableGraphic
{
    [SerializeField] private float lineWidth = 2f;
    [SerializeField] private float scrollSpeed = 80f;
    [SerializeField] private int sampleCount = 200;

    [Header("Zone Colors")]
    [SerializeField] private Color lawyeredUpColor = new Color(0.6f, 0.85f, 1.0f);
    [SerializeField] private Color deflectingColor = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color sweetSpotColor = new Color(1.0f, 0.85f, 0.2f);
    [SerializeField] private Color rattledColor = new Color(1.0f, 0.55f, 0.1f);
    [SerializeField] private Color shutdownColor = new Color(0.9f, 0.15f, 0.15f);

    private float[] samples;
    private int writeIndex;
    private float phase;        // 0-1 within current heartbeat cycle
    private float bpm = 60f;
    private float spikeAmplitude = 0.3f;

    public override Texture mainTexture => Texture2D.whiteTexture;

    protected override void Awake()
    {
        base.Awake();
        samples = new float[sampleCount];
    }

    public void SetStress(float stress)
    {
        stress = Mathf.Clamp01(stress);
        bpm = Mathf.Lerp(60f, 160f, stress);
        spikeAmplitude = Mathf.Lerp(0.3f, 1.0f, stress);
    }

    public void SetZone(StressZone zone)
    {
        color = zone switch
        {
            StressZone.LawyeredUp => lawyeredUpColor,
            StressZone.Deflecting => deflectingColor,
            StressZone.SweetSpot => sweetSpotColor,
            StressZone.Rattled => rattledColor,
            StressZone.Shutdown => shutdownColor,
            _ => sweetSpotColor
        };
    }

    private void Update()
    {
        if (samples == null) return;

        float beatsPerSecond = bpm / 60f;
        float pixelsPerSecond = scrollSpeed;
        float samplesPerSecond = (sampleCount / rectTransform.rect.width) * pixelsPerSecond;
        if (samplesPerSecond <= 0f || float.IsNaN(samplesPerSecond)) return;

        float samplesToAdvance = samplesPerSecond * Time.deltaTime;
        int wholeSamples = Mathf.FloorToInt(samplesToAdvance);
        // accumulate fractional via phase advancement
        float phaseStep = beatsPerSecond * Time.deltaTime / Mathf.Max(1, wholeSamples);

        for (int i = 0; i < wholeSamples; i++)
        {
            phase += phaseStep;
            if (phase >= 1f) phase -= 1f;

            samples[writeIndex] = EvaluateECG(phase);
            writeIndex = (writeIndex + 1) % sampleCount;
        }

        if (wholeSamples > 0)
            SetVerticesDirty();
    }

    private float EvaluateECG(float t)
    {
        // Simplified ECG waveform pattern
        // [0.00 - 0.10] baseline
        if (t < 0.10f)
            return Noise();

        // [0.10 - 0.125] sharp QRS spike up
        if (t < 0.125f)
        {
            float local = (t - 0.10f) / 0.025f;
            return spikeAmplitude * local + Noise();
        }

        // [0.125 - 0.15] sharp QRS spike down
        if (t < 0.15f)
        {
            float local = (t - 0.125f) / 0.025f;
            return spikeAmplitude * (1f - 2f * local) + Noise();
        }

        // [0.15 - 0.20] recovery dip below baseline
        if (t < 0.20f)
        {
            float local = (t - 0.15f) / 0.05f;
            return -spikeAmplitude * 0.3f * (1f - local) + Noise();
        }

        // [0.40 - 0.50] gentle T-wave bump
        if (t >= 0.40f && t < 0.50f)
        {
            float local = (t - 0.40f) / 0.10f;
            float tWave = Mathf.Sin(local * Mathf.PI);
            return spikeAmplitude * 0.2f * tWave + Noise();
        }

        // else baseline with tiny noise
        return Noise();
    }

    private float Noise()
    {
        return Random.Range(-0.02f, 0.02f);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (samples == null || samples.Length < 2) return;

        Rect rect = rectTransform.rect;
        float halfHeight = rect.height * 0.5f;
        float halfLine = lineWidth * 0.5f;
        float xStep = rect.width / (sampleCount - 1);

        Color32 c = color;

        for (int i = 0; i < sampleCount - 1; i++)
        {
            int idxA = (writeIndex + i) % sampleCount;
            int idxB = (writeIndex + i + 1) % sampleCount;

            float xA = rect.xMin + i * xStep;
            float xB = rect.xMin + (i + 1) * xStep;
            float yA = samples[idxA] * halfHeight;
            float yB = samples[idxB] * halfHeight;

            // Direction perpendicular to the segment for line thickness
            Vector2 dir = new Vector2(xB - xA, yB - yA);
            Vector2 perp = new Vector2(-dir.y, dir.x).normalized * halfLine;

            int vi = i * 4;
            vh.AddVert(new Vector3(xA - perp.x, yA - perp.y), c, Vector4.zero);
            vh.AddVert(new Vector3(xA + perp.x, yA + perp.y), c, Vector4.zero);
            vh.AddVert(new Vector3(xB + perp.x, yB + perp.y), c, Vector4.zero);
            vh.AddVert(new Vector3(xB - perp.x, yB - perp.y), c, Vector4.zero);

            vh.AddTriangle(vi, vi + 1, vi + 2);
            vh.AddTriangle(vi, vi + 2, vi + 3);
        }
    }
}
