using UnityEngine;

public class KeyboardManager : MonoBehaviour
{
    [Header("Keyboard Settings")]
    [SerializeField] private GameObject keyboardPrefab;      // Prefab of your keyboard
    [SerializeField] private Transform keyboardSpawnPoint;   // Where you want it to appear in the scene

    private GameObject activeKeyboard;

    // Call this when you want to show the keyboard
    public void ShowKeyboard()
    {
        // If there's already a keyboard, do nothing
        if (activeKeyboard != null)
            return;

        // Spawn the keyboard at the given position and rotation
        if (keyboardPrefab != null && keyboardSpawnPoint != null)
        {
            activeKeyboard = Instantiate(keyboardPrefab, keyboardSpawnPoint.position, keyboardSpawnPoint.rotation);
        }
        else
        {
            Debug.LogWarning("Keyboard prefab or spawn point not set in Inspector.");
        }
    }

    // Call this when you want to hide or destroy the keyboard
    public void HideKeyboard()
    {
        if (activeKeyboard != null)
        {
            Destroy(activeKeyboard);
            activeKeyboard = null;
        }
    }
}
