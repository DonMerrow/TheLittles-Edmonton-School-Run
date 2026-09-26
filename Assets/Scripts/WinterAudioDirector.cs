using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class WinterAudioDirector : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private RiverValleyWhiteout whiteout;
    [SerializeField] private AudioClip windLoop;
    [SerializeField] private AudioClip snowImpact;
    [SerializeField] private AudioClip iceScrape;
    private AudioSource wind;
    private AudioSource voice;
    private AudioSource crowd;
    private AudioSource celebration;
    private AudioClip schoolCheer;
    private readonly List<SpeechRequest> speechQueue=new();
    private float nextSpeechAllowed;
    private float nextMotherAccepted;
    private AudioClip lastMotherClip;
    private RiverValleyGameDirector director;
    public bool Muted { get; private set; }
    public int VoiceEventsPlayed { get; private set; }
    public int RecordedVoiceEventsPlayed { get; private set; }
    public int MotherVoiceEventsPlayed { get; private set; }
    public int SchoolCheersPlayed { get; private set; }

    public AudioClip SnowImpact => snowImpact;

    private sealed class SpeechRequest
    {
        public AudioClip Clip;
        public float Volume;
        public bool Recorded;
        public bool Mother;
    }

    private void Start()
    {
        wind = gameObject.AddComponent<AudioSource>();
        wind.clip = windLoop;
        wind.loop = true;
        wind.playOnAwake = false;
        wind.spatialBlend = 0f;
        wind.volume = 0.12f;
        if (windLoop != null) wind.Play();
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0f;
        voice.volume = 0.62f;
        crowd = gameObject.AddComponent<AudioSource>();
        crowd.clip = CreateCrowdLoop();
        crowd.loop = true;
        crowd.playOnAwake = false;
        crowd.spatialBlend = 0f;
        crowd.volume = 0.035f;
        crowd.Play();
        celebration = gameObject.AddComponent<AudioSource>();
        celebration.playOnAwake = false;
        celebration.spatialBlend = 0f;
        celebration.volume = 0.72f;
        if (GetComponent<RiverValleyCuriousChatter>() == null)
            gameObject.AddComponent<RiverValleyCuriousChatter>();
        director = FindFirstObjectByType<RiverValleyGameDirector>();
        Speak("DANNY", "The Little Public School is beyond the river valley. The storm is just weather—probably.");
    }

    private void Update()
    {
        if (wind == null) return;
        UpdateSpeechQueue();
        float storm = whiteout != null ? whiteout.Intensity : 0f;
        wind.volume = Mathf.Lerp(0.12f, 0.62f, storm);
        wind.pitch = Mathf.Lerp(0.88f, 1.12f, storm);
        if (crowd != null)
        {
            float children = director != null ? Mathf.Clamp01(director.RescuedKids / 18f) : 0f;
            float schoolApproach = director != null ? Mathf.InverseLerp(0.72f, 1f, director.Progress) : 0f;
            float speechDuck=voice!=null&&voice.isPlaying?0.34f:1f;
            crowd.volume = Mathf.Lerp(0.025f, 0.16f, Mathf.Max(children, schoolApproach))*speechDuck;
            crowd.pitch = Mathf.Lerp(0.96f, 1.08f, children);
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
        {
            Muted = !Muted;
            AudioListener.volume = Muted ? 0f : 1f;
        }
    }

    public void PlaySnowImpact(Vector3 position, float volume = 0.8f)
    {
        if (snowImpact != null) AudioSource.PlayClipAtPoint(snowImpact, position, volume);
    }

    public void PlayIceScrape(Vector3 position)
    {
        if (iceScrape != null) AudioSource.PlayClipAtPoint(iceScrape, position, 0.62f);
    }

    public void Speak(string speaker, string words)
    {
        if (string.IsNullOrWhiteSpace(words)) return;
        // Dialogue audio follows the one line that is visible now. A backlog
        // made old warnings and "falling behind" calls play minutes after the
        // pictured event, so every new panel replaces any unfinished old line.
        speechQueue.Clear();
        if(voice!=null&&voice.isPlaying)voice.Stop();
        bool isMother=(speaker??string.Empty).ToUpperInvariant().Contains("MOTHER");
        AudioClip recorded=RecordedVoice(speaker,words);
        // Unrecorded text remains readable in the dialogue panel. The old
        // oscillator fallback sounded robotic and turned every line into noise.
        if(recorded==null)return;
        // Do not let a repeated chase trigger fill the queue with the same
        // Mother recording. Her on-screen line may still update, while the
        // soundscape gets a natural pause before the next distinct call.
        if(isMother&&(recorded==lastMotherClip||Time.unscaledTime<nextMotherAccepted))return;
        if(isMother)
        {
            lastMotherClip=recorded;
            nextMotherAccepted=Time.unscaledTime+7.5f;
        }
        PlaySpeechNow(recorded,isMother?0.96f:0.76f,isMother);
    }

    public void SpeakPriority(string speaker,string words)
    {
        AudioClip recorded=RecordedVoice(speaker,words);
        if(recorded==null||voice==null)return;
        speechQueue.Clear();
        if(voice.isPlaying)voice.Stop();
        bool isMother=(speaker??string.Empty).ToUpperInvariant().Contains("MOTHER");
        if(isMother)
        {
            lastMotherClip=recorded;
            nextMotherAccepted=Time.unscaledTime+7.5f;
        }
        PlaySpeechNow(recorded,isMother?0.96f:0.76f,isMother);
    }

    public void PlaySchoolCheer()
    {
        if(celebration==null)return;
        if(schoolCheer==null)schoolCheer=CreateSchoolCheer();
        celebration.PlayOneShot(schoolCheer);
        SchoolCheersPlayed++;
    }

    private static AudioClip CreateSchoolCheer()
    {
        // A short, non-verbal schoolyard cheer: layered whoops and claps.
        // The spoken thank-you stays on screen; no mismatched synthetic words.
        const int rate=24000;
        const float duration=3.1f;
        int count=Mathf.CeilToInt(rate*duration);
        float[] samples=new float[count];
        System.Random random=new(160924);
        for(int i=0;i<count;i++)
        {
            float t=i/(float)rate;
            float fade=Mathf.Clamp01(t*8f)*Mathf.Clamp01((duration-t)*3f);
            float clapPhase=(t*8.5f)%1f;
            float staggeredClap=((t*9.7f+0.43f)%1f);
            float claps=(Mathf.Exp(-clapPhase*24f)+
                0.7f*Mathf.Exp(-staggeredClap*23f))*0.24f;
            float noise=((float)random.NextDouble()*2f-1f)*claps;
            float whoopA=Mathf.Sin(2f*Mathf.PI*(420f*t+92f*t*t))*
                Mathf.Pow(Mathf.Max(0f,Mathf.Sin(Mathf.PI*2.1f*t)),2f)*0.09f;
            float whoopB=Mathf.Sin(2f*Mathf.PI*(535f*t+61f*t*t))*
                Mathf.Pow(Mathf.Max(0f,Mathf.Sin(Mathf.PI*(1.7f*t+0.35f))),2f)*0.065f;
            samples[i]=Mathf.Clamp((noise+whoopA+whoopB)*fade,-0.8f,0.8f);
        }
        AudioClip clip=AudioClip.Create("Children cheering for Danny",count,1,rate,false);
        clip.SetData(samples,0);
        return clip;
    }

    public bool SpeakGreeting(string speaker,string words)
    {
        if(voice==null||voice.isPlaying||string.IsNullOrWhiteSpace(words)||
            Time.unscaledTime<nextSpeechAllowed)return false;
        AudioClip recorded=RecordedVoice(speaker,words);
        if(recorded==null)return false;
        PlaySpeechNow(recorded,0.64f,false);
        return true;
    }

    private void PlaySpeechNow(AudioClip clip,float volume,bool mother)
    {
        if(clip==null||voice==null)return;
        voice.clip=clip;
        voice.volume=volume;
        voice.Play();
        VoiceEventsPlayed++;
        RecordedVoiceEventsPlayed++;
        if(mother)MotherVoiceEventsPlayed++;
        nextSpeechAllowed=Time.unscaledTime+clip.length+Random.Range(0.55f,0.90f);
    }

    private void QueueSpeech(AudioClip clip,float volume,bool recorded,bool mother,bool priority)
    {
        if(clip==null||voice==null)return;
        SpeechRequest request=new(){Clip=clip,Volume=volume,Recorded=recorded,Mother=mother};
        if(priority)
        {
            int insert=0;
            while(insert<speechQueue.Count&&speechQueue[insert].Mother)insert++;
            speechQueue.Insert(insert,request);
        }
        else if(speechQueue.Count<5)speechQueue.Add(request);
        while(speechQueue.Count>7)speechQueue.RemoveAt(speechQueue.Count-1);
    }

    private void UpdateSpeechQueue()
    {
        if(voice==null||voice.isPlaying||Time.unscaledTime<nextSpeechAllowed||speechQueue.Count==0)return;
        SpeechRequest request=speechQueue[0];
        speechQueue.RemoveAt(0);
        voice.clip=request.Clip;
        voice.volume=request.Volume;
        voice.Play();
        VoiceEventsPlayed++;
        if(request.Recorded)RecordedVoiceEventsPlayed++;
        if(request.Mother)MotherVoiceEventsPlayed++;
        nextSpeechAllowed=Time.unscaledTime+request.Clip.length+Random.Range(0.55f,0.90f);
    }

    private static AudioClip CreateDialogueMurmur(string speaker,string words)
    {
        float pitch = VoicePitch(speaker);
        float duration = Mathf.Clamp(0.48f + words.Length * 0.012f, 0.65f, 2.2f);
        const int rate = 16000;
        int sampleCount = Mathf.CeilToInt(rate * duration);
        float[] samples = new float[sampleCount];
        int syllables = Mathf.Clamp(words.Split(' ').Length / 2, 3, 12);
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)rate;
            float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * Mathf.PI * syllables / duration)), 0.55f);
            float wobble = 1f + Mathf.Sin(time * 17f) * 0.055f;
            float tone = Mathf.Sin(time * Mathf.PI * 2f * pitch * wobble) * 0.20f +
                Mathf.Sin(time * Mathf.PI * 4f * pitch) * 0.055f;
            float edge = Mathf.Clamp01(time * 18f) * Mathf.Clamp01((duration - time) * 14f);
            samples[i] = tone * pulse * edge;
        }
        AudioClip clip = AudioClip.Create($"{speaker} dialogue murmur", sampleCount, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static readonly string[] MotherRecordedKeys =
    {
        "toddler snow pants", "getting-dressed song", "goodbye kiss", "love note in your lunch",
        "clean long underwear", "mittens are still tied", "bubble-bath dance", "emergency cuddle",
        "baby voice", "toilet before school", "inside every sock", "dinosaur pajamas",
        "tiny snow dumpling", "enough apple slices", "seven-thirty bedtime", "public smooches",
        "careful little penguin", "orange hood in any blizzard", "taught you that hockey stance",
        "first-day picture", "everybody look how responsible", "introduce mommy", "socks wet",
        "hug in front of the whole school", "runaway marshmallow", "one kiss for every block",
        "orange hood would betray", "wipe your nose", "backup socks and your dignity",
        "everybody is going to hear", "cheeks are cold", "baby wipes",
        "all the way to saskatchewan", "full river-safety lecture",
        "captain fluffy", "spoon with the little train", "brave explorer in enormous letters",
        "emergency handkerchief", "orange coat from space", "bath-time hockey league",
        "lucky pebble", "full name embroidered", "banana has a smiley face",
        "kindergarten snowman drawing", "family whistle", "socks on the radiator",
        "crusts exactly", "extremely huggable", "tooth-brushing phase",
        "very special shoes", "teddy bear is watching", "snack container rattling",
        "tissues with cartoon ducks", "wallet and mommy has witnesses", "secret handshake has seven hugs",
        "excellent listener", "pom-pom the size", "marry your snow shovel",
        "one perfectly warm mitten", "full coat inspection", "check both ears for frost",
        "one centimetre crooked", "three emergency kisses", "official mitten count",
        "comb you said was embarrassing", "spot that pom-pom", "thermos and mommy were both worried",
        "very public hug ready", "crossing guard will hear", "celebratory napkin", "purple crayon",
        "not wet the bed", "marry your mittens", "baby teeth are still", "bathtub dinosaur song",
        "captain cozy", "toast cut into stars", "glitter unicorns", "underwear has rockets"
    };

    private static readonly string[] MotherRecordedClips =
    {
        "Mom_ToddlerSnowPants", "Mom_GettingDressedSong", "Mom_GoodbyeKiss", "Mom_LunchLoveNote",
        "Mom_LongUnderwear", "Mom_TiedMittens", "Mom_BubbleBathDance", "Mom_EmergencyCuddle",
        "Mom_BabyVoice", "Mom_ToiletReminder", "Mom_NamedSocks", "Mom_DinosaurPajamas",
        "Mom_SnowDumpling", "Mom_AppleSlices", "Mom_Bedtime", "Mom_PublicSmooches",
        "Mom_CarefulPenguin", "Mom_BlizzardHood", "Mom_HockeyStance", "Mom_FirstDayPicture",
        "Mom_PubliclyResponsible", "Mom_IntroduceFriends", "Mom_WetSocks", "Mom_WholeSchoolHug",
        "Mom_CaughtMarshmallow", "Mom_KissEveryBlock", "Mom_HoodBetrayal", "Mom_WipeNose",
        "Mom_BackupDignity", "Mom_ProudAnnouncement", "Mom_ColdCheeks", "Mom_BabyWipes",
        "Mom_RiverSaskatchewan", "Mom_RiverLecture",
        "Mom_Fresh01", "Mom_Fresh02", "Mom_Fresh03", "Mom_Fresh04", "Mom_Fresh05", "Mom_Fresh06",
        "Mom_Fresh07", "Mom_Fresh08", "Mom_Fresh09", "Mom_Fresh10", "Mom_Fresh11", "Mom_Fresh12",
        "Mom_Fresh13", "Mom_Fresh14", "Mom_Fresh15", "Mom_Fresh16", "Mom_Fresh17", "Mom_Fresh18",
        "Mom_Fresh19", "Mom_Fresh20", "Mom_Fresh21", "Mom_Fresh22", "Mom_Fresh23", "Mom_Fresh24",
        "Mom_Catch13", "Mom_Catch14", "Mom_Catch15", "Mom_Catch16", "Mom_Catch17", "Mom_Catch18",
        "Mom_Catch19", "Mom_Catch20", "Mom_Catch21", "Mom_Catch22", "Mom_Catch23", "Mom_Catch24",
        "Mom_PurpleCrayon",
        "Mom_BedDry", "Mom_MarryMittens", "Mom_BabyTeeth", "Mom_DinosaurSong",
        "Mom_CaptainCozy", "Mom_StarToast", "Mom_GlitterUnicorns", "Mom_RocketUnderwear"
    };

    private static AudioClip RecordedVoice(string speaker,string words)
    {
        string who=(speaker??string.Empty).ToUpperInvariant();
        string line=(words??string.Empty).ToLowerInvariant();
        string clip=null;
        if(who.Contains("MOTHER"))
        {
            for(int i=0;i<MotherRecordedKeys.Length;i++)
                if(line.Contains(MotherRecordedKeys[i]))
                {
                    clip=MotherRecordedClips[i];
                    break;
                }
            if(clip!=null) { }
            else if(line.Contains("responsible"))clip="Mom_Responsible";
            else if(line.Contains("precious")||line.Contains("poopsie"))clip="Mom_PreciousPoopsie";
            else if(line.Contains("hood")||line.Contains("pumpkin"))clip="Mom_PumpkinPants";
            else if(line.Contains("fast boots")||line.Contains("snuggle"))clip="Mom_FastBoots";
            else if(line.Contains("pudding"))clip="Mom_PuddingCup";
            else if(line.Contains("bunny")||line.Contains("mittens"))clip="Mom_BunnyBoots";
            else if(line.Contains("sweet pea")||line.Contains("napkin"))clip="Mom_SweetPea";
            else if(line.Contains("honey muffin")||line.Contains("footprints"))clip="Mom_HoneyMuffin";
            else if(line.Contains("cuddle bug")||line.Contains("corner"))clip="Mom_CuddleBug";
            // An unmatched line used to fall back to Mom_PookyBear. That made
            // unrelated events repeatedly play the same "right behind you"
            // recording. Unmatched dialogue now stays readable but silent
            // until it has its own correctly matched recording.
        }
        else if(who.Contains("DANNY")&&line.Contains("beyond the river valley"))clip="Danny_Intro";
        else if(who.Contains("DANNY")&&line.Contains("ride that snowboard"))clip="Danny_WantSnowboard";
        else if(who.Contains("DANNY")&&line.Contains("skip school"))clip="Danny_SkipSchool";
        else if(who.Contains("DANNY")&&line.Contains("field of rabbits"))clip="Danny_RabbitField";
        else if(who.Contains("REPORTER")&&line.Contains("stuck in the snow"))clip="Reporter_Buried";
        else if(who.Contains("REPORTER")&&line.Contains("left the group"))clip="Reporter_Scattered";
        else if(who.Contains("REPORTER")&&line.Contains("starting to wander"))clip="Reporter_Waiting";
        else if(who.Contains("REPORTER")&&line.Contains("falling behind"))clip="Reporter_Behind";
        else if(who.Contains("REPORTER")&&line.Contains("ran back to the rabbits"))clip="Reporter_Rabbit";
        else if(who.Contains("RABBIT CLUB")&&line.Contains("school has no rabbits"))clip="RabbitClub_Reluctant";
        else if(who.Contains("CHILD")&&line.Contains("what's that"))clip="Child_WhatsThat";
        else if(who.Contains("CHILD")&&line.Contains("why does it"))clip="Child_WhyDoesIt";
        else if(who.Contains("CHILD")&&line.Contains("at school yet"))clip="Child_AtSchoolYet";
        else if(who.Contains("CHILD")&&line.Contains("can i pet"))clip="Child_CanIPetIt";
        else if(who.Contains("CHILD")&&line.Contains("did you see"))clip="Child_DidYouSeeThat";
        else if(who.Contains("CHILD")&&line.Contains("go this way"))clip="Child_WhatIfThisWay";
        else if(who.Contains("CHILD")&&line.Contains("plow"))clip="Child_Plow";
        else if(who.Contains("CHILD")&&line.Contains("hot chocolate"))clip="Children_RiverCocoa";
        else if(who.Contains("RESCUE")&&line.Contains("blanket first"))clip="Rescue_BlanketCocoa";
        else if(who.Contains("PARAMEDIC")&&line.Contains("cocoa secured"))clip="Paramedic_CocoaSecured";
        else if(who.Contains("DRIVER")&&line.Contains("cross at the corner"))clip="Driver_CrossCorner";
        else if(who.Contains("DRIVER")&&line.Contains("little maniacs"))clip="Driver_LittleManiacs";
        else if(who.Contains("DRIVER")&&line.Contains("school run"))clip="Driver_Snowbank";
        else if(who.Contains("DRIVER")&&line.Contains("crazy kids"))clip="Driver_Sidewalk";
        else if(who.Contains("HOCKEY")&&line.Contains("first to three"))clip="Hockey_Start";
        else if(who.Contains("PRINCIPAL"))clip="Principal_Finish";
        else if(line=="hi, danny!")clip="Greeting_HiDanny";
        else if(line=="morning!")clip="Greeting_Morning";
        else if(line=="hey there!")clip="Greeting_HeyThere";
        else if(line=="how's it going?")clip="Greeting_HowsItGoing";
        else if(line=="stay warm!")clip="Greeting_StayWarm";
        else if(line=="good morning!")clip="Greeting_GoodMorning";
        else if(line=="hi, neighbour!")clip="Greeting_HiNeighbour";
        else if(line=="cold one, eh?")clip="Greeting_ColdOne";
        else if(line=="see you at school!")clip="Greeting_SeeSchool";
        else if(line=="beautiful alberta day!")clip="Greeting_AlbertaDay";
        return string.IsNullOrEmpty(clip)?null:
            Resources.Load<AudioClip>("Littles/RiverValley/Voices/"+clip);
    }

    private static float VoicePitch(string speaker)
    {
        string value = (speaker ?? string.Empty).ToUpperInvariant();
        if (value.Contains("MOTHER")) return 255f;
        if (value.Contains("CHILD") || value.Contains("KID") || value.Contains("MAYA")) return 310f;
        if (value.Contains("DANNY")) return 285f;
        if (value.Contains("PLOW")) return 135f;
        if (value.Contains("PRINCIPAL") || value.Contains("MR.") || value.Contains("MS.")) return 190f;
        return 225f;
    }

    private static AudioClip CreateCrowdLoop()
    {
        const int rate = 16000;
        const float duration = 5f;
        int count = Mathf.CeilToInt(rate * duration);
        float[] samples = new float[count];
        uint noise = 918273u;
        for (int i = 0; i < count; i++)
        {
            float time = i / (float)rate;
            noise = noise * 1664525u + 1013904223u;
            float hiss = (((noise >> 8) & 0xFFFF) / 32767.5f - 1f) * 0.025f;
            float voices = Mathf.Sin(time * Mathf.PI * 2f * (178f + Mathf.Sin(time * 1.7f) * 12f)) * 0.045f +
                Mathf.Sin(time * Mathf.PI * 2f * (233f + Mathf.Sin(time * 2.3f) * 17f)) * 0.032f;
            float chatterPulse = 0.25f + Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 4.7f)), 2f) * 0.75f;
            samples[i] = (voices + hiss) * chatterPulse;
        }
        AudioClip clip = AudioClip.Create("Children and winter street ambience", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        AudioListener.volume = 1f;
    }

#if UNITY_EDITOR
    public void Configure(Transform newPlayer, RiverValleyWhiteout newWhiteout,
        AudioClip newWindLoop, AudioClip newSnowImpact, AudioClip newIceScrape)
    {
        player = newPlayer;
        whiteout = newWhiteout;
        windLoop = newWindLoop;
        snowImpact = newSnowImpact;
        iceScrape = newIceScrape;
    }
#endif
}
