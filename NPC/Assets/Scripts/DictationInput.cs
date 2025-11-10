using UnityEngine;
using UnityEngine.UI;
using Meta.WitAi;
using Meta.WitAi.Dictation;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Events;
// using Meta.WitAi.Requests; // Commented out - not needed for now
using Oculus.Voice.Dictation;
using TMPro;

public enum DictationState
{
    Idle,
    Starting,
    Listening,
    Processing,
    Error
}

public class DictationInput : MonoBehaviour
{
    [Header("Dictation Components")]
    public AppDictationExperience dictation; // drag [BuildingBlock] Dictation here
    public TMP_InputField userInput;         // drag your text input field here
    
    [Header("UI Feedback")]
    public TMP_Text statusText;              // Optional: Text to show status (e.g., "Listening...", "Processing...")
    public Image micButtonImage;            // Optional: Image component of mic button for color changes
    public Color idleColor = Color.white;
    public Color listeningColor = Color.red;
    public Color processingColor = Color.yellow;
    public Color errorColor = Color.magenta;
    
    private string accumulatedText = "";     // stores text across multiple recording sessions
    private DictationState currentState = DictationState.Idle;
    private bool isProcessingRequest = false;

    void Start()
    {
        // Note: To adjust pause tolerance, select [BuildingBlock] Dictation in Hierarchy
        // In Inspector, expand "Voice Service" > "Runtime Configuration"
        // Adjust "Endpoint Speech Threshold" (silence detection, try 1.5-3 seconds)
        // Adjust "Max Recording Time" (maximum recording length, try 120 seconds)
        
        UpdateUIState(DictationState.Idle);
    }

    void OnEnable()
    {
        if (dictation != null)
        {
            // Register all important event callbacks
            dictation.DictationEvents.OnFullTranscription.AddListener(OnDictationComplete);
            dictation.DictationEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            dictation.DictationEvents.OnStartListening.AddListener(OnStartListening);
            dictation.DictationEvents.OnStoppedListening.AddListener(OnStoppedListening);
            dictation.DictationEvents.OnError.AddListener(OnError);
            dictation.DictationEvents.OnRequestCompleted.AddListener(OnRequestCompleted);
            // Commented out - Wit AI events not needed for now
            // dictation.DictationEvents.OnRequestCreated.AddListener(OnRequestCreated);
            // dictation.DictationEvents.OnMicDataSent.AddListener(OnMicDataSent);
        }
    }

    void OnDisable()
    {
        if (dictation != null)
        {
            // Unregister all event callbacks
            dictation.DictationEvents.OnFullTranscription.RemoveListener(OnDictationComplete);
            dictation.DictationEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            dictation.DictationEvents.OnStartListening.RemoveListener(OnStartListening);
            dictation.DictationEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            dictation.DictationEvents.OnError.RemoveListener(OnError);
            dictation.DictationEvents.OnRequestCompleted.RemoveListener(OnRequestCompleted);
            // Commented out - Wit AI events not needed for now
            // dictation.DictationEvents.OnRequestCreated.RemoveListener(OnRequestCreated);
            // dictation.DictationEvents.OnMicDataSent.RemoveListener(OnMicDataSent);
        }
    }

    // Commented out - Wit AI events not needed for now
    // private void OnRequestCreated(WitRequest request)
    // {
    //     Debug.Log("Dictation: Request created");
    //     isProcessingRequest = true;
    //     UpdateUIState(DictationState.Processing);
    // }

    // private void OnMicDataSent(WitRequest request)
    // {
    //     Debug.Log("Dictation: Audio data sent for processing");
    //     UpdateUIState(DictationState.Processing);
    // }

    private void OnStartListening()
    {
        Debug.Log("Dictation: Started listening");
        currentState = DictationState.Listening;
        isProcessingRequest = false;
        
        // Sync current keyboard input into accumulated text when starting to listen
        SyncKeyboardInput();
        
        UpdateUIState(DictationState.Listening);
    }

    private void OnStoppedListening()
    {
        Debug.Log("Dictation: Stopped listening");
        // Only update to idle if we're not processing a request
        if (!isProcessingRequest)
        {
            UpdateUIState(DictationState.Idle);
        }
    }

    private void OnRequestCompleted()
    {
        Debug.Log("Dictation: Request completed");
        isProcessingRequest = false;
        // If we're not listening anymore, go to idle
        if (!dictation.Active)
        {
            UpdateUIState(DictationState.Idle);
        }
    }

    private void OnError(string error, string message)
    {
        Debug.LogError($"Dictation Error: {error} - {message}");
        currentState = DictationState.Error;
        isProcessingRequest = false;
        UpdateUIState(DictationState.Error);
        
        // Auto-recover after a short delay
        Invoke(nameof(ResetToIdle), 2f);
    }

    private void ResetToIdle()
    {
        if (currentState == DictationState.Error)
        {
            UpdateUIState(DictationState.Idle);
        }
    }

    // Called while user is speaking (live updates)
    private void OnPartialTranscription(string transcription)
    {
        Debug.Log("Partial: " + transcription);

        // Update UI on main thread - but DON'T update accumulatedText yet
        // Only show preview, final text will be added in OnDictationComplete
        MainThreadDispatcher.RunOnMainThread(() =>
        {
            if (userInput != null)
            {
                // Show accumulated text + current partial transcription (preview only)
                string displayText = accumulatedText;
                if (!string.IsNullOrEmpty(accumulatedText) && !string.IsNullOrEmpty(transcription))
                {
                    displayText += " ";
                }
                displayText += transcription;
                
                // Only update if the field is not currently focused (to avoid conflicts with keyboard input)
                if (!userInput.isFocused)
                {
                    userInput.text = displayText;
                    userInput.SetTextWithoutNotify(displayText); // Force UI update
                    Debug.Log("Partial transcription preview updated in UI: " + displayText);
                }
            }
        });
    }

    // Called when user finishes speaking (final result)
    private void OnDictationComplete(string transcription)
    {
        Debug.Log("Complete: " + transcription);

        // Update UI on main thread
        MainThreadDispatcher.RunOnMainThread(() =>
        {
            if (userInput != null && !string.IsNullOrEmpty(transcription))
            {
                // If input field is focused, get current text (may have keyboard input)
                string currentText = userInput.isFocused ? userInput.text : accumulatedText;
                
                // Check if current text already contains this transcription (avoid duplicates)
                // If current text ends with the transcription, don't add it again
                if (!currentText.EndsWith(transcription))
                {
                    // If current text differs from accumulated text, user typed something new
                    // Merge: use current text as base, then append dictation
                    if (currentText != accumulatedText && !string.IsNullOrEmpty(currentText))
                    {
                        accumulatedText = currentText;
                    }
                    
                    // Add this transcription to accumulated text (only if not already there)
                    if (!string.IsNullOrEmpty(accumulatedText))
                    {
                        accumulatedText += " ";
                    }
                    accumulatedText += transcription;
                }
                else
                {
                    // Transcription already in text, just use current accumulated text
                    accumulatedText = currentText;
                }
                
                // Update input field with accumulated text
                userInput.text = accumulatedText;
                userInput.SetTextWithoutNotify(accumulatedText); // Force UI update
                Debug.Log("Complete transcription updated in UI: " + accumulatedText);
            }
            
            // Update state after receiving final transcription
            isProcessingRequest = false;
            if (!dictation.Active)
            {
                UpdateUIState(DictationState.Idle);
            }
        });
    }

    private void UpdateUIState(DictationState newState)
    {
        currentState = newState;
        
        // Update UI immediately - try MainThreadDispatcher first, but fallback to direct update
        try
        {
            MainThreadDispatcher.RunOnMainThread(() => UpdateUIStateInternal(newState));
        }
        catch
        {
            // If MainThreadDispatcher doesn't exist or fails, update directly (we're likely on main thread)
            UpdateUIStateInternal(newState);
        }
    }
    
    private void UpdateUIStateInternal(DictationState newState)
    {
        // Update status text
        if (statusText != null)
        {
            switch (newState)
            {
                case DictationState.Idle:
                    statusText.text = "Tap to speak";
                    break;
                case DictationState.Starting:
                    statusText.text = "Starting...";
                    break;
                case DictationState.Listening:
                    statusText.text = "Listening...";
                    break;
                case DictationState.Processing:
                    statusText.text = "Processing...";
                    break;
                case DictationState.Error:
                    statusText.text = "Error - Try again";
                    break;
            }
            Debug.Log($"Status text updated: {statusText.text}");
        }
        
        // Update button color
        if (micButtonImage != null)
        {
            Color targetColor = idleColor;
            switch (newState)
            {
                case DictationState.Idle:
                    targetColor = idleColor;
                    break;
                case DictationState.Starting:
                case DictationState.Listening:
                    targetColor = listeningColor;
                    break;
                case DictationState.Processing:
                    targetColor = processingColor;
                    break;
                case DictationState.Error:
                    targetColor = errorColor;
                    break;
            }
            micButtonImage.color = targetColor;
            Debug.Log($"Mic button color updated to {targetColor} for state {newState}. Image component: {(micButtonImage != null ? "Found" : "NULL")}");
        }
        else
        {
            Debug.LogWarning("micButtonImage is not assigned in DictationInput component!");
        }
    }

    // Call this from your microphone button's OnClick event
    public void ToggleDictation()
    {
        if (dictation == null)
        {
            Debug.LogError("Dictation component is not assigned!");
            return;
        }

        try
        {
            if (dictation.Active)
            {
                // Stop recording
                Debug.Log("Stopping dictation...");
                dictation.Deactivate();
                UpdateUIState(DictationState.Idle);
            }
            else
            {
                // Start recording (keeps previous text)
                Debug.Log("Starting dictation...");
                UpdateUIState(DictationState.Starting);
                dictation.Activate();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error toggling dictation: {e.Message}");
            UpdateUIState(DictationState.Error);
        }
    }

    // Optional: Call this to clear the accumulated text (e.g., after sending message)
    public void ClearAccumulatedText()
    {
        accumulatedText = "";
        MainThreadDispatcher.RunOnMainThread(() =>
        {
            if (userInput != null)
            {
                userInput.text = "";
                userInput.SetTextWithoutNotify(""); // Force UI update
            }
        });
    }
    
    // Sync keyboard input with accumulated text
    public void SyncKeyboardInput()
    {
        if (userInput != null)
        {
            // Update accumulated text from keyboard input
            accumulatedText = userInput.text;
        }
    }
}