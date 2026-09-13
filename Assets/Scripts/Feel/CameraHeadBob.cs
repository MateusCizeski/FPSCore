using UnityEngine;

namespace FPSCore
{
    /// <summary>
    /// Módulo opcional de polimento visual: balanço cíclico da câmera ao andar.
    /// Só lê CurrentSpeed/IsGrounded do Motor — não exige nenhuma mudança nele.
    /// </summary>
    public class CameraHeadBob : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig _config;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private Transform _cameraTransform;

        private Vector3 _originalLocalPosition;
        private float _bobTimer;

        private void Awake()
        {
            _originalLocalPosition = _cameraTransform.localPosition;
        }

        private void Update()
        {
            bool isMoving = _motor.IsGrounded && _motor.CurrentSpeed > 0.1f;

            Vector3 targetPosition;
            if (isMoving)
            {
                _bobTimer += Time.deltaTime * _config.BobFrequency * (_motor.CurrentSpeed / Mathf.Max(_config.WalkSpeed, 0.01f));
                float verticalOffset = Mathf.Sin(_bobTimer) * _config.BobAmplitude;
                float horizontalOffset = Mathf.Cos(_bobTimer * 0.5f) * _config.BobAmplitude * 0.5f;
                targetPosition = _originalLocalPosition + new Vector3(horizontalOffset, verticalOffset, 0f);
            }
            else
            {
                _bobTimer = 0f;
                targetPosition = _originalLocalPosition;
            }

            _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, targetPosition,
                Time.deltaTime * _config.BobSmoothSpeed);
        }
    }
}