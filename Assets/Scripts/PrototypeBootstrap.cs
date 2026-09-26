using System.Collections.Generic;
using UnityEngine;

namespace TheLittles
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private static Sprite squareSprite;
        private static Sprite circleSprite;

        private void Awake() => BuildWorld();

        private void BuildWorld()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.transform.position = new Vector3(-23f, 1.55f, -10f);
            camera.backgroundColor = new Color(0.48f, 0.61f, 0.67f);
            Ensure3DLighting();

            BuildNeighborhood();
            MakeVisual("Skewed far sidewalk", new Vector2(3f, -1.45f), new Vector2(63f, 1.2f),
                new Color(0.52f, 0.48f, 0.46f), -18, -2.4f);
            MakeVisual("Angled road", new Vector2(3f, -2.22f), new Vector2(63f, 1.25f),
                new Color(0.18f, 0.20f, 0.23f), -16, -1.4f);
            for (int i = 0; i < 24; i++)
                MakeVisual("Perspective road dash", new Vector2(-27f + i * 2.7f, -2.18f + (i % 4) * 0.012f),
                    new Vector2(1.0f, 0.055f), new Color(0.92f, 0.82f, 0.39f), -15, -2f);
            MakeSolid("Walkable curb", new Vector2(3f, -2.72f), new Vector2(64f, 0.62f),
                new Color(0.22f, 0.21f, 0.25f), 0);
            MakeSolid("Left scene wall", new Vector2(-28.15f, 0.8f), new Vector2(0.35f, 8f),
                new Color(0.1f, 0.1f, 0.12f, 0.08f), -30);
            MakeSolid("Right scene wall", new Vector2(34.15f, 0.8f), new Vector2(0.35f, 8f),
                new Color(0.1f, 0.1f, 0.12f, 0.08f), -30);

            BuildBusStop();

            LittleTeam team = new GameObject("Three Small Children").AddComponent<LittleTeam>();
            List<LittleMotor2D> children = new();
            Color[] accents =
            {
                new(0.29f, 0.9f, 0.82f), new(1f, 0.31f, 0.57f), new(0.96f, 0.82f, 0.29f)
            };
            string[] names = { "Moth", "Static", "Ink" };
            for (int i = 0; i < 3; i++)
                children.Add(MakeLittle(names[i], new Vector2(-23.8f - i * 0.92f, -2.02f), accents[i], i));
            team.Configure(children);

            MakeFriendlyKid("Helpful kid A", new Vector2(-16f, -1.70f),
                new Color(0.42f, 0.66f, 0.86f), "You are SO cute!", 0.45f);
            MakeFriendlyKid("Loud lawn mower", new Vector2(-9f, -1.70f),
                new Color(0.86f, 0.48f, 0.64f), "BRRRR! Sorry, tiny people!", -0.48f);
            MakeFriendlyKid("Helpful kid B", new Vector2(-2f, -1.68f),
                new Color(0.58f, 0.76f, 0.48f), "Let me hold you!", 0.18f);
            MakeFriendlyKid("Noisy leaf blower", new Vector2(6f, -1.70f),
                new Color(0.72f, 0.52f, 0.82f), "WHOOOOOSH!", -0.36f);
            MakeFriendlyKid("Helpful kid C", new Vector2(11f, -1.69f),
                new Color(0.82f, 0.66f, 0.40f), "Do you need help, tiny?", 0.55f);

            BuildBush(new Vector2(-12.5f, -1.72f), 0.65f);
            BuildBush(new Vector2(3.3f, -1.72f), -0.60f);
            BuildBush(new Vector2(14.0f, -1.72f), 0.42f);
            BuildDog(new Vector2(17.5f, -1.88f), -0.20f);
            BuildBus(team);
            camera.gameObject.AddComponent<GroupCameraFollow>().Configure(team, -23f, 27.2f);

            PrototypeHUD hud = new GameObject("Bus Stop HUD").AddComponent<PrototypeHUD>();
            hud.Configure(team);
        }

        private static void Ensure3DLighting()
        {
            Light[] existingLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            if (existingLights.Length == 0)
            {
                GameObject lightObject = new("Soft morning light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.92f, 0.82f);
                light.intensity = 1.15f;
                light.shadows = LightShadows.Soft;
                lightObject.transform.rotation = Quaternion.Euler(34f, -32f, 0f);
            }

            RenderSettings.ambientLight = new Color(0.42f, 0.48f, 0.58f);
        }

        private static void BuildNeighborhood()
        {
            GameObject loop = new("Seven-section old-cartoon neighborhood");
            for (int repeat = 0; repeat < 7; repeat++)
            {
                float baseX = -25.5f + repeat * 8.9f;
                MakeHouse(loop.transform, baseX, new Color(0.92f, 0.72f, 0.62f));
                MakeHouse(loop.transform, baseX + 4.2f, new Color(0.62f, 0.80f, 0.88f));
                Transform lawnPerson = MakePart(loop.transform, "Person watering lawn", new Vector2(baseX + 2.5f, -0.52f),
                    new Vector2(0.24f, 0.75f), new Color(0.35f, 0.25f, 0.30f), -25, false);
                MakePart(lawnPerson, "Watering arm", new Vector2(0.18f, 0.08f), new Vector2(0.38f, 0.07f),
                    new Color(0.82f, 0.70f, 0.62f), -24, false).localRotation = Quaternion.Euler(0f, 0f, -18f);
            }
        }

        private static void MakeHouse(Transform parent, float x, Color colour)
        {
            Transform face = MakePart(parent, "House face", new Vector2(x, 0.65f), new Vector2(3.0f, 2.35f), colour, -29, false);
            MakePart(face, "Roof", new Vector2(0f, 0.68f), new Vector2(1.12f, 0.34f),
                new Color(0.25f, 0.18f, 0.22f), -28, false).localRotation = Quaternion.Euler(0f, 0f, 45f);
            MakePart(face, "Window", new Vector2(-0.25f, 0.03f), new Vector2(0.19f, 0.28f),
                new Color(0.88f, 0.88f, 0.55f), -27, false);
            MakePart(face, "Door", new Vector2(0.28f, -0.25f), new Vector2(0.20f, 0.42f),
                new Color(0.22f, 0.18f, 0.24f), -27, false);
        }

        private static void BuildBusStop()
        {
            MakeVisual("Bus stop pole", new Vector2(25.4f, -1.0f), new Vector2(0.10f, 2.7f),
                new Color(0.16f, 0.17f, 0.20f), -4);
            MakeVisual("BUS STOP sign", new Vector2(25.4f, 0.28f), new Vector2(0.72f, 0.42f),
                new Color(0.95f, 0.82f, 0.30f), -3, -4f);
            MakeLabel("BUS STOP", new Vector2(25.4f, 0.28f), 33, Color.black, -2);
            MakeVisual("Fence", new Vector2(17.6f, -0.72f), new Vector2(3.4f, 0.10f),
                new Color(0.38f, 0.24f, 0.18f), -11, 3f);
            for (int i = 0; i < 6; i++)
                MakeVisual("Fence post", new Vector2(16.3f + i * 0.58f, -0.95f), new Vector2(0.08f, 1.15f),
                    new Color(0.38f, 0.24f, 0.18f), -10, 3f);
        }

        private static void MakeFriendlyKid(string label, Vector2 position, Color shirt, string line, float depth)
        {
            GameObject root = new(label);
            root.layer = 2;
            root.transform.position = position;
            float perspectiveScale = Mathf.Lerp(1.12f, 0.84f, (depth + 1f) * 0.5f);
            root.transform.localScale = Vector3.one * perspectiveScale;
            MakePart(root.transform, "Long legs", new Vector2(0f, -0.12f), new Vector2(0.32f, 1.30f),
                new Color(0.18f, 0.20f, 0.28f), 6, false);
            MakePart(root.transform, "Shirt", new Vector2(0f, 0.56f), new Vector2(0.72f, 0.88f), shirt, 7, true);
            MakePart(root.transform, "Big friendly head", new Vector2(0f, 1.25f), new Vector2(0.62f, 0.62f),
                new Color(0.84f, 0.70f, 0.62f), 8, true);
            MakePart(root.transform, "Reaching arm", new Vector2(-0.43f, 0.55f), new Vector2(0.55f, 0.12f),
                new Color(0.84f, 0.70f, 0.62f), 8, true).localRotation = Quaternion.Euler(0f, 0f, -18f);
            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.45f, 2.6f);
            TextMesh bubble = MakeLabel("", position + new Vector2(0f, 1.9f), 27, Color.white, 20);
            root.AddComponent<FriendlyKidObstacle>().Configure(line, bubble, depth);
        }

        private static void BuildDog(Vector2 position, float depth)
        {
            GameObject dog = new("Dog boss - dangerously friendly");
            dog.layer = 2;
            dog.transform.position = position;
            MakePart(dog.transform, "Dog body", new Vector2(0f, 0.14f), new Vector2(1.55f, 0.72f),
                new Color(0.52f, 0.31f, 0.20f), 8, true);
            MakePart(dog.transform, "Dog head", new Vector2(-0.72f, 0.52f), new Vector2(0.78f, 0.82f),
                new Color(0.62f, 0.39f, 0.24f), 9, true);
            MakePart(dog.transform, "Wagging tail", new Vector2(0.87f, 0.43f), new Vector2(0.72f, 0.13f),
                new Color(0.52f, 0.31f, 0.20f), 8, true).localRotation = Quaternion.Euler(0f, 0f, 35f);
            MakePart(dog.transform, "Tongue", new Vector2(-1.05f, 0.30f), new Vector2(0.22f, 0.35f),
                new Color(1f, 0.35f, 0.51f), 11, true);
            BoxCollider2D trigger = dog.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.8f, 1.8f);
            TextMesh bark = MakeLabel("", position + new Vector2(0f, 1.45f), 31, Color.white, 20);
            dog.AddComponent<PlayfulDogBoss>().Configure(bark, depth);
        }

        private static void BuildBush(Vector2 position, float depth)
        {
            GameObject bush = new("Suspicious rattling hedge");
            bush.layer = 2;
            bush.transform.position = position + Vector2.up * depth * 0.42f;
            float scale = Mathf.Lerp(1.10f, 0.82f, (depth + 1f) * 0.5f);
            bush.transform.localScale = Vector3.one * scale;
            MakePart(bush.transform, "Dark hedge", Vector2.zero, new Vector2(1.65f, 0.72f),
                new Color(0.10f, 0.24f, 0.17f), 4, true);
            MakePart(bush.transform, "Hedge eyes maybe", new Vector2(0.22f, 0.10f), new Vector2(0.08f, 0.035f),
                new Color(0.82f, 0.86f, 0.45f), 5, true);
            BoxCollider2D trigger = bush.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.2f, 1.5f);
            bush.AddComponent<RattlingBush>().Configure(depth);
        }

        private static void BuildBus(LittleTeam team)
        {
            GameObject bus = new("Angled short school bus");
            Vector3 parkedPosition = new(29.55f, -0.90f, 0f);
            bus.transform.position = parkedPosition;
            bus.transform.rotation = Quaternion.Euler(0f, 0f, 2.6f);
            MakePart(bus.transform, "Bus body", Vector2.zero, new Vector2(5.1f, 3.0f),
                new Color(0.95f, 0.72f, 0.12f), 3, false);
            MakePart(bus.transform, "Bus dark lower edge", new Vector2(0f, -1.05f), new Vector2(5.0f, 0.55f),
                new Color(0.17f, 0.16f, 0.19f), 4, false);
            for (int i = 0; i < 4; i++)
            {
                Transform window = MakePart(bus.transform, "Bus window", new Vector2(-1.4f + i * 0.83f, 0.55f),
                    new Vector2(0.63f, 0.74f), new Color(0.25f, 0.42f, 0.52f), 5, false);
                MakePart(window, "Kid leaning from window", new Vector2(0f, -0.12f), new Vector2(0.26f, 0.40f),
                    new Color(0.83f, 0.65f, 0.58f), 6, true);
            }
            MakePart(bus.transform, "Front wheel", new Vector2(1.65f, -1.30f), new Vector2(0.82f, 0.82f),
                new Color(0.07f, 0.07f, 0.09f), 5, true);
            MakePart(bus.transform, "Back wheel", new Vector2(-1.65f, -1.30f), new Vector2(0.82f, 0.82f),
                new Color(0.07f, 0.07f, 0.09f), 5, true);
            MakePart(bus.transform, "Open bus door", new Vector2(-2.15f, -0.22f), new Vector2(0.58f, 1.78f),
                new Color(0.08f, 0.10f, 0.13f), 7, false);
            MakePart(bus.transform, "High push bar", new Vector2(-2.28f, 0.08f), new Vector2(0.68f, 0.10f),
                new Color(0.82f, 0.84f, 0.86f), 8, false);

            GameObject goal = new("Bus door stacking zone");
            goal.transform.position = new Vector2(27.20f, -0.52f);
            goal.layer = 2;
            BoxCollider2D trigger = goal.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.45f, 4.2f);
            TextMesh status = MakeLabel("ALL THREE REACH THE DOOR", new Vector2(27.35f, 1.40f), 27, Color.white, 20);
            goal.AddComponent<BusDoorGoal>().Configure(team, status);
            TextMesh driver = MakeLabel("", new Vector2(29.0f, 2.0f), 26, new Color(1f, 0.86f, 0.32f), 21);
            bus.AddComponent<BusArrival>().Configure(team, goal, parkedPosition, driver);
        }

        private static LittleMotor2D MakeLittle(string label, Vector2 position, Color accent, int style)
        {
            GameObject root = new(label);
            root.transform.position = position;
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.45f, 0.82f);
            root.AddComponent<Willpower>();
            LittleVoice voice = root.AddComponent<LittleVoice>();
            voice.Configure(label, style);
            LittleVisualFactory.Create(root.transform, label, accent, style);
            LittleMotor2D motor = root.AddComponent<LittleMotor2D>();
            motor.Configure(0.58f + style * 0.09f, 11.7f + style * 9.3f, 0.72f + style * 0.22f, style);
            return motor;
        }

        private static GameObject MakeSolid(string label, Vector2 position, Vector2 size, Color colour, int order)
        {
            GameObject item = MakeVisual(label, position, size, colour, order);
            item.AddComponent<BoxCollider2D>();
            return item;
        }

        private static GameObject MakeVisual(string label, Vector2 position, Vector2 size, Color colour,
            int order, float angle = 0f)
        {
            GameObject item = new(label);
            item.transform.position = position;
            item.transform.localScale = size;
            item.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = SquareSprite();
            renderer.color = colour;
            renderer.sortingOrder = order;
            return item;
        }

        private static Transform MakePart(Transform parent, string label, Vector2 position, Vector2 size,
            Color colour, int order, bool circle)
        {
            GameObject item = new(label);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = size;
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = circle ? CircleSprite() : SquareSprite();
            renderer.color = colour;
            renderer.sortingOrder = order;
            return item.transform;
        }

        private static TextMesh MakeLabel(string value, Vector2 position, int fontSize, Color colour, int order)
        {
            GameObject label = new("World caption");
            label.transform.position = new Vector3(position.x, position.y, -0.5f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = fontSize;
            text.characterSize = 0.055f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = colour;
            text.GetComponent<MeshRenderer>().sortingOrder = order;
            return text;
        }

        private static Sprite SquareSprite()
        {
            if (squareSprite != null) return squareSprite;
            Texture2D texture = new(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return squareSprite;
        }

        private static Sprite CircleSprite()
        {
            if (circleSprite != null) return circleSprite;
            const int size = 32;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float alpha = Mathf.Clamp01(size * 0.48f - Vector2.Distance(new Vector2(x, y), center) + 0.8f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return circleSprite;
        }
    }
}
