using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WinterAudioAssetGenerator
{
    public const string Root = "Assets/Resources/Littles/RiverValley/Audio";
    public const string Wind = Root + "/Winter_Wind_Loop.wav";
    public const string Plow = Root + "/Snowplow_Roar_Loop.wav";
    public const string Bicycle = Root + "/Winter_Bicycle_Loop.wav";
    public const string Splat = Root + "/Snow_Splat.wav";
    public const string Ice = Root + "/Ice_Scrape.wav";
    public const string Land = Root + "/Snow_Land.wav";
    public const string SparkUp = Root + "/Spark_Up.wav";
    public const string SparkDown = Root + "/Spark_Down.wav";
    public const string RabbitHop = Root + "/Rabbit_Hop.wav";
    public const string CoyoteYip = Root + "/Coyote_Yip.wav";
    public const string CoyoteHowl = Root + "/Coyote_Howl.wav";
    public const string CarEngine = Root + "/Winter_Car_Engine_Loop.wav";
    public const string CarHorn = Root + "/Winter_Car_Horn.wav";
    public const string EmergencySiren = Root + "/Emergency_Siren_Loop.wav";

    public static string Crunch(int index) => Root + $"/Snow_Crunch_{index}.wav";

    public static void EnsureAll()
    {
        Directory.CreateDirectory(Path.GetFullPath(Root));
        const int rate = 22050;
        for (int i = 1; i <= 4; i++)
            WriteWav(Crunch(i), CrunchSound(rate, 0.24f + i * 0.012f, 1103u + (uint)i * 977u), rate);
        WriteWav(Land, CrunchSound(rate, 0.44f, 8849u, 0.88f), rate);
        WriteWav(Wind, WindSound(rate, 6f, 4219u), rate);
        WriteWav(Plow, PlowSound(rate, 3f, 7717u), rate);
        WriteWav(Bicycle, BicycleSound(rate, 2f, 9133u), rate);
        WriteWav(Splat, SplatSound(rate, 0.48f, 12239u), rate);
        WriteWav(Ice, IceSound(rate, 0.72f, 19441u), rate);
        WriteWav(SparkUp, SparkTone(rate, true), rate);
        WriteWav(SparkDown, SparkTone(rate, false), rate);
        WriteWav(RabbitHop, RabbitHopSound(rate, 0.24f, 37217u), rate);
        WriteWav(CoyoteYip, CoyoteYipSound(rate, 0.62f, 48131u), rate);
        WriteWav(CoyoteHowl, CoyoteHowlSound(rate, 2.65f, 59359u), rate);
        WriteWav(CarEngine, CarEngineSound(rate, 3f, 67181u), rate);
        WriteWav(CarHorn, CarHornSound(rate, 0.48f), rate);
        WriteWav(EmergencySiren, EmergencySirenSound(rate, 2.4f), rate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string path in new[] { Wind, Plow, Bicycle, Splat, Ice, Land, SparkUp, SparkDown,
            RabbitHop, CoyoteYip, CoyoteHowl, CarEngine, CarHorn, EmergencySiren,
            Crunch(1), Crunch(2), Crunch(3), Crunch(4) })
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }

    private static float[] CrunchSound(int rate, float seconds, uint seed, float strength = 0.70f)
    {
        int count = Mathf.CeilToInt(rate * seconds);
        float[] data = new float[count];
        float low = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float noise = NextNoise(ref seed);
            low += (noise - low) * 0.16f;
            float grains = Mathf.Abs(Mathf.Sin(i * 0.071f + seed * 0.0001f)) > 0.965f ? noise * 0.72f : 0f;
            float envelope = Mathf.Pow(1f - t, 1.55f);
            data[i] = Mathf.Clamp((low * 0.54f + grains + Mathf.Sin(i * 0.028f) * 0.13f) * envelope * strength, -0.95f, 0.95f);
        }
        return data;
    }

    private static float[] WindSound(int rate, float seconds, uint seed)
    {
        int count = Mathf.CeilToInt(rate * seconds);
        float[] data = new float[count];
        float low = 0f;
        for (int i = 0; i < count; i++)
        {
            float phase = i / (float)count;
            float noise = NextNoise(ref seed);
            low += (noise - low) * 0.018f;
            float gust = 0.38f + 0.16f * Mathf.Sin(phase * Mathf.PI * 6f) + 0.09f * Mathf.Sin(phase * Mathf.PI * 14f);
            data[i] = Mathf.Clamp(low * gust + Mathf.Sin(phase * Mathf.PI * 2f) * 0.018f, -0.62f, 0.62f);
        }
        return data;
    }

    private static float[] PlowSound(int rate, float seconds, uint seed)
    {
        int count = Mathf.CeilToInt(rate * seconds);
        float[] data = new float[count];
        float rumble = 0f;
        float blade = 0f;
        for (int i = 0; i < count; i++)
        {
            float time = i / (float)rate;
            float noise = NextNoise(ref seed);
            rumble += (noise - rumble) * 0.022f;
            blade += (noise - blade) * 0.19f;
            float dieselPulse = 0.72f + Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * Mathf.PI * 2f * 6.5f)), 4f) * 0.28f;
            float metalScrape = (noise - blade) * (0.12f + 0.05f * Mathf.Sin(time * Mathf.PI * 2f * 0.73f));
            data[i] = Mathf.Clamp((Mathf.Sin(time * Mathf.PI * 2f * 38f) * 0.25f +
                Mathf.Sin(time * Mathf.PI * 2f * 57f) * 0.18f +
                Mathf.Sin(time * Mathf.PI * 2f * 93f) * 0.08f + rumble * 0.34f) * dieselPulse +
                metalScrape, -0.88f, 0.88f);
        }
        return data;
    }

    private static float[] RabbitHopSound(int rate, float seconds, uint seed)
    {
        int count = Mathf.CeilToInt(rate * seconds);
        float[] data = new float[count];
        float soft = 0f;
        for (int i = 0; i < count; i++)
        {
            float time = i / (float)rate;
            float noise = NextNoise(ref seed);
            soft += (noise - soft) * 0.11f;
            float first = Mathf.Exp(-time * 38f);
            float secondTime = Mathf.Max(0f, time - 0.075f);
            float second = time >= 0.075f ? Mathf.Exp(-secondTime * 44f) * 0.72f : 0f;
            data[i] = Mathf.Clamp((soft * 0.44f + Mathf.Sin(time * Mathf.PI * 2f * 96f) * 0.18f) *
                (first + second), -0.55f, 0.55f);
        }
        return data;
    }

    private static float[] CoyoteYipSound(int rate, float seconds, uint seed)
    {
        int count = Mathf.CeilToInt(rate * seconds);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float time = i / (float)rate;
            float frequency = Mathf.Lerp(520f, 940f, Mathf.Pow(t, 0.58f)) + Mathf.Sin(time * 58f) * 22f;
            float envelope = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.55f) * Mathf.Exp(-t * 1.2f);
            float rasp = NextNoise(ref seed) * 0.045f;
            data[i] = Mathf.Clamp((Mathf.Sin(time * Mathf.PI * 2f * frequency) * 0.34f +
                Mathf.Sin(time * Mathf.PI * 4f * frequency) * 0.08f + rasp) * envelope, -0.62f, 0.62f);
        }
        return data;
    }

    private static float[] CoyoteHowlSound(int rate, float seconds, uint seed)
    {
        int count = Mathf.CeilToInt(rate * seconds);
        float[] data = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float time = i / (float)rate;
            float arch = Mathf.Sin(t * Mathf.PI);
            float frequency = 405f + arch * 175f + Mathf.Sin(time * 31f) * 7f;
            phase += frequency / rate * Mathf.PI * 2f;
            float attack = Mathf.Clamp01(t * 8f);
            float release = Mathf.Clamp01((1f - t) * 4.5f);
            float envelope = attack * release * (0.72f + Mathf.Sin(time * 9f) * 0.09f);
            float breath = NextNoise(ref seed) * 0.024f;
            data[i] = Mathf.Clamp((Mathf.Sin(phase) * 0.31f + Mathf.Sin(phase * 2.01f) * 0.075f + breath) * envelope,
                -0.58f, 0.58f);
        }
        return data;
    }

    private static float[] CarEngineSound(int rate,float seconds,uint seed)
    {
        int count=Mathf.CeilToInt(rate*seconds); float[] data=new float[count];
        for(int i=0;i<count;i++)
        {
            float t=i/(float)rate; seed=seed*1664525u+1013904223u;
            float noise=(((seed>>8)&0xffff)/32767.5f-1f)*0.035f;
            float motor=Mathf.Sin(t*Mathf.PI*2f*48f)*0.20f+Mathf.Sin(t*Mathf.PI*2f*96f)*0.07f;
            float pulse=0.78f+0.22f*Mathf.Sin(t*Mathf.PI*2f*8f);
            data[i]=(motor*pulse+noise)*0.72f;
        }
        return data;
    }

    private static float[] CarHornSound(int rate,float seconds)
    {
        int count=Mathf.CeilToInt(rate*seconds); float[] data=new float[count];
        for(int i=0;i<count;i++)
        {
            float t=i/(float)rate; float edge=Mathf.Clamp01(t*30f)*Mathf.Clamp01((seconds-t)*24f);
            data[i]=(Mathf.Sin(t*Mathf.PI*2f*392f)*0.34f+Mathf.Sin(t*Mathf.PI*2f*466f)*0.25f)*edge;
        }
        return data;
    }

    private static float[] EmergencySirenSound(int rate,float seconds)
    {
        int count=Mathf.CeilToInt(rate*seconds); float[] data=new float[count];
        for(int i=0;i<count;i++)
        {
            float time=i/(float)rate;
            float sweep=0.5f+0.5f*Mathf.Sin(time*Mathf.PI*2f/seconds*2f-Mathf.PI*0.5f);
            float frequency=Mathf.Lerp(610f,890f,sweep);
            float pulse=0.72f+0.28f*Mathf.Sin(time*Mathf.PI*2f*3.2f);
            data[i]=(Mathf.Sin(time*Mathf.PI*2f*frequency)*0.25f+
                Mathf.Sin(time*Mathf.PI*4f*frequency)*0.055f)*pulse;
        }
        return data;
    }

    private static float[] BicycleSound(int rate, float seconds, uint seed)
    {
        int count = Mathf.CeilToInt(rate * seconds);
        float[] data = new float[count];
        float air = 0f;
        for (int i = 0; i < count; i++)
        {
            float time = i / (float)rate;
            float noise = NextNoise(ref seed);
            air += (noise - air) * 0.12f;
            float wheel = Mathf.Sin(time * Mathf.PI * 2f * 23f) * 0.08f;
            float tick = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * Mathf.PI * 2f * 9f)), 24f) * 0.16f;
            data[i] = Mathf.Clamp(air * 0.18f + wheel + tick, -0.50f, 0.50f);
        }
        return data;
    }

    private static float[] SplatSound(int rate, float seconds, uint seed)
    {
        int count = Mathf.CeilToInt(rate * seconds);
        float[] data = new float[count];
        float soft = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float noise = NextNoise(ref seed);
            soft += (noise - soft) * 0.24f;
            float envelope = Mathf.Exp(-t * 6.2f);
            data[i] = Mathf.Clamp((soft * 0.75f + Mathf.Sin(i * 0.018f) * 0.22f) * envelope, -0.95f, 0.95f);
        }
        return data;
    }

    private static float[] IceSound(int rate, float seconds, uint seed)
    {
        int count = Mathf.CeilToInt(rate * seconds);
        float[] data = new float[count];
        float previous = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float noise = NextNoise(ref seed);
            float scrape = noise - previous;
            previous = noise;
            float envelope = Mathf.Sin(t * Mathf.PI);
            data[i] = Mathf.Clamp(scrape * 0.13f * envelope + Mathf.Sin(i * 0.081f) * 0.07f * envelope, -0.55f, 0.55f);
        }
        return data;
    }

    private static float[] SparkTone(int rate, bool rising)
    {
        const float seconds = 0.72f;
        int count = Mathf.CeilToInt(rate * seconds);
        float[] data = new float[count];
        float[] up = { 392f, 523.25f, 659.25f, 783.99f };
        float[] down = { 493.88f, 392f, 311.13f, 246.94f };
        float[] notes = rising ? up : down;
        for (int i = 0; i < count; i++)
        {
            float time = i / (float)rate;
            int note = Mathf.Clamp(Mathf.FloorToInt(time / (seconds / notes.Length)), 0, notes.Length - 1);
            float local = (time - note * (seconds / notes.Length)) / (seconds / notes.Length);
            float envelope = Mathf.Sin(Mathf.Clamp01(local) * Mathf.PI) * (1f - time / seconds * 0.35f);
            float wave = Mathf.Sin(time * Mathf.PI * 2f * notes[note]) * 0.32f +
                Mathf.Sin(time * Mathf.PI * 4f * notes[note]) * 0.08f;
            data[i] = wave * envelope;
        }
        return data;
    }

    private static float NextNoise(ref uint state)
    {
        state = state * 1664525u + 1013904223u;
        return ((state >> 8) & 0x00FFFFFF) / 8388607.5f - 1f;
    }

    private static void WriteWav(string assetPath, float[] samples, int sampleRate)
    {
        string fullPath = Path.GetFullPath(assetPath);
        using FileStream stream = new(fullPath, FileMode.Create, FileAccess.Write);
        using BinaryWriter writer = new(stream);
        int dataSize = samples.Length * 2;
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + dataSize);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);
        foreach (float sample in samples)
            writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
    }
}
