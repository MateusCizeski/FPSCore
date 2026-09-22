using UnityEngine;

namespace FPSCore
{
  /// <summary>
  /// Responsabilidade única: tocar som, escutando os eventos do PlayerMotor.
  /// Nunca é chamado diretamente por ninguém — é o exemplo mais simples de
  /// "conectar sem acoplamento direto" do pacote.
  /// </summary>
  [RequireComponent(typeof(AudioSource))]
  public class FootstepAudioPlayer : MonoBehaviour
  {
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private AudioClip[] _footstepClips;
    [SerializeField] private AudioClip _jumpClip;
    [SerializeField] private AudioClip _landClip;

    private AudioSource _audioSource;
    private int _lastFootstepIndex = -1;

    private void Awake()
    {
      _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
      if (_motor == null)
      {
        Debug.LogError($"[FPSCore] {nameof(FootstepAudioPlayer)} em '{name}' está sem referência de PlayerMotor. Componente desativado.", this);
        enabled = false;
        return;
      }

      _motor.OnFootstep += PlayFootstep;
      _motor.OnJump += PlayJump;
      _motor.OnLand += PlayLand;
    }

    private void OnDisable()
    {
      if (_motor == null)
      {
        return;
      }

      _motor.OnFootstep -= PlayFootstep;
      _motor.OnJump -= PlayJump;
      _motor.OnLand -= PlayLand;
    }

    private void PlayFootstep()
    {
      if (_footstepClips == null || _footstepClips.Length == 0)
      {
        return;
      }

      int index = Random.Range(0, _footstepClips.Length);
      // Evita repetir o mesmo clipe duas vezes seguidas quando há mais de uma opção.
      if (_footstepClips.Length > 1 && index == _lastFootstepIndex)
      {
        index = (index + 1) % _footstepClips.Length;
      }
      _lastFootstepIndex = index;

      _audioSource.PlayOneShot(_footstepClips[index]);
    }

    private void PlayJump()
    {
      if (_jumpClip != null)
      {
        _audioSource.PlayOneShot(_jumpClip);
      }
    }

    private void PlayLand()
    {
      if (_landClip != null)
      {
        _audioSource.PlayOneShot(_landClip);
      }
    }
  }
}
