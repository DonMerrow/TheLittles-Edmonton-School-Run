using UnityEngine;

namespace TheLittles
{
    /// <summary>
    /// Keeps gameplay independent from the artwork used to display a Little.
    /// The original procedural paper body and imported humanoid prefabs both
    /// receive the same movement and social-pose information through this API.
    /// </summary>
    public abstract class LittleVisualController : MonoBehaviour
    {
        public abstract void SetMotion(float horizontal, float requested, bool isGrounded,
            bool attention, bool crawling, float landing, float vertical, float weight);

        public abstract void SetSocial(LittleSocialAction action, Vector2 direction);
    }
}
