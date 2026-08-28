using System.Collections.Generic;
using System.Linq;
using Addressing;
using Theoden.Editor.Export;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool used to configure the project map and place POI pins.
/// </summary>
public sealed class TheodenMapEditorWindow : EditorWindow
{
    private const float MinEditorZoom = 1f;
    private const float MaxEditorZoom = 5f;

    [SerializeField]
    private DefaultAsset projectFolder;

    [SerializeField]
    private float editorZoom = MinEditorZoom;

    [SerializeField]
    private Vector2 mapScrollPosition = Vector2.zero;

    private TheodenProjectContext projectContext;
    private string projectLoadError;
    private int selectedPoiIndex;

    private TheodenProjectConfig ProjectConfig =>
        projectContext?.theodenProjectConfig;

    [MenuItem("THEODEN/5. Map Editor")]
    public static void Open()
    {
        TheodenMapEditorWindow window =
            GetWindow<TheodenMapEditorWindow>();

        window.titleContent = new GUIContent("THEODEN Map");
        window.minSize = new Vector2(650f, 600f);
        window.Show();
    }

    private void OnEnable()
    {
        projectContext = null;
        projectLoadError = null;

        if (projectFolder != null)
        {
            LoadSelectedProject();
        }
    }

    private void OnGUI()
    {
        DrawProjectFolderField();

        if (!string.IsNullOrWhiteSpace(projectLoadError))
        {
            EditorGUILayout.HelpBox(
                projectLoadError,
                MessageType.Error
            );

            return;
        }

        if (projectContext == null || ProjectConfig == null)
        {
            EditorGUILayout.HelpBox(
                "Select the root folder of a THEODEN project.",
                MessageType.Info
            );

            return;
        }

        EnsureMapDefinitionExists();

        MapDefinition mapDefinition = ProjectConfig.mapDefinition;

        if (mapDefinition == null)
        {
            EditorGUILayout.HelpBox(
                "The MapDefinition asset could not be created or loaded.",
                MessageType.Error
            );

            return;
        }

        DrawMapImageField(mapDefinition);
        DrawEditorZoomControls(mapDefinition.MapImage != null);

        POIRegistryEntry selectedPoi =
            DrawPoiSelection(mapDefinition);

        DrawSaveButton(mapDefinition);

        EditorGUILayout.Space(8f);

        if (mapDefinition.MapImage == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a map image before placing POI pins.",
                MessageType.Info
            );

            return;
        }

        DrawMapCanvas(mapDefinition, selectedPoi);
    }

    /// <summary>
    /// Draws the THEODEN project root folder selector and loads
    /// the corresponding project context.
    /// </summary>
    private void DrawProjectFolderField()
    {
        EditorGUI.BeginChangeCheck();

        DefaultAsset newProjectFolder =
            (DefaultAsset)EditorGUILayout.ObjectField(
                "Project Folder",
                projectFolder,
                typeof(DefaultAsset),
                false
            );

        if (EditorGUI.EndChangeCheck())
        {
            projectFolder = newProjectFolder;
            projectContext = null;
            projectLoadError = null;
            selectedPoiIndex = 0;

            ResetMapView();
            LoadSelectedProject();
        }

        if (projectFolder == null)
        {
            return;
        }

        string projectFolderPath =
            AssetDatabase.GetAssetPath(projectFolder);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField(
                "Project Path",
                projectFolderPath
            );

            if (ProjectConfig != null)
            {
                EditorGUILayout.TextField(
                    "Application",
                    ProjectConfig.applicationName
                );
            }
        }

        EditorGUILayout.Space(6f);
    }

    /// <summary>
    /// Loads and validates the project selected through its root folder.
    /// </summary>
    private void LoadSelectedProject()
    {
        if (projectFolder == null)
        {
            return;
        }

        string projectFolderPath =
            AssetDatabase.GetAssetPath(projectFolder);

        if (!AssetDatabase.IsValidFolder(projectFolderPath))
        {
            projectLoadError =
                "The selected asset is not a valid project folder.";

            return;
        }

        if (!TheodenProjectConfigLoader.TryLoadProjectContext(
                projectFolderPath,
                out projectContext,
                out string error))
        {
            projectContext = null;
            projectLoadError = error;
            return;
        }

        projectLoadError = null;
        EnsureMapDefinitionExists();
    }

    /// <summary>
    /// Draws the map sprite selector.
    /// </summary>
    private void DrawMapImageField(MapDefinition mapDefinition)
    {
        EditorGUI.BeginChangeCheck();

        Sprite newMapImage =
            (Sprite)EditorGUILayout.ObjectField(
                "Map Image",
                mapDefinition.MapImage,
                typeof(Sprite),
                false
            );

        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        Undo.RecordObject(mapDefinition, "Change map image");

        mapDefinition.SetMapImage(newMapImage);
        EditorUtility.SetDirty(mapDefinition);

        ResetMapView();
    }

    /// <summary>
    /// Draws the controls used to zoom and reset the map preview.
    /// </summary>
    private void DrawEditorZoomControls(bool hasMapImage)
    {
        using (new EditorGUI.DisabledScope(!hasMapImage))
        {
            EditorGUILayout.BeginHorizontal();

            float newZoom = EditorGUILayout.Slider(
                "Editor Zoom",
                editorZoom,
                MinEditorZoom,
                MaxEditorZoom
            );

            if (!Mathf.Approximately(newZoom, editorZoom))
            {
                editorZoom = newZoom;
                Repaint();
            }

            if (GUILayout.Button(
                    "Reset View",
                    GUILayout.Width(90f)))
            {
                ResetMapView();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        if (hasMapImage)
        {
            EditorGUILayout.HelpBox(
                "Map controls:\n" +
                "- Zoom: use the mouse wheel or the Editor Zoom slider.\n" +
                "- Move: use the horizontal and vertical scrollbars.\n" +
                "- Place or move a pin: select a POI and left-click on the map.",
                MessageType.Info
            );
        }
    }

    /// <summary>
    /// Draws the POI selector and removal controls.
    /// </summary>
    private POIRegistryEntry DrawPoiSelection(
        MapDefinition mapDefinition
    )
    {
        if (projectContext.poiRegistry == null)
        {
            EditorGUILayout.HelpBox(
                "The project does not contain a POIRegistry.",
                MessageType.Error
            );

            return null;
        }

        IReadOnlyList<POIRegistryEntry> pois =
            projectContext.poiRegistry.Pois;

        if (pois.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "The POIRegistry does not contain any POIs.",
                MessageType.Warning
            );

            return null;
        }

        selectedPoiIndex = Mathf.Clamp(
            selectedPoiIndex,
            0,
            pois.Count - 1
        );

        string[] options = pois
            .Select(poi =>
                $"{poi.DisplayName} ({poi.PoiId})")
            .ToArray();

        selectedPoiIndex = EditorGUILayout.Popup(
            "Selected POI",
            selectedPoiIndex,
            options
        );

        POIRegistryEntry selectedPoi = pois[selectedPoiIndex];

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.HelpBox(
            mapDefinition.ContainsPoi(selectedPoi.PoiId)
                ? "Click on the map to move this pin."
                : "Click on the map to place this pin.",
            MessageType.None
        );

        using (new EditorGUI.DisabledScope(
                   !mapDefinition.ContainsPoi(selectedPoi.PoiId)))
        {
            if (GUILayout.Button(
                    "Remove Pin",
                    GUILayout.Width(110f),
                    GUILayout.Height(38f)))
            {
                Undo.RecordObject(
                    mapDefinition,
                    "Remove map pin"
                );

                mapDefinition.RemovePin(selectedPoi.PoiId);
                EditorUtility.SetDirty(mapDefinition);
            }
        }

        EditorGUILayout.EndHorizontal();

        return selectedPoi;
    }

    /// <summary>
    /// Draws the command that persists pending asset changes.
    /// </summary>
    private void DrawSaveButton(
        MapDefinition mapDefinition)
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(
                "Save Map Configuration",
                GUILayout.Width(190f),
                GUILayout.Height(28f)))
        {
            SaveMapConfiguration(mapDefinition);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void SaveMapConfiguration(
        MapDefinition mapDefinition)
    {
        if (ProjectConfig == null ||
            projectFolder == null)
        {
            EditorUtility.DisplayDialog(
                "Map Configuration",
                "No valid THEODEN project is loaded.",
                "OK"
            );

            return;
        }

        EditorUtility.SetDirty(mapDefinition);
        AssetDatabase.SaveAssets();

        string projectRootFolderPath =
            AssetDatabase.GetAssetPath(projectFolder);

        if (!MapAddressablesSetupService
                .SetupMapDefinition(
                    mapDefinition,
                    ProjectConfig.projectId,
                    projectRootFolderPath,
                    out string error))
        {
            EditorUtility.DisplayDialog(
                "Map Addressables Setup Failed",
                error,
                "OK"
            );

            return;
        }

        string address =
            TheodenAddressablesNaming
                .GetMapDefinitionAddress(
                    ProjectConfig.projectId
                );

        Debug.Log(
            "[TheodenMapEditorWindow] Map saved and registered " +
            $"as Addressable: {address}"
        );
    }
    
    /// <summary>
    /// Draws the zoomable map inside a bidirectional scroll view.
    /// </summary>
    private void DrawMapCanvas(
        MapDefinition mapDefinition,
        POIRegistryEntry selectedPoi
    )
    {
        Rect canvasRect = GUILayoutUtility.GetRect(
            GUIContent.none,
            GUIStyle.none,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true),
            GUILayout.MinHeight(200f)
        );

        EditorGUI.DrawRect(
            canvasRect,
            new Color(0.16f, 0.16f, 0.16f)
        );

        const float outerPadding = 8f;
        const float contentPadding = 20f;
        const float scrollbarSpace = 16f;

        Rect viewportRect = new Rect(
            canvasRect.x + outerPadding,
            canvasRect.y + outerPadding,
            Mathf.Max(
                1f,
                canvasRect.width - outerPadding * 2f
            ),
            Mathf.Max(
                1f,
                canvasRect.height - outerPadding * 2f
            )
        );

        HandleMapZoomWithWheel(viewportRect);

        Rect baseAvailableRect = new Rect(
            contentPadding,
            contentPadding,
            Mathf.Max(
                1f,
                viewportRect.width -
                scrollbarSpace -
                contentPadding * 2f
            ),
            Mathf.Max(
                1f,
                viewportRect.height -
                scrollbarSpace -
                contentPadding * 2f
            )
        );

        Rect baseMapRect = CalculateAspectFitRect(
            baseAvailableRect,
            mapDefinition.MapImage
        );

        float displayedMapWidth =
            baseMapRect.width * editorZoom;

        float displayedMapHeight =
            baseMapRect.height * editorZoom;

        float usableViewportWidth = Mathf.Max(
            1f,
            viewportRect.width - scrollbarSpace
        );

        float usableViewportHeight = Mathf.Max(
            1f,
            viewportRect.height - scrollbarSpace
        );

        float contentWidth = Mathf.Max(
            usableViewportWidth,
            displayedMapWidth + contentPadding * 2f
        );

        float contentHeight = Mathf.Max(
            usableViewportHeight,
            displayedMapHeight + contentPadding * 2f
        );

        Rect contentRect = new Rect(
            0f,
            0f,
            contentWidth,
            contentHeight
        );

        Rect displayedMapRect = new Rect(
            (contentWidth - displayedMapWidth) * 0.5f,
            (contentHeight - displayedMapHeight) * 0.5f,
            displayedMapWidth,
            displayedMapHeight
        );

        mapScrollPosition = GUI.BeginScrollView(
            viewportRect,
            mapScrollPosition,
            contentRect,
            true,
            true
        );

        DrawSprite(
            displayedMapRect,
            mapDefinition.MapImage
        );

        DrawPins(
            displayedMapRect,
            mapDefinition,
            selectedPoi
        );

        GUI.EndScrollView();

        Rect interactiveViewportRect = new Rect(
            viewportRect.x,
            viewportRect.y,
            Mathf.Max(1f, viewportRect.width - scrollbarSpace),
            Mathf.Max(1f, viewportRect.height - scrollbarSpace)
        );

        HandlePinPlacement(
            interactiveViewportRect,
            displayedMapRect,
            mapDefinition,
            selectedPoi
        );
    }

    /// <summary>
    /// Changes the map zoom when the mouse wheel is used over the map.
    /// </summary>
    private void HandleMapZoomWithWheel(Rect viewportRect)
    {
        Event currentEvent = Event.current;

        if (currentEvent.type != EventType.ScrollWheel ||
            !viewportRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        editorZoom = Mathf.Clamp(
            editorZoom - currentEvent.delta.y * 0.1f,
            MinEditorZoom,
            MaxEditorZoom
        );

        Repaint();
        currentEvent.Use();
    }

    /// <summary>
    /// Places or moves the selected POI pin on the scrollable map.
    /// </summary>
    private void HandlePinPlacement(
        Rect viewportRect,
        Rect mapRect,
        MapDefinition mapDefinition,
        POIRegistryEntry selectedPoi
    )
    {
        Event currentEvent = Event.current;

        if (selectedPoi == null ||
            currentEvent.type != EventType.MouseDown ||
            currentEvent.button != 0 ||
            !viewportRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        Vector2 contentMousePosition =
            currentEvent.mousePosition -
            viewportRect.position +
            mapScrollPosition;

        if (!mapRect.Contains(contentMousePosition))
        {
            return;
        }

        Vector2 normalizedPosition = new Vector2(
            Mathf.InverseLerp(
                mapRect.xMin,
                mapRect.xMax,
                contentMousePosition.x
            ),
            Mathf.InverseLerp(
                mapRect.yMin,
                mapRect.yMax,
                contentMousePosition.y
            )
        );

        Undo.RecordObject(
            mapDefinition,
            "Place map pin"
        );

        if (!mapDefinition.UpdatePinPosition(
                selectedPoi.PoiId,
                normalizedPosition))
        {
            mapDefinition.AddPin(
                selectedPoi.PoiId,
                normalizedPosition
            );
        }

        EditorUtility.SetDirty(mapDefinition);

        Repaint();
        currentEvent.Use();
    }

    /// <summary>
    /// Draws the selected sprite, including sprites extracted
    /// from a larger texture.
    /// </summary>
    private static void DrawSprite(
        Rect destinationRect,
        Sprite sprite
    )
    {
        Texture2D texture = sprite.texture;
        Rect spriteRect = sprite.textureRect;

        Rect textureCoordinates = new Rect(
            spriteRect.x / texture.width,
            spriteRect.y / texture.height,
            spriteRect.width / texture.width,
            spriteRect.height / texture.height
        );

        GUI.DrawTextureWithTexCoords(
            destinationRect,
            texture,
            textureCoordinates,
            true
        );
    }

    /// <summary>
    /// Draws all configured pins over the map.
    /// </summary>
    private void DrawPins(
        Rect mapRect,
        MapDefinition mapDefinition,
        POIRegistryEntry selectedPoi
    )
    {
        GUIStyle labelStyle = new GUIStyle(
            EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.UpperCenter,
            normal =
            {
                textColor = Color.white
            }
        };

        foreach (MapPinDefinition pin in mapDefinition.Pins)
        {
            Vector2 pinPosition = new Vector2(
                mapRect.x +
                pin.NormalizedPosition.x * mapRect.width,
                mapRect.y +
                pin.NormalizedPosition.y * mapRect.height
            );

            bool isSelected =
                selectedPoi != null &&
                pin.PoiId == selectedPoi.PoiId;

            Handles.BeginGUI();

            Handles.color = isSelected
                ? new Color(0.52f, 0.77f, 0.25f)
                : new Color(0.12f, 0.35f, 0.16f);

            Handles.DrawSolidDisc(
                new Vector3(
                    pinPosition.x,
                    pinPosition.y,
                    0f
                ),
                Vector3.forward,
                isSelected ? 13f : 10f
            );

            Handles.color = Color.white;

            Handles.DrawSolidDisc(
                new Vector3(
                    pinPosition.x,
                    pinPosition.y,
                    0f
                ),
                Vector3.forward,
                4f
            );

            Handles.EndGUI();

            POIRegistryEntry registryEntry =
                projectContext.poiRegistry.GetById(pin.PoiId);

            string label = registryEntry != null
                ? registryEntry.DisplayName
                : pin.PoiId;

            GUI.Label(
                new Rect(
                    pinPosition.x - 80f,
                    pinPosition.y + 14f,
                    160f,
                    20f
                ),
                label,
                labelStyle
            );
        }
    }

    /// <summary>
    /// Calculates the largest rectangle that fits inside the
    /// available area while preserving the sprite aspect ratio.
    /// </summary>
    private static Rect CalculateAspectFitRect(
        Rect availableRect,
        Sprite sprite
    )
    {
        float spriteAspect =
            sprite.rect.width / sprite.rect.height;

        float availableAspect =
            availableRect.width / availableRect.height;

        if (spriteAspect > availableAspect)
        {
            float height =
                availableRect.width / spriteAspect;

            return new Rect(
                availableRect.x,
                availableRect.center.y - height * 0.5f,
                availableRect.width,
                height
            );
        }

        float width =
            availableRect.height * spriteAspect;

        return new Rect(
            availableRect.center.x - width * 0.5f,
            availableRect.y,
            width,
            availableRect.height
        );
    }

    /// <summary>
    /// Restores the editor-only map view state.
    /// </summary>
    private void ResetMapView()
    {
        editorZoom = MinEditorZoom;
        mapScrollPosition = Vector2.zero;
    }

    /// <summary>
    /// Creates and connects a MapDefinition for projects created
    /// before map support was introduced.
    /// </summary>
    private void EnsureMapDefinitionExists()
    {
        TheodenProjectConfig projectConfig = ProjectConfig;

        if (projectConfig == null ||
            projectConfig.mapDefinition != null)
        {
            return;
        }

        string configPath = projectConfig.configFolderPath;

        if (!AssetDatabase.IsValidFolder(configPath))
        {
            string selectedProjectPath =
                AssetDatabase.GetAssetPath(projectFolder);

            configPath = $"{selectedProjectPath}/Config";
        }

        if (!AssetDatabase.IsValidFolder(configPath))
        {
            projectLoadError =
                $"The project Config folder could not be found: {configPath}";

            return;
        }

        string mapDefinitionPath =
            $"{configPath}/MapDefinition.asset";

        MapDefinition mapDefinition =
            AssetDatabase.LoadAssetAtPath<MapDefinition>(
                mapDefinitionPath
            );

        if (mapDefinition == null)
        {
            mapDefinition = CreateInstance<MapDefinition>();

            AssetDatabase.CreateAsset(
                mapDefinition,
                mapDefinitionPath
            );
        }

        Undo.RecordObject(
            projectConfig,
            "Assign map definition"
        );

        projectConfig.mapDefinition = mapDefinition;

        EditorUtility.SetDirty(projectConfig);
        EditorUtility.SetDirty(mapDefinition);

        AssetDatabase.SaveAssets();
    }
}
