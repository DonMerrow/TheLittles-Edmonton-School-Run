using System.Collections.Generic;
using UnityEngine;

/// <summary>Adds a readable low-poly cab, wheels and working lights to prototype plows.</summary>
[DisallowMultipleComponent]
public sealed class RiverValleyPlowPolish : MonoBehaviour
{
    private readonly List<Transform> wheels = new();
    private Vector3 lastPosition;
    private Light beacon;

    private void Start()
    {
        lastPosition = transform.position;
        if (transform.Find("Finished plow details") != null) return;

        Material body = MaterialFor(name.ToLowerInvariant().Contains("valley")
            ? new Color(0.73f, 0.08f, 0.06f) : new Color(0.96f, 0.40f, 0.035f));
        Material dark = MaterialFor(new Color(0.035f, 0.055f, 0.075f));
        Material glass = MaterialFor(new Color(0.25f, 0.67f, 0.82f));
        Material lightMaterial = MaterialFor(new Color(1f, 0.82f, 0.18f));

        GameObject details = new("Finished plow details");
        details.transform.SetParent(transform, false);
        AddPart(PrimitiveType.Cube, "Truck chassis", details.transform,
            new Vector3(0f, 0.20f, 0.15f), new Vector3(2.15f, 0.42f, 3.45f), dark);
        AddPart(PrimitiveType.Cube, "Driver cab", details.transform,
            new Vector3(0f, 1.15f, -0.45f), new Vector3(1.82f, 1.55f, 1.55f), body);
        AddPart(PrimitiveType.Cube, "Salt box", details.transform,
            new Vector3(0f, 1.04f, 1.10f), new Vector3(2.05f, 1.35f, 1.65f), body);
        AddPart(PrimitiveType.Cube, "Wide windshield", details.transform,
            new Vector3(0f, 1.38f, -1.245f), new Vector3(1.48f, 0.68f, 0.045f), glass);
        AddPart(PrimitiveType.Cube, "Left headlamp", details.transform,
            new Vector3(-0.69f, 0.70f, -1.32f), new Vector3(0.25f, 0.22f, 0.09f), lightMaterial);
        AddPart(PrimitiveType.Cube, "Right headlamp", details.transform,
            new Vector3(0.69f, 0.70f, -1.32f), new Vector3(0.25f, 0.22f, 0.09f), lightMaterial);

        for (int side = -1; side <= 1; side += 2)
        for (int axle = -1; axle <= 1; axle += 2)
        {
            GameObject wheel = AddPart(PrimitiveType.Cylinder, "Turning winter tire", details.transform,
                new Vector3(side * 1.08f, 0.05f, axle * 1.05f), new Vector3(0.48f, 0.18f, 0.48f), dark);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheels.Add(wheel.transform);
        }

        GameObject beaconObject = AddPart(PrimitiveType.Sphere, "Amber rotating beacon", details.transform,
            new Vector3(0f, 2.03f, -0.42f), Vector3.one * 0.18f, lightMaterial);
        beacon = beaconObject.AddComponent<Light>();
        beacon.type = LightType.Point;
        beacon.color = new Color(1f, 0.52f, 0.06f);
        beacon.range = 6f;
        beacon.intensity = 2.2f;
    }

    private void Update()
    {
        float travelled = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
        float turn = travelled * 210f;
        foreach (Transform wheel in wheels)
            if (wheel != null) wheel.Rotate(Vector3.up, turn, Space.Self);
        if (beacon != null)
            beacon.intensity = 1.1f + (Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f) * 2.3f;
    }

    private static GameObject AddPart(PrimitiveType type, string partName, Transform parent,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        return part;
    }

    private static Material MaterialFor(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.hideFlags = HideFlags.DontSave;
        return material;
    }
}
