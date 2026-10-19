using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>
    /// Separates raw devices (keyboard, mouse, gamepad, touch) from gameplay actions. Uses the legacy Input
    /// Manager like Arcane-Brigade (works on PC/Android/iOS without extra packages).
    /// </summary>
    public sealed class InputAdapter
    {
        // virtual joystick (touch), in screen pixels
        public bool JoyActive;
        public Vector2 JoyOrigin, JoyOffset;
        public float JoyRadius = 90f;
        private int _joyFinger = -1;

        public Vector2 Move;
        public bool UltPressed, DashPressed, PausePressed, SpeedPressed, CancelPressed, UndoPressed, StartNightPressed;
        public bool PointerDown, PointerHeld, PointerUp;
        public Vector2 PointerScreen;
        public bool PointerOverUi;
        public float Zoom;       // positive = zoom in
        public Vector2 PanPixels; // screen-space drag delta for camera
        public bool TouchMode;
        private Vector2 _lastPointer;
        private float _pinchDist = -1f;
        public bool UltButtonTouched, DashButtonTouched;
        public Rect UltButtonScreen, DashButtonScreen;
        private bool _dragPanning;

        public void Update(bool nightControls, bool touchUi)
        {
            TouchMode = touchUi || Input.touchCount > 0;
            UltPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.JoystickButton5);
            DashPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.JoystickButton4);
            PausePressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.JoystickButton7);
            SpeedPressed = Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.JoystickButton6);
            CancelPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.JoystickButton2);
            UndoPressed = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand)) && Input.GetKeyDown(KeyCode.Z);
            StartNightPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.N);

            // keyboard / gamepad movement
            float h = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float v = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            Vector2 kb = Vector2.ClampMagnitude(new Vector2(h, v), 1f);
            Vector2 pad = Vector2.zero;
            try { pad = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); } catch { }
            if (pad.magnitude < 0.2f) pad = Vector2.zero;
            Move = pad.sqrMagnitude > kb.sqrMagnitude ? Vector2.ClampMagnitude(pad, 1f) : kb;

            // pointer (mouse or first non-joystick touch)
            PointerDown = PointerUp = false;
            Zoom = 0f;
            PanPixels = Vector2.zero;
            UltButtonTouched = DashButtonTouched = false;
            if (Input.touchCount > 0) UpdateTouch(nightControls);
            else
            {
                PointerScreen = Input.mousePosition;
                PointerDown = Input.GetMouseButtonDown(0);
                PointerHeld = Input.GetMouseButton(0);
                PointerUp = Input.GetMouseButtonUp(0);
                Zoom = Input.mouseScrollDelta.y;
                if (Input.GetMouseButton(2)) PanPixels = (Vector2)Input.mousePosition - _lastPointer;
                _lastPointer = Input.mousePosition;
                JoyActive = false;
            }
            PointerOverUi = Gui.OverUI(PointerScreen);
        }

        private void UpdateTouch(bool night)
        {
            bool tracked = false;
            int pointerFinger = -1;
            var t0 = default(Touch);
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                // ultimate / dash buttons
                if (t.phase == TouchPhase.Began)
                {
                    if (UltButtonScreen.Contains(t.position)) { UltButtonTouched = true; continue; }
                    if (DashButtonScreen.Contains(t.position)) { DashButtonTouched = true; continue; }
                }
                if (night)
                {
                    bool leftHalf = t.position.x < Screen.width * 0.5f;
                    if (_joyFinger < 0 && t.phase == TouchPhase.Began && leftHalf && !Gui.OverUI(t.position))
                    {
                        _joyFinger = t.fingerId;
                        JoyOrigin = t.position;
                    }
                    if (t.fingerId == _joyFinger)
                    {
                        tracked = true;
                        JoyRadius = Mathf.Max(70f, Screen.height * 0.09f);
                        if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { _joyFinger = -1; JoyOffset = Vector2.zero; }
                        else JoyOffset = Vector2.ClampMagnitude(t.position - JoyOrigin, JoyRadius);
                        continue;
                    }
                }
                if (pointerFinger < 0) { pointerFinger = t.fingerId; t0 = t; }
            }
            if (!tracked && _joyFinger >= 0) { _joyFinger = -1; JoyOffset = Vector2.zero; }
            JoyActive = _joyFinger >= 0;
            if (JoyActive)
            {
                var j = JoyOffset / JoyRadius;
                Move = j.magnitude < 0.15f ? Vector2.zero : j.normalized * Mathf.Clamp01((j.magnitude - 0.15f) / 0.85f);
            }
            // two-finger pinch zoom + pan (day)
            if (!night && Input.touchCount >= 2)
            {
                var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                float d = Vector2.Distance(a.position, b.position);
                if (_pinchDist > 0f) Zoom = (d - _pinchDist) / 60f;
                _pinchDist = d;
                PanPixels = (a.deltaPosition + b.deltaPosition) * 0.5f;
                PointerHeld = false;
                _dragPanning = true;
                return;
            }
            _pinchDist = -1f;
            if (pointerFinger >= 0)
            {
                PointerScreen = t0.position;
                PointerDown = t0.phase == TouchPhase.Began;
                PointerHeld = t0.phase != TouchPhase.Ended && t0.phase != TouchPhase.Canceled;
                PointerUp = t0.phase == TouchPhase.Ended;
                if (PointerDown) _dragPanning = false;
            }
            else PointerHeld = false;
        }
    }

    /// <summary>Orthographic camera: free pan/zoom by day, smooth follow of the General by night; optional shake.</summary>
    public sealed class CameraRig
    {
        public Camera Cam;
        public Vector2 Focus;
        public float Size = 9f;
        public float MinSize = 5f, MaxSize = 16f;
        private float _shake;
        public bool ShakeEnabled = true;
        public Vector2 MapSize;

        public void Shake(float amount)
        {
            if (ShakeEnabled) _shake = Mathf.Max(_shake, amount);
        }

        public void Update(float dt, bool follow, Vector2 target, Vector2 lookAhead, InputAdapter input)
        {
            if (follow)
            {
                Focus = Vector2.Lerp(Focus, target + lookAhead, 1f - Mathf.Exp(-dt * 6f));
                Size = Mathf.Lerp(Size, Mathf.Clamp(Size, 6.5f, 10f), dt * 2f);
            }
            else if (!input.PointerOverUi)
            {
                Vector2 kb = input.Move * dt * Size * 1.4f;
                Focus += kb;
                if (input.PanPixels != Vector2.zero)
                    Focus -= input.PanPixels * (Cam.orthographicSize * 2f / Screen.height);
            }
            if (!input.PointerOverUi && Mathf.Abs(input.Zoom) > 0.01f) Size = Mathf.Clamp(Size - input.Zoom * 0.8f, MinSize, MaxSize);
            float aspect = Cam.aspect;
            float halfW = Size * aspect, halfH = Size;
            Focus.x = Mathf.Clamp(Focus.x, Mathf.Min(halfW * 0.5f, MapSize.x / 2), Mathf.Max(MapSize.x - halfW * 0.5f, MapSize.x / 2));
            Focus.y = Mathf.Clamp(Focus.y, Mathf.Min(halfH * 0.5f, MapSize.y / 2), Mathf.Max(MapSize.y - halfH * 0.5f, MapSize.y / 2));
            _shake = Mathf.MoveTowards(_shake, 0f, dt * 2.5f);
            Vector2 off = _shake > 0f ? Random.insideUnitCircle * _shake * 0.25f : Vector2.zero;
            Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, Size, 1f - Mathf.Exp(-dt * 10f));
            Cam.transform.position = new Vector3(Focus.x + off.x, Focus.y + off.y, -10f);
        }

        public Vector2 ScreenToWorld(Vector2 screen) => Cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
        public Vector2 WorldToGui(Vector2 world)
        {
            var sp = Cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            return new Vector2(sp.x / Gui.Scale, (Screen.height - sp.y) / Gui.Scale);
        }
    }
}
