using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using TMPro;
using UnityEngine;
using Meta.WitAi.TTS.Utilities;

public class NPCChatter : MonoBehaviour
{
    string apiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    [Header("Voice Output")]
    public TTSSpeaker speaker;   // drag your TTSSpeaker GameObject here in Inspector

    [Header("Animation")]
    public Animator animator;    // drag your NPC's Animator here in Inspector
    public string isTalkingParameterName = "isTalking"; // Name of the Animator bool parameter

    [Header("UI")]
    public TMP_Text npcBubbleText;
    public TMP_InputField userInput;
    public DictationInput dictationInput;

    [Header("Model & Style")]
    public string model = "gpt-4o-mini";
    [TextArea(3,8)]
    public string systemPrompt = "You are a friendly in-world NPC. Answer briefly and helpfully.";

    readonly List<Message> history = new();
    private StringBuilder responseBuilder = new StringBuilder();
    
    // Public boolean to track talking state
    [Header("Debug")]
    [SerializeField] private bool isTalking = false;

    void Start()
    {
        Debug.Log("[NPCChatter] Start() called");
        history.Clear();
        history.Add(new Message(Role.System, systemPrompt));
        if (npcBubbleText)
            npcBubbleText.text = "Great work exploring the five pillars. Let's reflect through the Knowledge pillar, which centers awareness, truth-telling, and sharing wisdom. What's one idea or moment from today that really stuck with you?";
        
        // Initialize animator state
        SetTalkingState(false, "Start()");
    }

    public async void OnSendClicked()
    {
        Debug.Log("[NPCChatter] OnSendClicked() called");
        var text = userInput ? userInput.text.Trim() : null;
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("[NPCChatter] Empty text, returning early");
            return;
        }

        Debug.Log($"[NPCChatter] Processing user input: {text}");

        if (userInput)
        {
            userInput.text = "";
            userInput.SetTextWithoutNotify("");
        }

        if (dictationInput != null)
            dictationInput.ClearAccumulatedText();

        await SendMessageToNPC(text);
    }
    
    // New method to send message directly from string (for dictation input)
    public async Task SendMessageToNPC(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("[NPCChatter] Empty text, returning early");
            return;
        }

        Debug.Log($"[NPCChatter] Processing user input: {text}");

        responseBuilder.Clear();
        
        MainThreadDispatcher.RunOnMainThread(() =>
        {
            if (npcBubbleText) npcBubbleText.text = "";
            Debug.Log("[NPCChatter] Cleared NPC bubble text");
        });

        var request = new ChatRequest(history, model: model, temperature: 0.6f);
        var api = new OpenAIClient(new OpenAIAuthentication(apiKey));

        // Set talking state BEFORE response begins
        Debug.Log("[NPCChatter] Starting response - setting isTalking to true");
        SetTalkingState(true, "SendMessageToNPC - before streaming starts");

        var response = await api.ChatEndpoint.StreamCompletionAsync(
            request,
            async partial =>
            {
                var delta = partial.FirstChoice?.Delta?.ToString();
                if (!string.IsNullOrEmpty(delta))
                {
                    responseBuilder.Append(delta);
                    string currentText = responseBuilder.ToString();

                    MainThreadDispatcher.RunOnMainThread(() =>
                    {
                        if (npcBubbleText)
                        {
                            npcBubbleText.text = currentText;
                            Debug.Log($"[NPCChatter] Updated bubble text (length: {currentText.Length})");
                        }
                    });
                }
                await Task.CompletedTask;
            });

        Debug.Log("[NPCChatter] Streaming completed");
        history.Add(response.FirstChoice.Message);

        string finalResponse = responseBuilder.ToString();
        Debug.Log($"[NPCChatter] Final response received (length: {finalResponse.Length}): {finalResponse.Substring(0, Mathf.Min(50, finalResponse.Length))}...");

        // Handle TTS if available, otherwise just use text completion
        bool ttsAvailable = speaker != null && !string.IsNullOrEmpty(finalResponse);
        
        if (ttsAvailable)
        {
            Debug.Log("[NPCChatter] TTS Speaker available - starting speech");
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                speaker.Stop();
                speaker.Speak(finalResponse);
                Debug.Log("[NPCChatter] TTS Speak() called");
            });
            
            // Wait for TTS to complete
            StartCoroutine(WaitForSpeechEnd());
        }
        else
        {
            Debug.Log("[NPCChatter] TTS Speaker NOT available - using text completion timing");
            // If TTS not available, wait a bit after text completes, then stop talking
            StartCoroutine(WaitForTextCompletion());
        }
        
        if (history.Count > 20)
            history.RemoveRange(1, history.Count - 20);
    }

    private System.Collections.IEnumerator WaitForSpeechEnd()
    {
        Debug.Log("[NPCChatter] WaitForSpeechEnd() coroutine started");
        
        if (speaker == null)
        {
            Debug.LogWarning("[NPCChatter] Speaker is null in WaitForSpeechEnd - falling back to text completion");
            StartCoroutine(WaitForTextCompletion());
            yield break;
        }

        // Wait while speaker is speaking
        float timeout = 30f; // Max 30 seconds
        float elapsed = 0f;
        
        while (speaker.IsSpeaking && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning("[NPCChatter] TTS timeout reached - stopping talking state");
        }
        else
        {
            Debug.Log("[NPCChatter] TTS completed - setting isTalking to false");
        }

        SetTalkingState(false, "WaitForSpeechEnd - TTS completed");
    }

    private System.Collections.IEnumerator WaitForTextCompletion()
    {
        Debug.Log("[NPCChatter] WaitForTextCompletion() coroutine started");
        
        // Wait a short delay after text streaming completes to allow reading
        // Adjust this delay based on response length
        float baseDelay = 2f; // Base delay in seconds
        float additionalDelay = Mathf.Min(responseBuilder.Length * 0.05f, 5f); // Up to 5 more seconds based on length
        float totalDelay = baseDelay + additionalDelay;
        
        Debug.Log($"[NPCChatter] Waiting {totalDelay} seconds before stopping talking (text length: {responseBuilder.Length})");
        
        yield return new WaitForSeconds(totalDelay);
        
        Debug.Log("[NPCChatter] Text completion delay finished - setting isTalking to false");
        SetTalkingState(false, "WaitForTextCompletion - delay completed");
    }

    private void SetTalkingState(bool talking, string source)
    {
        if (isTalking == talking)
        {
            Debug.Log($"[NPCChatter] isTalking already {talking} (from {source}) - skipping update");
            return;
        }

        isTalking = talking;
        Debug.Log($"[NPCChatter] Setting isTalking to {talking} (source: {source})");

        if (animator != null)
        {
            if (animator.isActiveAndEnabled)
            {
                animator.SetBool(isTalkingParameterName, talking);
                Debug.Log($"[NPCChatter] Animator.SetBool('{isTalkingParameterName}', {talking}) called successfully");
            }
            else
            {
                Debug.LogWarning($"[NPCChatter] Animator is not active/enabled - cannot set bool");
            }
        }
        else
        {
            Debug.LogWarning("[NPCChatter] Animator is null - cannot set isTalking parameter");
        }
    }

    // Public getter for isTalking
    public bool IsTalking
    {
        get { return isTalking; }
    }
}
