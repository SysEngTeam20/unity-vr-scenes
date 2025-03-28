using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ubiq.Samples;

/// <summary>
/// Manages the joincode input UI for VR, using Ubiq's keyboard
/// </summary>
public class JoincodeInputManager : MonoBehaviour
{
    [Header("References")]
    public VRViewerManager vrViewerManager;
    public PortaltServerConfig serverConfig;
    public ScriptableObject ubiqServerConfig; // Direct reference to the Ubiq ServerConfig asset
    
    [Header("UI Elements")]
    public TMP_InputField joincodeInputField;
    public TMP_InputField serverIpInputField;
    public TMP_InputField serverPortInputField;
    public TMP_InputField ubiqServerIpInputField;
    public TMP_InputField ubiqServerPortInputField;
    public Keyboard keyboard;
    public Button submitButton;
    public Button cancelButton;
    public GameObject joincodePanel;
    
    [Header("Settings")]
    public string defaultJoinMessage = "Enter Join Code";
    public string defaultServerIpMessage = "Enter Server IP";
    public string defaultServerPortMessage = "Enter Port";
    public string defaultUbiqServerIpMessage = "Enter Ubiq Server IP";
    public string defaultUbiqServerPortMessage = "Enter Ubiq Port";
    public int maxCodeLength = 12;
    public int maxIpLength = 45; // IPv6 max length
    public int maxPortLength = 5; // Max 65535
    
    [Header("Ubiq Settings")]
    [Tooltip("Default Ubiq server IP if none is configured")]
    public string defaultUbiqServerIp = "127.0.0.1";
    [Tooltip("Default Ubiq server port if none is configured")]
    public int defaultUbiqServerPort = 8009;
    
    private bool isInitialized = false;
    private TMP_InputField activeInputField;
    
    private void Awake()
    {
        if (joincodePanel != null)
        {
            joincodePanel.SetActive(true);
        }
    }
    
    private void Start()
    {
        Initialize();
    }
    
    public void Initialize()
    {
        if (isInitialized) return;
        
        // Find references if they weren't set in the inspector
        if (vrViewerManager == null)
        {
            vrViewerManager = FindObjectOfType<VRViewerManager>();
        }
        
        if (serverConfig == null && vrViewerManager != null)
        {
            serverConfig = vrViewerManager.serverConfig;
        }
        
        // Try to find Ubiq server config if not assigned
        if (ubiqServerConfig == null)
        {
            // Try to load the asset from Resources folder or find it in the editor
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("ServerConfig");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                ubiqServerConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                Debug.Log($"Found Ubiq ServerConfig asset at: {path}");
            }
#endif
            
            if (ubiqServerConfig == null)
            {
                Debug.LogWarning("Could not find Ubiq ServerConfig asset. Please assign it manually.");
            }
        }
        
        // Set up the keyboard input
        if (keyboard != null)
        {
            keyboard.OnInput.AddListener(OnKeyboardInput);
        }
        
        // Set up button events
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitPressed);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelPressed);
        }

        // Set up input field selection events
        if (joincodeInputField != null)
        {
            joincodeInputField.onSelect.AddListener((s) => SetActiveInputField(joincodeInputField));
            // Set as initially active
            SetActiveInputField(joincodeInputField);
        }
        
        if (serverIpInputField != null)
        {
            serverIpInputField.onSelect.AddListener((s) => SetActiveInputField(serverIpInputField));
        }
        
        if (serverPortInputField != null)
        {
            serverPortInputField.onSelect.AddListener((s) => SetActiveInputField(serverPortInputField));
        }
        
        if (ubiqServerIpInputField != null)
        {
            ubiqServerIpInputField.onSelect.AddListener((s) => SetActiveInputField(ubiqServerIpInputField));
        }
        
        if (ubiqServerPortInputField != null)
        {
            ubiqServerPortInputField.onSelect.AddListener((s) => SetActiveInputField(ubiqServerPortInputField));
        }
        
        // Pre-fill with existing values if available
        if (serverConfig != null)
        {
            if (serverIpInputField != null)
            {
                serverIpInputField.text = serverConfig.serverIp;
            }
            
            if (serverPortInputField != null)
            {
                serverPortInputField.text = serverConfig.serverPort.ToString();
            }
        }
        
        // Pre-fill Ubiq server values if available
        PreFillUbiqSettings();
        
        isInitialized = true;
    }
    
    private void PreFillUbiqSettings()
    {
        // Default values
        string ip = defaultUbiqServerIp;
        int port = defaultUbiqServerPort;
        
        // Try to get current values from the Ubiq server config
        if (ubiqServerConfig != null)
        {
            // Use reflection to safely get properties from the ScriptableObject
            System.Type configType = ubiqServerConfig.GetType();
            
            // Try to get the sendToIp property
            System.Reflection.FieldInfo ipField = configType.GetField("sendToIp");
            if (ipField != null)
            {
                string configIp = ipField.GetValue(ubiqServerConfig) as string;
                if (!string.IsNullOrEmpty(configIp))
                {
                    ip = configIp;
                    Debug.Log($"Read Ubiq server IP from config: {ip}");
                }
            }
            
            // Try to get the sendToPort property
            System.Reflection.FieldInfo portField = configType.GetField("sendToPort");
            if (portField != null)
            {
                object portValue = portField.GetValue(ubiqServerConfig);
                if (portValue != null)
                {
                    // Handle different port field types
                    if (portValue is string portStr)
                    {
                        if (int.TryParse(portStr, out int parsedPort))
                        {
                            port = parsedPort;
                        }
                    }
                    else if (portValue is int intPort)
                    {
                        port = intPort;
                    }
                    Debug.Log($"Read Ubiq server port from config: {port}");
                }
            }
        }
        else
        {
            Debug.LogWarning("No Ubiq server config found, using default values");
        }
        
        // Set the UI field values
        if (ubiqServerIpInputField != null)
        {
            ubiqServerIpInputField.text = ip;
        }
        
        if (ubiqServerPortInputField != null)
        {
            ubiqServerPortInputField.text = port.ToString();
        }
    }
    
    private void SetActiveInputField(TMP_InputField inputField)
    {
        activeInputField = inputField;
        
        // Visual feedback that this field is selected could be added here
        // For example, highlighting the active field
    }
    
    public void ShowJoincodeUI()
    {
        if (!isInitialized)
        {
            Initialize();
        }
        
        if (joincodePanel != null)
        {
            joincodePanel.SetActive(true);
        }
        
        // Set placeholders and clear fields if needed
        if (joincodeInputField != null)
        {
            joincodeInputField.placeholder.GetComponent<TextMeshProUGUI>().text = defaultJoinMessage;
        }
        
        if (serverIpInputField != null)
        {
            serverIpInputField.placeholder.GetComponent<TextMeshProUGUI>().text = defaultServerIpMessage;
        }
        
        if (serverPortInputField != null)
        {
            serverPortInputField.placeholder.GetComponent<TextMeshProUGUI>().text = defaultServerPortMessage;
        }
        
        if (ubiqServerIpInputField != null)
        {
            ubiqServerIpInputField.placeholder.GetComponent<TextMeshProUGUI>().text = defaultUbiqServerIpMessage;
        }
        
        if (ubiqServerPortInputField != null)
        {
            ubiqServerPortInputField.placeholder.GetComponent<TextMeshProUGUI>().text = defaultUbiqServerPortMessage;
        }
        
        // Pre-fill with existing values
        if (serverConfig != null)
        {
            if (serverIpInputField != null)
            {
                serverIpInputField.text = serverConfig.serverIp;
            }
            
            if (serverPortInputField != null)
            {
                serverPortInputField.text = serverConfig.serverPort.ToString();
            }
        }
        
        // Pre-fill Ubiq server values
        PreFillUbiqSettings();
        
        // Focus the first field
        SetActiveInputField(joincodeInputField);
    }
    
    public void HideJoincodeUI()
    {
        if (joincodePanel != null)
        {
            joincodePanel.SetActive(false);
        }
    }
    
    private void OnKeyboardInput(KeyCode keyCode)
    {
        if (activeInputField == null) return;
        
        // Handle special keys
        if (keyCode == KeyCode.Backspace)
        {
            // Delete last character
            if (activeInputField.text.Length > 0)
            {
                activeInputField.text = activeInputField.text.Substring(0, activeInputField.text.Length - 1);
            }
        }
        else if (keyCode == KeyCode.Return)
        {
            // Submit the code
            OnSubmitPressed();
        }
        else if (keyCode == KeyCode.Escape)
        {
            // Cancel
            OnCancelPressed();
        }
        else if (keyCode == KeyCode.Tab)
        {
            // Cycle through input fields
            CycleInputFields();
        }
        else
        {
            int maxLength = maxCodeLength;
            bool isValid = false;
            
            // Determine max length and valid characters based on active field
            if (activeInputField == joincodeInputField)
            {
                maxLength = maxCodeLength;
                char character = (char)keyCode;
                isValid = char.IsLetterOrDigit(character) || character == '-';
            }
            else if (activeInputField == serverIpInputField || activeInputField == ubiqServerIpInputField)
            {
                maxLength = maxIpLength;
                char character = (char)keyCode;
                isValid = char.IsLetterOrDigit(character) || character == '.' || character == ':';
            }
            else if (activeInputField == serverPortInputField || activeInputField == ubiqServerPortInputField)
            {
                maxLength = maxPortLength;
                char character = (char)keyCode;
                isValid = char.IsDigit(character);
            }
            
            // Add character if valid and within length limit
            if (isValid && activeInputField.text.Length < maxLength)
            {
                activeInputField.text += (char)keyCode;
            }
        }
    }
    
    private void CycleInputFields()
    {
        if (activeInputField == joincodeInputField && serverIpInputField != null)
        {
            SetActiveInputField(serverIpInputField);
        }
        else if (activeInputField == serverIpInputField && serverPortInputField != null)
        {
            SetActiveInputField(serverPortInputField);
        }
        else if (activeInputField == serverPortInputField && ubiqServerIpInputField != null)
        {
            SetActiveInputField(ubiqServerIpInputField);
        }
        else if (activeInputField == ubiqServerIpInputField && ubiqServerPortInputField != null)
        {
            SetActiveInputField(ubiqServerPortInputField);
        }
        else
        {
            SetActiveInputField(joincodeInputField);
        }
    }
    
    private void OnSubmitPressed()
    {
        if (serverConfig == null)
        {
            Debug.LogError("Server config not found!");
            return;
        }
        
        bool configChanged = false;
        
        // Process join code
        if (joincodeInputField != null)
        {
            string joincode = joincodeInputField.text.Trim();
            if (!string.IsNullOrEmpty(joincode))
            {
                Debug.Log($"Setting pairing code to: {joincode}");
                serverConfig.pairingCode = joincode;
                configChanged = true;
            }
        }
        
        // Process server IP
        if (serverIpInputField != null)
        {
            string serverIp = serverIpInputField.text.Trim();
            if (!string.IsNullOrEmpty(serverIp))
            {
                Debug.Log($"Setting server IP to: {serverIp}");
                serverConfig.serverIp = serverIp;
                configChanged = true;
            }
        }
        
        // Process port
        if (serverPortInputField != null)
        {
            string portText = serverPortInputField.text.Trim();
            if (!string.IsNullOrEmpty(portText) && int.TryParse(portText, out int port))
            {
                if (port > 0 && port <= 65535)
                {
                    Debug.Log($"Setting server port to: {port}");
                    serverConfig.serverPort = port;
                    configChanged = true;
                }
                else
                {
                    Debug.LogWarning("Invalid port number. Must be between 1-65535.");
                }
            }
        }
        
        // Process Ubiq server settings
        if (ubiqServerIpInputField != null && ubiqServerPortInputField != null)
        {
            string ip = ubiqServerIpInputField.text.Trim();
            string portText = ubiqServerPortInputField.text.Trim();
            
            if (!string.IsNullOrEmpty(ip) && 
                !string.IsNullOrEmpty(portText) && 
                int.TryParse(portText, out int port) && 
                port > 0 && port <= 65535)
            {
                string address = $"{ip}:{port}";
                Debug.Log($"Setting Ubiq server address to: {address}");
                
                // Save Ubiq address to PlayerPrefs for persistence
                PlayerPrefs.SetString("UbiqServerAddress", address);
                PlayerPrefs.Save();
                
                // Notify any components that need to update their Ubiq connection
                UpdateUbiqConnection(address);
                
                configChanged = true;
            }
        }
        
        if (configChanged)
        {
            // Auto-load if VRViewerManager is available
            if (vrViewerManager != null && !string.IsNullOrEmpty(vrViewerManager.activityIdToLoad))
            {
                vrViewerManager.LoadActivity();
            }
            
            // Hide UI
            HideJoincodeUI();
        }
        else
        {
            Debug.LogWarning("No valid configuration changes were made.");
        }
    }
    
    private void UpdateUbiqConnection(string address)
    {
        if (string.IsNullOrEmpty(address) || !address.Contains(":"))
        {
            Debug.LogError("Invalid address format. Expected 'ip:port'");
            return;
        }
        
        string[] parts = address.Split(':');
        if (parts.Length < 2)
        {
            Debug.LogError("Invalid address format. Missing port number.");
            return;
        }
        
        string ip = parts[0];
        if (!int.TryParse(parts[1], out int port))
        {
            Debug.LogError("Invalid port number format.");
            return;
        }
        
        Debug.Log($"Updating Ubiq server connection to {ip}:{port}");
        
        // Update the Ubiq server config if available
        if (ubiqServerConfig != null)
        {
            try
            {
                System.Type configType = ubiqServerConfig.GetType();
                
                // Update the sendToIp property
                System.Reflection.FieldInfo ipField = configType.GetField("sendToIp");
                if (ipField != null)
                {
                    ipField.SetValue(ubiqServerConfig, ip);
                    Debug.Log($"Updated Ubiq server IP to: {ip}");
                }
                else
                {
                    Debug.LogWarning("Could not find sendToIp field in Ubiq server config");
                }
                
                // Update the sendToPort property
                System.Reflection.FieldInfo portField = configType.GetField("sendToPort");
                if (portField != null)
                {
                    // Check if the port field is a string or int
                    if (portField.FieldType == typeof(string))
                    {
                        portField.SetValue(ubiqServerConfig, port.ToString());
                    }
                    else if (portField.FieldType == typeof(int))
                    {
                        portField.SetValue(ubiqServerConfig, port);
                    }
                    Debug.Log($"Updated Ubiq server port to: {port}");
                }
                else
                {
                    Debug.LogWarning("Could not find sendToPort field in Ubiq server config");
                }
                
                // Mark the asset as dirty to save changes
                UnityEditor.EditorUtility.SetDirty(ubiqServerConfig);
                
                Debug.Log("Ubiq server config updated successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating Ubiq server config: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("No Ubiq server config asset found, changes won't persist");
        }
        
        // Broadcast the address change to other components as a fallback
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
        {
            rootObject.BroadcastMessage("SetUbiqAddress", address, SendMessageOptions.DontRequireReceiver);
        }
    }
    
    private void OnCancelPressed()
    {
        HideJoincodeUI();
    }
    
    private void OnDestroy()
    {
        // Clean up event listeners
        if (keyboard != null)
        {
            keyboard.OnInput.RemoveListener(OnKeyboardInput);
        }
        
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(OnSubmitPressed);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelPressed);
        }
        
        if (joincodeInputField != null)
        {
            joincodeInputField.onSelect.RemoveAllListeners();
        }
        
        if (serverIpInputField != null)
        {
            serverIpInputField.onSelect.RemoveAllListeners();
        }
        
        if (serverPortInputField != null)
        {
            serverPortInputField.onSelect.RemoveAllListeners();
        }
        
        if (ubiqServerIpInputField != null)
        {
            ubiqServerIpInputField.onSelect.RemoveAllListeners();
        }
        
        if (ubiqServerPortInputField != null)
        {
            ubiqServerPortInputField.onSelect.RemoveAllListeners();
        }
    }
}