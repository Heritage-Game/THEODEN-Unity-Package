using System;
using UnityEngine.UIElements;

public class SecondPanelController
{
    public event Action OnBackClicked;

    public SecondPanelController(VisualElement root)
    {
        var button = root.Q<Button>("buttonBack");
        button.clicked += () => OnBackClicked?.Invoke();
    }
}