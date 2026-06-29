using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class MediaImagePicker
{
    private const string ImagePickerVisualElementName = "ImagePicker";
    public static void TryEnhance(
        VisualElement parent,
        SerializedProperty templateProperty)
    {
        Debug.Log("TryEnhance called");

        var imagesProp = FindImagesProperty(templateProperty);
        if (imagesProp == null)
            return;
        if (parent.Q<Button>(ImagePickerVisualElementName)!=null)
            return;
        

        DrawFolderPicker(parent, imagesProp);
    }

    private static SerializedProperty FindImagesProperty(
        SerializedProperty templateProperty)
    {
        // navigation is tied to the Json structure
        var mediaProp = templateProperty
            .FindPropertyRelative("gameData")
            ?.FindPropertyRelative("pointOfInterest")
            ?.FindPropertyRelative("media");

        var imagesProp = mediaProp?.FindPropertyRelative("images");

        if (imagesProp == null)
            return null;

        if (!imagesProp.isArray ||
            imagesProp.arrayElementType != "string")
            return null;

        return imagesProp;
    }
    
    private static void DrawFolderPicker(
        VisualElement parent,
        SerializedProperty imagesProp)
    {
        Debug.Log("DrawFolderPicker CALLED");
        var button = new Button(() =>
        {
            Debug.Log("Button callback running");
            var folder = EditorUtility.OpenFolderPanel(
                "Select Images Folder",
                Application.dataPath,
                ""
            );

            if (string.IsNullOrEmpty(folder))
            {
                Debug.Log("Folder was empty (Cancel pressed)");
                return;
            }
                

            if (!folder.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog(
                    "Invalid folder",
                    "Folder must be inside Assets/",
                    "OK"
                );
                Debug.Log($"Folder not inside Assets: {folder}");
                Debug.Log($"Application.dataPath: {Application.dataPath}");
                return;
            }

            var relativePath =
                "Assets" + folder.Substring(Application.dataPath.Length);
            relativePath = relativePath.Replace("\\", "/");
            Debug.Log($"Searching in: {relativePath}");

            Debug.Log($"Searching in: {relativePath}");


            var guids = AssetDatabase.FindAssets(
                "t:Texture2D t:Sprite",
                new[] { relativePath }
            );
            Debug.Log("Found count: " + guids.Length);


            foreach (var guid in guids)
            {
                imagesProp.arraySize++;
                imagesProp
                    .GetArrayElementAtIndex(imagesProp.arraySize - 1)
                    .stringValue = guid;
            }

            imagesProp.serializedObject.ApplyModifiedProperties();
        })
        {
            name = ImagePickerVisualElementName,
            text = "Add images from folder"
        };

        parent.Add(button);
    }

}

