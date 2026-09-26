using System.Collections.Generic;
using UnityEngine;

public sealed class RiverValleyKidFollower : MonoBehaviour
{
    private const int GroundHitCapacity = 16;
    private Transform target;
    private Vector3 offset;
    private Vector3 scatterDestination;
    private readonly RaycastHit[] groundHits = new RaycastHit[GroundHitCapacity];
    private readonly RaycastHit[] obstacleHits = new RaycastHit[12];
    private Animator[] animators;
    private RiverValleyEncounter encounter;
    private int formationSlot;
    private bool scattered;
    private bool buried;
    private bool waiting;
    private GameObject snowCover;
    private GameObject coldOverlay;
    private Vector3 burialGroundPosition;
    private Vector3 tumbleStartPosition;
    private Quaternion tumbleStartRotation;
    private Quaternion tumbleEndRotation;
    private Vector3 tumbleEndPosition;
    private float tumbleStarted;
    private float tumbleDuration;
    private bool tumbling;
    private bool cold;
    private bool bootsUp;
    private static int tumbleSequence;
    private bool targetSampleReady;
    private Vector3 previousTargetPosition;
    private Vector3 formationForward = Vector3.forward;
    private Vector3 smoothedVelocity;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private float joinedAt;
    private float lastGroundedMoveAt;
    private float lastBreadcrumbRecoveryAt;
    private RiverValleyWhiteout whiteout;
    private RiverValleyKidFollower handPartner;
    private float nextPartnerSearch;
    private Animator humanoidAnimator;
    private Transform leftUpperArm;
    private Transform leftLowerArm;
    private Transform leftHand;
    private Transform rightUpperArm;
    private Transform rightLowerArm;
    private Transform rightHand;
    private bool holdingHands;
    private HandRig[] handRigs;
    private Transform[] visibleCastMembers;

    private sealed class HandRig
    {
        public Transform Root;
        public Transform LeftUpper;
        public Transform LeftLower;
        public Transform LeftHand;
        public Transform RightUpper;
        public Transform RightLower;
        public Transform RightHand;
    }
    private Vector3 previousVisualPosition;
    private bool visualDirectionReady;
    private float regatheredAt;
    private Vector3 intendedFacingDirection = Vector3.forward;
    private float currentMovementSpeed;
    private Vector3 lastSupportedPosition;
    private Vector3[] visibleLocalPositions;
    private Quaternion[] visibleLocalRotations;
    // Follow Danny's actual path around corners instead of cutting through scenery.
    private readonly List<Vector3> routeBreadcrumbs=new();
    private Vector3 lastBreadcrumbPosition;

    public bool IsScattered => scattered;
    public bool IsBuried => buried;
    public bool IsWaiting => waiting;
    public bool IsTumbling => tumbling;
    public bool IsCold => cold;
    public bool IsHoldingHands => holdingHands;
    public bool IsMoving => currentMovementSpeed > 0.12f;
    public float FollowingSeconds => Time.time-regatheredAt;
    public RiverValleyEncounter Encounter => encounter;
    public Vector3 IntendedFacingDirection => intendedFacingDirection;
    public bool CanTakePlowHit => !scattered && !buried && !tumbling && !waiting &&
        Time.time - joinedAt >= 4.5f && Time.time - lastGroundedMoveAt <= 0.45f;

    public void Configure(Transform newTarget, int index, RiverValleyEncounter sourceEncounter)
    {
        enabled=true;
        transform.SetParent(null, true);
        encounter = sourceEncounter;
        // Recruited clusters used to keep every child's ambient/street motor.
        // Those motors rotated and moved the visible bodies inside the moving
        // follower root, making them face backwards or look dragged sideways.
        foreach(RiverValleyAmbientActor ambient in GetComponentsInChildren<RiverValleyAmbientActor>(true))
            if(ambient!=null)ambient.enabled=false;
        foreach(RiverValleySchoolStream stream in GetComponentsInChildren<RiverValleySchoolStream>(true))
            if(stream!=null)stream.enabled=false;
        CapsuleCollider followerCollider = GetComponent<CapsuleCollider>();
        if (followerCollider == null) followerCollider = gameObject.AddComponent<CapsuleCollider>();
        followerCollider.enabled = true;
        followerCollider.isTrigger = true;
        followerCollider.center = new Vector3(0f, 0.82f, 0f);
        followerCollider.height = 1.65f;
        followerCollider.radius = 0.42f;
        // A kinematic body lets a moving child trigger another lost-child
        // encounter. That creates the intended chain reaction where the school
        // train itself gathers children instead of Danny touching every marker.
        Rigidbody followerBody = GetComponent<Rigidbody>();
        if (followerBody == null) followerBody = gameObject.AddComponent<Rigidbody>();
        followerBody.detectCollisions = true;
        followerBody.isKinematic = true;
        followerBody.useGravity = false;
        // Formation motion is authored directly every frame. Rigidbody
        // interpolation re-applied an older pose afterward, producing a
        // visible one-frame warp and occasionally making a moving child face
        // exactly backward.
        followerBody.interpolation = RigidbodyInterpolation.None;
        animators = GetComponentsInChildren<Animator>();
        foreach(Animator animator in animators)
            if(animator!=null)animator.applyRootMotion=false;
        List<HandRig> foundRigs=new();
        foreach (Animator candidate in animators)
            if (candidate != null && candidate.isHuman)
            {
                HandRig rig=new()
                {
                    Root=candidate.transform,
                    LeftUpper=candidate.GetBoneTransform(HumanBodyBones.LeftUpperArm),
                    LeftLower=candidate.GetBoneTransform(HumanBodyBones.LeftLowerArm),
                    LeftHand=candidate.GetBoneTransform(HumanBodyBones.LeftHand),
                    RightUpper=candidate.GetBoneTransform(HumanBodyBones.RightUpperArm),
                    RightLower=candidate.GetBoneTransform(HumanBodyBones.RightLowerArm),
                    RightHand=candidate.GetBoneTransform(HumanBodyBones.RightHand)
                };
                foundRigs.Add(rig);
                humanoidAnimator = candidate;
                leftUpperArm = rig.LeftUpper;
                leftLowerArm = rig.LeftLower;
                leftHand = rig.LeftHand;
                rightUpperArm = rig.RightUpper;
                rightLowerArm = rig.RightLower;
                rightHand = rig.RightHand;
            }
        handRigs=foundRigs.ToArray();
        WinterCastIdentity[] identities=GetComponentsInChildren<WinterCastIdentity>(true);
        visibleCastMembers=new Transform[identities.Length];
        visibleLocalPositions=new Vector3[identities.Length];
        visibleLocalRotations=new Quaternion[identities.Length];
        for(int i=0;i<identities.Length;i++)
        {
            Transform member=identities[i]!=null?identities[i].transform:null;
            visibleCastMembers[i]=member;
            if(member==null)continue;
            visibleLocalPositions[i]=transform.InverseTransformPoint(member.position);
            visibleLocalRotations[i]=Quaternion.Inverse(transform.rotation)*member.rotation;
        }
        whiteout = FindFirstObjectByType<RiverValleyWhiteout>();
        joinedAt = Time.time;
        lastGroundedMoveAt = Time.time;
        foreach (Animator animator in animators)
            if (animator != null && animator.GetComponent<WinterAnimationEventRelay>() == null)
                animator.gameObject.AddComponent<WinterAnimationEventRelay>();
        Regather(newTarget, index);
        lastSupportedPosition=transform.position;
        AlignVisibleChildren();
    }

    public void Regather(Transform newTarget, int index)
    {
        bool rescuedFromSnow = buried;
        target = newTarget;
        scattered = false;
        buried = false;
        waiting = false;
        tumbling = false;
        if (snowCover != null) Destroy(snowCover);
        snowCover = null;
        if (rescuedFromSnow)
        {
            cold = true;
            BuildColdOverlay();
        }
        Vector3 uprightForward = newTarget != null
            ? Vector3.ProjectOnPlane(newTarget.forward, Vector3.up)
            : Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (uprightForward.sqrMagnitude < 0.01f) uprightForward = Vector3.forward;
        transform.rotation = Quaternion.LookRotation(uprightForward.normalized, Vector3.up);
        if (TryFindGround(transform.position, out RaycastHit rescueGround))
            transform.position = new Vector3(transform.position.x, rescueGround.point.y + 0.015f, transform.position.z);
        formationSlot = Mathf.Max(0, index);
        offset=FormationOffset(whiteout!=null?whiteout.Intensity:0f);
        handPartner = null;
        holdingHands = false;
        ResetTargetSample();
        routeBreadcrumbs.Clear();
        if(newTarget!=null)
        {
            lastBreadcrumbPosition=newTarget.position;
            routeBreadcrumbs.Add(lastBreadcrumbPosition);
        }
        previousVisualPosition=transform.position;
        visualDirectionReady=true;
        regatheredAt=Time.time;

        if (animators != null)
            for (int i = 0; i < animators.Length; i++)
                if (animators[i] != null)
                    animators[i].speed = 0.92f + ((formationSlot + i * 2) % 5) * 0.035f;
    }

    public void HoldForCinematic(Vector3 safePosition,Vector3 lookAt)
    {
        transform.position=safePosition;
        Vector3 facing=Vector3.ProjectOnPlane(lookAt-safePosition,Vector3.up);
        if(facing.sqrMagnitude>0.01f)
        {
            intendedFacingDirection=facing.normalized;
            transform.rotation=Quaternion.LookRotation(intendedFacingDirection,Vector3.up);
        }
        if(animators!=null)
            foreach(Animator animator in animators)
                if(animator!=null)animator.SetFloat(SpeedHash,0f);
        currentMovementSpeed=0f;
        CapsuleCollider followerCollider=GetComponent<CapsuleCollider>();
        if(followerCollider!=null)followerCollider.enabled=false;
        AlignVisibleChildren();
        enabled=false;
    }

    public void ResumeAfterCinematic(Transform newTarget,Vector3 safePosition)
    {
        transform.position=safePosition;
        CapsuleCollider followerCollider=GetComponent<CapsuleCollider>();
        if(followerCollider!=null)followerCollider.enabled=true;
        enabled=true;
        Regather(newTarget,formationSlot);
        AlignVisibleChildren();
    }

    /// <summary>
    /// Hands the visible children to the school-arrival sequence. Their
    /// follower root stops steering them so every actual child can form one
    /// continuous line and walk through the door independently.
    /// </summary>
    public Transform[] ReleaseForSchoolLine()
    {
        List<Transform> released=new();
        HashSet<Transform> unique=new();
        Transform[] members=visibleCastMembers;
        if(members==null||members.Length==0)
        {
            WinterCastIdentity[] identities=GetComponentsInChildren<WinterCastIdentity>(true);
            members=new Transform[identities.Length];
            for(int i=0;i<identities.Length;i++)members[i]=identities[i]!=null?identities[i].transform:null;
        }

        CapsuleCollider followerCollider=GetComponent<CapsuleCollider>();
        if(followerCollider!=null)followerCollider.enabled=false;
        Rigidbody followerBody=GetComponent<Rigidbody>();
        if(followerBody!=null)followerBody.detectCollisions=false;

        foreach(Transform member in members)
        {
            if(member==null||!unique.Add(member))continue;
            foreach(Collider childCollider in member.GetComponentsInChildren<Collider>(true))
                if(childCollider!=null)childCollider.enabled=false;
            RiverValleyAmbientActor ambient=member.GetComponent<RiverValleyAmbientActor>();
            if(ambient!=null)ambient.enabled=false;
            RiverValleySchoolStream stream=member.GetComponent<RiverValleySchoolStream>();
            if(stream!=null)stream.enabled=false;
            if(member!=transform)member.SetParent(null,true);
            released.Add(member);
        }

        target=null;
        waiting=false;
        holdingHands=false;
        enabled=false;
        return released.ToArray();
    }

    /// <summary>Restores a delivered group as a stationary search target for a later level.</summary>
    public void ResetAsLostAt(Vector3 position,Quaternion facing)
    {
        gameObject.SetActive(true);
        transform.SetParent(null,true);
        transform.SetPositionAndRotation(position,facing);
        target=null;
        scattered=false;
        buried=false;
        waiting=false;
        tumbling=false;
        holdingHands=false;
        currentMovementSpeed=0f;
        if(snowCover!=null)Destroy(snowCover);
        snowCover=null;

        if(visibleCastMembers!=null)
            for(int i=0;i<visibleCastMembers.Length;i++)
            {
                Transform member=visibleCastMembers[i];
                if(member==null)continue;
                member.gameObject.SetActive(true);
                if(member!=transform)
                {
                    member.SetParent(transform,false);
                    if(visibleLocalPositions!=null&&i<visibleLocalPositions.Length)
                        member.localPosition=visibleLocalPositions[i];
                    if(visibleLocalRotations!=null&&i<visibleLocalRotations.Length)
                        member.localRotation=visibleLocalRotations[i];
                }
            }
        if(animators!=null)
            foreach(Animator animator in animators)
                if(animator!=null){animator.SetFloat(SpeedHash,0f);animator.speed=1f;}
        CapsuleCollider followerCollider=GetComponent<CapsuleCollider>();
        if(followerCollider!=null)followerCollider.enabled=false;
        Rigidbody followerBody=GetComponent<Rigidbody>();
        if(followerBody!=null)followerBody.detectCollisions=false;
        enabled=false;
    }

    public void Scatter(int index)
    {
        Vector3 right = target != null ? target.right : transform.right;
        Vector3 forward = target != null ? target.forward : transform.forward;
        float side = index % 2 == 0 ? 1f : -1f;
        float back = index % 3 == 0 ? -1f : 1f;
        scatterDestination = transform.position + right * side * (2.8f + index * 0.55f) +
            forward * back * (1.4f + index * 0.35f);
        if (Physics.Raycast(scatterDestination + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 26f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            scatterDestination.y = hit.point.y;
        target = null;
        scattered = true;
        targetSampleReady = false;
        encounter?.PrepareRegather(scatterDestination, 38f);
    }

    public void ScatterTo(Vector3 destination, float requiredSpark, string newName)
    {
        scatterDestination = destination;
        if (Physics.Raycast(destination + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 26f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            scatterDestination.y = hit.point.y;
        target = null;
        waiting = false;
        scattered = true;
        targetSampleReady = false;
        if (!string.IsNullOrEmpty(newName)) gameObject.name = newName;
        encounter?.PrepareRegather(scatterDestination, requiredSpark);
    }

    public void BuryInSnow(Vector3 destination)
    {
        ScatterTo(destination, 46f, "Snow-stuck child — needs Danny");
        buried = true;
        cold = true;
        burialGroundPosition = scatterDestination;
        tumbleStartPosition = transform.position;
        tumbleStartRotation = transform.rotation;
        bootsUp = (++tumbleSequence % 3) == 0;
        float yaw = transform.eulerAngles.y + (bootsUp ? 18f : (tumbleSequence % 2 == 0 ? 82f : -82f));
        tumbleEndPosition = burialGroundPosition + Vector3.up * (bootsUp ? 1.48f : 0.72f);
        tumbleEndRotation = Quaternion.Euler(0f, yaw, bootsUp ? 174f : (tumbleSequence % 2 == 0 ? 82f : -82f));
        // Keep the comic wipeout readable long enough for the player to see
        // the child leave the plow blade, rotate, and land in the bank.
        tumbleDuration = 1.48f + (tumbleSequence % 3) * 0.12f;
        tumbleStarted = Time.time;
        tumbling = true;
        if (snowCover != null) Destroy(snowCover);
        snowCover = null;
        if (animators != null)
            foreach (Animator animator in animators)
                if (animator != null) animator.SetFloat(SpeedHash, 0f);
    }

    public void WaitAt(Transform post, int index)
    {
        if (scattered || post == null) return;
        target = post;
        waiting = true;
        float angle = index * 1.31f;
        float radius = 0.75f + (index % 3) * 0.22f;
        offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        ResetTargetSample();
    }

    private void Update()
    {
        if (tumbling)
        {
            UpdatePlowTumble();
            return;
        }
        if (coldOverlay != null)
            coldOverlay.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 17f) * 2.5f);

        Vector3 desired;
        float maximumSpeed;
        if (scattered)
        {
            desired = scatterDestination;
            maximumSpeed = 1.15f;
        }
        else
        {
            if (target == null) return;
            UpdateFormationHeading();
            float storm=whiteout!=null?whiteout.Intensity:0f;
            Vector3 formationOffset=FormationOffset(storm);
            offset=Vector3.Lerp(offset,formationOffset,1f-Mathf.Exp(-3.4f*Time.deltaTime));
            Vector3 right = Vector3.Cross(Vector3.up, formationForward).normalized;
            desired = target.position + right * offset.x + formationForward * offset.z;
            RecordDannyTrail();
            while(routeBreadcrumbs.Count>0&&
                Vector3.ProjectOnPlane(routeBreadcrumbs[0]-transform.position,Vector3.up).magnitude<1.15f)
                routeBreadcrumbs.RemoveAt(0);
            if(routeBreadcrumbs.Count>1&&Time.time-lastGroundedMoveAt>1.7f&&
               Time.time-lastBreadcrumbRecoveryAt>1.7f)
            {
                // A trail point on a stair lip or behind a rail can be
                // unreachable. Walk toward the next point; never teleport.
                routeBreadcrumbs.RemoveAt(0);
                lastBreadcrumbRecoveryAt=Time.time;
            }
            if(Time.time-lastGroundedMoveAt>3.2f&&routeBreadcrumbs.Count>0)
            {
                // If scenery made several old trail crumbs unreachable, stop
                // wiggling at that spot and resume toward Danny's live path.
                routeBreadcrumbs.Clear();
                desired=target.position+right*offset.x+formationForward*offset.z;
            }
            if(routeBreadcrumbs.Count>0)desired=routeBreadcrumbs[0];
            maximumSpeed = waiting ? 1.30f : 3.35f;
        }

        Vector3 flat = Vector3.ProjectOnPlane(desired - transform.position, Vector3.up);
        float dannyGap=!scattered&&target!=null
            ? Vector3.ProjectOnPlane(target.position-transform.position,Vector3.up).magnitude : 0f;
        if (!scattered && !waiting)
            maximumSpeed = Mathf.Lerp(3.4f, 5.2f, Mathf.InverseLerp(3f, 14f, dannyGap));
        float response = scattered ? 1.25f : flat.magnitude > 2.8f ? 2.05f : 1.65f;
        float speed = Mathf.Clamp(Mathf.Max(0f, flat.magnitude - 0.08f) * response, 0f, maximumSpeed);
        // A breadcrumb is intentionally close to the child's feet. Steering
        // toward it must not slow a distant child to a shuffle every 0.7 m.
        if(!scattered&&!waiting&&dannyGap>3f)
            speed=Mathf.Max(speed,Mathf.Lerp(2.8f,maximumSpeed,Mathf.InverseLerp(3f,12f,dannyGap)));
        if (flat.magnitude > 0.12f)
        {
            if (!TryChooseGroundedStep(flat.normalized,speed*Time.deltaTime,desired,out Vector3 next))
            {
                speed=0f;
                next=transform.position;
            }
            else lastGroundedMoveAt = Time.time;
            transform.position = next;
            // Inside the huddle, sideways slot corrections must not make a
            // child turn across or away from the route.  Everyone faces the
            // direction Danny is actually leading; a distant straggler still
            // faces their genuine catch-up path until they rejoin.
            intendedFacingDirection = !scattered && !waiting && flat.magnitude < 5.25f &&
                formationForward.sqrMagnitude > 0.01f ? formationForward : flat.normalized;
            Quaternion travelFacing = Quaternion.LookRotation(intendedFacingDirection, Vector3.up);
            float facingTravel = Vector3.Dot(
                Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized,
                intendedFacingDirection);
            // Never let a child visibly moonwalk while the formation changes
            // direction. A backward-facing child turns at once; ordinary
            // corners retain a quick, softened turn.
            transform.rotation = facingTravel < 0.05f
                ? travelFacing
                : Quaternion.Slerp(transform.rotation, travelFacing,
                    1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        currentMovementSpeed=speed;

        // Distant children now sprint back into formation. The previous safety
        // teleport was visible in a small game window and looked like warping.
        if (animators != null)
        {
            float animationSpeed = 0f;
            if (speed > 0.08f)
                animationSpeed = Mathf.Lerp(2f,4.65f,
                    Mathf.InverseLerp(1.2f,maximumSpeed,speed));
            foreach (Animator animator in animators)
                if (animator != null)
                {
                    animator.SetFloat(SpeedHash, animationSpeed, 0.09f, Time.deltaTime);
                    animator.speed = speed > 0.08f
                        ? Mathf.Lerp(0.92f,1.12f,Mathf.InverseLerp(0.15f,maximumSpeed,speed))
                        : 1f;
                }
        }
    }

    private void RecordDannyTrail()
    {
        if(target==null)return;
        Vector3 next=target.position;
        if(Vector3.ProjectOnPlane(next-lastBreadcrumbPosition,Vector3.up).magnitude<0.72f)return;
        routeBreadcrumbs.Add(next);
        lastBreadcrumbPosition=next;
        if(routeBreadcrumbs.Count>600)routeBreadcrumbs.RemoveAt(0);
    }

    private void LateUpdate()
    {
        // Followers are animated by authored motion, not a CharacterController,
        // so the physical city-edge collider alone cannot stop a bad formation
        // target or a tumble from placing them beyond the playable map.
        Vector3 contained=transform.position;
        contained.x=Mathf.Clamp(contained.x,-99.5f,99.5f);
        contained.z=Mathf.Clamp(contained.z,-51.5f,435.5f);
        if(contained.y<-34f)contained=lastSupportedPosition;
        if(contained!=transform.position)
        {
            if(TryFindGround(contained,out RaycastHit edgeGround))
                contained.y=edgeGround.point.y+0.015f;
            transform.position=contained;
            tumbling=false;
            scattered=false;
            waiting=false;
        }
        if(TryFindGround(transform.position,out RaycastHit support)&&
            Mathf.Abs(transform.position.y-support.point.y)<0.65f)
            lastSupportedPosition=transform.position;
        // Animator evaluation and stair grounding can occur after the normal
        // movement update. Face the actual completed frame displacement so a
        // child can never finish a step looking the opposite way.
        if(!visualDirectionReady)
        {
            previousVisualPosition=transform.position;
            visualDirectionReady=true;
        }
        else
        {
            Vector3 travelled=Vector3.ProjectOnPlane(transform.position-previousVisualPosition,Vector3.up);
            previousVisualPosition=transform.position;
            if(!tumbling&&!scattered&&!waiting&&travelled.sqrMagnitude>=0.000004f)
            {
                Vector3 facing=intendedFacingDirection.sqrMagnitude>0.01f
                    ? intendedFacingDirection.normalized : travelled.normalized;
                transform.rotation=Quaternion.LookRotation(facing,Vector3.up);
            }
        }
        // A recruited cluster contains several complete character prefabs.
        // Their humanoid animators can restore each prefab's imported yaw
        // after the group root has turned. Reassert the visible bodies' world
        // facing after animation evaluation so all children actually look in
        // the direction the group is travelling.
        AlignVisibleChildren();
        GroundVisibleChildren();
        // Different imported rigs have different arm proportions. Pulling
        // their hand bones together stretched some arms metres across the
        // road, reading as a flying shovel or sign. The compact formation now
        // provides the huddle without deforming anyone.
        holdingHands=false;
    }

    private void AlignVisibleChildren()
    {
        if(visibleCastMembers==null||tumbling)return;
        Quaternion facing=transform.rotation;
        foreach(Transform member in visibleCastMembers)
            if(member!=null&&member!=transform)
                member.rotation=facing;
    }

    private Vector3 FormationOffset(float storm)
    {
        // A narrow two-abreast school line stays behind Danny and fits on the
        // river stairs without surrounding an activity or blocking traffic.
        int columns=2;
        int row=formationSlot/columns;
        int column=formationSlot%columns;
        float width=Mathf.Lerp(0.88f,0.70f,storm);
        float depth=Mathf.Lerp(0.96f,0.78f,storm);
        float centred=column-(columns-1)*0.5f;
        return new Vector3(centred*width,0f,-1.25f-row*depth);
    }

    private void GroundVisibleChildren()
    {
        if(visibleCastMembers==null||tumbling||buried)return;
        foreach(Transform member in visibleCastMembers)
        {
            if(member==null||member==transform)continue;
            if(!TryFindGround(member.position+Vector3.up*0.15f,out RaycastHit ground))continue;
            float difference=ground.point.y-member.position.y;
            // One encounter can contain several independently positioned
            // children. On stairs each body needs its own tread height.
            if(Mathf.Abs(difference)>1.1f)continue;
            Vector3 position=member.position;
            position.y=ground.point.y+0.015f;
            member.position=position;
        }
    }

    private void UpdateActualHandHolding()
    {
        holdingHands=false;
        // Twisting arm bones over a running animation made knees, shoulders and
        // mittens visibly flutter. Keep this as a stopped-huddle detail; moving
        // children use their authored walking pose without procedural overrides.
        if(tumbling||scattered||target==null||humanoidAnimator==null||currentMovementSpeed>0.12f)
        {
            handPartner=null;
            return;
        }
        // Most encounters bring a small cluster rather than one child. Pair
        // the actual little bodies inside that cluster first, so their real
        // mitten meshes meet. This is the hand-holding visible in the huddle.
        if(handRigs!=null&&handRigs.Length>=2)
        {
            for(int i=0;i+1<handRigs.Length;i+=2)
                AimHandPair(handRigs[i],handRigs[i+1]);
            return;
        }
        if(waiting)return;
        int column=formationSlot%3;
        if(column==2)return;
        int partnerSlot=column==0?formationSlot+1:formationSlot-1;
        if((handPartner==null||handPartner.scattered||handPartner.waiting||handPartner.target!=target||
            handPartner.formationSlot!=partnerSlot)&&Time.time>=nextPartnerSearch)
        {
            nextPartnerSearch=Time.time+0.45f;
            handPartner=null;
            foreach(RiverValleyKidFollower candidate in FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None))
                if(candidate!=null&&!candidate.scattered&&!candidate.waiting&&candidate.target==target&&
                    candidate.formationSlot==partnerSlot){handPartner=candidate;break;}
        }
        if(handPartner==null)return;

        bool partnerOnRight=Vector3.Dot(transform.right,handPartner.transform.position-transform.position)>0f;
        Transform ownUpper=partnerOnRight?rightUpperArm:leftUpperArm;
        Transform ownLower=partnerOnRight?rightLowerArm:leftLowerArm;
        Transform ownHand=partnerOnRight?rightHand:leftHand;
        Transform otherHand=partnerOnRight?handPartner.leftHand:handPartner.rightHand;
        if(ownUpper==null||ownLower==null||ownHand==null||otherHand==null)return;
        if(Vector3.Distance(transform.position,handPartner.transform.position)>1.8f)return;

        Vector3 meetingPoint=Vector3.Lerp(ownHand.position,otherHand.position,0.5f);
        AimArmJoint(ownLower,ownHand,meetingPoint,32f);
        AimArmJoint(ownUpper,ownHand,meetingPoint,24f);
        AimArmJoint(ownLower,ownHand,meetingPoint,18f);
        holdingHands=Vector3.Distance(ownHand.position,otherHand.position)<0.24f;
    }

    private void AimHandPair(HandRig first,HandRig second)
    {
        if(first==null||second==null)return;
        bool firstIsLeft=Vector3.Dot(transform.right,second.Root.position-first.Root.position)>0f;
        HandRig left=firstIsLeft?first:second;
        HandRig right=firstIsLeft?second:first;
        if(left.RightUpper==null||left.RightLower==null||left.RightHand==null||
            right.LeftUpper==null||right.LeftLower==null||right.LeftHand==null)return;
        Vector3 meeting=Vector3.Lerp(left.RightHand.position,right.LeftHand.position,0.5f);
        AimArmJoint(left.RightLower,left.RightHand,meeting,34f);
        AimArmJoint(left.RightUpper,left.RightHand,meeting,26f);
        AimArmJoint(right.LeftLower,right.LeftHand,meeting,34f);
        AimArmJoint(right.LeftUpper,right.LeftHand,meeting,26f);
        AimArmJoint(left.RightLower,left.RightHand,meeting,18f);
        AimArmJoint(right.LeftLower,right.LeftHand,meeting,18f);
        // Do not translate hand bones away from their skeletons. The arm aim
        // provides a close, ordinary hand-holding pose without detached
        // mitten spheres floating beside the children.
        holdingHands=Vector3.Distance(left.RightHand.position,right.LeftHand.position)<0.28f;
    }

    private static void AimArmJoint(Transform joint,Transform hand,Vector3 targetPosition,float maximumDegrees)
    {
        Vector3 current=hand.position-joint.position;
        Vector3 wanted=targetPosition-joint.position;
        if(current.sqrMagnitude<0.00001f||wanted.sqrMagnitude<0.00001f)return;
        Quaternion delta=Quaternion.FromToRotation(current,wanted);
        delta.ToAngleAxis(out float angle,out Vector3 axis);
        if(angle>180f)angle-=360f;
        if(Mathf.Abs(angle)>maximumDegrees)delta=Quaternion.AngleAxis(Mathf.Sign(angle)*maximumDegrees,axis);
        joint.rotation=delta*joint.rotation;
    }

    private void UpdatePlowTumble()
    {
        float t = Mathf.Clamp01((Time.time - tumbleStarted) / Mathf.Max(0.1f, tumbleDuration));
        float eased = Mathf.SmoothStep(0f, 1f, t);
        Vector3 position = Vector3.Lerp(tumbleStartPosition, tumbleEndPosition, eased);
        position.y += Mathf.Sin(t * Mathf.PI) * (2.45f + (bootsUp ? 0.48f : 0f));
        transform.position = position;
        Quaternion rolling = Quaternion.AngleAxis(t * (bootsUp ? 900f : 720f), Vector3.forward);
        transform.rotation = Quaternion.Slerp(tumbleStartRotation * rolling, tumbleEndRotation, eased * eased);
        if (t < 1f) return;

        tumbling = false;
        transform.position = tumbleEndPosition;
        transform.rotation = tumbleEndRotation;
        BuildAngularSnowbank();
        BuildColdOverlay();
    }

    public void TugByCoyote(Vector3 toward, float distance)
    {
        if (!buried || tumbling) return;
        Vector3 flat = Vector3.ProjectOnPlane(toward-transform.position,Vector3.up);
        if (flat.sqrMagnitude < 0.002f) return;
        Vector3 delta=flat.normalized*Mathf.Max(0f,distance);
        transform.position+=delta;
        scatterDestination+=delta;
        burialGroundPosition+=delta;
        tumbleEndPosition+=delta;
        if(snowCover!=null) snowCover.transform.position+=delta;
    }

    private void BuildAngularSnowbank()
    {
        if (snowCover != null) Destroy(snowCover);
        snowCover = new GameObject(bootsUp
            ? "Angular plow bank — boots and mittens visible"
            : "Angular plow bank — child stuck sideways");
        snowCover.transform.position = burialGroundPosition;
        snowCover.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Material snow = MakeMaterial(new Color(0.82f, 0.93f, 0.99f));
        CreateWedge("Compacted plow ridge", snowCover.transform,
            new Vector3(-0.34f, 0f, 0f), new Vector3(1.65f, 0.88f, 1.35f), -9f, snow);
        CreateWedge("Broken snow slab", snowCover.transform,
            new Vector3(0.52f, 0.02f, 0.18f), new Vector3(1.18f, 0.62f, 1.05f), 19f, snow);
    }

    private void BuildColdOverlay()
    {
        if (coldOverlay != null) return;
        coldOverlay = new GameObject("Clinging snow and cold shiver");
        coldOverlay.transform.SetParent(transform, false);
        // Cold is communicated by the shiver animation and HUD. The former
        // cube clumps read as floating white bricks beside every child.
    }

    private static void CreateWedge(string name, Transform parent, Vector3 localPosition,
        Vector3 size, float yaw, Material material)
    {
        GameObject wedge = new(name);
        wedge.transform.SetParent(parent, false);
        wedge.transform.localPosition = localPosition;
        wedge.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Mesh mesh = new() { name = name + " mesh" };
        float x = size.x * 0.5f;
        float z = size.z * 0.5f;
        mesh.vertices = new[]
        {
            new Vector3(-x,0f,-z), new Vector3(x,0f,-z), new Vector3(0f,size.y,-z),
            new Vector3(-x,0f,z),  new Vector3(x,0f,z),  new Vector3(0f,size.y,z)
        };
        mesh.triangles = new[]
        {
            0,2,1, 3,4,5, 0,1,4, 0,4,3,
            0,3,5, 0,5,2, 1,2,5, 1,5,4
        };
        mesh.RecalculateNormals();
        wedge.AddComponent<MeshFilter>().sharedMesh = mesh;
        wedge.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new(shader) { color = color, hideFlags = HideFlags.DontSave };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        return material;
    }

    private void ResetTargetSample()
    {
        targetSampleReady = target != null;
        previousTargetPosition = target != null ? target.position : Vector3.zero;
        smoothedVelocity = Vector3.zero;
        if (target != null)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
            if (flatForward.sqrMagnitude > 0.001f) formationForward = flatForward.normalized;
        }
    }

    private void UpdateFormationHeading()
    {
        if (target == null) return;
        if (!targetSampleReady)
        {
            ResetTargetSample();
            return;
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 frameVelocity = Vector3.ProjectOnPlane(target.position - previousTargetPosition, Vector3.up) / deltaTime;
        previousTargetPosition = target.position;
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, frameVelocity,
            1f - Mathf.Exp(-8f * Time.deltaTime));

        // Looking around should not whirl fifty children around Danny. The
        // formation turns only after he actually travels in a new direction.
        if (!waiting && smoothedVelocity.sqrMagnitude > 0.05f)
        {
            Vector3 travelForward = smoothedVelocity.normalized;
            formationForward = Vector3.Slerp(formationForward, travelForward,
                1f - Mathf.Exp(-3.2f * Time.deltaTime)).normalized;
        }
    }

    private bool TryFindGround(Vector3 position, out RaycastHit bestHit)
    {
        bestHit = default;
        float bestDistance = float.PositiveInfinity;
        int count = Physics.RaycastNonAlloc(position + Vector3.up * 6f, Vector3.down,
            groundHits, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = groundHits[i];
            if (candidate.collider == null || candidate.collider.transform.IsChildOf(transform)) continue;
            if(!IsUsableGround(candidate.collider,candidate.normal))continue;
            if (candidate.distance >= bestDistance) continue;
            bestDistance = candidate.distance;
            bestHit = candidate;
        }
        return bestDistance < float.PositiveInfinity;
    }

    private static bool IsUsableGround(Collider candidate,Vector3 normal)
    {
        if(candidate==null||normal.y<0.48f)return false;
        if(candidate is CharacterController||candidate.GetComponentInParent<DannySpark>()!=null||
            candidate.GetComponentInParent<RiverValleyKidFollower>()!=null||
            candidate.GetComponentInParent<RiverValleyAnimalMotion>()!=null||
            candidate.GetComponentInParent<RiverValleySafeCar>()!=null||
            candidate.GetComponentInParent<RiverValleyHazardMover>()!=null)return false;
        string label=(candidate.name+" "+candidate.transform.root.name).ToLowerInvariant();
        return !label.Contains("solid exterior")&&!label.Contains("invisible stair edge")&&
            !label.Contains("safety boundary")&&!label.Contains("building")&&!label.Contains("roof")&&
            !label.Contains("sign")&&!label.Contains("tree trunk")&&
            !label.Contains("visible brown trunk")&&!label.Contains("hockey stick")&&
            !label.Contains("guardrail")&&!label.Contains("rail")&&!label.Contains("fence");
    }

    private bool TryChooseGroundedStep(Vector3 wantedDirection,float distance,Vector3 desired,out Vector3 chosen)
    {
        chosen=transform.position;
        if(distance<=0f||wantedDirection.sqrMagnitude<0.001f)return false;
        float[] angles={0f,18f,-18f,36f,-36f,58f,-58f,82f,-82f,108f,-108f};
        // A patient child tries a wider way around a drift or another body,
        // but never snaps vertically onto a metre-high ledge.
        float maximumStep=Time.time-lastGroundedMoveAt>0.85f?0.46f:0.34f;
        float bestScore=float.PositiveInfinity;
        bool found=false;
        foreach(float angle in angles)
        {
            Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*wantedDirection;
            Vector3 candidate=transform.position+direction*distance;
            if(!TryFindGround(candidate,out RaycastHit ground))continue;
            float heightDelta=ground.point.y-transform.position.y;
            bool descendingStair=heightDelta<0f&&
                (ground.collider.name.StartsWith("Stair ")||
                 ground.collider.name.Contains("stair landing")||
                 ground.collider.name.Contains("return ramp"));
            float allowedStep=descendingStair?0.90f:maximumStep;
            if(Mathf.Abs(heightDelta)>allowedStep||ground.normal.y<0.48f)continue;
            candidate.y=ground.point.y+0.015f;
            if(!PathIsClear(candidate))continue;
            float score=Vector3.ProjectOnPlane(desired-candidate,Vector3.up).sqrMagnitude+
                Mathf.Abs(angle)*0.0025f;
            if(score>=bestScore)continue;
            bestScore=score;
            chosen=candidate;
            found=true;
        }
        return found;
    }

    private bool PathIsClear(Vector3 destination)
    {
        Vector3 motion=Vector3.ProjectOnPlane(destination-transform.position,Vector3.up);
        float distance=motion.magnitude;
        if(distance<0.01f)return true;
        Vector3 bottom=transform.position+Vector3.up*0.27f;
        Vector3 top=transform.position+Vector3.up*1.28f;
        int count=Physics.CapsuleCastNonAlloc(bottom,top,0.22f,motion.normalized,obstacleHits,
            distance+0.04f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            Collider obstacle=obstacleHits[i].collider;
            if(obstacle==null||obstacle.transform.IsChildOf(transform)||
                obstacle.GetComponentInParent<DannySpark>()!=null||
                obstacle.GetComponentInParent<RiverValleyKidFollower>()!=null)continue;
            // A child who has brushed into the rounded edge of a snowbank or
            // sidewalk must be allowed to step back out. CapsuleCast reports
            // that initial overlap at distance zero for every direction.
            if(obstacleHits[i].distance<=0.025f)continue;
            string label=(obstacle.name+" "+obstacle.transform.root.name).ToLowerInvariant();
            // Treads and packed ramps are the surface being climbed; their
            // shallow front faces are not walls. Everything else—buildings,
            // trees, vehicles and scenery—must be walked around.
            if(label.Contains("stair ")||label.Contains("landing")||label.Contains("return ramp")||
               label.Contains("neighbourhood snow base"))continue;
            return false;
        }
        return true;
    }

    public void OnFootstep(AnimationEvent animationEvent) { }
    public void OnLand(AnimationEvent animationEvent) { }
}
