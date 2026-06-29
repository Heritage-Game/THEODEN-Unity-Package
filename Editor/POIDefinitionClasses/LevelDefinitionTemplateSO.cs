using UnityEngine;

[CreateAssetMenu(fileName = "LevelDefinitionTemplateSO", menuName = "Scriptable Objects/LevelDefinitionTemplateSO")]
public class LevelDefinitionTemplateSO : ScriptableObject
{
    [SerializeReference] public LevelTemplateBase template;
}
