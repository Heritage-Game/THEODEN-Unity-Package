using System;
using System.Collections.Generic;
using Core.Models;
using Core.ViewModels;
using UnityEngine.UIElements;
using UnityEngine;
//panel controller is the View
//the View only emits events that are then handled by the ViewModel
//The wiring place is the AppBootrstrapper
public class CodexController
{
    // Selection of the codex buttons
    public event Action<CodexItemDefinition> OnItemSelected;
    private VisualElement _root;
    private Label _titleLabel;
    private ScrollView _buttonsContainer;
    
   

    public CodexController(VisualElement root)
    {
        _root = root;
        _buttonsContainer = _root.Q<ScrollView>("CodexButtonsContainer");
    }

    public void BuildMenu(List<CodexItemDefinition> items)
    {
        foreach (var item in items)
        {
            var button = new Button();
            button.text = item.levelTitle;

            button.clicked += () =>
                OnItemSelected?.Invoke(item);

            _buttonsContainer.Add(button);
        }
    }

    
    //how to call the template
    //var template = templateForMenuButton.CloneTree();
    //buttonsContainer.Add(template);
}
