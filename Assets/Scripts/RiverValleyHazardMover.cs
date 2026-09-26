using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class RiverValleyHazardMover : MonoBehaviour
{
    [SerializeField] private Vector3 pointA;
    [SerializeField] private Vector3 pointB;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float waitAtEnds = 1f;
    [SerializeField] private string speaker = "SNOWPLOW";
    [SerializeField, TextArea] private string line = "A wall of snow wins the right of way.";
    [SerializeField] private float sparkDamage = 10f;
    [SerializeField] private float slowScale = 0.48f;
    [SerializeField] private float slowSeconds = 2.5f;
    [SerializeField] private float activateWhenPlayerZ = -1000f;
    [SerializeField] private AudioClip movingLoop;
    [SerializeField] private AudioClip impactClip;
    [SerializeField] private ParticleSystem snowSpray;
    private Transform player;
    private RiverValleyGameDirector director;
    private WinterAudioDirector audioDirector;
    private AudioSource movingSource;
    private bool towardB = true;
    private float waitUntil;
    private float nextHit;
    private float nextFollowerHit;
    private bool levelTwoDanger;
    private bool levelThreeDanger;
    private Renderer[] visualRenderers;
    private readonly RaycastHit[] groundHits=new RaycastHit[16];

    private void Start()
    {
        director = FindFirstObjectByType<RiverValleyGameDirector>();
        audioDirector = FindFirstObjectByType<WinterAudioDirector>();
        player = director != null ? director.Player : null;
        visualRenderers=GetComponentsInChildren<Renderer>(true);
        if (movingLoop != null)
        {
            movingSource = gameObject.AddComponent<AudioSource>();
            movingSource.clip = movingLoop;
            movingSource.loop = true;
            movingSource.playOnAwake = false;
            movingSource.spatialBlend = 1f;
            movingSource.minDistance = 4f;
            movingSource.maxDistance = 68f;
            movingSource.dopplerLevel = 0.45f;
            movingSource.volume = 0f;
            movingSource.Play();
        }
    }

    private void Update()
    {
        bool isPlow=name.ToLowerInvariant().Contains("plow");
        bool active = isPlow || (player != null && player.position.z >= activateWhenPlayerZ);
        if(isPlow&&visualRenderers!=null)
            foreach(Renderer renderer in visualRenderers)
                if(renderer!=null&&!renderer.enabled)renderer.enabled=true;
        if (movingSource != null)
        {
            float nearby = player == null ? 0f : 1f - Mathf.InverseLerp(8f, 68f, Vector3.Distance(player.position, transform.position));
            movingSource.volume = Mathf.MoveTowards(movingSource.volume, active ? nearby : 0f, Time.deltaTime * 1.7f);
            float audibleSpeed=levelThreeDanger?speed*1.58f:levelTwoDanger?speed*1.28f:speed;
            movingSource.pitch = Mathf.Lerp(0.86f, 1.08f, Mathf.Clamp01(audibleSpeed / 7f));
        }
        if (!active || Time.time < waitUntil) return;
        Vector3 destination = towardB ? pointB : pointA;
        Vector3 before = transform.position;
        float effectiveSpeed=levelThreeDanger?speed*1.58f:levelTwoDanger?speed*1.28f:speed;
        transform.position = Vector3.MoveTowards(transform.position, destination, effectiveSpeed*Time.deltaTime);
        if(isPlow&&TryFindGround(transform.position,out float groundY))
        {
            Vector3 grounded=transform.position;
            grounded.y=groundY+0.08f;
            transform.position=grounded;
        }
        Vector3 direction = transform.position-before;
        if (direction.sqrMagnitude>0.001f)
        {
            // The handmade vehicles are modelled with their blade/front on
            // local -Z, so face that end into travel instead of driving them
            // backward down the street.
            bool visualFrontIsNegativeZ = name.ToLowerInvariant().Contains("plow") ||
                name.ToLowerInvariant().Contains("bicycle");
            Vector3 facing = visualFrontIsNegativeZ ? -direction.normalized : direction.normalized;
            transform.rotation=Quaternion.LookRotation(facing,Vector3.up);
        }
        if ((transform.position-destination).sqrMagnitude<0.01f) { towardB=!towardB; waitUntil=Time.time+waitAtEnds; }
    }

    private bool TryFindGround(Vector3 position,out float y)
    {
        y=position.y;
        float nearest=float.PositiveInfinity;
        int count=Physics.RaycastNonAlloc(position+Vector3.up*5f,Vector3.down,groundHits,12f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            Collider candidate=groundHits[i].collider;
            if(candidate==null||candidate.transform.IsChildOf(transform))continue;
            if(groundHits[i].distance>=nearest)continue;
            nearest=groundHits[i].distance;
            y=groundHits[i].point.y;
        }
        return nearest<float.PositiveInfinity;
    }

    private void OnTriggerEnter(Collider other)
    {
        RiverValleyKidFollower follower = other.GetComponentInParent<RiverValleyKidFollower>();
        if (follower != null && name.ToLowerInvariant().Contains("plow"))
        {
            Vector3 localContact = transform.InverseTransformPoint(follower.transform.position);
            bool actuallyAtBlade = localContact.z < -1.0f && Mathf.Abs(localContact.x) < 2.35f;
            bool readableNearDanny = player != null &&
                Vector3.ProjectOnPlane(follower.transform.position-player.position,Vector3.up).magnitude < 15f;
            Camera gameCamera=Camera.main;
            Vector3 viewport=gameCamera!=null?gameCamera.WorldToViewportPoint(transform.position):Vector3.back;
            bool visiblyInGameCamera=viewport.z>0f&&viewport.x>-0.08f&&viewport.x<1.08f&&
                viewport.y>-0.08f&&viewport.y<1.08f;
            if (Time.time >= nextFollowerHit && follower.CanTakePlowHit &&
                actuallyAtBlade && readableNearDanny && visiblyInGameCamera)
            {
                // One readable mishap per visible pass. The old sub-second
                // cooldown let the blade sweep through a tight school group
                // and launch the first two or three children in a row.
                nextFollowerHit = Time.time + 7f;
                Vector3 bladeSide = transform.right * (Mathf.Sin(Time.time * 5.1f) >= 0f ? 1f : -1f);
                director?.FollowerBuriedByPlow(follower, follower.transform.position + bladeSide * 1.8f);
                if (impactClip != null) AudioSource.PlayClipAtPoint(impactClip, follower.transform.position, 0.72f);
                else audioDirector?.PlaySnowImpact(follower.transform.position, 0.72f);
                if (snowSpray != null) snowSpray.Emit(90);
            }
            return;
        }
        if (Time.time < nextHit || other.GetComponentInParent<DannySpark>() == null) return;
        nextHit=Time.time+3f;
        director?.LevelTwoHazardLaunch(transform,speaker,line,sparkDamage);
        if (impactClip != null) AudioSource.PlayClipAtPoint(impactClip, other.transform.position, 0.82f);
        else audioDirector?.PlaySnowImpact(other.transform.position);
        if (snowSpray != null) snowSpray.Emit(70);
    }

    public void EnableLevelTwoDanger()
    {
        levelTwoDanger=true;
    }

    public void EnableLevelThreeDanger()
    {
        levelTwoDanger=true;
        levelThreeDanger=true;
        waitAtEnds=Mathf.Max(0.35f,waitAtEnds*0.72f);
    }

#if UNITY_EDITOR
    public void Configure(Vector3 a, Vector3 b, float newSpeed, float newWait, string newSpeaker,
        string newLine, float damage, float newSlowScale, float newSlowSeconds, float activationZ)
    {
        pointA=a; pointB=b; speed=newSpeed; waitAtEnds=newWait; speaker=newSpeaker; line=newLine;
        sparkDamage=damage; slowScale=newSlowScale; slowSeconds=newSlowSeconds; activateWhenPlayerZ=activationZ;
        transform.position=a;
    }

    public void ConfigureAudio(AudioClip newMovingLoop, AudioClip newImpactClip, ParticleSystem newSnowSpray)
    {
        movingLoop = newMovingLoop;
        impactClip = newImpactClip;
        snowSpray = newSnowSpray;
    }
#endif
}
