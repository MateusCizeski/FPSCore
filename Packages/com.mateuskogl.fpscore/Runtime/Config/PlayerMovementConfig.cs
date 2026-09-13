using UnityEngine;

namespace FPSCore
{
  /// <summary>
  /// Dados puros de configuração de movimento em primeira pessoa.
  /// Nenhum comportamento aqui — só números. Cada jogo pode ter seu próprio
  /// asset (personagem mais lento num jogo de terror, mais ágil noutro)
  /// sem duplicar ou tocar em nenhum script.
  /// </summary>
  [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "FPSCore/Player Movement Config")]
  public class PlayerMovementConfig : ScriptableObject
  {
    [Header("Velocidade")]
    public float WalkSpeed = 4f;
    public float RunSpeed = 8f;

    [Header("Pulo e Gravidade")]
    public float JumpHeight = 1.2f;
    public float Gravity = -20f;
    public float StickToGroundForce = 5f;

    [Header("Câmera / Mouse")]
    public float MouseSensitivity = 2f;
    [Tooltip("Ângulo mínimo (negativo) e máximo de inclinação vertical da câmera.")]
    public float MinVerticalAngle = -80f;
    public float MaxVerticalAngle = 80f;

    [Header("Som de Passo")]
    public float WalkStepInterval = 0.5f;
    public float RunStepInterval = 0.3f;

    [Header("Head Bob (opcional)")]
    public float BobFrequency = 8f;
    public float BobAmplitude = 0.05f;
    public float BobSmoothSpeed = 8f;

    [Header("FOV Kick (opcional)")]
    public float RunFov = 65f;
    public float FovKickDuration = 0.2f;
  }
}
