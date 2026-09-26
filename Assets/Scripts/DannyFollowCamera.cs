using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DannyFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float positionSmooth = 8f;
    [SerializeField] private float verticalTargetSmooth = 4f;
    [SerializeField] private float lookHeight = 0.72f;
    [SerializeField] private float distance = 2.65f;
    [SerializeField] private float yaw;
    [SerializeField] private float pitch = 10f;
    [SerializeField] private float orbitSensitivity = 0.12f;
    [SerializeField] private float zoomSensitivity = 0.004f;
    [SerializeField] private float minimumPitch = -8f;
    [SerializeField] private float maximumPitch = 48f;
    [SerializeField] private float collisionRadius = 0.14f;
    private Vector3 smoothedLookPoint;
    private bool lookPointInitialized;
    private float playerDistance;
    private float framedDistance;
    private bool firstPerson;
    private Renderer[] targetRenderers;
    private Vector3 cinematicFocus;
    private Vector3 cinematicOffset=new(3.4f,2f,-3.8f);
    private float cinematicUntil;
    private bool cinematicActive;
    private float previousPinchDistance;

    public bool IsFirstPerson => firstPerson;

    public void Configure(Transform followTarget)
    {
        target = followTarget;
        if(target!=null)yaw=target.eulerAngles.y;
        lookPointInitialized=false;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (targetRenderers == null) targetRenderers = target.GetComponentsInChildren<Renderer>(true);
        if (playerDistance <= 0f)
        {
            playerDistance = distance;
            framedDistance = distance;
        }
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (mouse != null)
        {
            // Click once inside the game to capture the pointer.  From then
            // on, normal mouse movement aims the camera; no look key needs to
            // be held. Escape releases the pointer in the Editor.
            if ((mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame) &&
                Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * orbitSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * orbitSensitivity, minimumPitch, maximumPitch);
            }
            SetZoom(playerDistance - mouse.scroll.ReadValue().y * zoomSensitivity);
        }
        Touchscreen touchscreen=Touchscreen.current;
        if(RiverValleyMobileControls.IsAvailable)
        {
            Vector2 touchLook=RiverValleyMobileControls.LookDelta;
            yaw+=touchLook.x*orbitSensitivity;
            pitch=Mathf.Clamp(pitch-touchLook.y*orbitSensitivity,minimumPitch,maximumPitch);
            if(RiverValleyMobileControls.CameraPressed)TogglePerspective();
        }
        if(touchscreen!=null&&touchscreen.touches.Count>=2&&
            touchscreen.touches[0].press.isPressed&&touchscreen.touches[1].press.isPressed)
        {
            float pinchDistance=Vector2.Distance(touchscreen.touches[0].position.ReadValue(),
                touchscreen.touches[1].position.ReadValue());
            if(previousPinchDistance>0f)
                SetZoom(playerDistance-(pinchDistance-previousPinchDistance)*0.008f);
            previousPinchDistance=pinchDistance;
        }
        else previousPinchDistance=0f;
        if (keyboard != null)
        {
            if (keyboard.rKey.wasPressedThisFrame) ResetThirdPerson(true);
            if (keyboard.cKey.wasPressedThisFrame) TogglePerspective();
        }
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 look = gamepad.rightStick.ReadValue();
            yaw += look.x * 95f * Time.deltaTime;
            pitch = Mathf.Clamp(pitch - look.y * 75f * Time.deltaTime, minimumPitch, maximumPitch);
            SetZoom(playerDistance-(gamepad.rightTrigger.ReadValue()-gamepad.leftTrigger.ReadValue())*
                2.2f*Time.deltaTime);
            if (gamepad.rightStickButton.wasPressedThisFrame) TogglePerspective();
        }
        yaw = Mathf.Repeat(yaw + 180f, 360f) - 180f;

        if(Time.time<cinematicUntil)
        {
            Vector3 targetAnchor=target.position+Vector3.up*lookHeight;
            // A scene may finish by returning Danny to the route while an old
            // cinematic focus is still counting down. Never leave the player
            // behind in that case.
            if(Vector3.Distance(targetAnchor,cinematicFocus)>7.5f)
            {
                cinematicActive=false;
                cinematicUntil=0f;
                ResetThirdPerson(true);
            }
            else
            {
            cinematicActive=true;
            Vector3 cinematicPosition=cinematicFocus+cinematicOffset;
            transform.position=Vector3.Lerp(transform.position,cinematicPosition,
                1f-Mathf.Exp(-5.5f*Time.deltaTime));
            transform.rotation=Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(cinematicFocus-transform.position,Vector3.up),
                1f-Mathf.Exp(-6.5f*Time.deltaTime));
            return;
            }
        }
        if(cinematicActive)
        {
            cinematicActive=false;
            ResetThirdPerson(true);
        }

        if (firstPerson)
        {
            Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 eyePosition = target.position + Vector3.up * 1.42f + viewRotation * Vector3.forward * 0.10f;
            transform.position = Vector3.Lerp(transform.position, eyePosition,
                1f - Mathf.Exp(-13f * Time.deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, viewRotation,
                1f - Mathf.Exp(-13f * Time.deltaTime));
            return;
        }

        // Danny remains the camera anchor regardless of group size. Pulling
        // the camera toward dozens of children made the player tiny and left
        // first/third-person transitions several blocks away from him.
        framedDistance = Mathf.Lerp(framedDistance, playerDistance, 1f - Mathf.Exp(-4f * Time.deltaTime));
        Vector3 rawLookPoint = target.position + Vector3.up * lookHeight;
        if (!lookPointInitialized)
        {
            smoothedLookPoint = rawLookPoint;
            lookPointInitialized = true;
        }
        float horizontalBlend = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
        float verticalBlend = 1f - Mathf.Exp(-verticalTargetSmooth * Time.deltaTime);
        smoothedLookPoint.x = Mathf.Lerp(smoothedLookPoint.x, rawLookPoint.x, horizontalBlend);
        smoothedLookPoint.z = Mathf.Lerp(smoothedLookPoint.z, rawLookPoint.z, horizontalBlend);
        smoothedLookPoint.y = Mathf.Lerp(smoothedLookPoint.y, rawLookPoint.y, verticalBlend);
        Vector3 lookPoint = smoothedLookPoint;
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desired = lookPoint + orbit * (Vector3.back * framedDistance);
        Vector3 cameraRay = desired - lookPoint;
        if (cameraRay.sqrMagnitude > 0.01f &&
            Physics.SphereCast(lookPoint, collisionRadius, cameraRay.normalized, out RaycastHit obstruction,
                cameraRay.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            obstruction.collider != null &&
            !obstruction.collider.transform.IsChildOf(target) &&
            !target.IsChildOf(obstruction.collider.transform))
        {
            float safeDistance = Mathf.Max(0.55f, obstruction.distance - collisionRadius);
            desired = lookPoint + cameraRay.normalized * safeDistance;
        }
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-positionSmooth * Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(lookPoint - transform.position, Vector3.up),
            1f - Mathf.Exp(-positionSmooth * Time.deltaTime));
    }

    public void TogglePerspective()
    {
        cinematicUntil=0f;
        cinematicActive=false;
        if(firstPerson)
        {
            ResetThirdPerson(true);
            return;
        }
        firstPerson = true;
        yaw = target != null ? target.eulerAngles.y : yaw;
        pitch = 0f;
        lookPointInitialized = false;
        if (targetRenderers == null) return;
        foreach (Renderer renderer in targetRenderers)
            if (renderer != null && renderer is not ParticleSystemRenderer)
                renderer.enabled = !firstPerson;
    }

    public void FocusCinematic(Vector3 worldPoint,float seconds)
    {
        if(firstPerson)TogglePerspective();
        cinematicFocus=worldPoint;
        cinematicOffset=new Vector3(3.4f,2f,-3.8f);
        cinematicUntil=Time.time+Mathf.Max(0f,seconds);
        lookPointInitialized=false;
    }

    public void FocusCinematicFrom(Vector3 worldPoint,float seconds,Vector3 cameraOffset)
    {
        FocusCinematic(worldPoint,seconds);
        cinematicOffset=cameraOffset;
    }

    public void TrackCinematic(Vector3 worldPoint,float graceSeconds=0.55f)
    {
        if(firstPerson)TogglePerspective();
        cinematicFocus=worldPoint;
        cinematicUntil=Mathf.Max(cinematicUntil,Time.time+Mathf.Max(0.1f,graceSeconds));
        cinematicActive=true;
    }

    public void RecenterOnDanny()
    {
        cinematicActive=false;
        cinematicUntil=0f;
        ResetThirdPerson(true);
    }

    private void SetZoom(float value)
    {
        playerDistance=Mathf.Clamp(value,1.65f,4.4f);
    }

    private void ResetThirdPerson(bool snap)
    {
        if(target==null)return;
        firstPerson=false;
        cinematicUntil=0f;
        yaw=target.eulerAngles.y;
        pitch=10f;
        SetZoom(2.65f);
        framedDistance=playerDistance;
        smoothedLookPoint=target.position+Vector3.up*lookHeight;
        lookPointInitialized=true;
        if(targetRenderers!=null)
            foreach(Renderer renderer in targetRenderers)
                if(renderer!=null&&renderer is not ParticleSystemRenderer)renderer.enabled=true;
        if(!snap)return;
        Quaternion orbit=Quaternion.Euler(pitch,yaw,0f);
        Vector3 desired=smoothedLookPoint+orbit*(Vector3.back*framedDistance);
        transform.position=desired;
        transform.rotation=Quaternion.LookRotation(smoothedLookPoint-transform.position,Vector3.up);
    }

    private void OnGUI()
    {
        if(Touchscreen.current==null||firstPerson||target==null)return;
        float size=Mathf.Max(58f,Screen.height*0.075f);
        float y=Screen.height-size-22f;
        GUI.Box(new Rect(Screen.width-size*2.45f-22f,y-29f,size*2.45f,27f),"CAMERA");
        if(GUI.RepeatButton(new Rect(Screen.width-size*2.45f-22f,y,size,size),"−"))
            SetZoom(playerDistance+2.1f*Time.unscaledDeltaTime);
        if(GUI.RepeatButton(new Rect(Screen.width-size-22f,y,size,size),"+"))
            SetZoom(playerDistance-2.1f*Time.unscaledDeltaTime);
    }

    private void OnDisable()
    {
        if (targetRenderers != null)
            foreach (Renderer renderer in targetRenderers)
                if (renderer != null && renderer is not ParticleSystemRenderer)
                    renderer.enabled = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
