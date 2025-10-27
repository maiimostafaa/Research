using UnityEngine;
using TMPro;

public class KeyboardManager : MonoBehaviour
{
    [SerializeField] private OVRVirtualKeyboard keyboard;
    [SerializeField] private TMP_InputField inputField;
    
    private void Start()
    {
        // Hide keyboard initially
        if (keyboard != null)
        {
            keyboard.gameObject.SetActive(false);
        }
        
        // Listen for input field selection
        if (inputField != null)
        {
            inputField.onSelect.AddListener(OnInputSelected);
            inputField.onDeselect.AddListener(OnInputDeselected);
        }
    }
    
    private void OnInputSelected(string text)
    {
        ShowKeyboard();
    }
    
    private void OnInputDeselected(string text)
    {
        HideKeyboard();
    }
    
    public void ShowKeyboard()
    {
        if (keyboard != null)
        {
            keyboard.gameObject.SetActive(true);
            // Position keyboard in front of user or near input field
            PositionKeyboard();
        }
    }
    
    public void HideKeyboard()
    {
        if (keyboard != null)
        {
            keyboard.gameObject.SetActive(false);
        }
    }
    
    private void PositionKeyboard()
    {
        // Position keyboard in front of the camera/user
        Transform cameraTransform = Camera.main.transform;
        keyboard.transform.position = cameraTransform.position + cameraTransform.forward * 1.5f;
        keyboard.transform.rotation = Quaternion.LookRotation(keyboard.transform.position - cameraTransform.position);
    }
}