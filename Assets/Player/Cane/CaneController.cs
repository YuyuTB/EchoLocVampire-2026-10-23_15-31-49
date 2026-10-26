using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CaneController : MonoBehaviour
{
    [Header("Input Configuration")]
    [Tooltip("Reference to the Input Action used for tapping the ground (e.g., Attack).")]
    [SerializeField] private InputActionReference attackActionReference;

    [Header("Raycast Settings")]
    [Tooltip("Maximum downward distance to detect the ground from the cane's origin.")]
    [SerializeField] private float raycastDistance = 3.0f;

    [Tooltip("LayerMask to filter what counts as ground (optional, set to Everything by default).")]
    [SerializeField] private LayerMask groundLayerMask = ~0;

    // Shader property IDs cached for optimal runtime performance
    private static readonly int ImpactPositionID = Shader.PropertyToID("_ImpactPosition");
    private static readonly int ImpactTimeID = Shader.PropertyToID("_ImpactTime");

    [SerializeField] private GameObject HandAndCane;
    private Vector3 initialLocalPosition;
    private Coroutine tapCoroutine;

    #region Unity Lifecycle Methods

    private void Awake()
    {
        if (HandAndCane != null)
        {
            initialLocalPosition = HandAndCane.transform.localPosition;
        }
    }

    private void OnEnable()
    {
        RegisterInputEvents();
    }

    private void OnDisable()
    {
        UnregisterInputEvents();
    }

    #endregion

    #region Input Handlers

    private void RegisterInputEvents()
    {
        if (attackActionReference != null && attackActionReference.action != null)
        {
            attackActionReference.action.Enable();
            attackActionReference.action.performed += OnAttackInputTriggered;
        }
    }

    private void UnregisterInputEvents()
    {
        if (attackActionReference != null && attackActionReference.action != null)
        {
            attackActionReference.action.performed -= OnAttackInputTriggered;
            attackActionReference.action.Disable();
        }
    }

    private void OnAttackInputTriggered(InputAction.CallbackContext context)
    {
        ExecuteCaneTap();
    }

    #endregion

    #region Core Logic

    private void ExecuteCaneTap()
    {
        PlayerController player = transform.GetComponentInParent<PlayerController>();
        if (player != null && player.isInCutscene) return;
        AnimateTap();
        // Start raycast directly from the Cane GameObject position
        Vector3 rayOrigin = transform.position;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayerMask))
        {
            Vector3 impactPoint = hit.point;

            // Broadcast global parameters to all Shader Graph materials
            Shader.SetGlobalVector(ImpactPositionID, new Vector4(impactPoint.x, impactPoint.y, impactPoint.z, 0.0f));
            Shader.SetGlobalFloat(ImpactTimeID, Time.time);
        }
    }

    #endregion

    #region Debug & Visualization

    private void OnDrawGizmosSelected()
    {
        // Visualize the downward raycast in the Scene View when selected
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, Vector3.down * raycastDistance);
    }

    #endregion

    private void AnimateTap()
    {
        
        if (HandAndCane != null)
        {
            if (tapCoroutine != null)
            {
                StopCoroutine(tapCoroutine);
            }
            tapCoroutine = StartCoroutine(TapRoutine());
        }
    }

    private IEnumerator TapRoutine()
    {
        float duration = 0.08f; // Vitesse de la frappe (très réactive)
        float liftHeight = 0.15f; // Hauteur du soulèvement

        Vector3 targetUp = initialLocalPosition + new Vector3(0, liftHeight, 0);
        Transform caneTransform = HandAndCane.transform;

        // 1. Soulèvement de la canne
        float elapsed = 0f;
        while (elapsed < duration)
        {
            caneTransform.localPosition = Vector3.Lerp(initialLocalPosition, targetUp, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        caneTransform.localPosition = targetUp;

        // 2. Coup vers le bas (frappe au sol)
        elapsed = 0f;
        while (elapsed < duration)
        {
            caneTransform.localPosition = Vector3.Lerp(targetUp, initialLocalPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        caneTransform.localPosition = initialLocalPosition;
        tapCoroutine = null;
    }
}