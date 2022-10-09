using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class StartupUI : MonoBehaviour
{
    public UIDocument ui;

    private TextField lobbycodeField;

    // Start is called before the first frame update
    void Start()
    {
        Application.runInBackground = true;

        var root = ui.rootVisualElement;
        lobbycodeField = root.Q<TextField>("lobbycode");

        root.Q<Button>("join").clicked += JoinButtonPressed;
        root.Q<Button>("host").clicked += HostButtonPressed;
    }

    void HostButtonPressed()
    {
        MultiplayerConfig.host = true;
        SceneManager.LoadScene("Main");
    }

    void JoinButtonPressed()
    {
        MultiplayerConfig.host = false;
        MultiplayerConfig.joiningLobbyCode = lobbycodeField.text;
        SceneManager.LoadScene("Main");
    }
}
