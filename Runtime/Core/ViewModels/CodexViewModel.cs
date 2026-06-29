using System;
using System.Collections.Generic;
using Core.Models;
using UnityEngine;

namespace Core.ViewModels
{
    public class MainMenuViewModel
    {
        public void HandleCodexItem(CodexItemDefinition codexItem)
        {
            switch (codexItem.actionType)
            {
                case MenuActionType.LoadScene:
                    Debug.Log("Loading scene" + codexItem.target);
                    break;
                    
                case MenuActionType.OpenPopUp:
                    Debug.Log("Opening popup" + codexItem.target);
                    break;
                case MenuActionType.FinishScene:
                    Debug.Log("Finishing scene" + codexItem.target);
                    break;
            }
        }
    }
}