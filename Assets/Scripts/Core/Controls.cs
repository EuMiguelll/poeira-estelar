using UnityEngine.InputSystem;

// Ações de entrada compartilhadas: teclado (WASD/setas) e controle (Xbox ou qualquer gamepad).
public static class Controls
{
    public static readonly InputAction Move;
    public static readonly InputAction Dash;
    public static readonly InputAction Pause;

    static Controls()
    {
        Move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        Move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        Move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        Move.AddBinding("<Gamepad>/leftStick");
        Move.AddBinding("<Gamepad>/dpad");

        Dash = new InputAction("Dash", InputActionType.Button);
        Dash.AddBinding("<Keyboard>/space");
        Dash.AddBinding("<Keyboard>/leftShift");
        Dash.AddBinding("<Gamepad>/buttonSouth");
        Dash.AddBinding("<Gamepad>/rightTrigger");

        Pause = new InputAction("Pause", InputActionType.Button);
        Pause.AddBinding("<Keyboard>/escape");
        Pause.AddBinding("<Keyboard>/p");
        Pause.AddBinding("<Gamepad>/start");

        Move.Enable();
        Dash.Enable();
        Pause.Enable();
    }
}
