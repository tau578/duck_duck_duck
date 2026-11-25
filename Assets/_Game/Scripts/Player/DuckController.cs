using UnityEngine;
using UnityEngine.InputSystem;

namespace Duck
{
    /// <summary>Moves the duck with the on-screen stick (or WASD/arrows in the editor) inside the map bounds.</summary>
    public class DuckController : MonoBehaviour
    {
        public float moveSpeed = 6f;
        public Vector2 mapHalfSize = new Vector2(14f, 21f);
        public VirtualJoystick joystick;
        [Tooltip("Child that bobs while walking (procedural animation).")]
        public Transform visual;
        public float bobHeight = 0.18f;
        public float bobSpeed = 14f;

        public static DuckController Instance { get; private set; }

        [Tooltip("When set (by AutoShooter), the duck faces this point instead of its move direction.")]
        public Transform aimTarget;
        public bool inputEnabled = true;

        public Vector3 MoveDirection { get; private set; }
        public bool IsMoving => MoveDirection.sqrMagnitude > 0.0001f;

        float bobPhase;

        void Awake() { Instance = this; }

        void Update()
        {
            Vector2 input = inputEnabled ? ReadInput() : Vector2.zero;
            Vector3 dir = new Vector3(input.x, 0f, input.y);
            MoveDirection = dir;

            Vector3 p = transform.position + dir * (moveSpeed * Time.deltaTime);
            p.x = Mathf.Clamp(p.x, -mapHalfSize.x, mapHalfSize.x);
            p.z = Mathf.Clamp(p.z, -mapHalfSize.y, mapHalfSize.y);
            transform.position = p;

            Vector3 face = dir;
            if (aimTarget != null && aimTarget.gameObject.activeInHierarchy)
            {
                face = aimTarget.position - transform.position;
                face.y = 0f;
            }
            if (face.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(face, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 15f * Time.deltaTime);
            }

            AnimateBob();
        }

        Vector2 ReadInput()
        {
            Vector2 input = joystick != null ? joystick.Value : Vector2.zero;

            var kb = Keyboard.current;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f;
                if (k != Vector2.zero) input = k.normalized;
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        void AnimateBob()
        {
            if (visual == null) return;
            if (IsMoving)
                bobPhase += Time.deltaTime * bobSpeed;
            else
                bobPhase = Mathf.MoveTowards(bobPhase, Mathf.Round(bobPhase / Mathf.PI) * Mathf.PI, Time.deltaTime * bobSpeed);

            float h = Mathf.Abs(Mathf.Sin(bobPhase)) * bobHeight;
            float squash = 1f - Mathf.Abs(Mathf.Cos(bobPhase)) * (IsMoving ? 0.06f : 0f);
            visual.localPosition = new Vector3(0f, h, 0f);
            visual.localScale = new Vector3(1f / squash, squash, 1f / squash);
        }
    }
}
