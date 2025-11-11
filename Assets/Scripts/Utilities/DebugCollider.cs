using UnityEngine;

/// <summary>
/// Script helper để debug và visualize colliders trong Scene view
/// Attach vào GameObject có collider để thấy bounds trong Scene view
/// </summary>
public class DebugCollider : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private bool showWireframe = true;
    [SerializeField] private bool showLabel = false;

    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    void OnDrawGizmos()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();

        if (boxCollider != null)
        {
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (showWireframe)
            {
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else
            {
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }

#if UNITY_EDITOR
            if (showLabel)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2,
                    $"{gameObject.name}\n{(boxCollider.isTrigger ? "Trigger" : "Collider")}"
                );
            }
#endif
        }
    }
}
