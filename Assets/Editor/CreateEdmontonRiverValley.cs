using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CreateEdmontonRiverValley
{
    private const string OneShotRefreshMarker = "Temp/CodexRefreshEdmontonRiverValley.once";
    private const string SourceScene = "Assets/Scenes/DannyGameplayTest.unity";
    private const string ScenePath = "Assets/Scenes/EdmontonRiverValleySchoolRun.unity";
    private const string CastRoot = "Assets/Resources/Littles/WinterCast";
    private const string RiverRoot = "Assets/Resources/Littles/RiverValley";
    private const string AnimalRoot = RiverRoot + "/Animals/Quaternius";
    private const string AnimalResourceRoot = "Littles/RiverValley/Animals/Quaternius/";
    private const string MaterialRoot = RiverRoot + "/Materials";
    private const string CityRoot = "Assets/SimplePoly City - Low Poly Assets/Prefab";
    private const int StairCount = 180;
    private const float StairTopZ = 135f;
    private const float StairDepth = 0.38f;
    private const float StairRise = 0.115f;
    private const int StepsPerFlight = 45;
    private const float StairLandingDepth = 2.4f;
    private static Transform world;
    private static Scene scene;

    [MenuItem("Tools/The Littles/Create Edmonton River Valley School Run")]
    public static void CreateMenu() => Create(false);

    [InitializeOnLoadMethod]
    private static void RefreshOnceWhenEditorIsSafe()
    {
        if(!System.IO.File.Exists(OneShotRefreshMarker))return;
        EditorApplication.update-=TryOneShotRefresh;
        EditorApplication.update+=TryOneShotRefresh;
    }

    private static void TryOneShotRefresh()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        EditorApplication.update-=TryOneShotRefresh;
        System.IO.File.Delete(OneShotRefreshMarker);
        Create(false);
    }

    public static void CreateBatch()
    {
        Create(true);
        EditorApplication.Exit(0);
    }

    private static void Create(bool batch)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EnsureFolder(RiverRoot);
        EnsureFolder(MaterialRoot);
        QuaterniusAnimalAssetSetup.Prepare();
        WinterAudioAssetGenerator.EnsureAll();
        scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
        if (!scene.IsValid()) throw new InvalidOperationException("Danny test scene could not be opened");

        GameObject danny = GameObject.Find("Danny Player");
        Camera camera = Camera.main;
        Light sun = GameObject.Find("Directional Light")?.GetComponent<Light>() ?? UnityEngine.Object.FindFirstObjectByType<Light>();
        GameObject lightRoot = sun != null ? sun.transform.root.gameObject : null;
        if (danny == null || camera == null) throw new InvalidOperationException("Danny or his camera is missing");

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == danny || root == camera.gameObject || root == lightRoot) continue;
            UnityEngine.Object.DestroyImmediate(root);
        }
        danny.transform.SetPositionAndRotation(new Vector3(-66f, 0.08f, -5f), Quaternion.Euler(0f,90f,0f));
        camera.GetComponent<DannyFollowCamera>()?.Configure(danny.transform);
        world = new GameObject("EDMONTON RIVER VALLEY SCHOOL RUN").transform;

        Material snow = Mat("Snow", new Color(0.88f,0.94f,0.98f), 0.18f);
        Material packedSnow = Mat("Packed Snow", new Color(0.68f,0.82f,0.90f), 0.32f);
        Material road = Mat("Winter Road", new Color(0.10f,0.15f,0.21f), 0.12f);
        Material concrete = Mat("Cold Concrete", new Color(0.42f,0.49f,0.54f), 0.10f);
        Material rail = Mat("Stair Rails", new Color(0.12f,0.18f,0.24f), 0.62f);
        Material orange = Mat("Safety Orange", new Color(0.96f,0.28f,0.035f), 0.14f);
        Material red = Mat("Hazard Red", new Color(0.72f,0.055f,0.035f), 0.18f);
        Material dark = Mat("Rubber", new Color(0.025f,0.03f,0.035f), 0.08f);
        Material evergreen = Mat("Evergreen", new Color(0.055f,0.22f,0.16f), 0.06f);
        Material bark = Mat("Bark", new Color(0.19f,0.09f,0.04f), 0.04f);
        Material school = Mat("School Warm", new Color(0.55f,0.18f,0.08f), 0.12f);
        Material footprint = Mat("Pressed Boot Prints", new Color(0.24f,0.34f,0.42f), 0.02f);
        Material rabbit = Mat("Winter Rabbit", new Color(0.58f,0.43f,0.30f), 0.16f);
        Material coyote = Mat("Valley Coyote", new Color(0.46f,0.31f,0.18f), 0.12f);
        Material windowGlow = GlowMat("Warm Edmonton Windows", new Color(1f,0.56f,0.12f), 2.4f);
        Material festivalBlue = GlowMat("Festival Blue", new Color(0.08f,0.70f,1f), 1.8f);
        Material festivalPink = GlowMat("Festival Pink", new Color(1f,0.18f,0.55f), 1.6f);
        Material helloBubble = Mat("Friendly Hello Bubble", new Color(1f,0.96f,0.78f), 0.28f);

        ConfigureWinterLighting(sun, camera);

        BuildResidentialStreet(snow, packedSnow, road, concrete, rail,orange,footprint);
        BuildNeighbourhoodPolish(snow, evergreen, bark, dark, orange, windowGlow, festivalBlue, festivalPink);
        (Transform railStart, Transform railEnd, float bottomY, float bottomZ) = BuildRiverStairs(snow, concrete, rail,evergreen,bark,dark);
        BuildValleyTrail(snow, packedSnow, road, evergreen, bark, school, bottomY, bottomZ);

        DannySpark spark = danny.GetComponent<DannySpark>() ?? danny.AddComponent<DannySpark>();
        DannySparkHUD oldHud = danny.GetComponent<DannySparkHUD>();
        if (oldHud != null) UnityEngine.Object.DestroyImmediate(oldHud);
        DannyTestController movement = danny.GetComponent<DannyTestController>();
        CharacterController controller = danny.GetComponent<CharacterController>();
        SparkWorldMood mood = camera.GetComponent<SparkWorldMood>() ?? camera.gameObject.AddComponent<SparkWorldMood>();
        mood.Configure(spark);
        ParticleSystem storm = BuildPlayerSnow(danny.transform);
        RiverValleyWhiteout whiteout = world.gameObject.AddComponent<RiverValleyWhiteout>();
        whiteout.Configure(danny.transform, storm, sun, 88f, 242f, 330f);
        AudioClip windLoop = Audio(WinterAudioAssetGenerator.Wind);
        AudioClip snowSplat = Audio(WinterAudioAssetGenerator.Splat);
        AudioClip iceScrape = Audio(WinterAudioAssetGenerator.Ice);
        WinterAudioDirector audioDirector = world.gameObject.AddComponent<WinterAudioDirector>();
        audioDirector.Configure(danny.transform, whiteout, windLoop, snowSplat, iceScrape);
        Animator dannyAnimator = danny.GetComponentInChildren<Animator>();
        DannyWinterTrail winterTrail = dannyAnimator.GetComponent<DannyWinterTrail>() ??
            dannyAnimator.gameObject.AddComponent<DannyWinterTrail>();
        winterTrail.Configure(new[] { Audio(WinterAudioAssetGenerator.Crunch(1)), Audio(WinterAudioAssetGenerator.Crunch(2)),
            Audio(WinterAudioAssetGenerator.Crunch(3)), Audio(WinterAudioAssetGenerator.Crunch(4)) },
            Audio(WinterAudioAssetGenerator.Land), footprint);
        DannySparkFeedback feedback = world.gameObject.AddComponent<DannySparkFeedback>();
        feedback.Configure(spark, danny.transform, Audio(WinterAudioAssetGenerator.SparkUp),
            Audio(WinterAudioAssetGenerator.SparkDown));

        RiverValleyGameDirector director = world.gameObject.AddComponent<RiverValleyGameDirector>();
        RiverValleyMapSafety mapSafety=world.gameObject.AddComponent<RiverValleyMapSafety>();
        mapSafety.Configure(danny.transform,controller);
        GameObject mom = Cast("Mom", new Vector3(-79f,0.08f,-5f), 90f, "Mom — always coming, never quite close");
        RiverValleyAmbientActor momAmbient=mom.GetComponent<RiverValleyAmbientActor>();
        if(momAmbient!=null)UnityEngine.Object.DestroyImmediate(momAmbient);
        RiverValleyMomChase momChase = mom.AddComponent<RiverValleyMomChase>();
        momChase.Configure(danny.transform, director, whiteout);
        director.Configure(danny.transform, spark, movement, controller, whiteout, momChase, -5f, 418f);
        AlbertaNeighbourhoodLife neighbourhoodLife = world.gameObject.AddComponent<AlbertaNeighbourhoodLife>();
        neighbourhoodLife.Configure(helloBubble);

        BuildCastAndEncounters(director, railStart, railEnd, bottomY, bottomZ, rabbit, coyote, snow, orange, dark);
        BuildStreetExpansionAndSideAdventures(director,momChase,snow,packedSnow,road,concrete,
            evergreen,bark,rabbit,coyote,dark,orange,red,windowGlow);
        BuildRiverEmergencyRescue(momChase,bottomY,snow,orange,red,dark,windowGlow,
            Audio(WinterAudioAssetGenerator.EmergencySiren));
        BuildHazards(director, snow, orange, red, dark, bottomY, bottomZ,
            Audio(WinterAudioAssetGenerator.Plow), Audio(WinterAudioAssetGenerator.Bicycle), snowSplat);
        BuildWorldSafetyBoundary();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Validate();
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Edmonton river-valley school run created and validated: " + ScenePath);
        if (!batch) Selection.activeGameObject = danny;
    }

    private static void ConfigureWinterLighting(Light sun, Camera camera)
    {
        if (sun != null)
        {
            sun.type = LightType.Directional;
            sun.color = new Color(1f,0.86f,0.72f);
            sun.intensity = 1.22f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sun.transform.rotation = Quaternion.Euler(38f,-32f,0f);
        }
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.42f,0.66f,0.88f);
            camera.allowHDR = true;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.44f,0.64f,0.86f);
        RenderSettings.ambientEquatorColor = new Color(0.28f,0.40f,0.55f);
        RenderSettings.ambientGroundColor = new Color(0.16f,0.21f,0.29f);
        RenderSettings.reflectionIntensity = 0.72f;
        RenderSettings.fogColor = new Color(0.66f,0.78f,0.88f);
    }

    private static void BuildResidentialStreet(Material snow, Material packedSnow, Material road, Material concrete, Material rail,
        Material orange, Material routePrint)
    {
        // A real approach block now precedes the original street. Danny walks
        // east from home and makes one obvious left turn onto the northbound
        // school route instead of spawning directly in the long corridor.
        Primitive("West approach snow base",PrimitiveType.Cube,new Vector3(-42f,-0.42f,3f),
            new Vector3(92f,0.65f,92f),snow);
        PathSegment("West approach sidewalk",new Vector3(-73f,-0.10f,-5f),
            new Vector3(0f,-0.10f,-5f),4.2f,concrete);
        PathSegment("West approach residential road",new Vector3(-73f,-0.15f,2.4f),
            new Vector3(0f,-0.15f,2.4f),10f,road);
        PathSegment("West approach far sidewalk",new Vector3(-73f,-0.10f,9.2f),
            new Vector3(0f,-0.10f,9.2f),3.2f,concrete);
        SnowRidge("Snowbank closes the tempting wrong way",new Vector3(4.0f,0.02f,-5f),
            new Vector3(2.5f,0.92f,4.0f),snow,true);
        IconArrowSign("Approach school clue one",new Vector3(-55f,0f,-8.0f),1,"school",
            new Color(0.94f,0.42f,0.04f));
        IconArrowSign("Approach school clue two",new Vector3(-29f,0f,-8.0f),1,"school",
            new Color(0.18f,0.52f,0.84f));
        IconArrowSign("School corner left-turn clue",new Vector3(-4.0f,0f,-8.0f),0,"school",
            new Color(0.94f,0.42f,0.04f));
        for(int i=0;i<11;i++)
        {
            float x=-61f+i*5.2f;
            RouteBootPrint($"School-route bootprint {i+1}",
                new Vector3(x,0.018f,-5f+(i%2==0?-0.30f:0.30f)),90f,routePrint,i%2==0);
        }

        Primitive("Neighbourhood Snow Base", PrimitiveType.Cube, new Vector3(2f,-0.42f,62f), new Vector3(56f,0.65f,150f), snow);
        Primitive("Long Sidewalk", PrimitiveType.Cube, new Vector3(0f,-0.10f,62f), new Vector3(4.2f,0.20f,145f), concrete);
        Primitive("Residential Road", PrimitiveType.Cube, new Vector3(7.4f,-0.15f,62f), new Vector3(10f,0.22f,145f), road);
        Primitive("Far Sidewalk", PrimitiveType.Cube, new Vector3(14.2f,-0.10f,62f), new Vector3(3.2f,0.20f,145f), concrete);
        // Start beyond the corner so the approach sidewalk really joins this
        // block. The old continuous bank silently sealed the left turn.
        Primitive("Near Snowbank segment A",PrimitiveType.Cube,new Vector3(-2.65f,0.28f,28f),new Vector3(1.1f,0.62f,50f),packedSnow);
        Primitive("Near Snowbank segment B",PrimitiveType.Cube,new Vector3(-2.65f,0.28f,101.5f),new Vector3(1.1f,0.62f,67f),packedSnow);
        // Segmented roadside snowbanks leave real crosswalk openings. The old
        // single 145-metre collider made the road an accidental wall.
        Primitive("Road snowbank segment A",PrimitiveType.Cube,new Vector3(2.7f,0.24f,7.5f),new Vector3(0.75f,0.52f,36f),packedSnow);
        Primitive("Road snowbank segment B",PrimitiveType.Cube,new Vector3(2.7f,0.24f,51f),new Vector3(0.75f,0.52f,41f),packedSnow);
        Primitive("Road snowbank segment C",PrimitiveType.Cube,new Vector3(2.7f,0.24f,93f),new Vector3(0.75f,0.52f,33f),packedSnow);
        Primitive("Road snowbank segment D",PrimitiveType.Cube,new Vector3(2.7f,0.24f,120f),new Vector3(0.75f,0.52f,11f),packedSnow);
        foreach(float crossingZ in new[]{28f,74f,112f})
            for(int stripe=0;stripe<5;stripe++)
                Primitive($"Snowy crosswalk {crossingZ:0} stripe {stripe+1}",PrimitiveType.Cube,
                    new Vector3(5.1f+stripe*1.25f,-0.025f,crossingZ),new Vector3(0.72f,0.035f,3.6f),snow);

        PlaceCity("Buildings/Building_House_01_color01.prefab", "Danny's modest Jasper-area home", new Vector3(-80f,0f,-35f), Quaternion.Euler(0f,0f,0f), Vector3.one*0.52f);
        for(int i=0;i<4;i++)
            PlaceCity(i%2==0?"Buildings/Building_House_03_color03.prefab":"Buildings/Building_House_02_color03.prefab",
                $"West approach neighbour home {i+1}",new Vector3(-52f+i*14f,0f,40f),
                Quaternion.Euler(0f,180f,0f),Vector3.one*(0.52f+(i%2)*0.05f));
        string[] richHomes =
        {
            "Buildings/Building_House_04_color01.prefab", "Buildings/Building_House_03_color03.prefab",
            "Buildings/Building_House_04_color02.prefab", "Buildings/Building_House_02_color03.prefab"
        };
        for (int i=0;i<8;i++)
        {
            float z=18f+i*15f;
            // Leave deliberate gaps for both bonus fields. River-view home 5
            // previously overlapped the coyote exit and behaved like an
            // invisible waist-high shelf across Danny's return trail.
            if(i==3||i==4||i==5||i==7)continue;
            PlaceCity(richHomes[i%richHomes.Length], $"River-view home {i+1}", new Vector3(i%2==0?-23f:30f,0f,z),
                Quaternion.Euler(0f,i%2==0?90f:-90f,0f), Vector3.one*(1.02f+(i%3)*0.08f));
            if (i%2==0)
            {
                // The imported streetlight's mesh pivot left its orange lamp
                // floating beside a worker. Build a clearly grounded fixture.
                float lampZ=z+5f;
                Beam($"Winter street lamp post {i+1}",new Vector3(2.1f,0f,lampZ),
                    new Vector3(2.1f,4.5f,lampZ),0.085f,rail);
                Beam($"Winter street lamp arm {i+1}",new Vector3(2.1f,4.5f,lampZ),
                    new Vector3(3.0f,4.43f,lampZ),0.065f,rail);
                Primitive($"Winter street lamp bulb {i+1}",PrimitiveType.Sphere,
                    new Vector3(3.0f,4.38f,lampZ),Vector3.one*0.23f,orange);
            }
        }
        for (int i=0;i<6;i++)
        {
            if(i==3)continue; // keep the marked rabbit crossing visibly and physically open
            SnowRidge($"Wind-carved angular snow ridge {i+1}",
                new Vector3(-3.5f+(i%2)*18f,0.02f,12f+i*21f),
                new Vector3(2.4f,0.72f,4.4f),snow,true);
        }

        Primitive("Stair overlook deck", PrimitiveType.Cube, new Vector3(0f,-0.1f,131.5f), new Vector3(8.5f,0.22f,7f), concrete);
        Beam("Overlook left guardrail", new Vector3(-4.1f,0.1f,129f), new Vector3(-4.1f,1.25f,135f), 0.10f, rail);
        GameObject overlookRight=Beam("Overlook right guardrail", new Vector3(4.1f,0.1f,129f), new Vector3(4.1f,1.25f,135f), 0.10f, rail);
        // The winter loop joins from this side. Keep the rail visible but let
        // the broad, supported junction function as an entrance to the deck.
        UnityEngine.Object.DestroyImmediate(overlookRight.GetComponent<Collider>());
    }

    private static void BuildNeighbourhoodPolish(Material snow, Material evergreen, Material bark,
        Material dark, Material orange, Material windowGlow, Material festivalBlue, Material festivalPink)
    {
        GameObject polish = new("COLOURFUL ALBERTA WINTER DETAILS");
        polish.transform.SetParent(world,false);

        // Warm porch pools stop the winter street from reading as a flat dev
        // corridor. Only every second lamp casts light to keep the scene fast.
        for (int i=0;i<8;i++)
        {
            float z=13f+i*15.5f;
            float x=i%2==0?-5.7f:16.2f;
            GameObject lamp=new($"Warm porch lamp {i+1}");
            lamp.transform.SetParent(polish.transform,false);
            lamp.transform.localPosition=new Vector3(x,0f,z);
            VisualCylinder("Dark lamp post",lamp.transform,new Vector3(0f,1.45f,0f),new Vector3(0.055f,1.45f,0.055f),dark);
            VisualSphere("Golden lamp",lamp.transform,new Vector3(0f,2.92f,0f),Vector3.one*0.24f,windowGlow);
            if(i%2==0)
            {
                GameObject lightObject=new("Warm pool of light");
                lightObject.transform.SetParent(lamp.transform,false);
                lightObject.transform.localPosition=new Vector3(0f,2.75f,0f);
                Light light=lightObject.AddComponent<Light>();
                light.type=LightType.Point; light.color=new Color(1f,0.58f,0.25f);
                light.range=8.5f; light.intensity=2.2f; light.shadows=LightShadows.Soft;
            }
        }

        // Little spruces, snowmen and bright scarves give children landmarks
        // that are readable even once the storm starts to build.
        for(int i=0;i<10;i++)
        {
            float x=i%2==0?-6.1f:16.8f;
            float z=8f+i*12.3f;
            BuildReadableSpruce($"Neighbourhood spruce {i+1}",new Vector3(x,0f,z),
                3.3f+(i%3)*0.28f,evergreen,bark,snow,polish.transform);
        }
        // Edmonton's residential walks are lined with mature deciduous trees.
        // Keep the crowns high so the trunks remain obvious instead of reading
        // as green shapes floating above the snow.
        for(int i=0;i<13;i++)
        {
            float x=-72f+i*5.6f;
            float z=i%2==0?-10.7f:13.4f;
            BuildWinterStreetTree($"West approach boulevard tree {i+1}",new Vector3(x,0f,z),
                4.1f+(i%4)*0.28f,evergreen,bark,snow,polish.transform);
        }
        for(int i=0;i<12;i++)
        {
            float x=i%2==0?-6.4f:17.2f;
            float z=6f+i*10.6f;
            BuildWinterStreetTree($"Main-street boulevard tree {i+1}",new Vector3(x,0f,z),
                4.3f+(i%3)*0.32f,evergreen,bark,snow,polish.transform);
        }
        BuildSnowman(polish.transform,"Snowman waving near home",new Vector3(-4.8f,0f,20f),orange,dark,snow,festivalBlue);
        BuildSnowman(polish.transform,"Snowman at the crosswalk",new Vector3(16.1f,0f,70f),orange,dark,snow,festivalPink);
        BuildSnowman(polish.transform,"Snowman watching hockey",new Vector3(-4.9f,0f,106f),orange,dark,snow,festivalBlue);

        // A small festival-light canopy marks the dramatic stair overlook.
        Beam("Overlook festival cable",new Vector3(-4f,3.15f,129.2f),new Vector3(4f,3.15f,129.2f),0.025f,dark,polish.transform);
        for(int i=0;i<13;i++)
        {
            float x=-3.75f+i*0.625f;
            Material bulb=i%3==0?windowGlow:i%3==1?festivalBlue:festivalPink;
            VisualSphere($"Overlook festival bulb {i+1}",polish.transform,
                new Vector3(x,2.98f,129.2f),Vector3.one*0.12f,bulb);
        }
    }

    private static void BuildSnowman(Transform parent,string name,Vector3 position,Material orange,
        Material dark,Material snow,Material scarf)
    {
        GameObject root=new(name); root.transform.SetParent(parent,false); root.transform.localPosition=position;
        VisualSphere("Snow body",root.transform,new Vector3(0f,0.55f,0f),new Vector3(1.05f,1.10f,1.05f),snow);
        VisualSphere("Snow middle",root.transform,new Vector3(0f,1.28f,0f),new Vector3(0.78f,0.80f,0.78f),snow);
        VisualSphere("Snow head",root.transform,new Vector3(0f,1.89f,0f),new Vector3(0.59f,0.59f,0.59f),snow);
        VisualCube("Bright scarf",root.transform,new Vector3(0f,1.58f,0f),new Vector3(0.72f,0.12f,0.68f),scarf);
        VisualCube("Carrot nose",root.transform,new Vector3(0f,1.88f,-0.39f),new Vector3(0.10f,0.10f,0.36f),orange);
        VisualSphere("Coal eye left",root.transform,new Vector3(-0.15f,2.00f,-0.27f),Vector3.one*0.065f,dark);
        VisualSphere("Coal eye right",root.transform,new Vector3(0.15f,2.00f,-0.27f),Vector3.one*0.065f,dark);
        // Thin branch arms read exactly like detached hockey sticks at a
        // distance. Chunky mittens keep the wave without adding stray lines
        // across the scene.
        VisualSphere("Snowman mitten left",root.transform,new Vector3(-0.48f,1.42f,0f),
            new Vector3(0.16f,0.18f,0.16f),scarf);
        VisualSphere("Snowman mitten right",root.transform,new Vector3(0.48f,1.42f,0f),
            new Vector3(0.16f,0.18f,0.16f),scarf);
    }

    private static (Transform,Transform,float,float) BuildRiverStairs(Material snow, Material concrete, Material rail,
        Material evergreen, Material bark, Material dark)
    {
        GameObject stairs = new("Edmonton river-valley stairs — 180 steps");
        stairs.transform.SetParent(world,false);
        for (int i=0;i<StairCount;i++)
        {
            float y=-i*StairRise-0.09f;
            float z=StairZ(i);
            Primitive($"Stair {i+1:000}",PrimitiveType.Cube,new Vector3(0f,y,z),new Vector3(6.2f,0.18f,StairDepth+0.035f),concrete,stairs.transform);
            if (i%StepsPerFlight==StepsPerFlight-1 && i<StairCount-1)
            {
                float landingTop=-i*StairRise;
                float landingStart=z+StairDepth*0.5f;
                float landingEnd=StairZ(i+1)-StairDepth*0.5f;
                float landingLength=landingEnd-landingStart;
                Primitive($"Level stair landing {(i+1)/StepsPerFlight}",PrimitiveType.Cube,
                    new Vector3(0f,landingTop-0.09f,(landingStart+landingEnd)*0.5f),
                    new Vector3(7.4f,0.18f,landingLength+0.03f),concrete,stairs.transform);
                Primitive($"Packed snow on landing {(i+1)/StepsPerFlight}",PrimitiveType.Cube,
                    new Vector3(0f,landingTop+0.012f,(landingStart+landingEnd)*0.5f),
                    new Vector3(7.2f,0.024f,landingLength-0.10f),snow,stairs.transform);
                GameObject landingLeft=Beam($"Landing {(i+1)/StepsPerFlight} left rail",
                    new Vector3(-3.45f,landingTop+0.82f,landingStart),new Vector3(-3.45f,landingTop+0.82f,landingEnd),0.10f,rail,stairs.transform);
                GameObject landingRight=Beam($"Landing {(i+1)/StepsPerFlight} right rail",
                    new Vector3(3.45f,landingTop+0.82f,landingStart),new Vector3(3.45f,landingTop+0.82f,landingEnd),0.10f,rail,stairs.transform);
                UnityEngine.Object.DestroyImmediate(landingLeft.GetComponent<Collider>());
                UnityEngine.Object.DestroyImmediate(landingRight.GetComponent<Collider>());
            }
        }
        // Invisible upright side walls follow the flights in short sections.
        // They behave like real stair containment, without turning a single
        // diagonal rail collider into a launch ramp.
        for(int first=0;first<StairCount;first+=9)
        {
            int last=Mathf.Min(StairCount-1,first+8);
            float zStart=StairZ(first)-StairDepth*0.55f;
            float zEnd=StairZ(last)+StairDepth*0.55f;
            float centreY=-(first+last)*0.5f*StairRise+0.72f;
            for(int side=-1;side<=1;side+=2)
            {
                GameObject wall=Primitive($"Invisible stair edge {first+1:000}-{last+1:000} {(side<0?"left":"right")}",
                    PrimitiveType.Cube,new Vector3(side*3.30f,centreY,(zStart+zEnd)*0.5f),
                    new Vector3(0.24f,2.65f,zEnd-zStart),concrete,stairs.transform);
                UnityEngine.Object.DestroyImmediate(wall.GetComponent<Renderer>());
            }
        }
        float bottomY=-(StairCount-1)*StairRise;
        float bottomZ=StairZ(StairCount-1);
        Primitive("Road-level bottom stair landing",PrimitiveType.Cube,
            new Vector3(0f,bottomY-0.09f,bottomZ+1.55f),new Vector3(7.8f,0.18f,3.1f),concrete,stairs.transform);
        Primitive("Snow cap on bottom stair landing",PrimitiveType.Cube,
            new Vector3(0f,bottomY+0.012f,bottomZ+1.55f),new Vector3(7.6f,0.024f,3.0f),snow,stairs.transform);
        Vector3 railTop=new(0f,0.82f,StairTopZ-0.45f);
        Vector3 railBottom=new(0f,bottomY+0.82f,bottomZ+0.45f);
        GameObject dareRail = Beam("Danny dare rail",railTop,railBottom,0.115f,rail,stairs.transform);
        GameObject leftRail = Beam("Left stair handrail",railTop+Vector3.left*2.85f,railBottom+Vector3.left*2.85f,0.10f,rail,stairs.transform);
        GameObject rightRail = Beam("Right stair handrail",railTop+Vector3.right*2.85f,railBottom+Vector3.right*2.85f,0.10f,rail,stairs.transform);
        // These are visual rails. Letting the CharacterController step onto
        // their long diagonal colliders turned them into invisible ramps and
        // made Danny appear able to fly above the stairs.
        UnityEngine.Object.DestroyImmediate(dareRail.GetComponent<Collider>());
        UnityEngine.Object.DestroyImmediate(leftRail.GetComponent<Collider>());
        UnityEngine.Object.DestroyImmediate(rightRail.GetComponent<Collider>());
        Transform start=new GameObject("Rail Ride Start").transform;
        start.SetParent(stairs.transform,false); start.position=railTop+Vector3.up*0.55f;
        Transform end=new GameObject("Rail Ride Finish").transform;
        end.SetParent(stairs.transform,false); end.position=railBottom+Vector3.up*0.30f+Vector3.forward*1.4f;
        BuildStairTreeTunnel(stairs.transform,snow,evergreen,bark,dark);
        BuildStairSwingRing(stairs.transform,Vector3.Lerp(railTop,railBottom,0.44f),rail);
        return (start,end,bottomY,bottomZ);
    }

    private static void BuildStairTreeTunnel(Transform parent,Material snow,Material evergreen,Material bark,Material dark)
    {
        GameObject tunnel=new("Spruce-and-owl stair tunnel");
        tunnel.transform.SetParent(parent,false);
        Material owlBrown=Mat("River Owl Brown",new Color(0.26f,0.17f,0.10f),0.14f);
        Material owlEye=GlowMat("River Owl Eyes",new Color(1f,0.68f,0.06f),1.7f);
        for(int i=0;i<16;i++)
        {
            int step=Mathf.Min(StairCount-1,5+i*11);
            float ground=-step*StairRise;
            float z=StairZ(step);
            for(int sideIndex=0;sideIndex<2;sideIndex++)
            {
                float side=sideIndex==0?-1f:1f;
                GameObject tree=BuildReadableSpruce($"Stair tunnel spruce {i+1} {(side<0?"west":"east")}",
                    new Vector3(side*(4.55f+(i%3)*0.22f),ground,z),5.1f+(i%3)*0.22f,
                    evergreen,bark,snow,tunnel.transform);
                if((i+sideIndex)%5==1)
                {
                    GameObject owl=new($"Watchful river owl {i+1}-{sideIndex+1}");
                    owl.transform.SetParent(tree.transform,false);
                    owl.transform.localPosition=new Vector3(-side*0.55f,2.38f,-0.25f);
                    VisualSphere("Owl body",owl.transform,new Vector3(0f,0.18f,0f),new Vector3(0.26f,0.38f,0.24f),owlBrown);
                    VisualSphere("Owl head",owl.transform,new Vector3(0f,0.48f,-0.03f),new Vector3(0.30f,0.28f,0.27f),owlBrown);
                    VisualSphere("Owl eye left",owl.transform,new Vector3(-0.10f,0.51f,-0.24f),Vector3.one*0.052f,owlEye);
                    VisualSphere("Owl eye right",owl.transform,new Vector3(0.10f,0.51f,-0.24f),Vector3.one*0.052f,owlEye);
                }
            }
        }

        int[] branchSteps={52,104,151};
        for(int i=0;i<branchSteps.Length;i++)
        {
            int step=branchSteps[i];
            float ground=-step*StairRise;
            float z=StairZ(step);
            GameObject trigger=new($"Low hanging trick branch {i+1}");
            trigger.transform.SetParent(tunnel.transform,false);
            trigger.transform.localPosition=new Vector3(0f,ground+2.05f,z);
            BoxCollider collider=trigger.AddComponent<BoxCollider>();
            collider.isTrigger=true;
            collider.size=new Vector3(5.35f,0.34f,1.05f);
            trigger.AddComponent<RiverValleyLowBranch>();
            GameObject branch=Beam("Low spruce branch",new Vector3(-3.1f,ground+2.05f,z),
                new Vector3(3.1f,ground+2.05f,z+0.16f),0.13f,bark,trigger.transform);
            RemoveCollider(branch);
            for(int needle=0;needle<5;needle++)
                VisualSphere($"Snowy hanging needles {needle+1}",trigger.transform,
                    new Vector3(-2.0f+needle,0f,0.05f),new Vector3(0.62f,0.28f,0.44f),evergreen);
        }
    }

    private static void BuildStairSwingRing(Transform parent, Vector3 railPoint, Material rail)
    {
        Material ringMaterial=Mat("Playground Ring Orange",new Color(1f,0.34f,0.035f),0.42f);
        GameObject rig=new("Stair Swing Ring Target");
        rig.transform.SetParent(parent,true);
        rig.transform.position=railPoint+Vector3.up*1.90f;
        rig.AddComponent<RiverValleySwingRing>();

        Vector3 anchor=rig.transform.position;
        Vector3 crossbarCenter=anchor+Vector3.up*1.18f;
        RemoveCollider(Beam("Swing ring overhead bar",crossbarCenter+Vector3.left*3.15f,
            crossbarCenter+Vector3.right*3.15f,0.10f,rail,rig.transform));
        RemoveCollider(Beam("Swing ring left support",new Vector3(-3.15f,railPoint.y+0.12f,railPoint.z),
            crossbarCenter+Vector3.left*3.15f,0.10f,rail,rig.transform));
        RemoveCollider(Beam("Swing ring right support",new Vector3(3.15f,railPoint.y+0.12f,railPoint.z),
            crossbarCenter+Vector3.right*3.15f,0.10f,rail,rig.transform));
        RemoveCollider(Beam("Swing ring left strap",anchor+new Vector3(-0.22f,0.31f,0f),
            crossbarCenter+Vector3.left*0.55f,0.035f,ringMaterial,rig.transform));
        RemoveCollider(Beam("Swing ring right strap",anchor+new Vector3(0.22f,0.31f,0f),
            crossbarCenter+Vector3.right*0.55f,0.035f,ringMaterial,rig.transform));

        const int segments=18;
        const float radius=0.34f;
        for(int i=0;i<segments;i++)
        {
            float a0=Mathf.PI*2f*i/segments;
            float a1=Mathf.PI*2f*(i+1)/segments;
            Vector3 p0=anchor+new Vector3(Mathf.Cos(a0)*radius,Mathf.Sin(a0)*radius,0f);
            Vector3 p1=anchor+new Vector3(Mathf.Cos(a1)*radius,Mathf.Sin(a1)*radius,0f);
            RemoveCollider(Beam($"Orange grab ring segment {i+1:00}",p0,p1,0.052f,ringMaterial,rig.transform));
        }
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider collider=go != null ? go.GetComponent<Collider>() : null;
        if(collider != null) UnityEngine.Object.DestroyImmediate(collider);
    }

    private static void BuildValleyTrail(Material snow, Material packedSnow, Material road, Material evergreen, Material bark, Material school, float bottomY, float bottomZ)
    {
        float trailStart=bottomZ+2f;
        float trailEnd=425f;
        float length=trailEnd-trailStart;
        // The valley is intentionally asymmetric: city-centre hill and coyote
        // woods on the left, the North Saskatchewan River on the right.
        Primitive("City-centre valley floor left",PrimitiveType.Cube,new Vector3(-19f,bottomY-0.55f,(trailStart+trailEnd)*0.5f),new Vector3(31f,0.9f,length+18f),snow);
        Primitive("River-bank valley floor right",PrimitiveType.Cube,new Vector3(10.5f,bottomY-0.55f,(trailStart+trailEnd)*0.5f),new Vector3(14f,0.9f,length+18f),snow);
        Primitive("Shared-use river valley path",PrimitiveType.Cube,new Vector3(0f,bottomY-0.08f,(trailStart+trailEnd)*0.5f),new Vector3(7f,0.18f,length),road);
        Primitive("Icy path edge left",PrimitiveType.Cube,new Vector3(-4.15f,bottomY, (trailStart+trailEnd)*0.5f),new Vector3(1.3f,0.28f,length),packedSnow);
        Primitive("Icy path edge right",PrimitiveType.Cube,new Vector3(4.15f,bottomY, (trailStart+trailEnd)*0.5f),new Vector3(1.3f,0.28f,length),packedSnow);
        Primitive("City-centre hill west of path",PrimitiveType.Cube,new Vector3(-34f,bottomY+3.2f,(trailStart+trailEnd)*0.5f),new Vector3(23f,7f,length+20f),snow);
        Primitive("Far river embankment east of water",PrimitiveType.Cube,new Vector3(38f,bottomY+2.2f,(trailStart+trailEnd)*0.5f),new Vector3(15f,5f,length+20f),snow);
        Material river=Mat("Frozen North Saskatchewan River",new Color(0.25f,0.50f,0.66f),0.72f);
        Primitive("North Saskatchewan River — right side",PrimitiveType.Cube,new Vector3(24f,bottomY-0.92f,315f),new Vector3(15f,0.30f,205f),river);

        // The first route choice begins at the stair bottom. Both forest
        // branches eventually rejoin the school trail, but they hold different
        // children and encounters.
        PathSegment("Y trail — city-centre forest left",new Vector3(0f,bottomY,bottomZ+3f),new Vector3(-19f,bottomY,237f),5.2f,road);
        PathSegment("Y trail — river-view right",new Vector3(0f,bottomY,bottomZ+3f),new Vector3(14f,bottomY,237f),5.2f,road);
        PathSegment("Y trail — left return",new Vector3(-19f,bottomY,237f),new Vector3(-4f,bottomY,270f),5.2f,road);
        PathSegment("Y trail — right return",new Vector3(14f,bottomY,237f),new Vector3(4f,bottomY,270f),5.2f,road);
        SnowRidge("Split-route climbable snow ridge",new Vector3(0f,bottomY,bottomZ+17f),new Vector3(6.2f,1.4f,4.5f),snow,true);
        IconArrowSign("Buried school sign",new Vector3(-4.35f,bottomY,bottomZ+7f),0,"school",new Color(0.92f,0.58f,0.08f));
        IconArrowSign("City-centre hill sign",new Vector3(-5.15f,bottomY,bottomZ+12f),-1,"paw",new Color(0.22f,0.55f,0.34f));
        IconArrowSign("River view sign",new Vector3(5.15f,bottomY,bottomZ+12f),1,"river",new Color(0.38f,0.64f,0.84f));

        for (int i=0;i<34;i++)
        {
            float side=i%2==0?-1f:1f;
            float x=side*(8f+(i%5)*2.25f);
            float z=trailStart+5f+i*6.1f;
            float h=4.2f+(i%4)*0.75f;
            BuildReadableSpruce($"Valley spruce {i+1}",new Vector3(x,bottomY,z),h,evergreen,bark,snow);
        }
        for(int i=0;i<22;i++)
        {
            float side=i%2==0?-1f:1f;
            float x=side*(9f+(i%5)*2.4f);
            float z=218f+i*3.0f;
            float h=3.8f+(i%4)*0.55f;
            BuildReadableSpruce($"Choice forest spruce {i+1}",new Vector3(x,bottomY,z),h,evergreen,bark,snow);
        }
        for(int i=0;i<7;i++)
        {
            GameObject patch = Primitive($"Valley ice patch {i+1}",PrimitiveType.Cylinder,
                new Vector3((i%3-1)*1.2f,bottomY+0.025f,238f+i*24f),new Vector3(1.25f,0.025f,2.4f),packedSnow);
            patch.GetComponent<Collider>().isTrigger = true;
            patch.AddComponent<RiverValleySlipPatch>();
        }

        // Alternating snowdrifts create a readable slalom instead of a long,
        // empty developer corridor. Every gate retains a generous child-sized
        // route through it, even in the whiteout.
        SnowRidge("Windblown drift gate — left",new Vector3(-2.15f,bottomY+0.02f,265f),new Vector3(3.2f,1.05f,2.2f),snow,true);
        SnowRidge("Windblown drift gate — right",new Vector3(2.05f,bottomY+0.02f,298f),new Vector3(3.1f,1.08f,2.4f),snow,true);
        SnowRidge("Whiteout drift gate — left",new Vector3(-2.45f,bottomY+0.02f,332f),new Vector3(2.4f,0.92f,2.0f),snow,true);
        SnowRidge("Whiteout drift gate — right",new Vector3(2.45f,bottomY+0.02f,332f),new Vector3(2.4f,0.92f,2.0f),snow,true);
        SnowRidge("School approach drift",new Vector3(0.9f,bottomY+0.02f,389f),new Vector3(3.5f,1.0f,2.4f),snow,true);

        PlaceCity("Buildings/Building_Residential_color01.prefab","The Little Public School",new Vector3(-9f,bottomY,418f),Quaternion.Euler(0f,90f,0f),Vector3.one*1.35f);
        Primitive("THE LITTLE PUBLIC SCHOOL — WARM ENTRANCE",PrimitiveType.Cube,new Vector3(0f,bottomY+2.4f,419f),new Vector3(7.5f,1.0f,0.35f),school);
        SchoolNameSign(new Vector3(0f,bottomY+3.75f,418.55f),"THE LITTLE PUBLIC SCHOOL",school);
        Primitive("School finish path",PrimitiveType.Cube,new Vector3(0f,bottomY-0.04f,416f),new Vector3(8f,0.22f,12f),packedSnow);
    }

    private static void BuildCastAndEncounters(RiverValleyGameDirector director, Transform railStart, Transform railEnd,
        float bottomY, float bottomZ, Material rabbitMaterial, Material coyoteMaterial, Material snow, Material orange, Material dark)
    {
        ParentHandoff("Parent handoff on Danny's approach block",new Vector3(-47f,0.08f,-5f),"MRS. NGUYEN",
            "Morning, Danny. These three are headed to school too. Could they join your orange-coat crew?");
        GameObject dog=Cast("DogWalkerTheo",new Vector3(0.7f,0.08f,26f),180f,"Theo and the very pettable dog");
        Encounter("Pet the dog",RiverEncounterKind.PetDog,new Vector3(0.7f,1f,26f),new Vector3(3.6f,2.2f,4f),"THEO","He found something under the snow. Also, he has selected you as his best friend.","Pet the dog",9f,0f,true,dog.transform);

        GameObject nookKid1=Cast("SnowFortKid",new Vector3(-5.2f,0.08f,35f),125f,"Child hiding behind Danny's street snowbank");
        Encounter("Hidden child near home",RiverEncounterKind.LostKid,new Vector3(-1.3f,1f,35f),new Vector3(7.8f,2.5f,5f),"NOOK KID","I was building a fort and the street disappeared. Danny: Join the school group. Fort citizenship can wait.","Bring the hidden child along",13f,40f,true,nookKid1.transform);
        Coyote("Alley coyote watching the early snowbanks",new Vector3(6.2f,0.08f,39f),coyoteMaterial,dark);
        GameObject rose=Cast("NeighbourRose",new Vector3(-0.8f,0.08f,51f),180f,"Neighbour Rose on the ice");
        rose.transform.rotation=Quaternion.Euler(0f,180f,8f);
        Encounter("Help Rose up",RiverEncounterKind.PositiveAdult,new Vector3(-0.8f,1f,51f),new Vector3(4f,2.2f,4f),"ROSE","Thank you, Danny. Kindness is warmer than these ridiculous mittens.","Help Rose stand",14f,0f,true,rose.transform);
        GameObject nookKid2=Cast("ClassmateMaya",new Vector3(4.6f,0.08f,66f),220f,"Child waiting beside a buried driveway");
        Encounter("Hidden child driveway",RiverEncounterKind.LostKid,new Vector3(0.8f,1f,66f),new Vector3(8f,2.5f,5f),"DRIVEWAY KID","The plow made a mountain and my shortcut became geography. Can I come with you?","Add the child to the school group",13f,42f,true,nookKid2.transform);
        GameObject elder2=Cast("NeighbourRose",new Vector3(1.1f,0.08f,72f),180f,"Older neighbour with dropped groceries");
        Encounter("Gather older neighbour groceries",RiverEncounterKind.PositiveAdult,new Vector3(1.1f,1f,72f),new Vector3(4f,2.3f,4f),"MR. PATEL","My oranges made a break for the river. Thank you for catching the ambitious ones.","Help gather the groceries",9f,0f,true,elder2.transform);
        WaitPost("Streetlight group stop",new Vector3(1.9f,0f,77f),"the amber streetlight",orange,dark);
        Coyote("Urban coyote following the street plow",new Vector3(-6.1f,0.08f,76f),coyoteMaterial,dark);

        GameObject guard=Cast("CrossingGuard",new Vector3(0f,0.08f,82f),180f,"Crossing guard storm warning");
        Encounter("Storm warning",RiverEncounterKind.StormWarning,new Vector3(0f,1f,82f),new Vector3(4f,2.2f,4f),"CROSSING GUARD","Storm is coming fast. Keep the smaller kids together and use the lights.","Listen and promise to help",10f,0f,true,guard.transform);
        Encounter("Mom sees Danny's group",RiverEncounterKind.MomCall,new Vector3(0f,1f,94f),new Vector3(6f,2.6f,2f),"MOTHER","I see you being responsible, Pooky-Wooky! Mommy is watching!","",8f,0f,false);

        // These actors use stick-free bodies. WinterCastActivity supplies the single
        // grounded gameplay stick, avoiding the imported hockey accessories that can
        // detach from their hands when the humanoid animation is retargeted.
        GameObject goalie=Cast("SnowFortKid",new Vector3(-1.4f,0.08f,108f),180f,"Street-hockey goalie");
        GameObject winger=Cast("ClassmateMaya",new Vector3(1.5f,0.08f,112f),180f,"Street-hockey winger");
        goalie.AddComponent<WinterCastActivity>().Configure(WinterCastActivityKind.HockeyGoalie);
        winger.AddComponent<WinterCastActivity>().Configure(WinterCastActivityKind.HockeyStickhandler);
        GameObject rally=new("Street hockey tennis-ball rally");
        rally.transform.SetParent(world,true);
        rally.AddComponent<WinterHockeyRally>().Configure(goalie.transform,winger.transform);
        Encounter("One long street hockey game",RiverEncounterKind.PlayfulKid,new Vector3(0f,1f,110f),new Vector3(6f,2.3f,7f),"HOCKEY KIDS","First to three before school. The bell cannot see us from here.","Play a real street-hockey game",12f,0f,true,winger.transform);
        GameObject rabbitTop=Rabbit("Overlook rabbit",new Vector3(3.1f,0.15f,122f),rabbitMaterial,dark);
        Encounter("Pet overlook rabbit",RiverEncounterKind.PetRabbit,new Vector3(2.2f,0.8f,122f),new Vector3(3.6f,1.8f,3.5f),"DANNY","The rabbit accepts three pats, one nose twitch, and no further paperwork.","Pet the rabbit",7f,0f,true,rabbitTop.transform);
        GameObject teens=Cast("DogWalkerTheo",new Vector3(2.6f,0.08f,126f),210f,"Teenager advice trap");
        Encounter("Teen advice trap",RiverEncounterKind.TeenAdvice,new Vector3(2.6f,1f,126f),new Vector3(3f,2.2f,3f),"TEENAGER","Stay in school. Danny: I am literally trying to get there. Teenager: Good. The advice is working.","Attempt to leave politely",5f,0f,true,teens.transform);
        WaitPost("Overlook group stop",new Vector3(-3.25f,0f,126f),"the stair overlook",orange,dark);
        GameObject spectatorFamily=ParentHandoff("Snowboard spectator family",new Vector3(-3.4f,0.08f,119f),"MR. RIVARD",
            "Okay, Danny—take these three to school so I can watch the next sick trick. Kids: Dad! Parent: Educationally sick.");
        RiverValleyParentWalker spectatorWalker=spectatorFamily.GetComponent<RiverValleyParentWalker>();
        if(spectatorWalker!=null)spectatorWalker.enabled=false;
        GameObject dare=Cast("SledKid",new Vector3(-2.6f,0.08f,128.5f),110f,"Kid daring Danny at the stairs");
        dare.AddComponent<WinterCastActivity>().Configure(WinterCastActivityKind.Snowboarder);
        GameObject practiceBoarder=Cast("SledKid",new Vector3(4.6f,0.08f,124.5f),205f,"Snowboarder practising at the overlook");
        practiceBoarder.AddComponent<WinterCastActivity>().Configure(WinterCastActivityKind.Snowboarder);
        Encounter("Edmonton hockey-card stair dare",RiverEncounterKind.SkateDare,new Vector3(0f,1f,130.5f),new Vector3(7f,2.4f,6f),"SNOWBOARD KID","One Edmonton hockey card says you cannot ride the centre rail. I stake my rare Northern Lights Goalie.","Stake one card and accept the snowboard dare",0f,0f,true,dare.transform,railStart,railEnd);

        float bottomGround=bottomY+0.08f;
        GameObject kid1=Cast("SnowFortKid",new Vector3(-1.4f,bottomGround,bottomZ+18f),180f,"Lost kid at the stair bottom");
        Encounter("Lost kid one",RiverEncounterKind.LostKid,new Vector3(-1.4f,bottomY+1f,bottomZ+18f),new Vector3(4f,2.3f,4f),"LOST KID","I lost the group in the snow. Danny: Stay with me. We are making our own group.","Bring the lost kid along",15f,55f,true,kid1.transform);
        GameObject teenHelper=Cast("DogWalkerTheo",new Vector3(3.3f,bottomGround,bottomZ+31f),200f,"Helpful teenager at stair bottom");
        Encounter("Helpful teen huddles group",RiverEncounterKind.HelpfulTeen,new Vector3(1.7f,bottomY+1f,bottomZ+31f),new Vector3(5f,2.4f,4f),"TEEN HELPER","I can keep everyone at the lamp while you check the next snowbank. Little kids: mittens together.","Ask the teenager to help the group",8f,0f,true,teenHelper.transform);
        WaitPost("Valley lamp group stop",new Vector3(-3.25f,bottomY,bottomZ+38f),"the valley lamp",orange,dark);
        GameObject rabbitValley=Rabbit("Valley trail rabbit",new Vector3(-5.1f,bottomGround,268f),rabbitMaterial,dark);
        Encounter("Pet valley rabbit",RiverEncounterKind.PetRabbit,new Vector3(-2.6f,bottomY+0.8f,268f),new Vector3(5.5f,2f,4f),"MAYA","Can we pet it? Danny: One gentle pat each. The rabbit has a schedule.","Let the group pet the rabbit",8f,0f,true,rabbitValley.transform);
        GameObject kid2=Cast("ClassmateMaya",new Vector3(1.6f,bottomGround,278f),180f,"Maya in the building storm");
        Encounter("Lost kid Maya",RiverEncounterKind.LostKid,new Vector3(1.6f,bottomY+1f,278f),new Vector3(4f,2.3f,4f),"MAYA","I could not see the stairs anymore. Danny: Then follow my orange hood.","Lead Maya through the storm",16f,55f,true,kid2.transform);
        GameObject nookKid3=Cast("SledKid",new Vector3(5.2f,bottomGround,291f),260f,"Child hidden behind a valley snow pile");
        Encounter("Hidden snow-pile child",RiverEncounterKind.LostKid,new Vector3(1.8f,bottomY+1f,291f),new Vector3(8f,2.4f,5f),"SNOW-PILE KID","I was digging a tunnel. Then weather happened. Danny: Weather does that here.","Gather the tunnel builder",15f,52f,true,nookKid3.transform);
        GameObject neighbour=Cast("DogWalkerTheo",new Vector3(-3.1f,bottomGround,304f),145f,"Unhoused neighbour Sam near the warming vent");
        Encounter("Share Danny's lunch",RiverEncounterKind.GiveLunch,new Vector3(-1.3f,bottomY+1f,304f),new Vector3(5.5f,2.5f,5f),"SAM","You keep half, young man. Storms are easier when nobody pretends they are alone.","Share Danny's lunch with Sam",18f,0f,true,neighbour.transform);
        GameObject crow=Cast("DogWalkerTheo",new Vector3(5.7f,bottomGround,310f),250f,"Distant witness to the crows");
        Encounter("Crow remembers Mom",RiverEncounterKind.CrowEcho,new Vector3(0f,bottomY+1f,310f),new Vector3(7f,2.5f,5f),"CROW","PUMPKIN-PANTS!  SNUGGLE-BOTTOM!  EMERGENCY KISSES!  Danny: This name will outlive me.","Acknowledge the impossible bird",3f,0f,true,crow.transform);
        GameObject coyote=Coyote("Curious valley coyote",new Vector3(5.4f,bottomGround,322f),coyoteMaterial,dark);
        Encounter("Coyote kid comedy",RiverEncounterKind.CoyoteScare,new Vector3(0f,bottomY+1f,322f),new Vector3(9f,2.5f,5f),"COYOTE","The coyote stops, sneezes, and one child immediately declares a new career.","",0f,0f,false,coyote.transform);
        GameObject kid3=Cast("SnowFortKid",new Vector3(-1.5f,bottomGround,338f),180f,"Lost snow-fort kid in whiteout");
        Encounter("Lost kid whiteout",RiverEncounterKind.LostKid,new Vector3(-1.5f,bottomY+1f,338f),new Vector3(4f,2.3f,4f),"LOST KID","I only found you because your coat is the last colour left.","Guide the child to school",18f,62f,true,kid3.transform);
        WaitPost("Whiteout group stop",new Vector3(3.2f,bottomY,344f),"the blue emergency pole",orange,dark);
        Encounter("Sam's whiteout warning",RiverEncounterKind.HelperRescue,new Vector3(0f,bottomY+1f,357f),new Vector3(7f,2.8f,2f),"SAM","Danny—right side, now! The plow is hidden in its own snow cloud!","",0f,0f,false,neighbour.transform);
        GameObject elder3=Cast("NeighbourRose",new Vector3(-2.7f,bottomGround,374f),150f,"Older walker in the whiteout");
        Encounter("Guide older walker",RiverEncounterKind.PositiveAdult,new Vector3(-1f,bottomY+1f,374f),new Vector3(5.5f,2.5f,5f),"MS. CHEN","I can hear your group even when I cannot see the path. May I walk behind the orange hood too?","Guide Ms. Chen through the whiteout",12f,0f,true,elder3.transform);
        GameObject teen2=Cast("DogWalkerTheo",new Vector3(2.5f,bottomGround,391f),190f,"Helpful teen at school approach");
        Encounter("Helpful teen counts the group",RiverEncounterKind.HelpfulTeen,new Vector3(1.2f,bottomY+1f,391f),new Vector3(5f,2.5f,5f),"TEEN HELPER","I count heads; you watch the path. Nobody becomes a coyote twice.","Let the teenager count the children",8f,0f,true,teen2.transform);

        // Warm-grey rabbits remain readable against the white ground. Only
        // two are interaction objectives; the others make the route feel alive.
        Rabbit("Front-yard rabbit",new Vector3(-4.8f,0.15f,58f),rabbitMaterial,dark);
        Rabbit("Snowbank rabbit",new Vector3(5.0f,0.15f,118f),rabbitMaterial,dark);
        Rabbit("River path rabbit west",new Vector3(-5.0f,bottomGround,286f),rabbitMaterial,dark);
        Rabbit("River path rabbit east",new Vector3(5.1f,bottomGround,334f),rabbitMaterial,dark);
        // Keep the whole animated body on the broad packed path. At x=-5 its
        // rear paws straddled the raised path edge and appeared buried.
        Rabbit("School approach rabbit",new Vector3(-3.8f,bottomGround,388f),rabbitMaterial,dark);

        // Sixteen tucked-away clusters of four or five, plus four parent
        // handoffs of three children and the individual children above, make
        // a deliberately ridiculous school train. Side-path placements reward
        // looking around instead of only holding forward.
        // Two groups sit on snow-built "impossible" perches. They look like
        // platform-game prizes from the street, but each has a narrow bank of
        // climbable snow steps for players who explore instead of holding W.
        PrizePerch("Secret fort prize perch",new Vector3(-5.4f,0.08f,18f),1.85f,snow,orange);
        PrizePerch("Overlook garage prize perch",new Vector3(-5.2f,0.08f,116f),2.10f,snow,orange);
        Vector3[] hiddenClusters=
        {
            new(-5.4f,1.93f,18f), new(15.0f,0.08f,40f), new(-5.2f,2.18f,116f),
            new(-4.0f,-45f*StairRise+0.08f,StairZ(45)),
            new(4.0f,-90f*StairRise+0.08f,StairZ(90)),
            new(-4.0f,-135f*StairRise+0.08f,StairZ(135)),
            new(-13f,bottomGround,229f), new(13f,bottomGround,229f),
            new(-18f,bottomGround,245f), new(18f,bottomGround,245f),
            new(-5.2f,bottomGround,286f), new(5.2f,bottomGround,366f),
            new(-9.5f,0.08f,54f), new(9.5f,0.08f,102f),
            new(-5.0f,bottomGround,318f), new(5.0f,bottomGround,402f)
        };
        for(int i=0;i<hiddenClusters.Length;i++)
        {
            int clusterSize=4+(i%2);
            GameObject cluster=KidCluster($"Hidden school-child cluster {i+1:00}",hiddenClusters[i],clusterSize);
            bool prizeKids=i==0||i==2;
            RiverValleyEncounter clusterEncounter=Encounter($"Find hidden child cluster {i+1:00}",RiverEncounterKind.LostKid,
                hiddenClusters[i]+Vector3.up,new Vector3(5.5f,prizeKids?6.5f:2.5f,5.5f),prizeKids?"SECRET KIDS":"HIDDEN KIDS",
                prizeKids
                    ? "You found the snow-perch kids! Danny: That looked impossible from the sidewalk. Kids: That was the point."
                    : "We found a brilliant hiding place. Unfortunately, it also hid the way to school. Danny: Orange hood, this way.",
                prizeKids?"Climb the snow steps and rescue the prize group":$"Gather the {clusterSize} hidden children",prizeKids?18f:12f,42f+(i%4)*4f,true,cluster.transform);
            clusterEncounter.SetGroupSize(clusterSize);
        }

        ParentHandoff("Parent handoff near home",new Vector3(0.9f,0.08f,16f),"MRS. OKAFOR",
            "Danny, could these two walk with your group? Danny: Yes. Parent: They know three shortcuts. None are shortcuts.");
        ParentHandoff("Parent handoff across road",new Vector3(14.2f,0.08f,88f),"MR. LEBLANC",
            "You have quite the school train. Could my two join? One talks to every snowman. Danny: That seems manageable.");
        ParentHandoff("Parent handoff river branch",new Vector3(12f,bottomGround,235f),"MRS. SINGH",
            "The river path is beautiful and confusing. Please take these two to school. They packed emergency crackers for everyone.");
        ParentHandoff("Parent handoff forest branch",new Vector3(-12f,bottomGround,248f),"MR. CARDINAL",
            "They will stay together if they stay with your group. Danny: That is also how my group works, mostly.");

        GameObject storyteller1=Cast("DogWalkerTheo",new Vector3(-9f,bottomGround,242f),120f,"Adult with a famously endless shovel story");
        Encounter("Endless shovel story",RiverEncounterKind.TalkativeAdult,new Vector3(-9f,bottomY+1f,242f),new Vector3(4f,2.5f,4f),"MR. DORAN",
            "When I was your age, this entire valley had one shovel, and everyone shared it alphabetically...","Listen politely",5f,0f,true,storyteller1.transform);
        GameObject storyteller2=Cast("NeighbourRose",new Vector3(8f,bottomGround,272f),210f,"Adult explaining every possible school route");
        Encounter("Endless directions",RiverEncounterKind.TalkativeAdult,new Vector3(6f,bottomY+1f,272f),new Vector3(5f,2.5f,4f),"MS. BOUCHARD",
            "School is left unless you face north, then it is emotionally right, except after a heavy snow...","Ask for directions",5f,0f,true,storyteller2.transform);

        GameObject principal=Cast("Principal",new Vector3(0f,bottomGround,412f),180f,"Principal waiting at school");
        Encounter("School finish",RiverEncounterKind.Finish,new Vector3(0f,bottomY+1f,412f),new Vector3(8f,2.5f,7f),"PRINCIPAL","Welcome to The Little Public School, Danny. We have been expecting you. The other children appear to have been expecting you too.","Enter school with the group",25f,0f,true,principal.transform);

        Encounter("Evasion checkpoint 1",RiverEncounterKind.MomEvasion,new Vector3(0f,1f,38f),new Vector3(5f,2.5f,1f),"DANNY","Mom called 'Pooky Bear' from half a block away. I am still ahead.","",7f,0f,false);
        Encounter("Evasion checkpoint 2",RiverEncounterKind.MomEvasion,new Vector3(0f,1f,99f),new Vector3(5f,2.5f,1f),"DANNY","A snowbank broke line of sight. Dignity temporarily preserved.","",8f,0f,false);
        Encounter("Evasion checkpoint stairs",RiverEncounterKind.MomEvasion,new Vector3(0f,-90f*StairRise+1f,StairZ(90)),new Vector3(6.5f,3f,1f),"DANNY","She is taking every stair carefully. Love is relentless but not fast.","",10f,0f,false);
        Encounter("Evasion checkpoint valley",RiverEncounterKind.MomEvasion,new Vector3(0f,bottomY+1f,252f),new Vector3(8f,2.5f,1f),"DANNY","The storm swallowed Mom's voice. That is either freedom or foreshadowing.","",10f,0f,false);
        Encounter("Evasion checkpoint whiteout",RiverEncounterKind.MomEvasion,new Vector3(0f,bottomY+1f,360f),new Vector3(8f,2.5f,1f),"DANNY","Zero visibility. Maximum possibility. Keep everyone close.","",12f,0f,false);
    }

    private static void BuildHazards(RiverValleyGameDirector director, Material snow, Material orange, Material red,
        Material dark, float bottomY, float bottomZ, AudioClip plowLoop, AudioClip bicycleLoop, AudioClip snowSplat)
    {
        GameObject plow=HazardRoot("Cross-street snowplow",new Vector3(14f,0.08f,74f),new Vector3(3.4f,1.7f,5.5f));
        VisualCube("Plow body",plow.transform,new Vector3(0f,0.55f,0f),new Vector3(2.3f,1.3f,3.8f),orange);
        VisualCube("Plow blade",plow.transform,new Vector3(0f,0.10f,-2.2f),new Vector3(3.7f,0.75f,0.35f),dark);
        RiverValleyHazardMover plowMove=plow.AddComponent<RiverValleyHazardMover>();
        plowMove.Configure(new Vector3(14f,0.08f,74f),new Vector3(-4f,0.08f,74f),5.8f,1.25f,"SNOWPLOW","The plow throws a frozen wall across the sidewalk. Danny is now mostly snow.",16f,0.35f,4.0f,45f);
        ParticleSystem streetSpray = BuildSpray(plow.transform,snow,260f);
        plowMove.ConfigureAudio(plowLoop,snowSplat,streetSpray);

        GameObject valleyPlow=HazardRoot("Valley path snowplow",new Vector3(0f,bottomY+0.08f,bottomZ+18f),new Vector3(3.6f,1.8f,5.8f));
        VisualCube("Valley plow body",valleyPlow.transform,new Vector3(0f,0.55f,0f),new Vector3(2.4f,1.35f,4f),red);
        VisualCube("Valley plow blade",valleyPlow.transform,new Vector3(0f,0.12f,-2.3f),new Vector3(4.2f,0.8f,0.4f),dark);
        RiverValleyHazardMover valleyMove=valleyPlow.AddComponent<RiverValleyHazardMover>();
        valleyMove.Configure(new Vector3(0f,bottomY+0.08f,bottomZ+18f),new Vector3(0f,bottomY+0.08f,376f),7.0f,1.4f,"PLOW DRIVER","Sorry, kid! I did not see anyone in the whiteout!",18f,0.30f,4.5f,220f);
        ParticleSystem valleySpray = BuildSpray(valleyPlow.transform,snow,480f);
        valleyMove.ConfigureAudio(plowLoop,snowSplat,valleySpray);

        BuildBicycle("Winter bicycle one",new Vector3(-1.3f,bottomY+0.65f,248f),new Vector3(-1.3f,bottomY+0.65f,386f),8.4f,238f,red,dark,bicycleLoop,snowSplat);
        BuildBicycle("Winter bicycle two",new Vector3(1.4f,bottomY+0.65f,382f),new Vector3(1.4f,bottomY+0.65f,252f),9.0f,255f,orange,dark,bicycleLoop,snowSplat);

        // These patches are narrow enough to dodge, but deep enough to grab
        // Danny's boots and let Mom close the gap if he cuts through them.
        DeepSnowPatch("Sticky driveway windrow",new Vector3(-0.85f,0.02f,43f),new Vector3(1.8f,0.52f,4.8f),snow);
        DeepSnowPatch("Sticky crosswalk slush",new Vector3(1.0f,0.02f,97f),new Vector3(1.7f,0.46f,4.2f),snow);
        DeepSnowPatch("Sticky valley drift one",new Vector3(-1.35f,bottomY+0.02f,286f),new Vector3(1.75f,0.58f,4.6f),snow);
        DeepSnowPatch("Sticky valley drift two",new Vector3(1.30f,bottomY+0.02f,347f),new Vector3(1.8f,0.62f,4.8f),snow);
        DeepSnowPatch("Sticky final-run snow",new Vector3(-1.15f,bottomY+0.02f,399f),new Vector3(1.85f,0.64f,5.0f),snow);
    }

    private static void BuildRiverEmergencyRescue(RiverValleyMomChase mom,float bottomY,
        Material snow,Material orange,Material red,Material dark,Material windowGlow,AudioClip siren)
    {
        Vector3 truckPosition=new Vector3(10.5f,bottomY+0.10f,318f);
        foreach(Transform obstacle in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if(obstacle==null||!obstacle.name.StartsWith("Valley spruce"))continue;
            Vector3 offset=obstacle.position-truckPosition;
            if(Mathf.Abs(offset.x)<4.7f&&Mathf.Abs(offset.z)<5.2f)
                UnityEngine.Object.DestroyImmediate(obstacle.gameObject);
        }
        GameObject truck=new("RIVER RESCUE FIRETRUCK");
        truck.transform.SetParent(world,true);
        truck.transform.position=truckPosition;
        VisualCube("Firetruck red body",truck.transform,new Vector3(0f,1.25f,0f),new Vector3(3.2f,2.2f,6.8f),red);
        VisualCube("Firetruck white roof",truck.transform,new Vector3(0f,2.48f,0.5f),new Vector3(3.0f,0.24f,5.5f),snow);
        VisualCube("Firetruck rear bumper seat",truck.transform,new Vector3(0f,0.62f,-3.55f),new Vector3(3.55f,0.30f,0.72f),dark);
        VisualCube("Firetruck windshield",truck.transform,new Vector3(0f,1.75f,2.92f),new Vector3(2.65f,0.82f,0.12f),dark);
        for(int i=0;i<4;i++)
        {
            float x=i%2==0?-1.62f:1.62f; float z=i<2?-2.05f:2.05f;
            GameObject wheel=VisualCylinder($"Firetruck wheel {i+1}",truck.transform,new Vector3(x,0.52f,z),new Vector3(0.62f,0.25f,0.62f),dark);
            wheel.transform.localRotation=Quaternion.Euler(0f,0f,90f);
        }
        GameObject blueBeacon=VisualCube("Firetruck blue beacon",truck.transform,new Vector3(-0.72f,2.78f,0.45f),new Vector3(0.42f,0.22f,0.42f),windowGlow);
        GameObject redBeacon=VisualCube("Firetruck red beacon",truck.transform,new Vector3(0.72f,2.78f,0.45f),new Vector3(0.42f,0.22f,0.42f),orange);
        truck.AddComponent<RiverValleyEmergencyBeacon>().Configure(blueBeacon.transform,redBeacon.transform);

        GameObject captain=Cast("CrossingGuard",new Vector3(7.2f,bottomY+0.08f,315.3f),125f,"River rescue captain");
        GameObject paramedic=Cast("DogWalkerTheo",new Vector3(7.4f,bottomY+0.08f,320.7f),75f,"River rescue paramedic");
        GameObject firefighter=Cast("Principal",new Vector3(13.7f,bottomY+0.08f,315.3f),235f,"River rescue firefighter");
        GameObject secondMedic=Cast("NeighbourRose",new Vector3(13.5f,bottomY+0.08f,320.7f),305f,"River rescue second medic");
        GameObject[] rescueCrew={captain,paramedic,firefighter,secondMedic};
        foreach(GameObject responder in rescueCrew)
        {
            UnityEngine.Object.DestroyImmediate(responder.GetComponent<RiverValleyAmbientActor>());
            responder.transform.SetParent(truck.transform,true);
        }

        GameObject seat=new("Danny cocoa seat on firetruck bumper");
        seat.transform.SetParent(world,true);
        seat.transform.SetPositionAndRotation(new Vector3(10.5f,bottomY+0.10f,314.15f),Quaternion.Euler(0f,180f,0f));
        GameObject cocoa=VisualCylinder("Steaming cocoa cup",seat.transform,new Vector3(0.82f,0.82f,0.12f),new Vector3(0.16f,0.21f,0.16f),orange);
        VisualCube("Cocoa cup handle",cocoa.transform,new Vector3(0.20f,0f,0f),new Vector3(0.12f,0.14f,0.05f),orange);
        GameObject blanket=new("Warm rescue blanket around Danny");
        blanket.transform.SetParent(seat.transform,false);
        Material warmBlanket=Mat("Sunny emergency blanket",new Color(1f,0.72f,0.13f),0.34f);
        VisualCube("Blanket front",blanket.transform,new Vector3(0f,1.05f,-0.29f),new Vector3(1.30f,1.35f,0.10f),warmBlanket);
        VisualCube("Blanket shoulders",blanket.transform,new Vector3(0f,1.55f,0f),new Vector3(1.55f,0.22f,0.72f),warmBlanket);
        blanket.SetActive(false);

        GameObject home=new("River rescue return home");
        home.transform.SetParent(world,true);
        home.transform.SetPositionAndRotation(new Vector3(-66f,0.08f,-5f),Quaternion.Euler(0f,90f,0f));
        GameObject momArrival=new("Mother arrival beside firetruck");
        momArrival.transform.SetParent(world,true);
        momArrival.transform.position=new Vector3(6.9f,bottomY+0.08f,314.8f);

        GameObject trigger=new("North Saskatchewan river fall rescue trigger");
        trigger.transform.SetParent(world,true);
        trigger.transform.position=new Vector3(24f,bottomY+0.25f,315f);
        BoxCollider collider=trigger.AddComponent<BoxCollider>();
        collider.isTrigger=true;
        collider.size=new Vector3(14.5f,4.0f,205f);
        RiverValleyRiverRescue rescue=trigger.AddComponent<RiverValleyRiverRescue>();
        rescue.Configure(seat.transform,home.transform,momArrival.transform,blanket,mom,siren,
            rescueCrew.Select(responder=>responder.transform).ToArray());
    }

    private static void BuildStreetExpansionAndSideAdventures(RiverValleyGameDirector director,
        RiverValleyMomChase mom,Material snow,Material packedSnow,Material road,Material concrete,
        Material evergreen,Material bark,Material rabbitMaterial,Material coyoteMaterial,
        Material dark,Material orange,Material red,Material windowGlow)
    {
        GameObject expansion=new("SECOND NEIGHBOURHOOD BLOCK AND BONUS FIELDS");
        expansion.transform.SetParent(world,false);

        // A continuous snow shelf sits beneath the complete eastbound loop and
        // both dead ends. There is now nowhere for a careful walker to drop
        // through simply because the decorative road ended.
        // Begin east of the stair throat. Extending this shelf across x=0
        // covered the first stair flight and made Danny appear to walk on air
        // before suddenly dropping onto the lower steps.
        Primitive("Eastbound expansion snow base",PrimitiveType.Cube,new Vector3(48.25f,-0.42f,130f),
            new Vector3(87.5f,0.65f,62f),snow,expansion.transform);

        // This eastbound corner nearly doubles the home-to-stairs street and
        // adds two short cross streets with unmistakable plowed dead ends.
        PathSegment("Main street corner — eastbound road",new Vector3(7.4f,-0.15f,130f),
            new Vector3(84f,-0.15f,130f),10f,road);
        PathSegment("Main street corner — north sidewalk",new Vector3(7.4f,-0.09f,122.8f),
            new Vector3(84f,-0.09f,122.8f),3.4f,concrete);
        PathSegment("Main street corner — south sidewalk",new Vector3(7.4f,-0.09f,137.2f),
            new Vector3(84f,-0.09f,137.2f),3.4f,concrete);
        PathSegment("Dead-end cross street one",new Vector3(34f,-0.15f,105f),
            new Vector3(34f,-0.15f,155f),8.5f,road);
        PathSegment("Dead-end cross street two",new Vector3(61f,-0.15f,108f),
            new Vector3(61f,-0.15f,152f),8.5f,road);
        SnowRidge("Plowed cap — first north dead end",new Vector3(34f,0f,155f),new Vector3(9f,1.3f,3.2f),snow,true);
        SnowRidge("Plowed cap — first south dead end",new Vector3(34f,0f,105f),new Vector3(9f,1.3f,3.2f),snow,true);
        SnowRidge("Plowed cap — second north dead end",new Vector3(61f,0f,152f),new Vector3(9f,1.2f,3.2f),snow,true);
        SnowRidge("Plowed cap — second south dead end",new Vector3(61f,0f,108f),new Vector3(9f,1.2f,3.2f),snow,true);
        IconArrowSign("Corner choice sign",new Vector3(4.2f,0f,127f),-1,"school",new Color(0.88f,0.44f,0.08f));

        string[] homes={"Buildings/Building_House_04_color01.prefab","Buildings/Building_House_03_color03.prefab",
            "Buildings/Building_House_02_color03.prefab"};
        // Keep the larger prefabs wholly outside both cross-street corridors.
        // A house centred in the narrow middle block can still clip a road even
        // when its transform is clear because its balcony is over 20 m wide.
        float[] safeHouseX={17f,17f,80f,80f};
        for(int i=0;i<safeHouseX.Length;i++)
        {
            // Homes sit in the blocks between the two north-south roads.
            // The former x=31 facade occupied the x=34 road and sidewalk.
            float x=safeHouseX[i];
            PlaceCity(homes[i%homes.Length],$"Corner neighbourhood home {i+1}",
                new Vector3(x,0f,i%2==0?107f:153f),Quaternion.Euler(0f,i%2==0?0f:180f,0f),
                Vector3.one*(0.86f+(i%3)*0.08f));
        }
        BuildSideStreetLife(expansion.transform,snow,evergreen,bark,dark,orange,red);

        // Left: a dark but funny coyote woods entered from Danny's sidewalk.
        // This is a real explorable woods, not a trigger-sized platform. The
        // old floor ended only two metres beyond the encounter volume, so a
        // child or Danny could step off it and disappear below the world.
        Primitive("Coyote woods broad safe snow floor",PrimitiveType.Cube,new Vector3(-48f,-0.42f,75f),
            new Vector3(88f,0.85f,90f),snow);
        PathSegment("Coyote woods skipping-school trail",new Vector3(-1.8f,0f,62f),
            new Vector3(-31f,0f,73f),3.4f,packedSnow);
        for(int i=0;i<24;i++)
        {
            float x=-19f-(i%5)*5f; float z=47f+(i/5)*12f+(i%2)*3f;
            float h=3.5f+(i%4)*0.55f;
            BuildReadableSpruce($"Coyote woods spruce {i+1}",new Vector3(x,0f,z),h,
                evergreen,bark,snow,expansion.transform);
        }
        GameObject denObject=new("Coyote cub den");
        denObject.transform.SetParent(world,true);
        denObject.transform.position=new Vector3(-37f,0.02f,84f);
        Transform den=denObject.transform;
        VisualSphere("Soft den hollow",den,new Vector3(0f,0.06f,0f),new Vector3(2.2f,0.10f,1.65f),dark);
        for(int i=0;i<7;i++)
        {
            float angle=Mathf.Lerp(-112f,112f,i/6f)*Mathf.Deg2Rad;
            Vector3 edge=new(Mathf.Sin(angle)*2.25f,0.30f,Mathf.Cos(angle)*1.70f+0.38f);
            VisualSphere($"Snowy den edge {i+1}",den,edge,
                new Vector3(0.72f+(i%2)*0.18f,0.48f+(i%3)*0.10f,0.68f),snow);
        }
        for(int i=0;i<7;i++)
            Coyote($"Coyote woods pack member {i+1}",
                new Vector3(-25f-(i%4)*3.2f,0.08f,62f+(i/4)*15f),coyoteMaterial,dark);
        GameObject woodsZone=new("LEFT BONUS — COYOTE WOODS SKIPPING SCHOOL");
        woodsZone.transform.SetParent(world,true); woodsZone.transform.position=new Vector3(-28f,1.2f,73f);
        BoxCollider woodsCollider=woodsZone.AddComponent<BoxCollider>();
        woodsCollider.isTrigger=true; woodsCollider.size=new Vector3(30f,4f,58f);
        woodsZone.AddComponent<RiverValleySideAdventure>().Configure(RiverSideAdventureKind.CoyoteWoods,mom);
        GameObject cubTrigger=new("Coyote pack adoption circle"); cubTrigger.transform.SetParent(world,true);
        cubTrigger.transform.position=new Vector3(-31f,1f,79f);
        BoxCollider cubCollider=cubTrigger.AddComponent<BoxCollider>();
        cubCollider.isTrigger=true; cubCollider.size=new Vector3(7f,2.5f,7f);
        cubTrigger.AddComponent<CoyoteCubDetour>().Configure(den,0.08f);
        GameObject woodsLittles=KidCluster("Tiny lost coyote-woods children",new Vector3(-24f,0.08f,92f),4);
        woodsLittles.transform.localScale=Vector3.one*0.67f;
        RiverValleyEncounter woodsKids=Encounter("Find tiny children in coyote woods",RiverEncounterKind.LostKid,
            new Vector3(-24f,1f,92f),new Vector3(7f,3f,7f),"TINY WOODS KIDS",
            "We followed one paw print. Then there were several hundred. Can we follow your orange hood instead?",
            "Gather the tiny coyote-woods children",18f,48f,true,woodsLittles.transform);
        woodsKids.SetGroupSize(4);
        ThoughtZone("Danny considers the coyote woods",new Vector3(-4.2f,1.0f,62f),new Vector3(7f,2.4f,7f),
            "I want to skip school and see what is in those woods. Just for one minute.");

        // Right: a bright rabbit meadow busy enough to feel like a secret club.
        Primitive("Rabbit meadow white field",PrimitiveType.Cube,new Vector3(34f,-0.36f,78f),
            new Vector3(33f,0.68f,64f),snow);
        PathSegment("Rabbit meadow marked crossing",new Vector3(0f,0f,74f),
            new Vector3(17.8f,0f,74f),3.2f,packedSnow);
        PathSegment("Rabbit meadow skipping-school trail",new Vector3(14.7f,0f,70f),
            new Vector3(31f,0f,78f),3.6f,packedSnow);
        IconArrowSign("Rabbit meadow picture clue",new Vector3(13.2f,0f,68.6f),1,"rabbit",
            new Color(0.70f,0.28f,0.62f));
        Material rabbitTrail=Mat("Rabbit Trail Pawprints",new Color(0.32f,0.68f,0.86f),0.18f);
        for(int i=0;i<7;i++)
        {
            float t=i/6f;
            RabbitPawPrint($"Rabbit meadow pawprint {i+1}",
                Vector3.Lerp(new Vector3(15.5f,0.025f,70.4f),new Vector3(29f,0.025f,77f),t),
                64f+(i%2==0?-7f:7f),rabbitTrail);
        }
        for(int i=0;i<24;i++)
        {
            float x=22f+(i%6)*4.4f; float z=52f+(i/6)*14f+(i%2)*2.1f;
            Rabbit($"Rabbit meadow hopper {i+1:00}",new Vector3(x,0.16f,z),rabbitMaterial,dark);
        }
        GameObject meadowZone=new("RIGHT BONUS — GREAT WHITE RABBIT FIELD");
        meadowZone.transform.SetParent(world,true); meadowZone.transform.position=new Vector3(34f,1.2f,78f);
        BoxCollider meadowCollider=meadowZone.AddComponent<BoxCollider>();
        meadowCollider.isTrigger=true; meadowCollider.size=new Vector3(32f,4f,62f);
        meadowZone.AddComponent<RiverValleySideAdventure>().Configure(RiverSideAdventureKind.RabbitMeadow,mom);
        GameObject rabbitGirls=KidCluster("Rabbit-petting girl gang",new Vector3(31f,0.08f,72f),7);
        RiverValleyEncounter girls=Encounter("Meet the rabbit-petting girl gang",RiverEncounterKind.LostKid,
            new Vector3(31f,1f,72f),new Vector3(9f,2.8f,8f),"RABBIT CLUB",
            "School has no rabbits and this field has twenty-four. We will come only if every rabbit gets a goodbye.",
            "Invite the rabbit club to the school group",18f,38f,true,rabbitGirls.transform);
        girls.SetGroupSize(7);
        GameObject junie=Cast("ClassmateMaya",new Vector3(36f,0.08f,86f),210f,
            "Junie — rabbit-club member who keeps going back");
        junie.AddComponent<RiverValleyRabbitClubRunaway>().Configure(new Vector3(36f,0.08f,86f));
        RiverValleyEncounter junieEncounter=Encounter("Meet the runaway rabbit-club member",RiverEncounterKind.LostKid,
            new Vector3(36f,1f,86f),new Vector3(5f,2.5f,5f),"RABBIT CLUB",
            "School has no rabbits. I will try following, but I am not promising anything.",
            "Ask Junie to try the school walk",10f,44f,true,junie.transform);
        junieEncounter.SetGroupSize(1);
        GameObject meadowLittles=KidCluster("Very small lost rabbit-field children",new Vector3(40f,0.08f,97f),5);
        meadowLittles.transform.localScale=Vector3.one*0.62f;
        RiverValleyEncounter littleRabbitKids=Encounter("Find the very small rabbit-field children",RiverEncounterKind.LostKid,
            new Vector3(40f,0.9f,97f),new Vector3(8f,3f,8f),"LITTLE RABBIT KIDS",
            "The rabbits are taller than us when they stand up. Please lead both species toward school.",
            "Gather the smallest rabbit-field children",20f,45f,true,meadowLittles.transform);
        littleRabbitKids.SetGroupSize(5);
        GameObject meadowRabbit=Rabbit("Rabbit meadow friendly captain",new Vector3(29f,0.16f,61f),rabbitMaterial,dark);
        Encounter("Pet the rabbit meadow captain",RiverEncounterKind.PetRabbit,new Vector3(29f,0.9f,61f),
            new Vector3(4f,2f,4f),"CHILD","Can I pet it? Danny: The rabbit appears to accept one diplomatic pat.",
            "Pet the rabbit captain",14f,0f,true,meadowRabbit.transform);
        ThoughtZone("Danny notices the rabbit field",new Vector3(17.4f,1.0f,70f),new Vector3(6f,2.4f,7f),
            "That is a whole field of rabbits. School can wait for one tiny nose twitch.");
        ThoughtZone("Danny wants the stair snowboard",new Vector3(0f,1.0f,121f),new Vector3(7f,2.4f,7f),
            "I want to ride that snowboard. One rail, one hockey card, no adults looking.");

        // Other pupils are already headed to school as Danny returns.
        for(int i=0;i<5;i++)
        {
            GameObject stream=KidCluster($"Schoolbound sidewalk group {i+1}",
                new Vector3(-0.5f+(i%2)*1.2f,0.08f,16f+i*18f),2+(i%2));
            foreach(RiverValleyAmbientActor ambient in stream.GetComponentsInChildren<RiverValleyAmbientActor>(true))
                UnityEngine.Object.DestroyImmediate(ambient);
            stream.AddComponent<RiverValleySchoolStream>().Configure(new Vector3(0f,0.08f,126f),0.68f+i*0.06f);
        }

        AudioClip engine=Audio(WinterAudioAssetGenerator.CarEngine);
        AudioClip horn=Audio(WinterAudioAssetGenerator.CarHorn);
        BuildSafeCar("Careful Alberta car one",new Vector3(5.5f,0.55f,-7f),
            new Vector3(5.5f,0.55f,124f),5.4f,red,dark,windowGlow,engine,horn);
        BuildSafeCar("Careful Alberta car two",new Vector3(9.2f,0.55f,124f),
            new Vector3(9.2f,0.55f,-7f),5.8f,orange,dark,windowGlow,engine,horn);
        BuildSafeCar("Corner neighbourhood car",new Vector3(18f,0.55f,130f),
            new Vector3(78f,0.55f,130f),6.0f,red,dark,windowGlow,engine,horn);
        GameObject hockeyCar=BuildSafeCar("Dead-end hockey traffic car",new Vector3(61f,0.55f,109f),
            new Vector3(61f,0.55f,150f),4.7f,orange,dark,windowGlow,engine,horn);
        BuildDeadEndHockeyTraffic(hockeyCar.transform,red,dark);
    }

    private static GameObject BuildSafeCar(string name,Vector3 a,Vector3 b,float speed,Material body,
        Material dark,Material lights,AudioClip engine,AudioClip horn)
    {
        GameObject car=new(name);car.transform.SetParent(world,true);car.transform.position=a;
        VisualCube("Car lower body",car.transform,new Vector3(0f,0.26f,0f),new Vector3(1.75f,0.55f,3.5f),body);
        VisualCube("Car cabin",car.transform,new Vector3(0f,0.78f,0.20f),new Vector3(1.50f,0.70f,1.75f),body);
        for(int i=0;i<4;i++)
        {
            float x=i%2==0?-0.88f:0.88f;float z=i<2?-1.12f:1.12f;
            GameObject wheel=VisualCylinder($"Car wheel {i+1}",car.transform,
                new Vector3(x,0.08f,z),new Vector3(0.31f,0.13f,0.31f),dark);
            wheel.transform.localRotation=Quaternion.Euler(0f,0f,90f);
        }
        VisualSphere("Left headlight",car.transform,new Vector3(-0.52f,0.42f,1.78f),Vector3.one*0.16f,lights);
        VisualSphere("Right headlight",car.transform,new Vector3(0.52f,0.42f,1.78f),Vector3.one*0.16f,lights);
        car.AddComponent<RiverValleySafeCar>().Configure(a,b,speed,engine,horn);
        return car;
    }

    private static void BuildDeadEndHockeyTraffic(Transform trafficCar,Material goalMaterial,Material dark)
    {
        GameObject sceneRoot=new("DEAD-END STREET HOCKEY — CAR CLEARANCE SCENE");
        sceneRoot.transform.SetParent(world,true);

        GameObject goal=new("Movable street-hockey goal");
        goal.transform.SetParent(sceneRoot.transform,true);
        goal.transform.position=new Vector3(61f,0.02f,144.2f);
        VisualCube("Goal left post",goal.transform,new Vector3(-1.18f,0.75f,0f),new Vector3(0.10f,1.50f,0.10f),goalMaterial);
        VisualCube("Goal right post",goal.transform,new Vector3(1.18f,0.75f,0f),new Vector3(0.10f,1.50f,0.10f),goalMaterial);
        VisualCube("Goal crossbar",goal.transform,new Vector3(0f,1.47f,0f),new Vector3(2.46f,0.10f,0.10f),goalMaterial);
        for(int i=0;i<4;i++)
            VisualCube($"Goal net line {i+1}",goal.transform,new Vector3(-0.88f+i*0.59f,0.76f,0.47f),
                new Vector3(0.025f,1.35f,0.025f),dark).transform.localRotation=Quaternion.Euler(18f,0f,0f);
        for(int i=0;i<4;i++)
            VisualCube($"Goal net cross line {i+1}",goal.transform,new Vector3(0f,0.28f+i*0.32f,0.32f),
                new Vector3(2.25f,0.025f,0.025f),dark);

        GameObject teenA=Cast("DogWalkerTheo",new Vector3(58.5f,0.08f,142.6f),35f,
            "Dead-end hockey teenager one");
        GameObject teenB=Cast("ClassmateMaya",new Vector3(63.4f,0.08f,145.2f),215f,
            "Dead-end hockey teenager two");
        teenA.AddComponent<WinterCastActivity>().Configure(WinterCastActivityKind.HockeyStickhandler);
        teenB.AddComponent<WinterCastActivity>().Configure(WinterCastActivityKind.HockeyGoalie);
        GameObject rally=new("Dead-end hockey tennis-ball rally");
        rally.transform.SetParent(sceneRoot.transform,true);
        WinterHockeyRally hockeyRally=rally.AddComponent<WinterHockeyRally>();
        hockeyRally.Configure(teenA.transform,teenB.transform);
        sceneRoot.AddComponent<DeadEndHockeyTraffic>().Configure(goal.transform,trafficCar,
            teenA.transform,teenB.transform,hockeyRally,new Vector3(66.1f,0.02f,144.2f));
    }

    private static void DeepSnowPatch(string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject root=new(name); root.transform.SetParent(world,true); root.transform.position=position;
        BoxCollider trigger=root.AddComponent<BoxCollider>(); trigger.isTrigger=true;
        trigger.center=new Vector3(0f,size.y*0.45f,0f); trigger.size=new Vector3(size.x,size.y*1.15f,size.z);
        root.AddComponent<RiverValleyDeepSnow>();
        GameObject slabA=VisualCube("Broken deep-snow slab A",root.transform,new Vector3(-size.x*0.15f,size.y*0.28f,0f),
            new Vector3(size.x*0.78f,size.y*0.42f,size.z*0.92f),material);
        slabA.transform.localRotation=Quaternion.Euler(0f,-7f,5f);
        GameObject slabB=VisualCube("Broken deep-snow slab B",root.transform,new Vector3(size.x*0.24f,size.y*0.38f,size.z*0.08f),
            new Vector3(size.x*0.48f,size.y*0.54f,size.z*0.68f),material);
        slabB.transform.localRotation=Quaternion.Euler(0f,11f,-7f);
    }

    private static void SnowRidge(string name, Vector3 position, Vector3 size, Material material, bool solid)
    {
        GameObject root=new(name); root.transform.SetParent(world,true); root.transform.position=position;
        GameObject lower=VisualCube("Compacted lower ridge",root.transform,new Vector3(-size.x*0.10f,size.y*0.22f,0f),
            new Vector3(size.x,size.y*0.42f,size.z),material);
        lower.transform.localRotation=Quaternion.Euler(0f,-5f,4f);
        GameObject crest=VisualCube("Wind-cut ridge crest",root.transform,new Vector3(size.x*0.12f,size.y*0.58f,-size.z*0.08f),
            new Vector3(size.x*0.62f,size.y*0.50f,size.z*0.76f),material);
        crest.transform.localRotation=Quaternion.Euler(0f,9f,-9f);
        if(solid)
        {
            // Two shallow, real surfaces make each bank climbable from either
            // direction. The old single box collider behaved like a snow wall.
            const float slopeDegrees=24f;
            float run=Mathf.Max(size.z*0.48f,size.y/Mathf.Tan(slopeDegrees*Mathf.Deg2Rad));
            float rampLength=Mathf.Sqrt(run*run+size.y*size.y);
            Vector3 rampScale=new(size.x*0.82f,0.13f,rampLength);
            GameObject near=Primitive("Climbable near snow slope",PrimitiveType.Cube,position,
                rampScale,material,root.transform);
            near.transform.localPosition=new Vector3(0f,size.y*0.50f,-run*0.50f);
            near.transform.localRotation=Quaternion.Euler(-slopeDegrees,0f,0f);
            GameObject far=Primitive("Climbable far snow slope",PrimitiveType.Cube,position,
                rampScale,material,root.transform);
            far.transform.localPosition=new Vector3(0f,size.y*0.50f,run*0.50f);
            far.transform.localRotation=Quaternion.Euler(slopeDegrees,0f,0f);
            GameObject landing=Primitive("Climbable ridge landing",PrimitiveType.Cube,position,
                new Vector3(size.x*0.82f,0.15f,0.72f),material,root.transform);
            landing.transform.localPosition=new Vector3(0f,size.y+0.01f,0f);
        }
    }

    private static void WaitPost(string name, Vector3 position, string placeName, Material glow, Material dark)
    {
        GameObject root=new(name); root.transform.SetParent(world,true); root.transform.position=position;
        BoxCollider trigger=root.AddComponent<BoxCollider>(); trigger.isTrigger=true;
        trigger.center=new Vector3(0f,1f,0f); trigger.size=new Vector3(4.6f,2.2f,4.6f);
        RiverValleyWaitPost post=root.AddComponent<RiverValleyWaitPost>(); post.Configure(placeName);
        GameObject pole=VisualCylinder("Wait marker pole",root.transform,new Vector3(0f,1.0f,0f),new Vector3(0.08f,1f,0.08f),dark);
        GameObject beacon=VisualSphere("Warm group beacon",root.transform,new Vector3(0f,2.15f,0f),new Vector3(0.34f,0.34f,0.34f),glow);
        pole.transform.localRotation=Quaternion.identity;
        beacon.transform.localRotation=Quaternion.identity;
    }

    private static GameObject KidCluster(string name, Vector3 position, int count)
    {
        GameObject root=new(name); root.transform.SetParent(world,true); root.transform.position=position;
        string[] assets={"SnowFortKid","ClassmateMaya","SledKid"};
        for(int i=0;i<count;i++)
        {
            float side=i-(count-1)*0.5f;
            GameObject child=Cast(assets[i%assets.Length],position+new Vector3(side*0.62f,0f,(i%2)*0.38f),
                0f,$"School child {name} {i+1}");
            child.transform.SetParent(root.transform,true);
            // A cluster owns the children's movement. Independent ambient
            // motors made the small bodies roam in different directions—and
            // scaling a tiny cluster amplified those local corrections into
            // apparent multi-metre jumps. They wait here until recruited,
            // then RiverValleyKidFollower moves the group as one huddle.
            RiverValleyAmbientActor childAmbient=child.GetComponent<RiverValleyAmbientActor>();
            if(childAmbient!=null)UnityEngine.Object.DestroyImmediate(childAmbient);
            // Toddlers, ordinary primary-school kids and lanky older children
            // should not look like one model duplicated at exactly one height.
            float ageScale=0.68f+((i*7+name.Length)%9)*0.05f;
            child.transform.localScale*=ageScale;
            foreach(Collider collider in child.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        }
        return root;
    }

    private static GameObject ParentHandoff(string name, Vector3 position, string speaker, string line)
    {
        GameObject walkingGroup=new(name+" — walking family"); walkingGroup.transform.SetParent(world,true);
        walkingGroup.transform.SetPositionAndRotation(position,Quaternion.Euler(0f,18f,0f));
        string parentAsset=name.GetHashCode()%2==0?"NeighbourRose":"DogWalkerTheo";
        GameObject parent=Cast(parentAsset,position+new Vector3(-1.25f,0f,0.35f),165f,name+" — parent");
        parent.transform.SetParent(walkingGroup.transform,true);
        GameObject children=KidCluster(name+" — three-child huddle",position+new Vector3(0.65f,0f,0f),3);
        children.transform.SetParent(walkingGroup.transform,true);
        RiverValleyEncounter encounter=Encounter(name,RiverEncounterKind.ParentHandoff,position+Vector3.up,
            new Vector3(5.5f,2.5f,5f),speaker,line,"Agree to walk all three children to school",10f,35f,true,children.transform);
        encounter.transform.SetParent(walkingGroup.transform,true);
        encounter.SetGroupSize(3);
        RiverValleyParentWalker walker=walkingGroup.AddComponent<RiverValleyParentWalker>(); walker.Configure(3.4f,0.76f);
        return walkingGroup;
    }

    private static void PrizePerch(string name, Vector3 basePosition, float height, Material snow, Material marker)
    {
        GameObject root=new(name); root.transform.SetParent(world,true); root.transform.position=basePosition;
        const int steps=7;
        for(int i=0;i<steps;i++)
        {
            float progress=(i+1)/(float)steps;
            float stepHeight=height/steps+0.10f;
            GameObject step=Primitive($"{name} snow step {i+1}",PrimitiveType.Cube,Vector3.zero,
                new Vector3(1.65f,stepHeight,1.15f),snow,root.transform);
            step.transform.localPosition=new Vector3(2.8f-i*0.47f,progress*height-stepHeight*0.5f,(i%2==0?-0.16f:0.16f));
        }
        GameObject top=Primitive($"{name} top",PrimitiveType.Cube,Vector3.zero,new Vector3(2.5f,0.34f,2.5f),snow,root.transform);
        top.transform.localPosition=new Vector3(0f,height-0.17f,0f);
        GameObject prize=VisualSphere($"{name} orange prize glow",root.transform,new Vector3(0f,height+2.05f,0f),Vector3.one*0.28f,marker);
        Light light=prize.AddComponent<Light>(); light.color=new Color(1f,0.48f,0.04f); light.range=4f; light.intensity=1.4f;
    }

    private static GameObject PathSegment(string name, Vector3 a, Vector3 b, float width, Material material)
    {
        Vector3 direction=b-a;
        GameObject path=Primitive(name,PrimitiveType.Cube,(a+b)*0.5f,new Vector3(width,0.18f,direction.magnitude),material);
        path.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(direction,Vector3.up).normalized,Vector3.up);
        return path;
    }

    private static GameObject RouteBootPrint(string name,Vector3 position,float yaw,Material material,bool left)
    {
        GameObject root=new(name);
        root.transform.SetParent(world,true);
        root.transform.SetPositionAndRotation(position,Quaternion.Euler(0f,yaw+(left?-4f:4f),0f));
        VisualSphere("Rounded boot toe",root.transform,new Vector3(0f,0f,0.17f),
            new Vector3(0.30f,0.026f,0.36f),material);
        VisualSphere("Boot heel",root.transform,new Vector3(0f,0f,-0.16f),
            new Vector3(0.22f,0.023f,0.20f),material);
        return root;
    }

    private static GameObject RabbitPawPrint(string name,Vector3 position,float yaw,Material material)
    {
        GameObject root=new(name);
        root.transform.SetParent(world,true);
        root.transform.SetPositionAndRotation(position,Quaternion.Euler(0f,yaw,0f));
        VisualSphere("Soft rabbit heel",root.transform,new Vector3(0f,0f,-0.05f),
            new Vector3(0.16f,0.018f,0.20f),material);
        VisualSphere("Rabbit toe left",root.transform,new Vector3(-0.10f,0f,0.17f),
            new Vector3(0.075f,0.016f,0.12f),material);
        VisualSphere("Rabbit toe right",root.transform,new Vector3(0.10f,0f,0.17f),
            new Vector3(0.075f,0.016f,0.12f),material);
        return root;
    }

    private static float StairZ(int stepIndex)
    {
        return StairTopZ + stepIndex * StairDepth + (stepIndex / StepsPerFlight) * StairLandingDepth;
    }

    // Wayfinding is pictorial so it still reads through snow and does not feel
    // like a row of developer labels. arrowDirection is -1 left, 0 ahead, 1 right.
    private static void IconArrowSign(string name, Vector3 position, int arrowDirection, string icon, Color color)
    {
        Material signMaterial=Mat(name+" Material",color,0.16f);
        Material poleMaterial=Mat("Direction Sign Posts",new Color(0.18f,0.16f,0.12f),0.06f);
        Material white=Mat("Wayfinding Icon White",new Color(0.96f,0.98f,1f),0.18f);
        GameObject root=new(name); root.transform.SetParent(world,true); root.transform.position=position;
        VisualCylinder("Half-buried sign post",root.transform,new Vector3(0f,0.78f,0f),new Vector3(0.09f,0.78f,0.09f),poleMaterial);
        VisualCube("Pictogram direction board",root.transform,new Vector3(0f,1.48f,0f),new Vector3(3.05f,1.25f,0.14f),signMaterial);

        // RiverValleyFacingSign aims the root's +Z face at the active camera.
        // Keep the pictograms on that same side so players see the school,
        // river, and paw arrows rather than the blank back of the board.
        const float front=0.082f;
        if(icon=="school")
        {
            VisualCube("School house",root.transform,new Vector3(-0.62f,1.42f,front),new Vector3(0.70f,0.46f,0.035f),white);
            GameObject roof=VisualCube("School roof",root.transform,new Vector3(-0.62f,1.73f,front),new Vector3(0.54f,0.54f,0.035f),white);
            roof.transform.localRotation=Quaternion.Euler(0f,0f,45f);
            VisualCube("School door",root.transform,new Vector3(-0.62f,1.31f,front-0.02f),new Vector3(0.18f,0.28f,0.04f),signMaterial);
        }
        else if(icon=="paw")
        {
            VisualSphere("Paw pad",root.transform,new Vector3(-0.62f,1.36f,front),new Vector3(0.34f,0.28f,0.04f),white);
            for(int i=0;i<3;i++)
                VisualSphere($"Paw toe {i+1}",root.transform,new Vector3(-0.90f+i*0.28f,1.72f+(i==1?0.08f:0f),front),new Vector3(0.14f,0.17f,0.04f),white);
        }
        else if(icon=="rabbit")
        {
            VisualSphere("Bunny sign face",root.transform,new Vector3(-0.62f,1.42f,front),
                new Vector3(0.34f,0.31f,0.04f),white);
            VisualSphere("Bunny sign left ear",root.transform,new Vector3(-0.80f,1.78f,front),
                new Vector3(0.12f,0.30f,0.04f),white).transform.localRotation=Quaternion.Euler(0f,0f,-9f);
            VisualSphere("Bunny sign right ear",root.transform,new Vector3(-0.44f,1.78f,front),
                new Vector3(0.12f,0.30f,0.04f),white).transform.localRotation=Quaternion.Euler(0f,0f,9f);
            VisualSphere("Bunny sign eye left",root.transform,new Vector3(-0.74f,1.48f,front+0.025f),
                new Vector3(0.045f,0.055f,0.025f),signMaterial);
            VisualSphere("Bunny sign eye right",root.transform,new Vector3(-0.50f,1.48f,front+0.025f),
                new Vector3(0.045f,0.055f,0.025f),signMaterial);
        }
        else
        {
            for(int i=0;i<3;i++)
            {
                GameObject wave=VisualCube($"River wave {i+1}",root.transform,new Vector3(-0.62f,1.25f+i*0.23f,front),new Vector3(0.82f,0.09f,0.035f),white);
                wave.transform.localRotation=Quaternion.Euler(0f,0f,i%2==0?7f:-7f);
            }
        }

        Vector3 shaftPosition=arrowDirection==0?new Vector3(0.68f,1.42f,front):new Vector3(0.67f,1.48f,front);
        GameObject shaft=VisualCube("Large route arrow shaft",root.transform,shaftPosition,
            arrowDirection==0?new Vector3(0.13f,0.66f,0.04f):new Vector3(0.72f,0.13f,0.04f),white);
        float side=arrowDirection==0?0f:Mathf.Sign(arrowDirection);
        Vector3 arrowTip=arrowDirection==0?new Vector3(0.68f,1.89f,front):new Vector3(0.67f+side*0.53f,1.48f,front);
        for(int branch=-1;branch<=1;branch+=2)
        {
            Vector3 branchPosition;
            float zAngle;
            if(arrowDirection==0)
            {
                branchPosition=arrowTip+new Vector3(branch*0.16f,-0.15f,0f);
                zAngle=branch*45f;
            }
            else
            {
                branchPosition=arrowTip+new Vector3(-side*0.15f,branch*0.16f,0f);
                zAngle=side*branch*45f;
            }
            GameObject head=VisualCube($"Route arrow head {branch}",root.transform,branchPosition,new Vector3(0.10f,0.42f,0.04f),white);
            head.transform.localRotation=Quaternion.Euler(0f,0f,zAngle);
        }
        // These are roadside clues, not billboards. Keep their pictograms
        // readable without letting them fill the camera or block a shortcut.
        root.transform.localScale=Vector3.one*0.32f;
        root.AddComponent<RiverValleyFacingSign>();
    }

    private static void SchoolNameSign(Vector3 position, string words, Material signMaterial)
    {
        GameObject root=new("The Little Public School camera-facing name sign");
        root.transform.SetParent(world,true);
        root.transform.position=position;
        VisualCube("School name board",root.transform,Vector3.zero,new Vector3(8.6f,1.0f,0.20f),signMaterial);
        GameObject label=new("The Little Public School words");
        label.transform.SetParent(root.transform,false);
        label.transform.localPosition=new Vector3(0f,0f,-0.111f);
        label.transform.localRotation=Quaternion.Euler(0f,180f,0f);
        TextMesh text=label.AddComponent<TextMesh>();
        text.text=words;
        text.fontSize=54;
        text.characterSize=0.065f;
        text.anchor=TextAnchor.MiddleCenter;
        text.alignment=TextAlignment.Center;
        text.color=Color.white;
        root.AddComponent<RiverValleyFacingSign>();
    }

    private static GameObject Rabbit(string name, Vector3 position, Material fur, Material dark)
    {
        GameObject root=new(name); root.transform.SetParent(world,true); root.transform.position=position;
        if(AddFoundRabbitVisual(root.transform))return root;
        // A deliberately toy-like Edmonton snow bunny. The imported rabbit
        // read as a long-limbed creature in motion; this one has an oversized
        // face, soft paws, pink ears and bright readable eyes.
        Material cream=Mat("Cartoon Bunny Cream",new Color(0.91f,0.93f,0.96f),0.20f);
        Material belly=Mat("Cartoon Bunny Belly",new Color(1f,0.98f,0.91f),0.24f);
        Material pink=Mat("Cartoon Bunny Pink",new Color(1f,0.48f,0.62f),0.20f);
        Material eyeWhite=Mat("Cartoon Bunny Eye White",Color.white,0.42f);
        VisualSphere("Cartoon rabbit round body",root.transform,new Vector3(0f,0.34f,0.03f),
            new Vector3(0.46f,0.42f,0.54f),cream);
        VisualSphere("Cartoon rabbit belly",root.transform,new Vector3(0f,0.36f,-0.43f),
            new Vector3(0.30f,0.31f,0.10f),belly);
        VisualSphere("Cartoon rabbit big head",root.transform,new Vector3(0f,0.72f,-0.39f),
            new Vector3(0.42f,0.39f,0.39f),cream);
        GameObject earL=VisualSphere("Cartoon rabbit left ear",root.transform,new Vector3(-0.17f,1.15f,-0.35f),
            new Vector3(0.15f,0.42f,0.13f),cream);
        GameObject earR=VisualSphere("Cartoon rabbit right ear",root.transform,new Vector3(0.17f,1.15f,-0.35f),
            new Vector3(0.15f,0.42f,0.13f),cream);
        earL.transform.localRotation=Quaternion.Euler(0f,0f,-9f);
        earR.transform.localRotation=Quaternion.Euler(0f,0f,9f);
        VisualSphere("Pink inside left ear",root.transform,new Vector3(-0.17f,1.17f,-0.47f),
            new Vector3(0.065f,0.30f,0.035f),pink).transform.localRotation=Quaternion.Euler(0f,0f,-9f);
        VisualSphere("Pink inside right ear",root.transform,new Vector3(0.17f,1.17f,-0.47f),
            new Vector3(0.065f,0.30f,0.035f),pink).transform.localRotation=Quaternion.Euler(0f,0f,9f);
        for(int side=-1;side<=1;side+=2)
        {
            float x=side*0.20f;
            VisualSphere(side<0?"Cartoon rabbit eye left":"Cartoon rabbit eye right",root.transform,
                new Vector3(x,0.79f,-0.70f),new Vector3(0.13f,0.15f,0.09f),eyeWhite);
            VisualSphere(side<0?"Cartoon rabbit pupil left":"Cartoon rabbit pupil right",root.transform,
                new Vector3(x,0.79f,-0.785f),new Vector3(0.064f,0.082f,0.035f),dark);
            VisualSphere(side<0?"Cartoon rabbit eye sparkle left":"Cartoon rabbit eye sparkle right",root.transform,
                new Vector3(x-0.018f,0.84f,-0.823f),Vector3.one*0.020f,eyeWhite);
            VisualSphere(side<0?"Rabbit hind leg left":"Rabbit hind leg right",root.transform,
                new Vector3(x*1.45f,0.18f,0.27f),new Vector3(0.24f,0.17f,0.32f),cream);
            VisualSphere(side<0?"Rabbit front paw left":"Rabbit front paw right",root.transform,
                new Vector3(x*1.10f,0.12f,-0.43f),new Vector3(0.14f,0.10f,0.22f),cream);
        }
        VisualSphere("Cartoon rabbit pink nose",root.transform,new Vector3(0f,0.67f,-0.80f),
            new Vector3(0.075f,0.055f,0.045f),pink);
        VisualCube("Cartoon rabbit tooth left",root.transform,new Vector3(-0.035f,0.58f,-0.79f),
            new Vector3(0.055f,0.09f,0.025f),eyeWhite);
        VisualCube("Cartoon rabbit tooth right",root.transform,new Vector3(0.035f,0.58f,-0.79f),
            new Vector3(0.055f,0.09f,0.025f),eyeWhite);
        VisualSphere("Cartoon rabbit fluffy tail",root.transform,new Vector3(0f,0.42f,0.55f),
            Vector3.one*0.24f,belly);
        return root;
    }

    private static bool AddFoundRabbitVisual(Transform parent)
    {
        const string modelPath=RiverRoot+"/Animals/FoundRabbit/Rabbit.fbx";
        GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if(model==null)return false;
        GameObject visual=PrefabUtility.InstantiatePrefab(model,scene) as GameObject;
        if(visual==null)return false;
        visual.name="Found rabbit animated model";
        visual.transform.SetParent(parent,false);
        visual.transform.localPosition=Vector3.zero;
        visual.transform.localRotation=Quaternion.identity;
        foreach(Collider collider in visual.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
        Renderer[] renderers=visual.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0){UnityEngine.Object.DestroyImmediate(visual);return false;}
        const string textureRoot=RiverRoot+"/Animals/FoundRabbit/";
        Texture2D furTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot+"Fur-skin.png");
        Texture2D skinTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot+"rabbit-skinn.png");
        if(furTexture==null||skinTexture==null)
            throw new InvalidOperationException("The found rabbit's licensed textures are missing.");
        Material furMaterial=Mat("Found rabbit fur",Color.white,0.18f);
        Material skinMaterial=Mat("Found rabbit skin",Color.white,0.18f);
        furMaterial.SetTexture("_BaseMap",furTexture);
        skinMaterial.SetTexture("_BaseMap",skinTexture);
        furMaterial.mainTexture=furTexture;
        skinMaterial.mainTexture=skinTexture;
        EditorUtility.SetDirty(furMaterial);
        EditorUtility.SetDirty(skinMaterial);
        foreach(Renderer renderer in renderers)
        {
            Material[] sources=renderer.sharedMaterials;
            for(int i=0;i<sources.Length;i++)
                sources[i]=sources[i]!=null&&sources[i].name.Contains("001")?skinMaterial:furMaterial;
            renderer.sharedMaterials=sources;
        }
        Bounds bounds=renderers[0].bounds;
        for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
        if(bounds.size.y<0.01f){UnityEngine.Object.DestroyImmediate(visual);return false;}
        visual.transform.localScale=Vector3.one*(1.05f/bounds.size.y);
        bounds=renderers[0].bounds;
        for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
        visual.transform.position+=Vector3.up*(parent.position.y-bounds.min.y);
        foreach(Animator animator in visual.GetComponentsInChildren<Animator>(true))
            animator.applyRootMotion=false;
        QuaterniusAnimalAnimator player=visual.AddComponent<QuaterniusAnimalAnimator>();
        player.Configure("Littles/RiverValley/Animals/FoundRabbit/Rabbit","Sitting");
        return true;
    }

    private static GameObject Coyote(string name, Vector3 position, Material fur, Material dark)
    {
        GameObject root=new(name); root.transform.SetParent(world,true); root.transform.position=position;
        root.transform.rotation=Quaternion.Euler(0f,245f,0f);
        if(AddImportedAnimalVisual(root.transform,"Wolf",0.38f,"Walk")) return root;
        VisualSphere("Coyote body",root.transform,new Vector3(0f,0.66f,0f),new Vector3(0.48f,0.58f,0.92f),fur);
        VisualSphere("Coyote chest",root.transform,new Vector3(0f,0.72f,-0.58f),new Vector3(0.42f,0.52f,0.44f),fur);
        VisualSphere("Coyote head",root.transform,new Vector3(0f,1.12f,-0.64f),new Vector3(0.38f,0.38f,0.42f),fur);
        VisualCube("Coyote muzzle",root.transform,new Vector3(0f,1.03f,-0.99f),new Vector3(0.24f,0.18f,0.38f),fur);
        VisualCube("Coyote ear left",root.transform,new Vector3(-0.20f,1.46f,-0.62f),new Vector3(0.13f,0.30f,0.14f),fur).transform.localRotation=Quaternion.Euler(0f,0f,-13f);
        VisualCube("Coyote ear right",root.transform,new Vector3(0.20f,1.46f,-0.62f),new Vector3(0.13f,0.30f,0.14f),fur).transform.localRotation=Quaternion.Euler(0f,0f,13f);
        for(int i=0;i<4;i++)
        {
            float x=i%2==0?-0.24f:0.24f; float z=i<2?-0.38f:0.42f;
            VisualCylinder($"Coyote leg {i+1}",root.transform,new Vector3(x,0.28f,z),new Vector3(0.10f,0.32f,0.10f),fur);
        }
        GameObject tail=VisualCylinder("Coyote tail",root.transform,new Vector3(0f,0.66f,0.86f),new Vector3(0.16f,0.66f,0.16f),fur);
        tail.transform.localRotation=Quaternion.Euler(58f,0f,0f);
        VisualSphere("Coyote nose",root.transform,new Vector3(0f,1.04f,-1.20f),Vector3.one*0.08f,dark);
        return root;
    }

    private static bool AddImportedAnimalVisual(Transform parent,string modelName,float scale,string animation)
    {
        string path=$"{AnimalRoot}/{modelName}.fbx";
        GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(prefab==null)return false;
        GameObject visual=PrefabUtility.InstantiatePrefab(prefab,scene) as GameObject;
        if(visual==null)return false;
        visual.name=$"{modelName} animated model";
        visual.transform.SetParent(parent,false);
        visual.transform.localPosition=Vector3.zero;
        visual.transform.localRotation=Quaternion.identity;
        visual.transform.localScale=Vector3.one*scale;
        foreach(Collider collider in visual.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
        foreach(Animator animator in visual.GetComponentsInChildren<Animator>(true))
            animator.applyRootMotion=false;
        QuaterniusAnimalAnimator player=visual.AddComponent<QuaterniusAnimalAnimator>();
        player.Configure(AnimalResourceRoot+modelName,animation);
        return true;
    }

    private static void BuildBicycle(string name, Vector3 a, Vector3 b, float speed, float activationZ,
        Material frame, Material tire, AudioClip bicycleLoop, AudioClip snowSplat)
    {
        GameObject bike=HazardRoot(name,a,new Vector3(1.2f,1.8f,2.5f));
        VisualCube("Bicycle frame",bike.transform,new Vector3(0f,0.25f,0f),new Vector3(0.16f,0.65f,1.55f),frame);
        GameObject front=VisualCylinder("Front wheel",bike.transform,new Vector3(0f,-0.05f,-0.82f),new Vector3(0.62f,0.12f,0.62f),tire);
        GameObject rear=VisualCylinder("Rear wheel",bike.transform,new Vector3(0f,-0.05f,0.82f),new Vector3(0.62f,0.12f,0.62f),tire);
        front.transform.localRotation=rear.transform.localRotation=Quaternion.Euler(0f,0f,90f);
        RiverValleyHazardMover mover=bike.AddComponent<RiverValleyHazardMover>();
        mover.Configure(a,b,speed,0.35f,"WINTER CYCLIST","A bicycle appears out of the white. In Edmonton, apparently this is reasonable.",11f,0.48f,2.6f,activationZ);
        mover.ConfigureAudio(bicycleLoop,snowSplat,null);
    }

    private static GameObject HazardRoot(string name, Vector3 position, Vector3 colliderSize)
    {
        GameObject root=new(name); root.transform.SetParent(world,true); root.transform.position=position;
        BoxCollider trigger=root.AddComponent<BoxCollider>(); trigger.isTrigger=true; trigger.size=colliderSize;
        return root;
    }

    private static GameObject Cast(string assetName, Vector3 position, float yaw, string sceneName)
    {
        string path=$"{CastRoot}/{assetName}/{assetName}.prefab";
        GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(prefab==null) throw new InvalidOperationException("Missing cast prefab: "+path);
        GameObject actor=PrefabUtility.InstantiatePrefab(prefab,scene) as GameObject;
        actor.name=sceneName; actor.transform.SetParent(world,true); actor.transform.SetPositionAndRotation(position,Quaternion.Euler(0f,yaw,0f));
        if(actor.GetComponent<WinterAnimationEventRelay>()==null)actor.AddComponent<WinterAnimationEventRelay>();
        foreach(WinterCastInteraction c in actor.GetComponents<WinterCastInteraction>()) UnityEngine.Object.DestroyImmediate(c);
        foreach(WinterCastNpcMotor c in actor.GetComponents<WinterCastNpcMotor>()) UnityEngine.Object.DestroyImmediate(c);
        foreach(Collider c in actor.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(c);
        foreach(Animator animator in actor.GetComponentsInChildren<Animator>(true))
            animator.applyRootMotion=false;
        actor.AddComponent<RiverValleyAmbientActor>();
        actor.AddComponent<WinterCastVisualPolish>();
        return actor;
    }

    private static RiverValleyEncounter Encounter(string name, RiverEncounterKind kind, Vector3 position, Vector3 size,
        string speaker, string line, string prompt, float spark, float minimum, bool interact, Transform actor=null, Transform railStart=null, Transform railEnd=null)
    {
        GameObject go=new(name); go.transform.SetParent(world,true); go.transform.position=position;
        // A lost child may stand behind a climbable drift or beside a solid
        // building. Let Danny call them over from the open path rather than
        // requiring him to force his controller into the scenery.
        if(kind==RiverEncounterKind.LostKid||kind==RiverEncounterKind.ParentHandoff)
            size=new Vector3(Mathf.Max(9f,size.x),Mathf.Max(3.5f,size.y),Mathf.Max(9f,size.z));
        BoxCollider col=go.AddComponent<BoxCollider>(); col.isTrigger=true; col.size=size;
        RiverValleyEncounter encounter=go.AddComponent<RiverValleyEncounter>();
        encounter.Configure(kind,speaker,line,prompt,spark,minimum,interact,actor,railStart,railEnd);
        return encounter;
    }

    private static ParticleSystem BuildPlayerSnow(Transform player)
    {
        GameObject go=new("Blowing snow that follows Danny"); go.transform.SetParent(player,false); go.transform.localPosition=new Vector3(0f,5f,8f);
        ParticleSystem ps=go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main=ps.main; main.loop=true; main.startLifetime=2.4f; main.startSpeed=6f; main.startSize=new ParticleSystem.MinMaxCurve(0.035f,0.13f); main.maxParticles=6000; main.simulationSpace=ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission=ps.emission; emission.rateOverTime=35f;
        ParticleSystem.ShapeModule shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(24f,10f,24f);
        ParticleSystem.VelocityOverLifetimeModule velocity=ps.velocityOverLifetime; velocity.enabled=true; velocity.space=ParticleSystemSimulationSpace.World; velocity.x=new ParticleSystem.MinMaxCurve(7f,12f); velocity.y=new ParticleSystem.MinMaxCurve(-1.8f,-3.8f); velocity.z=new ParticleSystem.MinMaxCurve(-3f,1f);
        ParticleSystemRenderer renderer=go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=ParticleMat();
        ps.Play(); return ps;
    }

    private static ParticleSystem BuildSpray(Transform parent, Material unused, float rate)
    {
        GameObject go=new("Snow spray"); go.transform.SetParent(parent,false); go.transform.localPosition=new Vector3(0f,0.35f,-2.4f);
        ParticleSystem ps=go.AddComponent<ParticleSystem>(); ParticleSystem.MainModule main=ps.main; main.startLifetime=1.1f; main.startSpeed=8f; main.startSize=new ParticleSystem.MinMaxCurve(0.05f,0.20f); main.maxParticles=1200;
        ParticleSystem.EmissionModule emission=ps.emission; emission.rateOverTime=rate;
        ParticleSystem.ShapeModule shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.angle=28f; shape.radius=1.5f;
        go.transform.localRotation=Quaternion.Euler(-20f,0f,0f); go.GetComponent<ParticleSystemRenderer>().sharedMaterial=ParticleMat(); ps.Play();
        return ps;
    }

    private static Material ParticleMat()
    {
        string path=MaterialRoot+"/Blowing Snow.mat";
        Material existing=AssetDatabase.LoadAssetAtPath<Material>(path); if(existing!=null)return existing;
        Shader shader=Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        Material material=new(shader); material.name="Blowing Snow"; material.color=Color.white; AssetDatabase.CreateAsset(material,path); return material;
    }

    private static void BuildSideStreetLife(Transform parent,Material snow,Material evergreen,Material bark,
        Material dark,Material orange,Material red)
    {
        // The extra block is a neighbourhood, not an empty testing grid.
        for(int i=0;i<14;i++)
        {
            float x=13f+i*5.5f;
            float z=i%2==0?117.8f:142.2f;
            BuildWinterStreetTree($"Corner boulevard tree {i+1}",new Vector3(x,0f,z),
                4.4f+(i%3)*0.35f,evergreen,bark,snow,parent);
        }
        for(int i=0;i<7;i++)
        {
            float x=18f+i*10f;
            GameObject mailbox=new($"Snowy neighbourhood mailbox {i+1}");
            mailbox.transform.SetParent(parent,false);
            mailbox.transform.localPosition=new Vector3(x,0f,i%2==0?113.8f:146.2f);
            VisualCylinder("Mailbox post",mailbox.transform,new Vector3(0f,0.62f,0f),new Vector3(0.07f,0.62f,0.07f),dark);
            VisualCube("Colourful mailbox",mailbox.transform,new Vector3(0f,1.18f,0f),new Vector3(0.52f,0.36f,0.70f),i%2==0?red:orange);
            VisualCube("Mailbox snow cap",mailbox.transform,new Vector3(0f,1.40f,0f),new Vector3(0.58f,0.10f,0.76f),snow);
        }
        for(int i=0;i<3;i++)
        {
            GameObject bench=new($"Bundled-up corner bench {i+1}");
            bench.transform.SetParent(parent,false);
            bench.transform.localPosition=new Vector3(25f+i*21f,0f,i%2==0?118.6f:141.4f);
            VisualCube("Bench seat",bench.transform,new Vector3(0f,0.48f,0f),new Vector3(2.4f,0.18f,0.55f),bark);
            VisualCube("Bench back",bench.transform,new Vector3(0f,0.98f,0.24f),new Vector3(2.4f,0.72f,0.16f),bark);
            Primitive("Bench leg left",PrimitiveType.Cube,Vector3.zero,new Vector3(0.15f,0.48f,0.42f),dark,bench.transform).transform.localPosition=new Vector3(-0.82f,0.24f,0f);
            Primitive("Bench leg right",PrimitiveType.Cube,Vector3.zero,new Vector3(0.15f,0.48f,0.42f),dark,bench.transform).transform.localPosition=new Vector3(0.82f,0.24f,0f);
        }
        BuildSnowman(parent,"Corner-block snowman",new Vector3(76f,0f,142f),orange,dark,snow,red);
        GameObject sled=new("Red sled waiting on the side street");
        sled.transform.SetParent(parent,false);
        sled.transform.localPosition=new Vector3(34f,0.08f,148f);
        GameObject sledDeck=VisualCube("Sled deck",sled.transform,new Vector3(0f,0.16f,0f),new Vector3(1.25f,0.12f,2.1f),red);
        sledDeck.transform.localRotation=Quaternion.Euler(0f,16f,0f);
        for(int side=-1;side<=1;side+=2)
            Beam($"Sled runner {side}",new Vector3(side*0.48f,0.02f,-0.95f),new Vector3(side*0.48f,0.02f,0.95f),0.055f,dark,sled.transform);
    }

    private static GameObject BuildWinterStreetTree(string name,Vector3 position,float height,Material canopy,
        Material bark,Material snow,Transform parent=null)
    {
        GameObject root=new(name);
        root.transform.SetParent(parent??world,true);
        root.transform.position=position;
        float trunkHeight=height*0.55f;
        GameObject trunk=Primitive("Clearly visible tree trunk",PrimitiveType.Cylinder,Vector3.zero,
            new Vector3(0.20f,trunkHeight*0.5f,0.20f),bark,root.transform);
        trunk.transform.localPosition=new Vector3(0f,trunkHeight*0.5f,0f);
        VisualSphere("Winter crown",root.transform,new Vector3(0f,height*0.72f,0f),
            new Vector3(height*0.28f,height*0.36f,height*0.28f),canopy);
        // A complete ring of small overlapping clumps reads as snow wrapping
        // around the crown. One large front clump looked exactly like a white
        // eyeball from the walking camera.
        const int snowClumps=14;
        for(int i=0;i<snowClumps;i++)
        {
            float angle=i*Mathf.PI*2f/snowClumps;
            GameObject clump=VisualSphere($"Crown snow border {i+1}",root.transform,
                new Vector3(Mathf.Cos(angle)*height*0.125f,height*0.79f,
                    Mathf.Sin(angle)*height*0.125f),
                new Vector3(height*0.090f,height*0.040f,height*0.066f),snow);
            clump.transform.localRotation=Quaternion.Euler(0f,-angle*Mathf.Rad2Deg,0f);
        }
        return root;
    }

    private static void BuildWorldSafetyBoundary()
    {
        GameObject boundary=new("COMPLETE PLAYABLE WORLD SAFETY BOUNDARY");
        boundary.transform.SetParent(world,false);
        void Wall(string name,Vector3 centre,Vector3 size)
        {
            GameObject wall=new(name);
            wall.transform.SetParent(boundary.transform,false);
            wall.transform.localPosition=centre;
            BoxCollider collider=wall.AddComponent<BoxCollider>();
            collider.size=size;
        }

        // Only the true outside edge is blocked. Earlier district-by-district
        // walls accidentally fenced off the woods, rabbit field, hidden kids
        // and harmless shortcuts even though those places are playable.
        const float centreY=-8f;
        const float height=48f;
        Wall("Playable world west outside edge",new Vector3(-101f,centreY,192f),new Vector3(0.6f,height,490f));
        Wall("Playable world east outside edge",new Vector3(101f,centreY,192f),new Vector3(0.6f,height,490f));
        Wall("Playable world south outside edge",new Vector3(0f,centreY,-53f),new Vector3(202f,height,0.6f));
        Wall("Playable world north outside edge",new Vector3(0f,centreY,437f),new Vector3(202f,height,0.6f));
    }

    private static GameObject PlaceCity(string relativePath,string name,Vector3 position,Quaternion rotation,Vector3 scale)
    {
        GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"{CityRoot}/{relativePath}");
        if(prefab==null) return null;
        GameObject go=PrefabUtility.InstantiatePrefab(prefab,scene) as GameObject;
        go.name=name; go.transform.SetParent(world,true); go.transform.SetPositionAndRotation(position,rotation); go.transform.localScale=scale;
        // Imported city prefabs use several different pivot conventions.
        // Set the rendered base on the requested street height so no building
        // hangs above the snow or leaves a camera-sized gap underneath.
        Renderer[] placedRenderers=go.GetComponentsInChildren<Renderer>(true);
        if(placedRenderers.Length>0)
        {
            Bounds placedBounds=placedRenderers[0].bounds;
            for(int i=1;i<placedRenderers.Length;i++)placedBounds.Encapsulate(placedRenderers[i].bounds);
            go.transform.position+=Vector3.up*(position.y-placedBounds.min.y);
        }
        if(relativePath.StartsWith("Buildings/",StringComparison.Ordinal)) AddSolidBuildingCollision(go);
        return go;
    }

    private static void AddSolidBuildingCollision(GameObject building)
    {
        foreach(Collider oldCollider in building.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(oldCollider);
        Renderer[] renderers=building.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)return;
        Bounds bounds=renderers[0].bounds;
        for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
        GameObject blocker=new(building.name+" — solid exterior");
        blocker.transform.SetParent(world,true);
        blocker.transform.position=bounds.center;
        BoxCollider box=blocker.AddComponent<BoxCollider>();
        box.size=new Vector3(Mathf.Max(1f,bounds.size.x*0.90f),Mathf.Max(2f,bounds.size.y),Mathf.Max(1f,bounds.size.z*0.90f));
    }

    private static GameObject Primitive(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material,Transform parent=null)
    {
        GameObject go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent??world,true); go.transform.position=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material; return go;
    }

    private static GameObject VisualCube(string name,Transform parent,Vector3 localPosition,Vector3 scale,Material material)
    {
        GameObject go=Primitive(name,PrimitiveType.Cube,Vector3.zero,scale,material,parent); go.transform.localPosition=localPosition; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }

    private static GameObject VisualCylinder(string name,Transform parent,Vector3 localPosition,Vector3 scale,Material material)
    {
        GameObject go=Primitive(name,PrimitiveType.Cylinder,Vector3.zero,scale,material,parent); go.transform.localPosition=localPosition; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }

    private static GameObject VisualSphere(string name,Transform parent,Vector3 localPosition,Vector3 scale,Material material)
    {
        GameObject go=Primitive(name,PrimitiveType.Sphere,Vector3.zero,scale,material,parent); go.transform.localPosition=localPosition; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }

    private static GameObject BuildReadableSpruce(string name,Vector3 position,float height,Material evergreen,
        Material bark,Material snow,Transform parent=null)
    {
        GameObject root=new(name);
        root.transform.SetParent(parent??world,true);
        root.transform.position=position;
        float h=Mathf.Max(2.8f,height);
        GameObject trunk=Primitive("Visible brown trunk",PrimitiveType.Cylinder,Vector3.zero,
            new Vector3(0.22f,h*0.27f,0.22f),bark,root.transform);
        trunk.transform.localPosition=new Vector3(0f,h*0.27f,0f);
        VisualCone("Low spruce boughs",root.transform,new Vector3(0f,h*0.42f,0f),
            new Vector3(h*0.53f,h*0.48f,h*0.53f),evergreen);
        VisualCone("Middle spruce boughs",root.transform,new Vector3(0f,h*0.64f,0f),
            new Vector3(h*0.39f,h*0.44f,h*0.39f),evergreen);
        VisualCone("Spruce crown",root.transform,new Vector3(0f,h*0.83f,0f),
            new Vector3(h*0.25f,h*0.34f,h*0.25f),evergreen);
        if(snow!=null)
        {
            VisualCone("Snow on low boughs",root.transform,new Vector3(0f,h*0.57f,0f),
                new Vector3(h*0.39f,h*0.075f,h*0.39f),snow);
            VisualCone("Snow on crown",root.transform,new Vector3(0f,h*0.92f,0f),
                new Vector3(h*0.17f,h*0.06f,h*0.17f),snow);
        }
        return root;
    }

    private static GameObject VisualCone(string name,Transform parent,Vector3 localPosition,Vector3 scale,Material material)
    {
        GameObject go=new(name);
        go.transform.SetParent(parent,false);
        go.transform.localPosition=localPosition;
        go.transform.localScale=scale;
        go.AddComponent<MeshFilter>().sharedMesh=SpruceConeMesh();
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
        return go;
    }

    private static Mesh SpruceConeMesh()
    {
        const string path=RiverRoot+"/LowPolySpruceCone.asset";
        Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null)return existing;
        const int segments=10;
        Vector3[] vertices=new Vector3[segments+2];
        int[] triangles=new int[segments*6];
        for(int i=0;i<segments;i++)
        {
            float angle=Mathf.PI*2f*i/segments;
            vertices[i]=new Vector3(Mathf.Cos(angle)*0.5f,-0.5f,Mathf.Sin(angle)*0.5f);
        }
        vertices[segments]=new Vector3(0f,0.5f,0f);
        vertices[segments+1]=new Vector3(0f,-0.5f,0f);
        for(int i=0;i<segments;i++)
        {
            int next=(i+1)%segments;
            int t=i*6;
            triangles[t]=i; triangles[t+1]=segments; triangles[t+2]=next;
            triangles[t+3]=segments+1; triangles[t+4]=i; triangles[t+5]=next;
        }
        Mesh mesh=new(){name="Low Poly Spruce Cone"};
        mesh.vertices=vertices;
        mesh.triangles=triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,path);
        return mesh;
    }

    private static RiverValleyThoughtZone ThoughtZone(string name,Vector3 position,Vector3 size,string thought)
    {
        GameObject root=new(name);
        root.transform.SetParent(world,true);
        root.transform.position=position;
        BoxCollider trigger=root.AddComponent<BoxCollider>();
        trigger.isTrigger=true;
        trigger.size=size;
        RiverValleyThoughtZone zone=root.AddComponent<RiverValleyThoughtZone>();
        zone.Configure(thought);
        return zone;
    }

    private static GameObject Beam(string name,Vector3 a,Vector3 b,float width,Material material,Transform parent=null)
    {
        Vector3 direction=b-a; GameObject beam=Primitive(name,PrimitiveType.Cube,(a+b)*0.5f,new Vector3(width,direction.magnitude,width),material,parent);
        beam.transform.rotation=Quaternion.FromToRotation(Vector3.up,direction.normalized); return beam;
    }

    private static Material Mat(string name,Color color,float smoothness)
    {
        string path=$"{MaterialRoot}/{name}.mat"; Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){ Shader shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"); material=new Material(shader){name=name}; AssetDatabase.CreateAsset(material,path); }
        material.color=color; if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color); if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",smoothness); EditorUtility.SetDirty(material); return material;
    }

    private static Material GlowMat(string name,Color color,float emission)
    {
        Material material=Mat(name,color,0.38f);
        Color glow=color*Mathf.Max(1f,emission);
        if(material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor",glow);
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static AudioClip Audio(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) throw new InvalidOperationException("Winter audio asset missing: " + path);
        return clip;
    }

    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return; string parent=path.Substring(0,path.LastIndexOf('/')); EnsureFolder(parent); AssetDatabase.CreateFolder(parent,path.Substring(path.LastIndexOf('/')+1));
    }

    private static void AddSceneToBuildSettings(string path)
    {
        if(EditorBuildSettings.scenes.Any(s=>s.path==path))return; EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(path,true)}).ToArray();
    }

    private static void Validate()
    {
        int stairs=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("Stair ")&&char.IsDigit(t.name.Last()));
        if(stairs<StairCount)throw new InvalidOperationException($"Only {stairs} river-valley stairs were built");
        int landings=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("Level stair landing")||t.name=="Road-level bottom stair landing");
        if(landings<4)throw new InvalidOperationException($"Only {landings} proper stair landings were built");
        if(UnityEngine.Object.FindFirstObjectByType<RiverValleyGameDirector>()==null)throw new InvalidOperationException("River-valley director missing");
        if(UnityEngine.Object.FindFirstObjectByType<RiverValleyWhiteout>()==null)throw new InvalidOperationException("Whiteout controller missing");
        if(UnityEngine.Object.FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None).Length<28)throw new InvalidOperationException("River encounters missing");
        if(UnityEngine.Object.FindObjectsByType<RiverValleyWaitPost>(FindObjectsSortMode.None).Length<4)throw new InvalidOperationException("Group wait posts missing");
        int clusteredChildren=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("School child "));
        if(clusteredChildren<44)throw new InvalidOperationException($"Only {clusteredChildren} clustered school children were built");
        if(UnityEngine.Object.FindObjectsByType<RiverValleyParentWalker>(FindObjectsSortMode.None).Length<4)throw new InvalidOperationException("Walking parent handoffs missing");
        if(GameObject.Find("Y trail — city-centre forest left")==null||GameObject.Find("Y trail — river-view right")==null)throw new InvalidOperationException("Stair-bottom route choice missing");
        if(UnityEngine.Object.FindObjectsByType<RiverValleyHazardMover>(FindObjectsSortMode.None).Length<4)throw new InvalidOperationException("Moving winter hazards missing");
        if(UnityEngine.Object.FindObjectsByType<RiverValleySlipPatch>(FindObjectsSortMode.None).Length<7)throw new InvalidOperationException("Interactive ice patches missing");
        if(UnityEngine.Object.FindObjectsByType<RiverValleyDeepSnow>(FindObjectsSortMode.None).Length<5)throw new InvalidOperationException("Sticky deep-snow patches missing");
        if(UnityEngine.Object.FindFirstObjectByType<WinterHockeyRally>()==null)throw new InvalidOperationException("Street-hockey rally missing");
        if(UnityEngine.Object.FindFirstObjectByType<WinterAudioDirector>()==null)throw new InvalidOperationException("Winter audio director missing");
        if(UnityEngine.Object.FindFirstObjectByType<DannyWinterTrail>()==null)throw new InvalidOperationException("Danny snow trail missing");
        if(UnityEngine.Object.FindFirstObjectByType<RiverValleyMomChase>()==null)throw new InvalidOperationException("Mom chase missing");
        if(UnityEngine.Object.FindFirstObjectByType<AlbertaNeighbourhoodLife>()==null)throw new InvalidOperationException("Alberta neighbourhood life missing");
        if(GameObject.Find("COLOURFUL ALBERTA WINTER DETAILS")==null)throw new InvalidOperationException("Winter art polish missing");
        if(UnityEngine.Object.FindObjectsByType<RiverValleySideAdventure>(FindObjectsSortMode.None).Length<2)throw new InvalidOperationException("Bonus side adventures missing");
        if(UnityEngine.Object.FindObjectsByType<RiverValleySafeCar>(FindObjectsSortMode.None).Length<4)throw new InvalidOperationException("Safe neighbourhood traffic missing");
        if(UnityEngine.Object.FindFirstObjectByType<DeadEndHockeyTraffic>()==null)
            throw new InvalidOperationException("Dead-end hockey traffic scene missing");
        if(GameObject.Find("Main street corner — eastbound road")==null)throw new InvalidOperationException("Extended corner street missing");
        if(GameObject.Find("West approach sidewalk")==null||GameObject.Find("School corner left-turn clue")==null)
            throw new InvalidOperationException("West approach street or school routing clues missing");
        if(UnityEngine.Object.FindFirstObjectByType<RiverValleyMapSafety>()==null)
            throw new InvalidOperationException("Out-of-map player recovery missing");
        GameObject worldBoundary=GameObject.Find("COMPLETE PLAYABLE WORLD SAFETY BOUNDARY");
        if(worldBoundary==null||worldBoundary.GetComponentsInChildren<BoxCollider>(true).Length!=4)
            throw new InvalidOperationException("Complete physical world-edge boundary missing");
        if(UnityEngine.Object.FindObjectsByType<RiverValleyFacingSign>(FindObjectsSortMode.None).Length<6)
            throw new InvalidOperationException("Camera-facing picture route signs missing");
        int rabbits=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Count(item=>item.name=="Found rabbit animated model"||item.name=="Cartoon rabbit big head");
        if(rabbits<20)throw new InvalidOperationException($"Only {rabbits} rabbits were built");
        if(GameObject.Find("Rabbit meadow picture clue")==null)
            throw new InvalidOperationException("Rabbit meadow picture clue missing");
        int solidBuildings=UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)
            .Count(collider=>collider!=null&&collider.name.EndsWith("— solid exterior",StringComparison.Ordinal));
        if(solidBuildings<10)throw new InvalidOperationException($"Only {solidBuildings} solid building exteriors were built");
        int stairEdges=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Count(item=>item.name.StartsWith("Invisible stair edge"));
        if(stairEdges<40)throw new InvalidOperationException($"Only {stairEdges} stair edge barriers were built");
        if(UnityEngine.Object.FindFirstObjectByType<RiverValleyRiverRescue>()==null)throw new InvalidOperationException("River emergency rescue missing");
        if(GameObject.Find("RIVER RESCUE FIRETRUCK")==null)throw new InvalidOperationException("River rescue firetruck missing");
        if(UnityEngine.Object.FindObjectsByType<RiverValleyThoughtZone>(FindObjectsSortMode.None).Length<3)
            throw new InvalidOperationException("Danny's proximity thoughts are missing");
        if(UnityEngine.Object.FindFirstObjectByType<RiverValleyRabbitClubRunaway>()==null)
            throw new InvalidOperationException("Rabbit-club runaway behaviour missing");
    }
}
