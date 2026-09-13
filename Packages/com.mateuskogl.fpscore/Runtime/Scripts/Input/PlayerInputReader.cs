using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSCore
{
  /// <summary>
  /// Único script do pacote que sabe que o Input System existe.
  /// As ações são montadas em código (sem depender de um .inputactions asset),
  /// então o pacote funciona plug-and-play em qualquer projeto que já tenha
  /// o pacote "Input System" instalado — nada pra configurar por fora.
  /// </summary>
  public class PlayerInputReader : MonoBehaviour
  {
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool RunHeld { get; private set; }

    public event Action JumpPressed;

    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _runAction;
    private InputAction _jumpAction;

    private void Awake()
    {
      _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
      _moveAction.AddCompositeBinding("2DVector")
          .With("Up", "<Keyboard>/w")
          .With("Down", "<Keyboard>/s")
          .With("Left", "<Keyboard>/a")
          .With("Right", "<Keyboard>/d");

      _lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");

      _runAction = new InputAction("Run", InputActionType.Button, "<Keyboard>/leftShift");

      _jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
    }

    private void OnEnable()
    {
      _moveAction.performed += OnMovePerformed;
      _moveAction.canceled += OnMovePerformed;
      _moveAction.Enable();

      _lookAction.performed += OnLookPerformed;
      _lookAction.canceled += OnLookPerformed;
      _lookAction.Enable();

      _runAction.performed += OnRunPerformed;
      _runAction.canceled += OnRunPerformed;
      _runAction.Enable();

      _jumpAction.performed += OnJumpPerformed;
      _jumpAction.Enable();
    }

    private void OnDisable()
    {
      _moveAction.performed -= OnMovePerformed;
      _moveAction.canceled -= OnMovePerformed;
      _moveAction.Disable();

      _lookAction.performed -= OnLookPerformed;
      _lookAction.canceled -= OnLookPerformed;
      _lookAction.Disable();

      _runAction.performed -= OnRunPerformed;
      _runAction.canceled -= OnRunPerformed;
      _runAction.Disable();

      _jumpAction.performed -= OnJumpPerformed;
      _jumpAction.Disable();
    }

    private void OnDestroy()
    {
      _moveAction.Dispose();
      _lookAction.Dispose();
      _runAction.Dispose();
      _jumpAction.Dispose();
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx) => MoveInput = ctx.ReadValue<Vector2>();
    private void OnLookPerformed(InputAction.CallbackContext ctx) => LookInput = ctx.ReadValue<Vector2>();
    private void OnRunPerformed(InputAction.CallbackContext ctx) => RunHeld = ctx.ReadValueAsButton();
    private void OnJumpPerformed(InputAction.CallbackContext ctx) => JumpPressed?.Invoke();
  }
}
