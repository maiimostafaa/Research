using UnityEngine;
using Meta.WitAi;
using Meta.WitAi.Dictation;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Events;
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
    
    [Header("NPC Communication")]
    public NPCChatter npcChatter;            // drag your NPCChatter component here
    
    [Header("UI Feedback")]
    public TMP_Text statusText;              // Text to show status (e.g., "Listening...", "Processing...")
    
    [Header("Collision Detection")]
    [Tooltip("Drag the specific collider (2D or 3D) that should trigger dictation")]
    public Collider targetCollider3D;        // For 3D colliders (CircleCollider, BoxCollider, etc.)
    public Collider2D targetCollider2D;      // For 2D colliders (CircleCollider2D, BoxCollider2D, etc.)
    
    private string currentTranscription = ""; // stores the current transcription session
    private DictationState currentState = DictationState.Idle;
    private bool isProcessingRequest = false;

    void Start()
    {
        // Note: To adjust pause tolerance, select [BuildingBlock] Dictation in Hierarchy
        // In Inspector, expand "Voice Service" > "Runtime Configuration"
        // Adjust "Endpoint Speech Threshold" (silence detection, try 1.5-3 seconds)
        // Adjust "Max Recording Time" (maximum recording length, try 120 seconds)
        
        UpdateStatusText(DictationState.Idle);
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
        currentTranscription = ""; // Reset transcription for new session
        UpdateStatusText(DictationState.Listening);
    }

    private void OnStoppedListening()
    {
        Debug.Log("Dictation: Stopped listening");
        // Only update to idle if we're not processing a request
        if (!isProcessingRequest)
        {
            UpdateStatusText(DictationState.Idle);
        }
    }

    private void OnRequestCompleted()
    {
        Debug.Log("Dictation: Request completed");
        isProcessingRequest = false;
        // If we're not listening anymore, go to idle
        if (!dictation.Active)
        {
            UpdateStatusText(DictationState.Idle);
        }
    }

    private void OnError(string error, string message)
    {
        Debug.LogError($"Dictation Error: {error} - {message}");
        currentState = DictationState.Error;
        isProcessingRequest = false;
        UpdateStatusText(DictationState.Error);
        
        // Auto-recover after a short delay
        Invoke(nameof(ResetToIdle), 2f);
    }

    private void ResetToIdle()
    {
        if (currentState == DictationState.Error)
        {
            UpdateStatusText(DictationState.Idle);
        }
    }

    // Called while user is speaking (live updates)
    private void OnPartialTranscription(string transcription)
    {
        Debug.Log("Partial: " + transcription);
        // Don't update any UI - just store for reference
        currentTranscription = transcription;
    }

    // Called when user finishes speaking (final result)
    private void OnDictationComplete(string transcription)
    {
        Debug.Log("Complete: " + transcription);

        // Update UI on main thread
        MainThreadDispatcher.RunOnMainThread(() =>
        {
            if (!string.IsNullOrEmpty(transcription))
            {
                currentTranscription = transcription;
                
                // Send text directly to NPC model
                if (npcChatter != null)
                {
                    Debug.Log("Sending transcription to NPC: " + transcription);
                    SendToNPC(transcription);
                }
                else
                {
                    Debug.LogWarning("NPCChatter component not assigned! Cannot send transcription.");
                }
            }
            
            // Update state after receiving final transcription
            isProcessingRequest = false;
            if (!dictation.Active)
            {
                UpdateStatusText(DictationState.Idle);
            }
        });
    }
    
    private void UpdateStatusText(DictationState newState)
    {
        currentState = newState;
        
        // Update status text on main thread
        try
        {
            MainThreadDispatcher.RunOnMainThread(() => UpdateStatusTextInternal(newState));
        }
        catch
        {
            // If MainThreadDispatcher doesn't exist or fails, update directly (we're likely on main thread)
            UpdateStatusTextInternal(newState);
        }
    }
    
    private void UpdateStatusTextInternal(DictationState newState)
    {
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
    }
    
    // Helper method to send text to NPC
    private void SendToNPC(string text)
    {
        if (npcChatter == null)
        {
            Debug.LogWarning("NPCChatter not assigned! Cannot send transcription.");
            return;
        }
        
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("Empty transcription, not sending to NPC.");
            return;
        }
        
        // Send text directly to NPC using the new method (fire-and-forget)
        _ = npcChatter.SendMessageToNPC(text);
    }


    // Call this from your button's OnClick event or collision detection
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
                UpdateStatusText(DictationState.Idle);
            }
            else
            {
                // Start recording
                Debug.Log("Starting dictation...");
                currentTranscription = ""; // Reset transcription
                UpdateStatusText(DictationState.Starting);
                dictation.Activate();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error toggling dictation: {e.Message}");
        }
    }
    
    // Collision detection - only react to the target collider (3D)
    private void OnTriggerEnter(Collider other)
    {
        // Only react if this is the target collider
        if (targetCollider3D != null && other == targetCollider3D)
        {
            Debug.Log("Target collider (3D) triggered - starting dictation");
            ToggleDictation();
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Only react if this is the target collider
        if (targetCollider3D != null && collision.collider == targetCollider3D)
        {
            Debug.Log("Target collider (3D) collided - starting dictation");
            ToggleDictation();
        }
    }
    
    // Collision detection - only react to the target collider (2D)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react if this is the target collider
        if (targetCollider2D != null && other == targetCollider2D)
        {
            Debug.Log("Target collider (2D) triggered - starting dictation");
            ToggleDictation();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only react if this is the target collider
        if (targetCollider2D != null && collision.collider == targetCollider2D)
        {
            Debug.Log("Target collider (2D) collided - starting dictation");
            ToggleDictation();
        }
    }

    // Optional: Call this to clear the current transcription
    public void ClearAccumulatedText()
    {
        currentTranscription = "";
    }
}