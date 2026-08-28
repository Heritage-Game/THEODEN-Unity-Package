using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Theoden.Editor.POIDefinitionClasses
{
    /// <summary>
    /// This class is a helper class used to handle the ReferencedProperty inside the Scriptable Object.
    /// It handles the cases in which no template is selected or the template property is not recognized, and it
    /// results to null.
    /// In case the "template" property inside the ScriptableObject is a valid reference, the PopertyField is added
    /// to the VisualElement "parent" and the template fields are diplayed in the window.
    /// <see cref="LevelDefinitionTemplateSO"/>
    /// </summary>
    public static class TemplateDrawer
    {
        private const string TemplateVisualElementName = "templateDrawerRoot";
        public static void DrawTemplate(
            VisualElement parent,
            SerializedProperty templateProperty)
        {
            if (templateProperty == null)
            {
                CheckOrCreateContainer(parent).Add(new Label("Template property is null"));
                return;
            }

            if (templateProperty.managedReferenceValue == null)
            {
                CheckOrCreateContainer(parent).Add(new Label("No template selected"));
                return;
            }

            var container = CheckOrCreateContainer(parent);
            container.Clear();
            
            
            
            // THIS LINE:
            //Take this managed reference --> look at its runtime concrete type
            //--> draw everything inside it recursively
            var field = new PropertyField(templateProperty);
            field.Bind(templateProperty.serializedObject);
            container.Add(field);

            if (container.parent == null)
                parent.Add(container);
            
            //MediaImagePicker.TryEnhance(container, templateProperty);
        }

        /// <summary>
        /// This method ensures that the VisualElement acting as a container for the fields of the template is only
        /// build when the parent VisualElement does not already contain it.
        /// When the template instance changes the container is cleared and the fields drawn again, in any other case
        /// the container VisualElement is kept as it is:
        /// </summary>
        /// <param name="parent"> The parent VisualElement </param>
        /// <returns></returns>
        private static VisualElement CheckOrCreateContainer(VisualElement parent)
        {
            var container = parent.Q<VisualElement>(TemplateVisualElementName);
            if(container != null)
                return container;
            
            container = new VisualElement
            {
                name = TemplateVisualElementName,
                style =
                {
                    flexDirection = FlexDirection.Column
                }
            };
            return container;
        }
    }
}