using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Runtime-built first campaign slice: one uninterrupted ten-block walk to
/// school. Encounters are avoidable, Will does not regenerate, and collection
/// always returns Allen to the front path rather than to a checkpoint.
/// </summary>
[DisallowMultipleComponent]
public sealed class SidewalkGameDirector : MonoBehaviour
{
    public const int BlockCount = 10;
    private const float BlockLength = 42f;
    private const float HomePathLength = 12f;
    private const float SidewalkWidth = 7f;

    private GameObject _playerObject;
    private Transform _player;
    private ThirdPersonController _motor;
    private StarterAssetsInputs _input;
    private CharacterController _controller;
    private Transform _world;
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Vector3 _routeForward;
    private float _will = 100f;
    private int _attempt = 1;
    private bool _busy;
    private bool _awaitingRetry;
    private bool _finished;
    private string _speaker = "MOTHER";
    private string _subtitle = "Allen, sweetheart, school is this way.";
    private readonly List<SidewalkEncounter> _encounters = new List<SidewalkEncounter>();
    private GUIStyle _subtitleStyle;
    private GUIStyle _smallStyle;
    private GUIStyle _buttonStyle;

    private struct Line
    {
        public string Speaker;
        public string Text;
        public float Seconds;
        public Line(string speaker, string text, float seconds = 2.7f)
        { Speaker = speaker; Text = text; Seconds = seconds; }
    }

    public void Initialize(GameObject playerObject)
    {
        if (_playerObject != null || playerObject == null) return;
        _playerObject = playerObject;
        _player = playerObject.transform;
        _motor = playerObject.GetComponent<ThirdPersonController>();
        _input = playerObject.GetComponent<StarterAssetsInputs>();
        _controller = playerObject.GetComponent<CharacterController>();
        _startPosition = _player.position;
        _startRotation = _player.rotation;
        _routeForward = Vector3.ProjectOnPlane(_player.forward, Vector3.up).normalized;
        if (_routeForward.sqrMagnitude < 0.1f) _routeForward = Vector3.forward;
        BuildCampaign();
    }

    private void BuildCampaign()
    {
        // Remove the Starter Assets obstacle course; it was useful for movement
        // tests but would intersect the authored sidewalk route.
        GameObject sampleEnvironment = GameObject.Find("Environment");
        if (sampleEnvironment != null) sampleEnvironment.SetActive(false);
        GameObject old = GameObject.Find("WalkToSchool_World");
        if (old != null) Destroy(old);
        _world = new GameObject("WalkToSchool_World").transform;
        _world.SetPositionAndRotation(_startPosition, Quaternion.LookRotation(_routeForward, Vector3.up));

        Material concrete = MakeMaterial("Sidewalk", new Color(0.48f, 0.53f, 0.55f));
        Material grass = MakeMaterial("Lawns", new Color(0.18f, 0.39f, 0.20f));
        Material road = MakeMaterial("Busy Road", new Color(0.075f, 0.085f, 0.095f));
        Material curb = MakeMaterial("Curb", new Color(0.62f, 0.64f, 0.63f));
        Material hedge = MakeMaterial("Hedges", new Color(0.08f, 0.27f, 0.12f));
        Material warm = MakeMaterial("People Warm", new Color(0.57f, 0.25f, 0.14f));
        Material cool = MakeMaterial("People Cool", new Color(0.12f, 0.22f, 0.43f));
        Material dog = MakeMaterial("Dog", new Color(0.31f, 0.17f, 0.08f));

        // A short home path establishes the safe origin before block one.
        CreateBlock("Home Path", new Vector3(0f, -0.09f, HomePathLength * 0.5f),
            new Vector3(3.2f, 0.18f, HomePathLength), concrete, true);

        SidewalkEncounter.Kind[] story =
        {
            SidewalkEncounter.Kind.MailCarrier, SidewalkEncounter.Kind.Dog,
            SidewalkEncounter.Kind.HedgeCat, SidewalkEncounter.Kind.Teens,
            SidewalkEncounter.Kind.Hose, SidewalkEncounter.Kind.Crows,
            SidewalkEncounter.Kind.FriendlyKid, SidewalkEncounter.Kind.Mower,
            SidewalkEncounter.Kind.Walker, SidewalkEncounter.Kind.Crows
        };

        for (int block = 0; block < BlockCount; block++)
        {
            float centerZ = HomePathLength + block * BlockLength + BlockLength * 0.5f;
            CreateBlock($"Block {block + 1:00} Sidewalk", new Vector3(0f, -0.09f, centerZ),
                new Vector3(SidewalkWidth, 0.18f, BlockLength - 0.12f), concrete, true);
            CreateBlock($"Block {block + 1:00} Lawn", new Vector3(-9.1f, -0.14f, centerZ),
                new Vector3(11.2f, 0.28f, BlockLength), grass, true);
            CreateBlock($"Block {block + 1:00} Road", new Vector3(11.2f, -0.16f, centerZ),
                new Vector3(15.4f, 0.25f, BlockLength), road, true);
            CreateBlock($"Block {block + 1:00} Curb", new Vector3(4.0f, 0.05f, centerZ),
                new Vector3(0.5f, 0.3f, BlockLength), curb, true);

            CreateEncounter(SidewalkEncounter.Kind.Street,
                new Vector3(10.8f, 0.9f, centerZ), new Vector3(12.8f, 1.8f, BlockLength - 0.3f), block);
            CreateEncounter(SidewalkEncounter.Kind.Yard,
                new Vector3(-8.1f, 0.9f, centerZ), new Vector3(8.8f, 1.8f, BlockLength - 0.3f), block);

            // Eight readable pieces of sidewalk activity per block. They make
            // the route busy without requiring external art packs.
            for (int prop = 0; prop < 8; prop++)
            {
                float z = centerZ - BlockLength * 0.43f + prop * (BlockLength * 0.12f);
                float x = prop % 2 == 0 ? -2.75f : 2.75f;
                if (prop % 3 == 0)
                    CreateBlock($"B{block + 1} Hedge {prop}", new Vector3(-4.1f, 0.65f, z),
                        new Vector3(1.0f, 1.3f, 2.1f), hedge, true);
                else
                    CreateBlock($"B{block + 1} Planter {prop}", new Vector3(x, 0.32f, z),
                        new Vector3(0.52f, 0.64f, 0.52f), prop % 2 == 0 ? warm : cool, true);
            }

            float encounterX = ((block * 37) % 5 - 2) * 0.78f;
            float encounterZ = centerZ + (block % 2 == 0 ? 5.5f : -5.5f);
            SidewalkEncounter encounter = CreateEncounter(story[block],
                new Vector3(encounterX, 1.05f, encounterZ), new Vector3(3.0f, 2.1f, 3.4f), block);
            BuildEncounterFigure(encounter.transform, story[block], warm, cool, dog, hedge);
            if (story[block] == SidewalkEncounter.Kind.Dog)
            {
                SidewalkDogChaser chase = encounter.gameObject.AddComponent<SidewalkDogChaser>();
                chase.Player = _player;
                chase.Director = this;
            }
        }

        float finishZ = HomePathLength + BlockCount * BlockLength + 2.5f;
        CreateBlock("School Gate Left", new Vector3(-2.8f, 2f, finishZ),
            new Vector3(0.45f, 4f, 0.45f), cool, true);
        CreateBlock("School Gate Right", new Vector3(2.8f, 2f, finishZ),
            new Vector3(0.45f, 4f, 0.45f), cool, true);
        CreateBlock("SCHOOL", new Vector3(0f, 3.8f, finishZ),
            new Vector3(5.7f, 0.5f, 0.45f), cool, true);
        CreateEncounter(SidewalkEncounter.Kind.Finish, new Vector3(0f, 1f, finishZ),
            new Vector3(5.5f, 2f, 2f), BlockCount);
    }

    private void BuildEncounterFigure(Transform parent, SidewalkEncounter.Kind kind,
        Material warm, Material cool, Material dog, Material hedge)
    {
        if (kind == SidewalkEncounter.Kind.HedgeCat)
        {
            Transform bush = CreateBlock("Hedge hiding the cat", Vector3.zero,
                new Vector3(2.8f, 1.7f, 2.2f), hedge, false, parent);
            CreateSphere("Left cat paw", new Vector3(-0.48f, 0.18f, -1.15f),
                new Vector3(0.28f, 0.16f, 0.45f), warm, parent);
            CreateSphere("Right cat paw", new Vector3(0.48f, 0.18f, -1.15f),
                new Vector3(0.28f, 0.16f, 0.45f), warm, parent);
            return;
        }
        if (kind == SidewalkEncounter.Kind.Dog)
        {
            CreateBlock("Dog body", parent, new Vector3(0f, -0.42f, 0f),
                new Vector3(1.15f, 0.62f, 0.52f), dog, false);
            CreateSphere("Dog head", new Vector3(0f, 0.0f, -0.48f),
                new Vector3(0.48f, 0.48f, 0.48f), dog, parent);
            return;
        }
        // Deliberately large adults make Allen feel Little without rescaling his
        // animation rig. Child and bird encounters use a smaller silhouette.
        float height = kind == SidewalkEncounter.Kind.FriendlyKid ||
                       kind == SidewalkEncounter.Kind.Crows ? 1.9f : 3.5f;
        CreateBlock("Figure body", parent, new Vector3(0f, height * 0.28f, 0f),
            new Vector3(height * 0.34f, height * 0.56f, height * 0.27f), cool, false);
        CreateSphere("Figure head", new Vector3(0f, height * 0.68f, 0f),
            Vector3.one * height * 0.23f, warm, parent);
    }

    private SidewalkEncounter CreateEncounter(SidewalkEncounter.Kind kind,
        Vector3 localPosition, Vector3 localSize, int block)
    {
        GameObject trigger = new GameObject($"Block {block + 1:00} - {kind}");
        trigger.transform.SetParent(_world, false);
        trigger.transform.localPosition = localPosition;
        BoxCollider collider = trigger.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = localSize;
        SidewalkEncounter encounter = trigger.AddComponent<SidewalkEncounter>();
        encounter.Director = this;
        encounter.EncounterKind = kind;
        encounter.Block = block;
        encounter.StartPosition = trigger.transform.position;
        _encounters.Add(encounter);
        return encounter;
    }

    public void Trigger(SidewalkEncounter encounter)
    {
        if (_busy || _awaitingRetry || _finished || encounter == null || encounter.Used) return;
        encounter.Used = true;
        if (encounter.EncounterKind == SidewalkEncounter.Kind.Street)
        {
            StartCoroutine(CollectionRoutine("street", new[]
            {
                new Line("MOTHER", "Allen! Sidewalk, baby. That road is not negotiating with us."),
                new Line("ALLEN", "I was conducting a traffic-flow experiment."),
                new Line("MOTHER", "Experiment concluded. My arms, now.")
            }));
            return;
        }
        if (encounter.EncounterKind == SidewalkEncounter.Kind.Finish)
        {
            StartCoroutine(FinishRoutine());
            return;
        }
        StartCoroutine(EncounterRoutine(encounter));
    }

    private IEnumerator EncounterRoutine(SidewalkEncounter encounter)
    {
        _busy = true;
        SetPlayerControl(false);
        Line[] lines = DialogueFor(encounter.EncounterKind,
            (_attempt + encounter.Block) % 2);
        for (int i = 0; i < lines.Length; i++)
        {
            Show(lines[i]);
            yield return new WaitForSeconds(lines[i].Seconds);
        }
        float damage = encounter.EncounterKind == SidewalkEncounter.Kind.Dog ? 35f : 25f;
        _will = Mathf.Max(0f, _will - damage);
        if (_will <= 0f)
        {
            yield return CollectionRoutine(encounter.EncounterKind.ToString(), new[]
            {
                new Line("ALLEN", "I have reached my daily limit of being perceived."),
                new Line("MOTHER", "There you are, my brave little Pumpkin-Pants."),
                new Line("ALLEN", "Please retire that name before the birds learn it.")
            });
            yield break;
        }
        _speaker = "ALLEN";
        _subtitle = $"Still going. {_will:0} Will remaining.";
        SetPlayerControl(true);
        _busy = false;
    }

    private IEnumerator CollectionRoutine(string source, Line[] lines)
    {
        _busy = true;
        SetPlayerControl(false);
        for (int i = 0; i < lines.Length; i++)
        {
            Show(lines[i]);
            yield return new WaitForSeconds(lines[i].Seconds);
        }
        _speaker = "MOTHER";
        _subtitle = "Pumpkin-Pants, want to play again?";
        _awaitingRetry = true;
        _busy = false;
    }

    private IEnumerator FinishRoutine()
    {
        _busy = true;
        SetPlayerControl(false);
        Show(new Line("ALLEN", "School. I survived affection, advice, wildlife, and municipal paving."));
        yield return new WaitForSeconds(3.5f);
        Show(new Line("MOTHER", "I knew you could do it, Snuggle-Bottom!"));
        yield return new WaitForSeconds(2.8f);
        Show(new Line("ALLEN", "Please let the bell erase that sentence."));
        _finished = true;
    }

    private void Update()
    {
        if (!_awaitingRetry) return;
        bool retry = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            retry = Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame;
#else
        retry = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
        if (retry) ResetRun();
    }

    private void ResetRun()
    {
        StopAllCoroutines();
        _attempt++;
        _will = 100f;
        _awaitingRetry = false;
        _finished = false;
        _busy = false;
        if (_controller != null) _controller.enabled = false;
        _player.SetPositionAndRotation(_startPosition, _startRotation);
        if (_controller != null) _controller.enabled = true;
        for (int i = 0; i < _encounters.Count; i++) _encounters[i].ResetForRun();
        SetPlayerControl(true);
        _speaker = "ALLEN";
        _subtitle = _attempt % 2 == 0
            ? "Again. This time I am avoiding every mammal." :
              "New plan: walk quickly and answer nothing.";
    }

    private void SetPlayerControl(bool enabled)
    {
        if (_motor != null) _motor.enabled = enabled;
        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.sprint = false;
            _input.jump = false;
        }
    }

    private void Show(Line line)
    {
        _speaker = line.Speaker;
        _subtitle = line.Text;
    }

    private Line[] DialogueFor(SidewalkEncounter.Kind kind, int variant)
    {
        switch (kind)
        {
            case SidewalkEncounter.Kind.MailCarrier:
                return variant == 0 ? new[] {
                    new Line("MAIL CARRIER", "School today? Good marks? Did Grandma send cookies?"),
                    new Line("ALLEN", "We use competencies, I do not discuss records, and those cookies had no witnesses."),
                    new Line("MAIL CARRIER", "So you ate them."), new Line("ALLEN", "This interview is over.") }
                : new[] { new Line("MAIL CARRIER", "Morning, little man! What grade are you in?"),
                    new Line("ALLEN", "I do not talk to strangers carrying federal correspondence."),
                    new Line("MAIL CARRIER", "I deliver your mail."), new Line("ALLEN", "Then you know too much already.") };
            case SidewalkEncounter.Kind.Dog:
                return new[] { new Line("DOG", "WOOF!"),
                    new Line("ALLEN", "No. Stop. Please stop licking my face. I am going to get pink eye."),
                    new Line("DOG", "WOOF! WOOF!"), new Line("ALLEN", "Your argument lacks citations.") };
            case SidewalkEncounter.Kind.HedgeCat:
                return new[] { new Line("CAT", "PRRRRRRRRR."),
                    new Line("ALLEN", "Nice kitty. Look, a fish. Somewhere else."),
                    new Line("CAT", "PRRRR."), new Line("ALLEN", "I accept that negotiations have failed.") };
            case SidewalkEncounter.Kind.Teens:
                return variant == 0 ? new[] { new Line("TEEN", "Don't smoke. Brush your teeth. Don't eat yellow snow."),
                    new Line("ALLEN", "I was walking to school, not requesting a survival manual."),
                    new Line("TEEN", "What's the square root of ninety-nine?"), new Line("ALLEN", "About 9.95. May I go now?") }
                : new[] { new Line("TEEN", "Look both ways. Also, all snow is dirty."),
                    new Line("ALLEN", "This is a sidewalk and it is August."),
                    new Line("TEEN", "Good. You're learning."), new Line("ALLEN", "I deeply disagree.") };
            case SidewalkEncounter.Kind.Hose:
                return new[] { new Line("NEIGHBOR", "Hi, kids!"), new Line("ALLEN", "Please do not turn that—"),
                    new Line("HOSE", "FSSSSHHHH!"), new Line("ALLEN", "I am now ninety percent sock.") };
            case SidewalkEncounter.Kind.Crows:
                return new[] { new Line("CROWS", "PUMPKIN-PANTS! PUMPKIN-PANTS!"),
                    new Line("ALLEN", "Mother has compromised the local wildlife."),
                    new Line("CROWS", "SNUGGLE-BOTTOM!"), new Line("ALLEN", "This ecosystem is hostile.") };
            case SidewalkEncounter.Kind.FriendlyKid:
                return new[] { new Line("KID", "Come on! You're too slow!"),
                    new Line("ALLEN", "That is my hand, not a tow cable."),
                    new Line("KID", "We're helping!"), new Line("ALLEN", "Your help has lateral acceleration.") };
            case SidewalkEncounter.Kind.Mower:
                return new[] { new Line("MOWER", "BRRRRRRRR!"), new Line("ALLEN", "Why is the lawn throwing geology?"),
                    new Line("ADULT", "Oh! Sorry, little guy!"), new Line("ALLEN", "Accident acknowledged. Mower off, please.") };
            case SidewalkEncounter.Kind.Yard:
                return new[] { new Line("NEIGHBOR", "Hey! Get those kids out of my yard!"),
                    new Line("ALLEN", "Singular kid. Briefly misplaced. Correcting now.") };
            default:
                return new[] { new Line("WALKER", "Oh my goodness, aren't you adorable!"),
                    new Line("ALLEN", "I am late, sentient, and not currently accepting hugs."),
                    new Line("WALKER", "Just one little squeeze!"), new Line("ALLEN", "That is exactly one too many.") };
        }
    }

    private void OnGUI()
    {
        if (_player == null) return;
        if (_subtitleStyle == null)
        {
            _subtitleStyle = new GUIStyle(GUI.skin.box) { fontSize = 24, alignment = TextAnchor.MiddleCenter,
                wordWrap = true, padding = new RectOffset(18, 18, 12, 12) };
            _subtitleStyle.normal.textColor = Color.white;
            _smallStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
            _smallStyle.normal.textColor = Color.white;
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 25, fontStyle = FontStyle.Bold };
        }
        float route = Mathf.Max(0f, Vector3.Dot(_player.position - _startPosition, _routeForward) - HomePathLength);
        int block = Mathf.Clamp(Mathf.FloorToInt(route / BlockLength) + 1, 1, BlockCount);
        GUI.Box(new Rect(18f, 62f, 360f, 38f), $"BLOCK {block}/10   ATTEMPT {_attempt}   NO CHECKPOINTS", _smallStyle);
        GUI.Box(new Rect(18f, 106f, 360f, 32f), "", _smallStyle);
        Color old = GUI.color;
        GUI.color = Color.Lerp(new Color(0.85f, 0.18f, 0.16f), new Color(0.18f, 0.75f, 0.39f), _will / 100f);
        GUI.DrawTexture(new Rect(22f, 110f, 352f * (_will / 100f), 24f), Texture2D.whiteTexture);
        GUI.color = old;
        GUI.Label(new Rect(28f, 108f, 340f, 28f), $"WILL TO EXIST  {_will:0}", _smallStyle);
        GUI.Box(new Rect(Screen.width * 0.14f, Screen.height - 138f, Screen.width * 0.72f, 112f),
            $"{_speaker}:  {_subtitle}", _subtitleStyle);
        if (_awaitingRetry)
        {
            Rect button = new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f - 38f, 340f, 76f);
            if (GUI.Button(button, "PLAY AGAIN\nSpace / Enter", _buttonStyle)) ResetRun();
        }
    }

    private Transform CreateBlock(string name, Vector3 localPosition, Vector3 localScale,
        Material material, bool keepCollider, Transform parent = null)
    {
        return CreateBlock(name, parent == null ? _world : parent, localPosition, localScale, material, keepCollider);
    }

    private static Transform CreateBlock(string name, Transform parent, Vector3 localPosition,
        Vector3 localScale, Material material, bool keepCollider)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = localScale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider) Destroy(obj.GetComponent<Collider>());
        return obj.transform;
    }

    private static void CreateSphere(string name, Vector3 localPosition, Vector3 localScale,
        Material material, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = localScale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(obj.GetComponent<Collider>());
    }

    private static Material MakeMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        material.enableInstancing = true;
        return material;
    }
}

public sealed class SidewalkEncounter : MonoBehaviour
{
    public enum Kind { MailCarrier, Dog, HedgeCat, Teens, Hose, Crows, FriendlyKid, Mower, Walker, Yard, Street, Finish }
    public SidewalkGameDirector Director;
    public Kind EncounterKind;
    public int Block;
    public bool Used;
    public Vector3 StartPosition;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            Director?.Trigger(this);
    }

    public void ResetForRun()
    {
        Used = false;
        transform.position = StartPosition;
    }
}

/// <summary>Block-two dog only gives chase when Allen lingers nearby.</summary>
public sealed class SidewalkDogChaser : MonoBehaviour
{
    public Transform Player;
    public SidewalkGameDirector Director;
    public float NoticeDistance = 11f;
    public float Speed = 3.3f;

    private void Update()
    {
        if (Player == null) return;
        Vector3 delta = Player.position - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude > NoticeDistance * NoticeDistance || delta.sqrMagnitude < 0.2f) return;
        transform.position += delta.normalized * (Speed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(delta.normalized, Vector3.up), 9f * Time.deltaTime);
    }
}
