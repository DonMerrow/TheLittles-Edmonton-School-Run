using UnityEngine;

namespace TheLittles
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Willpower))]
    public sealed class LittleMotor2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5.2f;
        [SerializeField] private float crawlSpeed = 2.7f;
        [SerializeField] private float jumpSpeed = 9.2f;
        [SerializeField] private float groundedDistance = 0.12f;
        [SerializeField] private LayerMask groundMask = ~(1 << 2);

        [Header("Paper wobble")]
        [SerializeField] private Transform visual;
        [SerializeField] private float tiltDegrees = 8f;
        [SerializeField] private float wobbleSpeed = 10f;
        [SerializeField, Range(0f, 1f)] private float stubbornness = 0.35f;

        private Rigidbody2D body;
        private BoxCollider2D box;
        private LittleVisualController visualController;
        private LittleVoice voice;
        private float moveCommand;
        private float actualMove;
        private float idleTime;
        private float personalitySeed;
        private float attentionUntil;
        private float landingPose;
        private float stumbleUntil;
        private float socialUntil;
        private float followCommand;
        private float rushBoost;
        private Vector2 socialTarget;
        private LittleSocialAction socialAction;
        private Vector2 stackTarget;
        private float configuredGravity;
        private float depthPosition;
        private float depthCommand;
        private float followDepth;
        private Vector3 visualHome;
        private bool stacking;
        private float previousVerticalSpeed;
        private Vector2 standingColliderSize;
        private Vector2 standingColliderOffset;
        private Vector2 spawnPosition;
        private bool wasGrounded;
        private bool jumpQueued;

        public bool IsActiveLittle { get; private set; }
        public bool IsHeldAtSwitch { get; private set; }
        public bool IsCrawling { get; private set; }
        public Willpower Willpower { get; private set; }
        public LittleVoice Voice => voice;
        public int StyleIndex { get; private set; }
        public bool IsStumbled => Time.time < stumbleUntil;
        public bool IsUnderAttention => Time.time < attentionUntil;
        public Vector2 Position => transform.position;
        public float DepthPosition => depthPosition;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            standingColliderSize = box.size;
            standingColliderOffset = box.offset;
            spawnPosition = transform.position;
            Willpower = GetComponent<Willpower>();
            voice = GetComponent<LittleVoice>();
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual != null) visualHome = visual.localPosition;
            if (visual != null) visualController = visual.GetComponent<LittleVisualController>();
            wasGrounded = IsGrounded();
        }

        public void Configure(float reluctance, float seed, float weight, int style)
        {
            stubbornness = Mathf.Clamp01(reluctance);
            personalitySeed = seed;
            body.mass = Mathf.Max(0.55f, weight);
            body.gravityScale = Mathf.Lerp(2.55f, 3.15f, Mathf.InverseLerp(0.65f, 1.25f, body.mass));
            configuredGravity = body.gravityScale;
            body.linearDamping = 0.35f;
            StyleIndex = style;
        }

        public void SetActiveLittle(bool value)
        {
            IsActiveLittle = value && !Willpower.IsInClass && !IsHeldAtSwitch;
            if (!IsActiveLittle)
            {
                moveCommand = 0f;
                depthCommand = 0f;
            }
            else voice?.Say(LittleLineKind.Selected);
        }

        public void SetMove(float value)
        {
            moveCommand = IsActiveLittle ? Mathf.Clamp(value, -1f, 1f) : 0f;
        }

        public void SetDepth(float value)
        {
            depthCommand = IsActiveLittle ? Mathf.Clamp(value, -1f, 1f) : 0f;
        }

        public void Follow(Vector2 target, float direction, bool rushing, float targetDepth)
        {
            if (IsActiveLittle || Willpower.IsInClass || IsHeldAtSwitch) return;
            float delta = target.x - transform.position.x;
            float distance = Mathf.Abs(delta);
            followCommand = distance < 0.42f ? 0f : Mathf.Sign(delta) * Mathf.InverseLerp(0.42f, 1.35f, distance);
            rushBoost = rushing ? 1f : 0f;
            followDepth = targetDepth;
            if (target.y - transform.position.y > 0.55f && IsGrounded()) jumpQueued = true;
        }

        public void StopFollowing()
        {
            followCommand = 0f;
            rushBoost = 0f;
        }

        public void SetDoorStack(bool value, Vector2 target)
        {
            stacking = value && !Willpower.IsInClass && !IsHeldAtSwitch;
            stackTarget = target;
            body.gravityScale = stacking ? 0.35f : configuredGravity;
            if (stacking)
            {
                moveCommand = 0f;
                followCommand = 0f;
                SetCrawlInternal(false);
            }
        }

        public void BeginSocial(LittleSocialAction action, Vector2 target, float duration)
        {
            socialAction = action;
            socialTarget = target;
            socialUntil = Time.time + duration;
        }

        public void Stumble()
        {
            if (IsHeldAtSwitch || Willpower.IsInClass || IsStumbled) return;
            stumbleUntil = Time.time + 1.8f;
            landingPose = 1f;
            SetCrawlInternal(true);
            body.AddForce(new Vector2(-Mathf.Sign(body.linearVelocity.x) * 1.4f, 1.2f), ForceMode2D.Impulse);
        }

        public void HelpUp()
        {
            if (!IsStumbled) return;
            stumbleUntil = Time.time + 0.22f;
            landingPose = Mathf.Min(landingPose, 0.42f);
            voice?.Say(LittleLineKind.Selected);
        }

        public void QueueJump()
        {
            if (!IsActiveLittle) return;
            jumpQueued = true;
            if (Random.value < 0.28f) voice?.Say(LittleLineKind.Jump);
        }

        public void SetCrawl(bool value)
        {
            if (!IsActiveLittle || Willpower.IsInClass || IsHeldAtSwitch) value = false;
            if (IsStumbled) value = true;
            SetCrawlInternal(value);
        }

        public void ApplyAttentionPull(Vector2 source, float strength)
        {
            if (Willpower.IsInClass || IsHeldAtSwitch) return;
            Vector2 direction = (source - (Vector2)transform.position).normalized;
            float resistance = Mathf.Abs(moveCommand) > 0.1f ? 0.36f : 1f;
            body.AddForce(direction * strength * resistance, ForceMode2D.Force);
            attentionUntil = Time.time + 0.18f;
            voice?.Say(LittleLineKind.Attention);
        }

        public void ApplyDepthPull(float targetDepth, float strength)
        {
            if (Willpower.IsInClass || IsHeldAtSwitch) return;
            depthPosition = Mathf.MoveTowards(depthPosition, Mathf.Clamp(targetDepth, -1f, 1f),
                strength * Time.deltaTime);
        }

        public void HoldAtSwitch(bool value)
        {
            IsHeldAtSwitch = value;
            if (value)
            {
                IsActiveLittle = false;
                moveCommand = 0f;
                body.linearVelocity = Vector2.zero;
                voice?.Say(LittleLineKind.Sacrifice);
            }
        }

        private void FixedUpdate()
        {
            if (Willpower.IsInClass || IsHeldAtSwitch) return;
            if (transform.position.y < -6f || transform.position.x < -28.3f || transform.position.x > 34.3f)
            {
                transform.position = spawnPosition;
                body.linearVelocity = Vector2.zero;
                Willpower.Drain(12f);
                voice?.Say(LittleLineKind.Land);
            }

            bool groundedNow = IsGrounded();
            float desiredDepth = IsActiveLittle ? depthPosition + depthCommand * Time.fixedDeltaTime * 1.35f : followDepth;
            depthPosition = Mathf.MoveTowards(depthPosition, Mathf.Clamp(desiredDepth, -1f, 1f),
                Time.fixedDeltaTime * (IsActiveLittle ? 1.35f : 1.05f));
            if (stacking)
            {
                Vector2 error = stackTarget - (Vector2)transform.position;
                body.AddForce(error * body.mass * 42f - body.linearVelocity * body.mass * 8f, ForceMode2D.Force);
                actualMove = body.linearVelocity.x;
                visualController?.SetMotion(actualMove / moveSpeed, error.x, groundedNow, false,
                    false, 0f, body.linearVelocity.y, body.mass);
                visualController?.SetSocial(LittleSocialAction.HelpUp, error);
                previousVerticalSpeed = body.linearVelocity.y;
                wasGrounded = groundedNow;
                return;
            }
            if (!wasGrounded && groundedNow && previousVerticalSpeed < -2.5f)
            {
                landingPose = 1f;
                voice?.Say(LittleLineKind.Land);
            }
            landingPose = Mathf.MoveTowards(landingPose, 0f, Time.fixedDeltaTime / 0.82f);

            if (Mathf.Abs(moveCommand) > 0.05f) idleTime = 0f;
            else idleTime += Time.fixedDeltaTime;

            float reluctantDrift = 0f;
            if (IsActiveLittle && idleTime > 0.62f && groundedNow)
            {
                float noise = Mathf.PerlinNoise(personalitySeed, Time.time * 0.48f) - 0.5f;
                if (Mathf.Abs(noise) > 0.20f) reluctantDrift = Mathf.Sign(noise) * stubbornness;
            }

            bool following = !IsActiveLittle && Mathf.Abs(followCommand) > 0.05f;
            float effectiveCommand = Mathf.Abs(moveCommand) > 0.05f ? moveCommand : following ? followCommand : reluctantDrift;
            if (IsStumbled) effectiveCommand *= 0.34f;
            if (!IsStumbled && IsCrawling && Time.time >= stumbleUntil && !IsActiveLittle) SetCrawlInternal(false);
            float speed = IsCrawling ? crawlSpeed : moveSpeed;
            speed *= 1f + rushBoost * 0.28f;
            float desired = effectiveCommand * speed;
            float response = Mathf.Abs(moveCommand) > 0.05f || following
                ? (IsCrawling ? 15f : 18f)
                : Mathf.Lerp(9f, 4.2f, stubbornness);
            float velocityError = desired - body.linearVelocity.x;
            float driveForce = velocityError * response * body.mass;
            if (landingPose > 0.58f) driveForce *= 0.38f;
            float maximumForce = body.mass * (IsCrawling ? 34f : 58f);
            body.AddForce(Vector2.right * Mathf.Clamp(driveForce, -maximumForce, maximumForce), ForceMode2D.Force);

            float maximumSpeed = speed * 1.12f;
            if (Mathf.Abs(body.linearVelocity.x) > maximumSpeed)
                body.linearVelocity = new Vector2(Mathf.Sign(body.linearVelocity.x) * maximumSpeed, body.linearVelocity.y);
            actualMove = body.linearVelocity.x;
            if (jumpQueued && groundedNow)
            {
                SetCrawlInternal(false);
                body.linearVelocity = new Vector2(body.linearVelocity.x * 0.12f, jumpSpeed);
            }
            jumpQueued = false;

            TryCrawlScramble(groundedNow);

            if (visual != null)
            {
                float target = -Mathf.Clamp(actualMove / moveSpeed, -1f, 1f) * (tiltDegrees + 5f);
                if (Mathf.Abs(moveCommand) < 0.05f && Mathf.Abs(actualMove) > 0.35f)
                    target = Mathf.Sign(actualMove) * (tiltDegrees + 2f);
                float z = Mathf.LerpAngle(visual.localEulerAngles.z, target, wobbleSpeed * Time.fixedDeltaTime);
                visual.localRotation = Quaternion.Euler(0f, 0f, z);
                visual.localPosition = visualHome + Vector3.up * depthPosition * 0.48f;
                float perspectiveScale = Mathf.Lerp(1.13f, 0.82f, (depthPosition + 1f) * 0.5f);
                visual.localScale = Vector3.one * perspectiveScale;
            }
            visualController?.SetMotion(actualMove / moveSpeed, effectiveCommand, groundedNow,
                Time.time < attentionUntil, IsCrawling, landingPose, body.linearVelocity.y, body.mass);
            visualController?.SetSocial(Time.time < socialUntil ? socialAction : LittleSocialAction.None,
                socialTarget - (Vector2)transform.position);

            if (IsActiveLittle && groundedNow && !IsCrawling && Mathf.Abs(actualMove) > moveSpeed * 0.78f &&
                Random.value < 0.0014f) Stumble();

            previousVerticalSpeed = body.linearVelocity.y;
            wasGrounded = groundedNow;
        }

        private void TryCrawlScramble(bool groundedNow)
        {
            if (!IsCrawling || !groundedNow || Mathf.Abs(moveCommand) < 0.2f) return;
            float direction = Mathf.Sign(moveCommand);
            Bounds bounds = box.bounds;
            Vector2 lowOrigin = new(bounds.center.x + direction * (bounds.extents.x + 0.03f), bounds.center.y);
            Vector2 highOrigin = lowOrigin + Vector2.up * 0.55f;
            bool lowBlocked = Physics2D.Raycast(lowOrigin, Vector2.right * direction, 0.22f, groundMask);
            bool highClear = !Physics2D.Raycast(highOrigin, Vector2.right * direction, 0.28f, groundMask);
            if (!lowBlocked || !highClear) return;

            body.linearVelocity = new Vector2(direction * 2.8f, 4.7f);
            voice?.Say(LittleLineKind.Crawl);
        }

        private void SetCrawlInternal(bool value)
        {
            if (IsCrawling == value) return;
            IsCrawling = value;
            box.size = value ? new Vector2(0.68f, 0.40f) : standingColliderSize;
            box.offset = value ? standingColliderOffset + Vector2.down * 0.21f : standingColliderOffset;
            if (value) voice?.Say(LittleLineKind.Crawl);
        }

        private bool IsGrounded()
        {
            Bounds bounds = box.bounds;
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y - 0.02f);
            return Physics2D.Raycast(origin, Vector2.down, groundedDistance, groundMask);
        }
    }
}
