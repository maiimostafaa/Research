using UnityEngine;
using TMPro;

public class KeyboardManager : MonoBehaviour
{
    public Canvas keyboardCanvas;
    public TMP_InputField inputField;
    public OVRVirtualKeyboard virtualKeyboard;
    public NPCChatter npcChatter;
    public Transform userCamera;
    public float distance = 1.2f;

    private void Awake()
    {
        keyboardCanvas.gameObject.SetActive(false);
        // Subscribe to input field events instead of OVRVirtualKeyboard events
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(HandleTextChanged);
            inputField.onEndEdit.AddListener(HandleTextEndEdit);
        }
    }

    public void OpenKeyboard()
    {
        Vector3 flatForward = userCamera.forward;
        flatForward.y = 0;

        keyboardCanvas.transform.position =
            userCamera.position + flatForward.normalized * distance;

        keyboardCanvas.transform.LookAt(
            keyboardCanvas.transform.position + flatForward);

        inputField.text = "";
        keyboardCanvas.gameObject.SetActive(true);
        
        // OVRVirtualKeyboard visibility is controlled by the GameObject's active state
        // No need to call Show() method
    }

    public void CloseKeyboard()
    {
        keyboardCanvas.gameObject.SetActive(false);
        // OVRVirtualKeyboard visibility is controlled by the GameObject's active state
        // No need to call Hide() method
    }

    private void HandleTextChanged(string newText)
    {
        // Text is already updated in the input field, no need to set it again
        // This handler can be used for additional logic if needed
    }

    private void HandleTextEndEdit(string text)
    {
        // Called when user finishes editing (e.g., presses Enter or submits)
        // Note: This is called when the input field loses focus or Enter is pressed
        // You can add logic here if needed, but SendMessage() is typically called from a button
    }

    public async void SendMessage()
    {
        string msg = inputField.text;

        if (!string.IsNullOrEmpty(msg))
        {
            await npcChatter.SendMessageToNPC(msg);
        }

        CloseKeyboard();
    }
}
