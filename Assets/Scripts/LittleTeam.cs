using System.Collections.Generic;
using UnityEngine;

namespace TheLittles
{
    public sealed class LittleTeam : MonoBehaviour
    {
        [SerializeField] private List<LittleMotor2D> members = new();
        private int activeIndex;
        private float touchMove;
        private bool touchJump;
        private bool touchSwitch;
        private bool touchCrawl;
        private float lastGroupMoveAt;
        private float nextSocialAt;
        private int socialTurn;
        private float touchDepth;
        private float nextGuidanceAt;

        public IReadOnlyList<LittleMotor2D> Members => members;
        public LittleMotor2D Active => members.Count == 0 ? null : members[activeIndex];
        public bool HasWon { get; private set; }
        public bool HasLost { get; private set; }
        public int AvailableCount => members.FindAll(m => !m.Willpower.IsInClass && !m.IsHeldAtSwitch).Count;

        public void Configure(List<LittleMotor2D> littles)
        {
            members = littles;
            foreach (LittleMotor2D little in members)
                little.Willpower.SentToClass += OnSentToClass;
            SelectFirstAvailable();
            nextSocialAt = Time.time + 3.5f;
        }

        private void Update()
        {
            if (HasWon || HasLost || Active == null) return;

            float keyboard = Input.GetAxisRaw("Horizontal");
            float movement = Mathf.Abs(touchMove) > 0.01f ? touchMove : keyboard;
            float keyboardDepth = Input.GetAxisRaw("Vertical");
            float depth = Mathf.Abs(touchDepth) > 0.01f ? touchDepth : keyboardDepth;
            Active.SetMove(movement);
            Active.SetDepth(depth);
            if (Mathf.Abs(movement) > 0.05f) lastGroupMoveAt = Time.time;
            bool crawling = Input.GetKey(KeyCode.C) || touchCrawl;
            Active.SetCrawl(crawling);

            if (Input.GetKeyDown(KeyCode.Space) || touchJump)
                Active.QueueJump();

            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.E) ||
                Input.GetKeyDown(KeyCode.Q) || touchSwitch)
                SelectNextAvailable();

            touchJump = false;
            touchSwitch = false;

            UpdateLittleChain(movement);
            UpdateHelpAndGrooming();
            UpdateRescueAndGuidance();
        }

        private void UpdateLittleChain(float movement)
        {
            if (Active == null) return;
            float direction = Mathf.Abs(movement) > 0.05f ? Mathf.Sign(movement) :
                (Active.GetComponent<Rigidbody2D>().linearVelocity.x < -0.08f ? -1f : 1f);
            LittleMotor2D predecessor = Active;
            bool rushing = Active.IsUnderAttention;
            for (int offset = 1; offset < members.Count; offset++)
            {
                int index = (activeIndex + offset) % members.Count;
                LittleMotor2D follower = members[index];
                if (follower.Willpower.IsInClass || follower.IsHeldAtSwitch) continue;
                Vector2 target = predecessor.Position - Vector2.right * direction * 0.92f;
                follower.Follow(target, direction, rushing, predecessor.DepthPosition);
                if (rushing && Vector2.Distance(follower.Position, predecessor.Position) < 0.82f)
                    predecessor.GetComponent<Rigidbody2D>().AddForce(Vector2.right * direction * 2.2f, ForceMode2D.Force);
                predecessor = follower;
            }
        }

        private void UpdateHelpAndGrooming()
        {
            foreach (LittleMotor2D fallen in members)
            {
                if (!fallen.IsStumbled || fallen.Willpower.IsInClass) continue;
                foreach (LittleMotor2D helper in members)
                {
                    if (helper == fallen || helper.IsStumbled || helper.Willpower.IsInClass) continue;
                    if (Vector2.Distance(helper.Position, fallen.Position) > 0.92f) continue;
                    helper.BeginSocial(LittleSocialAction.HelpUp, fallen.Position, 0.65f);
                    fallen.BeginSocial(LittleSocialAction.BeingGroomed, helper.Position, 0.65f);
                    fallen.HelpUp();
                    break;
                }
            }

            if (Time.time < nextSocialAt || Time.time - lastGroupMoveAt < 2.2f) return;
            List<LittleMotor2D> available = members.FindAll(l => !l.Willpower.IsInClass && !l.IsHeldAtSwitch && !l.IsStumbled);
            if (available.Count < 2) return;
            LittleMotor2D actor = available[socialTurn % available.Count];
            LittleMotor2D target = available[(socialTurn + 1) % available.Count];
            if (Vector2.Distance(actor.Position, target.Position) > 1.15f)
            {
                nextSocialAt = Time.time + 1.2f;
                return;
            }
            LittleSocialAction action = actor.StyleIndex == 2 ? LittleSocialAction.FixHair :
                actor.StyleIndex == 1 ? LittleSocialAction.TieShoes : LittleSocialAction.StraightenJacket;
            actor.BeginSocial(action, target.Position, 1.45f);
            target.BeginSocial(LittleSocialAction.BeingGroomed, actor.Position, 1.45f);
            actor.Willpower.Restore(1.2f);
            target.Willpower.Restore(2.0f);
            socialTurn++;
            nextSocialAt = Time.time + 5.2f;
        }

        public void SetTouchMove(float value) => touchMove = value;
        public void SetTouchDepth(float value) => touchDepth = value;
        public void TouchJump() => touchJump = true;
        public void TouchSwitch() => touchSwitch = true;
        public void SetTouchCrawl(bool value) => touchCrawl = value;

        public void SetDoorStack(bool value, Vector2 basePosition)
        {
            int height = 0;
            foreach (LittleMotor2D member in members)
            {
                if (member.Willpower.IsInClass || member.IsHeldAtSwitch) continue;
                member.SetDoorStack(value, basePosition + Vector2.up * height * 0.58f);
                height++;
            }
        }

        private void UpdateRescueAndGuidance()
        {
            foreach (LittleMotor2D member in members)
            {
                if (!member.Willpower.IsInClass) continue;
                foreach (LittleMotor2D helper in members)
                {
                    if (helper == member || helper.Willpower.IsInClass) continue;
                    float flatDistance = Vector2.Distance(helper.Position, member.Position);
                    float depthDistance = Mathf.Abs(helper.DepthPosition - member.DepthPosition);
                    if (flatDistance > 1.25f || depthDistance > 0.45f) continue;
                    member.Willpower.Restore(7f * Time.deltaTime);
                    helper.Willpower.Restore(1.2f * Time.deltaTime);
                    if (Time.time >= nextGuidanceAt)
                    {
                        helper.Voice.Say(LittleLineKind.Rescue);
                        nextGuidanceAt = Time.time + 2.6f;
                    }
                }
            }

            if (Time.time < nextGuidanceAt || Active == null) return;
            foreach (LittleMotor2D member in members)
            {
                if (member == Active || member.Willpower.IsInClass) continue;
                if (Mathf.Abs(member.Position.x - Active.Position.x) > 2.5f ||
                    Mathf.Abs(member.DepthPosition - Active.DepthPosition) > 0.85f)
                {
                    member.Voice.Say(LittleLineKind.LeftBehind);
                    nextGuidanceAt = Time.time + 3.0f;
                    return;
                }
            }

            if (Time.time - lastGroupMoveAt > 4f && Random.value < 0.012f)
            {
                members[Random.Range(0, members.Count)].Voice.Say(LittleLineKind.Question);
                nextGuidanceAt = Time.time + 4.2f;
            }
        }

        public void Win()
        {
            HasWon = true;
            if (Active != null) Active.SetMove(0f);
        }

        private void OnSentToClass(Willpower ignored)
        {
            bool anyoneAvailable = false;
            foreach (LittleMotor2D member in members)
                anyoneAvailable |= !member.Willpower.IsInClass && !member.IsHeldAtSwitch;

            if (!anyoneAvailable)
            {
                HasLost = true;
                return;
            }
            SelectNextAvailable();
        }

        private void SelectFirstAvailable()
        {
            activeIndex = -1;
            SelectNextAvailable();
        }

        private void SelectNextAvailable()
        {
            if (members.Count == 0) return;
            if (activeIndex >= 0 && activeIndex < members.Count)
                members[activeIndex].SetActiveLittle(false);

            foreach (LittleMotor2D member in members) member.StopFollowing();

            for (int offset = 1; offset <= members.Count; offset++)
            {
                int candidate = (activeIndex + offset + members.Count) % members.Count;
                LittleMotor2D little = members[candidate];
                if (!little.Willpower.IsInClass && !little.IsHeldAtSwitch)
                {
                    activeIndex = candidate;
                    little.SetActiveLittle(true);
                    return;
                }
            }
        }
    }
}
