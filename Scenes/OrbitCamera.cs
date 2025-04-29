using Godot;

namespace Procedural_Content.Scenes;

public partial class OrbitCamera : Camera3D
{
    // Modifier keys' speed multiplier
    private const float ShiftMultiplier = 2.5f;
    private const float AltMultiplier = 1.0f / ShiftMultiplier;

    [field: Export(PropertyHint.Range, "0.0f,1.0f")]
    public float Sensitivity { get; set; } = 0.25f;

    // Mouse state
    private Vector2 _mousePosition = new Vector2(0.0f, 0.0f);
    private float _totalPitch = 0.0f;

    // Movement state
    private Vector3 _direction = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 _velocity = new Vector3(0.0f, 0.0f, 0.0f);
    private float _acceleration = 30f;
    private float _deceleration = -10f;
    private float _velMultiplier = 4f;

    // Keyboard state
    private bool _w = false;
    private bool _s = false;
    private bool _a = false;
    private bool _d = false;
    private bool _q = false;
    private bool _e = false;
    private bool _shift = false;
    private bool _alt = false;

    public override void _Input(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            // Receives mouse motion
            case InputEventMouseMotion mouseMotionEvent:
                _mousePosition = mouseMotionEvent.Relative;
                break;
            // Receives mouse button input
            case InputEventMouseButton mouseButtonEvent:
                HandleMouseButtonEvent(mouseButtonEvent);
                break;
            // Receives key input
            case InputEventKey keyEvent:
                HandleKeyboardEvent(keyEvent);
                break;
        }
    }

    private void HandleKeyboardEvent(InputEventKey keyEvent)
    {
        switch (keyEvent.Keycode)
        {
            case Key.W:
            {
                _w = keyEvent.Pressed;
            }
                break;

            case Key.S:
            {
                _s = keyEvent.Pressed;
            }
                break;

            case Key.A:
            {
                _a = keyEvent.Pressed;
            }
                break;

            case Key.D:
            {
                _d = keyEvent.Pressed;
            }
                break;

            case Key.Q:
            {        
                _q = keyEvent.Pressed;
            }
                break;

            case Key.E:
            {
                _e = keyEvent.Pressed;
            }
                break;
        }
    }

    private void HandleMouseButtonEvent(InputEventMouseButton mouseButtonEvent)
    {
        switch (mouseButtonEvent.ButtonIndex)
        {
            case MouseButton.Right: // Only allows rotation if right click down
            {
                Input.MouseMode = mouseButtonEvent.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
            }
                break;

            case MouseButton.WheelUp: // Increases max velocity
            {
                _velMultiplier = Mathf.Clamp(_velMultiplier * 1.1f, 0.2f, 20f);
            }
                break;

            case MouseButton.WheelDown: // Decreases max velocity
            {
                _velMultiplier = Mathf.Clamp(_velMultiplier / 1.1f, 0.2f, 20f);
            }
                break;
        }
    }

    public override void _Process(double delta)
    {
        UpdateMouseLook();
        UpdateMovement((float)delta);
    }

    // Updates camera movement
    private void UpdateMovement(float delta)
    { 
        // Computes desired direction from key states
        _direction = Vector3.Zero;
        if (_d) _direction.X += 1.0f;
        if (_a) _direction.X -= 1.0f;
        if (_e) _direction.Y += 1.0f;
        if (_q) _direction.Y -= 1.0f;
        if (_s) _direction.Z += 1.0f;
        if (_w) _direction.Z -= 1.0f;

        // Computes the change in velocity due to desired direction and "drag"
        // The "drag" is a constant acceleration on the camera to bring it's velocity to 0
        var offset = _direction.Normalized() * _acceleration * _velMultiplier * delta
                     + _velocity.Normalized() * _deceleration * _velMultiplier * delta;

        // Compute modifiers' speed multiplier
        float speedMulti = 1.0f;
        if (_shift) speedMulti *= ShiftMultiplier;
        if (_alt) speedMulti *= AltMultiplier;
        
        // Checks if we should bother translating the camera
        if ((_direction == Vector3.Zero) && (offset.LengthSquared() > _velocity.LengthSquared()))
        {
            // Sets the velocity to 0 to prevent jittering due to imperfect deceleration
            _velocity = Vector3.Zero;
        }
        else
        {
            // Clamps speed to stay within maximum value (_vel_multiplier)
            _velocity.X = Mathf.Clamp(_velocity.X + offset.X, -_velMultiplier, _velMultiplier);
            _velocity.Y = Mathf.Clamp(_velocity.Y + offset.Y, -_velMultiplier, _velMultiplier);
            _velocity.Z = Mathf.Clamp(_velocity.Z + offset.Z, -_velMultiplier, _velMultiplier);

            Translate(_velocity * delta * speedMulti);
        }
    }

    // Updates mouse look
    private void UpdateMouseLook()
    {
        // Only rotates mouse if the mouse is 
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            return;
        }
        _mousePosition *= Sensitivity;
        var yaw = _mousePosition.X;
        var pitch = _mousePosition.Y;
        _mousePosition = Vector2.Zero;
            
        // Prevents looking up/down too far
        pitch = Mathf.Clamp(pitch, -90 - _totalPitch, 90 - _totalPitch);
        _totalPitch += pitch;
        
        RotateY(Mathf.DegToRad(-yaw));
        RotateObjectLocal(new Vector3(1.0f, 0.0f, 0.0f), Mathf.DegToRad(-pitch));
    }
}