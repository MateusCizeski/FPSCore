using System.Collections;
using UnityEngine;

namespace FPSCore
{
    /// <summary>
    /// Módulo opcional de polimento visual: aumenta levemente o FOV ao correr.
    /// Só escuta OnRunStateChanged do Motor — não exige nenhuma mudança nele.
    /// </summary>
    public class RunFovKick : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig _config;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private Camera _camera;

        private float _originalFov;
        private Coroutine _fovRoutine;

        private void Awake()
        {
            _originalFov = _camera.fieldOfView;
        }

        private void OnEnable()
        {
            _motor.OnRunStateChanged += HandleRunStateChanged;
        }

        private void OnDisable()
        {
            _motor.OnRunStateChanged -= HandleRunStateChanged;
        }

        private void HandleRunStateChanged(bool isRunning)
        {
            if (_fovRoutine != null)
            {
                StopCoroutine(_fovRoutine);
            }
            float targetFov = isRunning ? _config.RunFov : _originalFov;
            _fovRoutine = StartCoroutine(LerpFov(targetFov));
        }

        private IEnumerator LerpFov(float targetFov)
        {
            float startFov = _camera.fieldOfView;
            float elapsed = 0f;
            while (elapsed < _config.FovKickDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _config.FovKickDuration;
                _camera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
                yield return null;
            }
            _camera.fieldOfView = targetFov;
        }
    }
}
