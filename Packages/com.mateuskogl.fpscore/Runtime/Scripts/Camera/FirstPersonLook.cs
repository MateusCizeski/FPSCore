using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSCore
{
  /// <summary>
  /// Responsabilidade única: girar o corpo (eixo Y) e o pivô da câmera (eixo X).
  /// Nenhum movimento de posição acontece aqui.
  /// </summary>
  public class FirstPersonLook : MonoBehaviour
  {
    [SerializeField] private PlayerMovementConfig _config;
    [SerializeField] private PlayerInputReader _input;
    [SerializeField] private Transform _playerBody;
    [SerializeField] private Transform _cameraPivot;

    [Tooltip("<Mouse>/delta vem em pixels por frame — escala pra deixar a sensibilidade num range parecido com o antigo GetAxis(\"Mouse X\"). Ajuste no olho.")]
    [SerializeField] private float _deltaScale = 0.02f;

    private float _verticalRotation;
    private bool _lookLocked;
    private bool _cursorLocked = true;

    private void Start()
    {
      SetCursorLocked(true);
    }

    private void Update()
    {
      if (!_lookLocked)
      {
        ApplyLook();
      }

      HandleCursorToggle();
    }

    private void ApplyLook()
    {
      float sensitivity = _config.MouseSensitivity * _deltaScale;
      float yaw = _input.LookInput.x * sensitivity;
      float pitch = _input.LookInput.y * sensitivity;

      _playerBody.Rotate(Vector3.up * yaw);

      _verticalRotation -= pitch;
      _verticalRotation = Mathf.Clamp(_verticalRotation, _config.MinVerticalAngle, _config.MaxVerticalAngle);
      _cameraPivot.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }

    /// <summary>
    /// Trava o olhar independente do corpo — útil pra travar só a câmera
    /// numa cutscene, ou vice-versa.
    /// </summary>
    public void SetLookLocked(bool locked)
    {
      _lookLocked = locked;
    }

    private void HandleCursorToggle()
    {
      // Comportamento pontual de UI (Esc solta, clique esquerdo trava de novo) —
      // lido direto aqui, sem passar pelo PlayerInputReader, pois não é parte
      // do mapa de ações de gameplay.
      if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        SetCursorLocked(false);
      }
      else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
      {
        SetCursorLocked(true);
      }
    }

    private void SetCursorLocked(bool locked)
    {
      _cursorLocked = locked;
      Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
      Cursor.visible = !locked;
    }
  }
}
