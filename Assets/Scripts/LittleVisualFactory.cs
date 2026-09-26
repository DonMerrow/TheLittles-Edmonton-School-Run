using UnityEngine;

namespace TheLittles
{
    public static class LittleVisualFactory
    {
        private const string ResourceFolder = "Littles/";

        public static LittleVisualController Create(Transform parent, string littleName,
            Color accent, int style)
        {
            GameObject prefab = Resources.Load<GameObject>(ResourceFolder + littleName);
            if (prefab != null)
            {
                GameObject wrapper = new("Stylized 3D Body");
                wrapper.transform.SetParent(parent, false);
                GameObject instance = Object.Instantiate(prefab, wrapper.transform, false);
                instance.name = littleName + " Model";
                HumanoidLittleVisual humanoid = wrapper.AddComponent<HumanoidLittleVisual>();
                humanoid.Configure(instance);
                return humanoid;
            }

            GameObject paper = new("Procedural Body - replace with Resources/Littles/" + littleName);
            paper.transform.SetParent(parent, false);
            ProceduralLittleVisual procedural = paper.AddComponent<ProceduralLittleVisual>();
            procedural.Configure(accent, style);
            return procedural;
        }
    }
}
