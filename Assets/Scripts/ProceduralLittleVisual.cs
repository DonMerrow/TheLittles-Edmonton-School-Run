using UnityEngine;
using UnityEngine.Rendering;

namespace TheLittles
{
    public sealed class ProceduralLittleVisual : LittleVisualController
    {
        private static Sprite squareSprite;
        private static Sprite circleSprite;

        private Transform poseRoot;
        private Transform coat;
        private Transform head;
        private Transform fringe;
        private Transform leftEye;
        private Transform rightEye;
        private Transform leftPupil;
        private Transform rightPupil;
        private Transform leftCheek;
        private Transform rightCheek;
        private Transform mouth;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftElbow;
        private Transform rightElbow;
        private Transform leftHand;
        private Transform rightHand;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform leftKnee;
        private Transform rightKnee;
        private Transform leftBoot;
        private Transform rightBoot;
        private Transform shadow;
        private SortingGroup depthGroup;

        private float personalityOffset;
        private float fringeBaseAngle;
        private float movement;
        private float requestedMovement;
        private float verticalVelocity;
        private float crawlBlend;
        private float landingPose;
        private float bodyWeight = 0.9f;
        private float gaitPhase;
        private float displayFacing = 1f;
        private bool grounded;
        private bool underAttention;
        private LittleSocialAction socialAction;
        private Vector2 socialDirection;
        private Vector3 headHome;
        private Vector3 fringeHome;
        private Vector3 leftEyeHome;
        private Vector3 rightEyeHome;
        private Vector3 leftPupilHome;
        private Vector3 rightPupilHome;
        private Vector3 leftCheekHome;
        private Vector3 rightCheekHome;
        private Vector3 mouthHome;

        public void Configure(Color accent, int style)
        {
            depthGroup = gameObject.AddComponent<SortingGroup>();
            personalityOffset = style * 7.13f;
            Color black = new(0.045f, 0.042f, 0.055f);
            Color cloth = new(0.085f + style * 0.012f, 0.075f, 0.10f + style * 0.02f);
            Color skin = new(0.82f, 0.75f, 0.70f);

            shadow = Piece(transform, "Soft paper shadow", new Vector3(0f, -0.47f, 0f),
                new Vector2(0.62f, 0.12f), new Color(0f, 0f, 0f, 0.34f), 1, Circle());

            poseRoot = new GameObject("Articulated pose").transform;
            poseRoot.SetParent(transform, false);

            if (style == 0)
            {
                Piece(poseRoot, "Moth rounded back hair", new Vector3(0f, 0.18f, 0f),
                    new Vector2(0.54f, 0.56f), black, 2, Circle());
            }
            else if (style == 1)
            {
                Piece(poseRoot, "Static short back hair", new Vector3(0f, 0.24f, 0f),
                    new Vector2(0.47f, 0.42f), black, 2, Circle());
                for (int spike = 0; spike < 7; spike++)
                {
                    float x = -0.23f + spike * 0.075f;
                    Transform hairSpike = Piece(poseRoot, "Static hair spike " + spike,
                        new Vector3(x, 0.48f + Mathf.Abs(spike - 3) * -0.018f, 0f),
                        new Vector2(0.075f, 0.25f + (spike % 2) * 0.07f), black, 3, Square());
                    hairSpike.localRotation = Quaternion.Euler(0f, 0f, -38f + spike * 13f);
                }
            }
            else
            {
                Piece(poseRoot, "Ink full back hair", new Vector3(0f, 0.15f, 0f),
                    new Vector2(0.56f, 0.60f), black, 2, Circle());
                Piece(poseRoot, "Ink long left hair", new Vector3(-0.20f, -0.03f, 0f),
                    new Vector2(0.16f, 0.72f), black, 2, Circle());
                Piece(poseRoot, "Ink long right hair", new Vector3(0.20f, -0.03f, 0f),
                    new Vector2(0.16f, 0.72f), black, 2, Circle());
            }
            coat = Piece(poseRoot, "Dark coat", new Vector3(0f, -0.10f, 0f),
                new Vector2(0.43f, 0.50f), cloth, 5, Square());
            Piece(poseRoot, "Accent stripe", new Vector3(0f, -0.07f, 0f),
                new Vector2(0.065f, 0.40f), accent, 6, Square());

            leftArm = JointedLimb(poseRoot, "Left arm", new Vector3(-0.19f, 0.02f, 0f),
                0.15f, 0.14f, 0.075f, cloth, skin, 3, out leftElbow, out leftHand);
            rightArm = JointedLimb(poseRoot, "Right arm", new Vector3(0.19f, 0.02f, 0f),
                0.15f, 0.14f, 0.075f, cloth, skin, 7, out rightElbow, out rightHand);
            leftLeg = JointedLimb(poseRoot, "Left leg", new Vector3(-0.11f, -0.30f, 0f),
                0.13f, 0.14f, 0.09f, cloth, black, 3, out leftKnee, out leftBoot);
            rightLeg = JointedLimb(poseRoot, "Right leg", new Vector3(0.11f, -0.30f, 0f),
                0.13f, 0.14f, 0.09f, cloth, black, 7, out rightKnee, out rightBoot);

            head = Piece(poseRoot, "Head", new Vector3(0f, 0.22f, 0f),
                new Vector2(0.40f, 0.38f), skin, 8, Circle());
            headHome = head.localPosition;

            Vector3 fringePosition = style == 1 ? new Vector3(0.07f, 0.38f, 0f) :
                style == 2 ? new Vector3(-0.10f, 0.32f, 0f) : new Vector3(-0.06f, 0.35f, 0f);
            Vector2 fringeSize = style == 1 ? new Vector2(0.32f, 0.16f) :
                style == 2 ? new Vector2(0.48f, 0.31f) : new Vector2(0.42f, 0.23f);
            fringe = Piece(poseRoot, "Emo fringe", fringePosition, fringeSize, black, 11, Circle());
            fringeBaseAngle = style == 1 ? -12f : style == 2 ? 24f : 18f;
            fringe.localRotation = Quaternion.Euler(0f, 0f, fringeBaseAngle);
            fringeHome = fringe.localPosition;

            leftEye = Piece(poseRoot, "Left eye", new Vector3(-0.08f, 0.22f, 0f),
                new Vector2(0.060f, 0.075f), Color.white, 12, Circle());
            rightEye = Piece(poseRoot, "Right eye", new Vector3(0.08f, 0.22f, 0f),
                new Vector2(0.060f, 0.075f), Color.white, 12, Circle());
            leftPupil = Piece(poseRoot, "Left pupil", new Vector3(-0.08f, 0.215f, 0f),
                new Vector2(0.024f, 0.038f), black, 13, Circle());
            rightPupil = Piece(poseRoot, "Right pupil", new Vector3(0.08f, 0.215f, 0f),
                new Vector2(0.024f, 0.038f), black, 13, Circle());
            Color cheek = new(1f, 0.42f, 0.56f, 0.50f);
            leftCheek = Piece(poseRoot, "Left cheek", new Vector3(-0.125f, 0.155f, 0f),
                new Vector2(0.060f, 0.025f), cheek, 12, Circle());
            rightCheek = Piece(poseRoot, "Right cheek", new Vector3(0.125f, 0.155f, 0f),
                new Vector2(0.060f, 0.025f), cheek, 12, Circle());
            mouth = Piece(poseRoot, "Tiny polite mouth", new Vector3(0f, 0.135f, 0f),
                new Vector2(0.068f, 0.013f), black, 13, Circle());

            leftEyeHome = leftEye.localPosition;
            rightEyeHome = rightEye.localPosition;
            leftPupilHome = leftPupil.localPosition;
            rightPupilHome = rightPupil.localPosition;
            leftCheekHome = leftCheek.localPosition;
            rightCheekHome = rightCheek.localPosition;
            mouthHome = mouth.localPosition;
        }

        public override void SetMotion(float horizontal, float requested, bool isGrounded, bool attention,
            bool crawling, float landing, float vertical, float weight)
        {
            movement = Mathf.Clamp(horizontal, -1f, 1f);
            requestedMovement = Mathf.Clamp(requested, -1f, 1f);
            grounded = isGrounded;
            underAttention = attention;
            landingPose = Mathf.Clamp01(landing);
            verticalVelocity = vertical;
            bodyWeight = Mathf.Max(0.55f, weight);
            crawlBlend = Mathf.MoveTowards(crawlBlend, crawling ? 1f : 0f, Time.deltaTime * 5.5f);
        }

        public override void SetSocial(LittleSocialAction action, Vector2 direction)
        {
            socialAction = action;
            socialDirection = direction;
        }

        private void Update()
        {
            if (head == null) return;
            depthGroup.sortingOrder = 30 + Mathf.RoundToInt(-transform.position.y * 8f);
            float t = Time.time + personalityOffset;
            float walking = Mathf.Abs(movement);
            float idle = 1f - walking;
            float facing = Mathf.Sign(Mathf.Abs(movement) > 0.03f ? movement :
                Mathf.Abs(requestedMovement) > 0.03f ? requestedMovement : 1f);
            displayFacing = Mathf.MoveTowards(displayFacing, facing, Time.deltaTime * 7.5f);
            float turnNarrow = Mathf.Abs(displayFacing);
            float fourPoint = Mathf.Max(crawlBlend, landingPose);
            float weightTempo = Mathf.Lerp(1.15f, 0.72f, Mathf.InverseLerp(0.65f, 1.25f, bodyWeight));
            gaitPhase += walking * Time.deltaTime * 11.5f * weightTempo;
            if (walking < 0.04f)
            {
                float restingStep = Mathf.Round(gaitPhase / Mathf.PI) * Mathf.PI;
                gaitPhase = Mathf.MoveTowards(gaitPhase, restingStep, Time.deltaTime * 5f);
            }
            float gait = Mathf.Sin(gaitPhase);
            float footfall = Mathf.Abs(Mathf.Cos(gaitPhase));

            // The whole paper body folds toward all fours, then slowly unfolds.
            Vector3 weightedStep = new(-facing * gait * 0.022f * walking,
                -footfall * 0.035f * walking * Mathf.Lerp(0.75f, 1.25f,
                    Mathf.InverseLerp(0.65f, 1.25f, bodyWeight)), 0f);
            poseRoot.localPosition = Vector3.Lerp(weightedStep, new Vector3(0f, -0.20f, 0f), fourPoint);
            poseRoot.localRotation = Quaternion.Euler(0f, 0f, -facing * 66f * fourPoint);
            poseRoot.localScale = new Vector3(displayFacing, 1f + (1f - turnNarrow) * 0.12f, 1f);

            float bob = grounded ? Mathf.Abs(gait) * 0.035f * walking : 0.02f;
            float suspiciousLook = Mathf.Sin(t * 0.72f) * 0.025f * idle;
            head.localPosition = headHome + new Vector3(facing * walking * 0.045f + suspiciousLook, bob, 0f);
            head.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Sin(t * 1.8f) * 4f * idle + (underAttention ? Mathf.Sin(t * 13f) * 2f : 0f));
            Vector3 faceDelta = head.localPosition - headHome;
            leftEye.localPosition = leftEyeHome + faceDelta;
            rightEye.localPosition = rightEyeHome + faceDelta;
            leftCheek.localPosition = leftCheekHome + faceDelta;
            rightCheek.localPosition = rightCheekHome + faceDelta;
            mouth.localPosition = mouthHome + faceDelta;
            float pupilLook = facing * Mathf.Min(0.012f, walking * 0.012f);
            leftPupil.localPosition = leftPupilHome + faceDelta + Vector3.right * pupilLook;
            rightPupil.localPosition = rightPupilHome + faceDelta + Vector3.right * pupilLook;
            fringe.localPosition = fringeHome + new Vector3(facing * walking * 0.025f, bob * 0.7f, 0f);
            fringe.localRotation = Quaternion.Euler(0f, 0f, fringeBaseAngle + Mathf.Sin(t * 8f) * 2.2f);

            bool frightenedJump = !grounded && verticalVelocity > 0.35f;
            mouth.localScale = frightenedJump
                ? new Vector3(0.035f, 0.050f, 1f)
                : new Vector3(0.068f, 0.013f, 1f);
            if (frightenedJump)
            {
                leftArm.localRotation = Quaternion.Euler(0f, 0f, 145f);
                rightArm.localRotation = Quaternion.Euler(0f, 0f, -145f);
                leftElbow.localRotation = Quaternion.Euler(0f, 0f, -38f);
                rightElbow.localRotation = Quaternion.Euler(0f, 0f, 38f);
                leftLeg.localRotation = Quaternion.Euler(0f, 0f, 35f);
                rightLeg.localRotation = Quaternion.Euler(0f, 0f, -35f);
                leftKnee.localRotation = Quaternion.Euler(0f, 0f, -58f);
                rightKnee.localRotation = Quaternion.Euler(0f, 0f, 58f);
            }
            else if (fourPoint > 0.05f)
            {
                float crawlCycle = gait * 24f * walking;
                leftArm.localRotation = Quaternion.Euler(0f, 0f, -facing * (66f + crawlCycle));
                rightArm.localRotation = Quaternion.Euler(0f, 0f, -facing * (66f - crawlCycle));
                leftElbow.localRotation = Quaternion.Euler(0f, 0f, facing * (70f + crawlCycle * 0.4f));
                rightElbow.localRotation = Quaternion.Euler(0f, 0f, facing * (70f - crawlCycle * 0.4f));
                leftLeg.localRotation = Quaternion.Euler(0f, 0f, facing * (58f - crawlCycle));
                rightLeg.localRotation = Quaternion.Euler(0f, 0f, facing * (58f + crawlCycle));
                leftKnee.localRotation = Quaternion.Euler(0f, 0f, -facing * 92f);
                rightKnee.localRotation = Quaternion.Euler(0f, 0f, -facing * 92f);
            }
            else
            {
                float stride = gait * 38f * walking;
                float leftLift = Mathf.Max(0f, gait) * walking;
                float rightLift = Mathf.Max(0f, -gait) * walking;
                leftArm.localRotation = Quaternion.Euler(0f, 0f, stride * 0.72f);
                rightArm.localRotation = Quaternion.Euler(0f, 0f, -stride * 0.72f);
                leftElbow.localRotation = Quaternion.Euler(0f, 0f, -facing * (12f + rightLift * 38f));
                rightElbow.localRotation = Quaternion.Euler(0f, 0f, facing * (12f + leftLift * 38f));
                leftLeg.localRotation = Quaternion.Euler(0f, 0f, -stride);
                rightLeg.localRotation = Quaternion.Euler(0f, 0f, stride);
                leftKnee.localRotation = Quaternion.Euler(0f, 0f, facing * leftLift * 62f);
                rightKnee.localRotation = Quaternion.Euler(0f, 0f, -facing * rightLift * 62f);
                leftBoot.localRotation = Quaternion.Euler(0f, 0f, facing * (stride - leftLift * 30f));
                rightBoot.localRotation = Quaternion.Euler(0f, 0f, -facing * (stride - rightLift * 30f));
            }

            // Letting go reverses the pose: they throw their limbs forward to stop falling.
            bool braking = Mathf.Abs(requestedMovement) < 0.03f && Mathf.Abs(movement) > 0.12f;
            if (braking && fourPoint < 0.1f)
            {
                leftArm.localRotation = Quaternion.Euler(0f, 0f, -facing * 52f);
                rightArm.localRotation = Quaternion.Euler(0f, 0f, -facing * 38f);
                coat.localRotation = Quaternion.Euler(0f, 0f, facing * 7f);
            }
            else coat.localRotation = Quaternion.identity;

            float footTap = idle > 0.8f && Mathf.Sin(t * 5.4f) > 0.5f ? 0.035f : 0f;
            leftBoot.localPosition = new Vector3(0f, -0.14f + footTap, 0f);
            rightBoot.localPosition = new Vector3(0f, -0.14f + bob * 0.4f, 0f);

            ApplySocialPose(t, facing, fourPoint);

            float blink = Mathf.Sin(t * 0.91f) > 0.985f ? 0.12f : 1f;
            leftEye.localScale = new Vector3(0.060f, 0.075f * blink, 1f);
            rightEye.localScale = new Vector3(0.060f, 0.075f * blink, 1f);

            float shadowScale = grounded ? Mathf.Lerp(1f, 1.25f, fourPoint) : 0.68f;
            shadow.localScale = new Vector3(0.62f * shadowScale, 0.12f * shadowScale, 1f);
        }

        private void ApplySocialPose(float t, float facing, float fourPoint)
        {
            if (socialAction == LittleSocialAction.None || fourPoint > 0.2f) return;
            float toward = Mathf.Sign(Mathf.Abs(socialDirection.x) > 0.02f ? socialDirection.x : facing);
            float fuss = Mathf.Sin(t * 15f) * 7f;
            if (socialAction == LittleSocialAction.FixHair)
            {
                leftArm.localRotation = Quaternion.Euler(0f, 0f, -toward * (112f + fuss));
                rightArm.localRotation = Quaternion.Euler(0f, 0f, -toward * (78f - fuss));
                leftElbow.localRotation = Quaternion.Euler(0f, 0f, toward * 72f);
                rightElbow.localRotation = Quaternion.Euler(0f, 0f, toward * 68f);
            }
            else if (socialAction == LittleSocialAction.StraightenJacket)
            {
                leftArm.localRotation = Quaternion.Euler(0f, 0f, -toward * (72f + fuss));
                rightArm.localRotation = Quaternion.Euler(0f, 0f, -toward * (62f - fuss));
                leftElbow.localRotation = Quaternion.Euler(0f, 0f, toward * 82f);
                rightElbow.localRotation = Quaternion.Euler(0f, 0f, toward * 78f);
                coat.localRotation = Quaternion.Euler(0f, 0f, -fuss * 0.2f);
            }
            else if (socialAction == LittleSocialAction.TieShoes)
            {
                poseRoot.localRotation = Quaternion.Euler(0f, 0f, -toward * 32f);
                leftArm.localRotation = Quaternion.Euler(0f, 0f, -toward * (35f + fuss));
                rightArm.localRotation = Quaternion.Euler(0f, 0f, -toward * (48f - fuss));
                leftElbow.localRotation = Quaternion.Euler(0f, 0f, toward * 92f);
                rightElbow.localRotation = Quaternion.Euler(0f, 0f, toward * 92f);
            }
            else if (socialAction == LittleSocialAction.HelpUp)
            {
                leftArm.localRotation = Quaternion.Euler(0f, 0f, -toward * 82f);
                rightArm.localRotation = Quaternion.Euler(0f, 0f, -toward * 96f);
                leftElbow.localRotation = Quaternion.Euler(0f, 0f, toward * 64f);
                rightElbow.localRotation = Quaternion.Euler(0f, 0f, toward * 64f);
            }
            else if (socialAction == LittleSocialAction.BeingGroomed)
            {
                head.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 9f) * 7f);
                coat.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 12f) * 3f);
                leftBoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 13f) * 8f);
            }
        }

        private Transform JointedLimb(Transform parent, string label, Vector3 anchor,
            float upperLength, float lowerLength, float width, Color segmentColor, Color endColor,
            int order, out Transform lowerPivot, out Transform end)
        {
            Transform pivot = new GameObject(label + " pivot").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = anchor;
            Piece(pivot, label + " upper", new Vector3(0f, -upperLength * 0.5f, 0f),
                new Vector2(width, upperLength), segmentColor, order, Square());
            Piece(pivot, label + " joint", new Vector3(0f, -upperLength, 0f),
                new Vector2(width * 1.12f, width * 1.12f), segmentColor, order + 1, Circle());
            lowerPivot = new GameObject(label + " lower pivot").transform;
            lowerPivot.SetParent(pivot, false);
            lowerPivot.localPosition = new Vector3(0f, -upperLength, 0f);
            Piece(lowerPivot, label + " lower", new Vector3(0f, -lowerLength * 0.5f, 0f),
                new Vector2(width * 0.88f, lowerLength), segmentColor, order, Square());
            end = Piece(lowerPivot, label + " end", new Vector3(0f, -lowerLength, 0f),
                new Vector2(width * 1.65f, width * 0.82f), endColor, order + 2, Circle());
            return pivot;
        }

        private Transform Piece(Transform parent, string label, Vector3 position, Vector2 scale,
            Color color, int order, Sprite sprite)
        {
            GameObject part = new(label);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return part.transform;
        }

        private static Sprite Square()
        {
            if (squareSprite != null) return squareSprite;
            Texture2D texture = new(1, 1);
            texture.name = "Generated paper square";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return squareSprite;
        }

        private static Sprite Circle()
        {
            if (circleSprite != null) return circleSprite;
            const int size = 32;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            texture.name = "Generated paper circle";
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance + 0.8f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return circleSprite;
        }
    }
}
