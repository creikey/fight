using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class OnError : MonoBehaviour
{
    public UIDocument ui;
    public GameSession gs;

    private VisualElement root;
    private Label errlabel;
    // Start is called before the first frame update
    void Start()
    {
        root = ui.rootVisualElement;
        root.visible = false;
        root.Q<Button>("back").clicked += BackPressed;
        errlabel = root.Q<Label>("error");
    }

    private void Update()
    {
        root.visible = gs.State == GameSession.StateType.Error;
        errlabel.text = gs.error;
    }

    void BackPressed()
    {
        SceneManager.LoadScene("Startup");
    }

}
