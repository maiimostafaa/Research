using UnityEngine;
using UnityEngine.InputSystem;

public class Spray : MonoBehaviour
{
    [Header("Spray Settings")]
    public Transform sprayOrigin;
    public float sprayDistance = 5f;
    public GameObject paintDotPrefab;

    [Header("Input")]
    public InputActionReference sprayAction;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool showDebugRays = true;
    
    private int paintDotsCreated = 0;

    void Start()
    {

        if (enableDebugLogs)
        {
            Debug.Log($"Spray Origin: {(sprayOrigin != null ? sprayOrigin.name : "NULL")}");
            Debug.Log($"Paint Dot Prefab: {(paintDotPrefab != null ? paintDotPrefab.name : "NULL")}");
            Debug.Log($"Spray Action: {(sprayAction != null ? sprayAction.action.name : "NULL")}");
            Debug.Log($"Spray Distance: {sprayDistance}");
            
            if (sprayOrigin != null)
            {
                Debug.Log($"Spray Origin Position: {sprayOrigin.position}");
                Debug.Log($"Spray Origin Forward: {sprayOrigin.forward}");
            }
        }
    }

    void Update()
    {
        if (sprayAction != null)
        {
            bool isPressed = sprayAction.action.IsPressed();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[FRAME {Time.frameCount}] Spray action pressed: {isPressed}");
            }
            
            if (isPressed)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[FRAME {Time.frameCount}] About to call PerformSpray()");
                }
                PerformSpray();
                if (enableDebugLogs)
                {
                    Debug.Log($"[FRAME {Time.frameCount}] PerformSpray() completed");
                }
            }
        }
        else if (enableDebugLogs)
        {
            Debug.LogError("Spray Action is NULL");
        }
    }

    void PerformSpray()
    {
        Debug.LogError("PERFORMSPRAY ENTRY");
        
        if (sprayOrigin == null)
        {
            Debug.LogError("Spray Origin is NULL");
            return;
        }
        
        Debug.LogError("proceeding with PerformSpray()");
        
        Vector3 rayOrigin = sprayOrigin.position;
        Vector3 rayDirection = -sprayOrigin.forward; 
        
        Debug.LogError($"sprayOrigin.forward = {sprayOrigin.forward}");
        Debug.LogError($"rayDirection (after negation) = {rayDirection}");
        Debug.LogError($"Controller World Position: {sprayOrigin.position}");
        Debug.LogError($"Controller World Rotation: {sprayOrigin.rotation.eulerAngles}");
        Debug.LogError($"Controller Forward Direction: {sprayOrigin.forward}");
        
        RaycastHit hit;
        bool didHit = Physics.Raycast(rayOrigin, rayDirection, out hit, sprayDistance);
        
        Debug.LogError($"Raycast result: {(didHit ? "HIT" : "MISS")}");
        
        if (didHit)
        {
            Debug.LogError($"Hit point: {hit.point}");
            Debug.LogError($"Controller position: {sprayOrigin.position}");
            
            if (hit.collider.CompareTag("Wall"))
            {
                Paint(hit.point, hit.normal);
            }
        }
    }

    void Paint(Vector3 position, Vector3 normal)
    {
        if (paintDotPrefab == null)
        {
            Debug.LogError("Paint Dot Prefab is NULL");
            return;
        }


        Vector3 randomOffset = new Vector3(
            Random.Range(-0.3f, 0.3f),
            Random.Range(-0.3f, 0.3f),
            Random.Range(-0.1f, 0.1f)
        );
        
        Vector3 offset = normal * 0.01f;
        Vector3 finalPosition = position + offset + randomOffset;
        Quaternion rotation = Quaternion.LookRotation(normal);

        GameObject dot = Instantiate(paintDotPrefab, finalPosition, rotation);
        
        if (dot != null)
        {

            float randomSize = Random.Range(0.5f, 1.5f);
            dot.transform.localScale = Vector3.one * randomSize;
            paintDotsCreated++;
            
            Debug.Log($" Paint dot #{paintDotsCreated} created at: {finalPosition}");
        }
        else
        {
            Debug.LogError("Failed to instantiate paint dot");
        }
    }


    [ContextMenu("Test Paint at Origin Position")]
    public void TestPaint()
    {
        if (sprayOrigin != null)
        {
            Paint(sprayOrigin.position + sprayOrigin.forward, Vector3.back);
            Debug.Log("Test paint dot created");
        }
    }
}