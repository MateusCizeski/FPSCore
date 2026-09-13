using System;
using UnityEngine;

namespace FPSCore
{
  /// <summary>
  /// Responsabilidade única: mover o CharacterController. Não sabe nada
  /// sobre áudio, câmera ou diálogo — só expõe eventos e propriedades
  /// públicas pra quem quiser reagir ao movimento.
  /// </summary>
  [RequireComponent(typeof(CharacterController))]
  public class PlayerMotor : MonoBehaviour
  {
    [SerializeField] private PlayerMovementConfig _config;
    [SerializeField] private PlayerInputReader _input;

    [Tooltip("Layers consideradas 'chão' para projeção de movimento e detecção de piso. Padrão: todas — restrinja se o jogo tiver layers de gatilho/VFX que não devem contar como piso.")]
    [SerializeField] private LayerMask _groundMask = ~0;

    public event Action OnFootstep;
    public event Action OnJump;
    public event Action OnLand;
    public event Action<bool> OnRunStateChanged;

    public bool IsGrounded { get; private set; }
    public float CurrentSpeed { get; private set; }
    public bool IsRunning { get; private set; }

    private CharacterController _controller;
    private Vector3 _moveDirection;
    private bool _queuedJump;
    private bool _wasGrounded;
    private bool _movementLocked;
    private float _stepTimer;

    private void Awake()
    {
      _controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
      _input.JumpPressed += OnJumpPressed;
    }

    private void OnDisable()
    {
      _input.JumpPressed -= OnJumpPressed;
    }

    private void OnJumpPressed()
    {
      _queuedJump = true;
    }

    /// <summary>
    /// Zera a velocidade horizontal e ignora input de movimento enquanto ativo.
    /// Ponto de extensão principal: diálogo, cutscene, restrição de movimento
    /// de qualquer sistema externo, sem o Motor precisar conhecê-lo.
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
      _movementLocked = locked;
      if (locked)
      {
        _moveDirection.x = 0f;
        _moveDirection.z = 0f;
      }
    }

    private void Update()
    {
      bool isRunningNow = _input.RunHeld && HasMoveInput();
      if (isRunningNow != IsRunning)
      {
        IsRunning = isRunningNow;
        OnRunStateChanged?.Invoke(IsRunning);
      }

      HandleGroundTransition();
      ProgressFootstepCycle();
    }

    private void FixedUpdate()
    {
      Vector2 moveInput = _movementLocked ? Vector2.zero : _input.MoveInput;
      if (moveInput.sqrMagnitude > 1f)
      {
        moveInput.Normalize();
      }

      float targetSpeed = IsRunning ? _config.RunSpeed : _config.WalkSpeed;
      CurrentSpeed = moveInput.sqrMagnitude > 0f ? targetSpeed : 0f;

      // Move relativo à direção que o transform está olhando, não em eixo global.
      Vector3 desiredMove = transform.forward * moveInput.y + transform.right * moveInput.x;

      // Projeta o movimento na normal do chão, pra seguir rampa/terreno irregular
      // em vez de flutuar ou perder velocidade em superfícies inclinadas.
      if (Physics.SphereCast(transform.position, _controller.radius, Vector3.down,
              out RaycastHit hitInfo, _controller.height / 2f, _groundMask, QueryTriggerInteraction.Ignore))
      {
        desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
      }

      _moveDirection.x = desiredMove.x * targetSpeed;
      _moveDirection.z = desiredMove.z * targetSpeed;

      IsGrounded = _controller.isGrounded;

      if (IsGrounded)
      {
        // Força pequena e constante pra baixo: gruda o personagem no chão
        // em pequenas irregularidades, evitando "bounce" estranho.
        _moveDirection.y = -_config.StickToGroundForce;

        if (_queuedJump)
        {
          float jumpSpeed = Mathf.Sqrt(_config.JumpHeight * -2f * _config.Gravity);
          _moveDirection.y = jumpSpeed;
          _queuedJump = false;
          OnJump?.Invoke();
        }
      }
      else
      {
        _moveDirection.y += _config.Gravity * Time.fixedDeltaTime;
      }

      _controller.Move(_moveDirection * Time.fixedDeltaTime);
    }

    private void HandleGroundTransition()
    {
      bool groundedNow = _controller.isGrounded;
      if (!_wasGrounded && groundedNow)
      {
        _moveDirection.y = 0f;
        OnLand?.Invoke();
      }
      _wasGrounded = groundedNow;
    }

    private bool HasMoveInput()
    {
      return !_movementLocked && _input.MoveInput.sqrMagnitude > 0.01f;
    }

    private void ProgressFootstepCycle()
    {
      if (!IsGrounded || !HasMoveInput())
      {
        _stepTimer = 0f;
        return;
      }

      float interval = IsRunning ? _config.RunStepInterval : _config.WalkStepInterval;
      _stepTimer += Time.deltaTime;
      if (_stepTimer >= interval)
      {
        _stepTimer = 0f;
        OnFootstep?.Invoke();
      }
    }
  }
}
