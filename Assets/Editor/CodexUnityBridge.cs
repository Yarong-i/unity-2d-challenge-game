using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CodexUnityBridge
{
    private const string PrimaryScenePath = "Assets/asd.unity";
    private const string ProtectedSampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RequestPath = "Codex/scene_request.json";

    [MenuItem("Tools/Codex/Apply Scene Request")]
    public static void ApplySceneRequestFromMenu()
    {
        ApplySceneRequest();
    }

    [MenuItem("Tools/Codex/Create Empty Scene Request")]
    public static void CreateEmptySceneRequest()
    {
        string absolutePath = GetProjectRelativeAbsolutePath(RequestPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

        var request = new SceneRequest
        {
            scenePath = PrimaryScenePath,
            saveScene = true,
            allowProtectedSampleScene = false,
            objectsToCreate = Array.Empty<CreateObjectRequest>()
        };

        File.WriteAllText(absolutePath, JsonUtility.ToJson(request, true));
        AssetDatabase.Refresh();
        Debug.Log($"Codex scene request template written: {RequestPath}");
    }

    public static void ApplySceneRequestFromBatch()
    {
        try
        {
            ApplySceneRequest();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ApplySceneRequest()
    {
        string absolutePath = GetProjectRelativeAbsolutePath(RequestPath);
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException($"Scene request not found: {RequestPath}", absolutePath);

        string json = File.ReadAllText(absolutePath);
        var request = JsonUtility.FromJson<SceneRequest>(json);
        if (request == null)
            throw new InvalidOperationException($"Scene request could not be parsed: {RequestPath}");

        string scenePath = NormalizeAssetPath(string.IsNullOrWhiteSpace(request.scenePath) ? PrimaryScenePath : request.scenePath);
        if (string.Equals(scenePath, ProtectedSampleScenePath, StringComparison.OrdinalIgnoreCase) &&
            !request.allowProtectedSampleScene)
        {
            throw new InvalidOperationException(
                $"{ProtectedSampleScenePath} is protected. Set allowProtectedSampleScene only when the user explicitly asks for SampleScene changes.");
        }

        if (!File.Exists(GetProjectRelativeAbsolutePath(scenePath)))
            throw new FileNotFoundException($"Scene file not found: {scenePath}", scenePath);

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        int createdOrUpdated = 0;
        if (request.objectsToCreate != null)
        {
            foreach (var objectRequest in request.objectsToCreate)
            {
                if (objectRequest == null || string.IsNullOrWhiteSpace(objectRequest.name))
                    continue;

                CreateOrUpdateObject(objectRequest);
                createdOrUpdated++;
            }
        }

        if (request.saveScene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"Codex scene request applied to {scenePath}. Objects processed: {createdOrUpdated}");
    }

    private static void CreateOrUpdateObject(CreateObjectRequest request)
    {
        GameObject gameObject = GameObject.Find(request.name);

        if (gameObject != null && !request.replaceIfExists)
        {
            throw new InvalidOperationException(
                $"GameObject already exists: {request.name}. Set replaceIfExists to true to update it.");
        }

        if (gameObject == null)
        {
            gameObject = new GameObject(request.name);
            Undo.RegisterCreatedObjectUndo(gameObject, "Codex Create GameObject");
        }

        Transform parent = FindParent(request.parentPath);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = request.position;
        gameObject.transform.localEulerAngles = request.rotationEuler;
        gameObject.transform.localScale = request.scale == Vector3.zero ? Vector3.one : request.scale;

        if (!string.IsNullOrWhiteSpace(request.layerName))
        {
            int layer = LayerMask.NameToLayer(request.layerName);
            if (layer < 0)
                throw new InvalidOperationException($"Unknown layer name: {request.layerName}");

            gameObject.layer = layer;
        }

        if (!string.IsNullOrWhiteSpace(request.tag))
        {
            gameObject.tag = request.tag;
        }

        if (request.components == null)
            return;

        foreach (var componentRequest in request.components)
        {
            AddOrUpdateComponent(gameObject, componentRequest);
        }
    }

    private static Transform FindParent(string parentPath)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
            return null;

        GameObject parent = GameObject.Find(parentPath);
        if (parent == null)
            throw new InvalidOperationException($"Parent GameObject not found: {parentPath}");

        return parent.transform;
    }

    private static void AddOrUpdateComponent(GameObject gameObject, ComponentRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.type))
            return;

        Type componentType = FindComponentType(request.type);
        if (componentType == null)
            throw new InvalidOperationException($"Component type not found: {request.type}");

        if (componentType == typeof(Transform))
            return;

        Component component = gameObject.GetComponent(componentType);
        if (component == null)
            component = Undo.AddComponent(gameObject, componentType);

        if (!request.setEnabled)
            return;

        if (component is Collider2D collider2D)
        {
            collider2D.enabled = request.enabled;
            return;
        }

        if (component is Collider collider)
        {
            collider.enabled = request.enabled;
            return;
        }

        if (component is Renderer renderer)
        {
            renderer.enabled = request.enabled;
            return;
        }

        if (component is Behaviour behaviour)
        {
            behaviour.enabled = request.enabled;
        }
    }

    private static Type FindComponentType(string typeName)
    {
        if (string.Equals(typeName, nameof(Transform), StringComparison.Ordinal) ||
            string.Equals(typeName, typeof(Transform).FullName, StringComparison.Ordinal))
        {
            return typeof(Transform);
        }

        foreach (Type type in TypeCache.GetTypesDerivedFrom<Component>())
        {
            if (type.IsAbstract)
                continue;

            if (string.Equals(type.Name, typeName, StringComparison.Ordinal) ||
                string.Equals(type.FullName, typeName, StringComparison.Ordinal))
            {
                return type;
            }
        }

        return null;
    }

    private static string GetProjectRelativeAbsolutePath(string projectRelativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
    }

    private static string NormalizeAssetPath(string path)
    {
        return path.Replace("\\", "/").Trim();
    }

    [Serializable]
    public sealed class SceneRequest
    {
        public string scenePath = PrimaryScenePath;
        public bool saveScene = true;
        public bool allowProtectedSampleScene;
        public CreateObjectRequest[] objectsToCreate = Array.Empty<CreateObjectRequest>();
    }

    [Serializable]
    public sealed class CreateObjectRequest
    {
        public string name;
        public string parentPath;
        public bool replaceIfExists;
        public Vector3 position;
        public Vector3 rotationEuler;
        public Vector3 scale = Vector3.one;
        public string layerName = "Default";
        public string tag = "Untagged";
        public ComponentRequest[] components = Array.Empty<ComponentRequest>();
    }

    [Serializable]
    public sealed class ComponentRequest
    {
        public string type;
        public bool setEnabled;
        public bool enabled = true;
    }
}
