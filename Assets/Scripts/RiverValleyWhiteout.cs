using UnityEngine;

public sealed class RiverValleyWhiteout : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private ParticleSystem blowingSnow;
    [SerializeField] private Light sun;
    [SerializeField] private float stormBeginsZ = 105f;
    [SerializeField] private float whiteoutBeginsZ = 245f;
    [SerializeField] private float fullWhiteoutZ = 330f;
    private Color originalFog;
    private float originalSun;
    private bool initialized;
    private bool levelTwoStorm;
    private bool levelThreeStorm;
    public float Intensity { get; private set; }
    public bool IsLevelTwoStorm => levelTwoStorm;
    public string Stage => Intensity < 0.12f ? "Light snow" : Intensity < 0.55f ? "Storm building" : Intensity < 0.88f ? "Whiteout" : "Near-zero visibility";

    private void Start()
    {
        originalFog = RenderSettings.fogColor;
        originalSun = sun != null ? sun.intensity : 1f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || player == null) return;
        float early = Mathf.InverseLerp(stormBeginsZ, whiteoutBeginsZ, player.position.z) * 0.55f;
        float late = Mathf.InverseLerp(whiteoutBeginsZ, fullWhiteoutZ, player.position.z) * 0.45f;
        float routeIntensity = Mathf.Clamp01(early + late);
        float levelTwoPulse = (levelThreeStorm?0.79f:0.69f) +
            (Mathf.Sin(Time.time * 0.17f) + 1f) * (levelThreeStorm?0.045f:0.055f);
        Intensity = levelTwoStorm ? Mathf.Max(routeIntensity, levelTwoPulse) : routeIntensity;
        RenderSettings.fogDensity = Mathf.Lerp(0.004f, levelThreeStorm?0.118f:levelTwoStorm?0.105f:0.125f, Intensity);
        Color stormColour = levelThreeStorm
            ? new Color(0.20f,0.27f,0.38f)
            : levelTwoStorm
            ? new Color(0.34f,0.42f,0.55f)
            : new Color(0.88f,0.92f,0.95f);
        RenderSettings.fogColor = Color.Lerp(originalFog, stormColour, Intensity);
        if (sun != null) sun.intensity = Mathf.Lerp(originalSun, levelThreeStorm?0.10f:levelTwoStorm?0.17f:0.28f, Intensity);
        if (blowingSnow != null)
        {
            ParticleSystem.EmissionModule emission = blowingSnow.emission;
            emission.rateOverTime = Mathf.Lerp(35f, 1150f, Intensity);
            ParticleSystem.MainModule main = blowingSnow.main;
            main.startSpeed = Mathf.Lerp(5f, 18f, Intensity);
        }
    }

    public void BeginLevelTwoStorm()
    {
        levelTwoStorm = true;
    }

    public void BeginLevelThreeStorm()
    {
        levelTwoStorm=true;
        levelThreeStorm=true;
    }

#if UNITY_EDITOR
    public void Configure(Transform newPlayer, ParticleSystem newSnow, Light newSun,
        float newStormZ, float newWhiteoutZ, float newFullZ)
    {
        player=newPlayer; blowingSnow=newSnow; sun=newSun;
        stormBeginsZ=newStormZ; whiteoutBeginsZ=newWhiteoutZ; fullWhiteoutZ=newFullZ;
    }
#endif
}
