using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CodexUnityBridge
{
    private const string PrimaryScenePath = "Assets/asd.unity";
    private const string ProtectedSampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RequestPath = "Codex/scene_request.json";
    private const BindingFlags PublicInstanceBindingFlags = BindingFlags.Instance | BindingFlags.Public;

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

        Component component;
        if (componentType == typeof(Transform))
        {
            component = gameObject.transform;
        }
        else
        {
            component = gameObject.GetComponent(componentType);
            if (component == null)
                component = Undo.AddComponent(gameObject, componentType);
        }

        ApplyBoxCollider2DSettings(component, request.boxCollider2D);
        ApplyRigidbody2DSettings(component, request.rigidbody2D);
        ApplySpriteRendererSettings(component, request.spriteRenderer);
        ApplyFieldRequests(component, request.fields);
        ApplyEnabledSetting(component, request);
    }

    private static void ApplyEnabledSetting(Component component, ComponentRequest request)
    {
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

    private static void ApplyBoxCollider2DSettings(Component component, BoxCollider2DSettings settings)
    {
        if (!HasBoxCollider2DSettings(settings))
            return;

        var boxCollider = component as BoxCollider2D;
        if (boxCollider == null)
            throw new InvalidOperationException(
                $"boxCollider2D settings can only be applied to BoxCollider2D, not {component.GetType().Name}.");

        if (settings.setIsTrigger)
            boxCollider.isTrigger = settings.isTrigger;

        if (settings.setSize)
            boxCollider.size = settings.size;

        if (settings.setOffset)
            boxCollider.offset = settings.offset;
    }

    private static void ApplyRigidbody2DSettings(Component component, Rigidbody2DSettings settings)
    {
        if (!HasRigidbody2DSettings(settings))
            return;

        var rigidbody = component as Rigidbody2D;
        if (rigidbody == null)
            throw new InvalidOperationException(
                $"rigidbody2D settings can only be applied to Rigidbody2D, not {component.GetType().Name}.");

        if (settings.setBodyType)
            rigidbody.bodyType = ParseEnumValue<RigidbodyType2D>(settings.bodyType, "Rigidbody2D.bodyType");

        if (settings.setGravityScale)
            rigidbody.gravityScale = settings.gravityScale;

        if (settings.setConstraints)
            rigidbody.constraints = ParseEnumFlags<RigidbodyConstraints2D>(
                settings.constraints,
                "Rigidbody2D.constraints");
    }

    private static void ApplySpriteRendererSettings(Component component, SpriteRendererSettings settings)
    {
        if (!HasSpriteRendererSettings(settings))
            return;

        var spriteRenderer = component as SpriteRenderer;
        if (spriteRenderer == null)
            throw new InvalidOperationException(
                $"spriteRenderer settings can only be applied to SpriteRenderer, not {component.GetType().Name}.");

        if (settings.setSortingLayerName)
        {
            if (string.IsNullOrWhiteSpace(settings.sortingLayerName))
                throw new InvalidOperationException("SpriteRenderer.sortingLayerName cannot be empty.");

            if (!SortingLayerExists(settings.sortingLayerName))
                throw new InvalidOperationException($"Sorting layer not found: {settings.sortingLayerName}");

            spriteRenderer.sortingLayerName = settings.sortingLayerName;
        }

        if (settings.setSortingOrder)
            spriteRenderer.sortingOrder = settings.sortingOrder;
    }

    private static bool HasBoxCollider2DSettings(BoxCollider2DSettings settings)
    {
        return settings != null &&
            (settings.setIsTrigger || settings.setSize || settings.setOffset);
    }

    private static bool HasRigidbody2DSettings(Rigidbody2DSettings settings)
    {
        return settings != null &&
            (settings.setBodyType || settings.setGravityScale || settings.setConstraints);
    }

    private static bool HasSpriteRendererSettings(SpriteRendererSettings settings)
    {
        return settings != null &&
            (settings.setSortingLayerName || settings.setSortingOrder);
    }

    private static void ApplyFieldRequests(Component component, FieldValueRequest[] fields)
    {
        if (fields == null)
            return;

        foreach (var fieldRequest in fields)
        {
            if (fieldRequest == null)
                continue;

            ApplyFieldRequest(component, fieldRequest);
        }
    }

    private static void ApplyFieldRequest(Component component, FieldValueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.name))
            throw new InvalidOperationException($"Field request for {component.GetType().Name} is missing a name.");

        Type componentType = component.GetType();
        FieldInfo field = componentType.GetField(request.name, PublicInstanceBindingFlags);
        if (field != null)
        {
            string context = $"{componentType.Name}.{request.name}";
            field.SetValue(component, ConvertFieldValue(request, field.FieldType, context));
            return;
        }

        PropertyInfo property = componentType.GetProperty(request.name, PublicInstanceBindingFlags);
        if (property != null)
        {
            string context = $"{componentType.Name}.{request.name}";
            if (!property.CanWrite || property.GetIndexParameters().Length > 0)
                throw new InvalidOperationException($"{context} is not a writable public field or property.");

            property.SetValue(component, ConvertFieldValue(request, property.PropertyType, context), null);
            return;
        }

        throw new InvalidOperationException(
            $"Public field or writable property not found: {componentType.Name}.{request.name}");
    }

    private static object ConvertFieldValue(FieldValueRequest request, Type targetType, string context)
    {
        if (string.IsNullOrWhiteSpace(request.valueType))
        {
            throw new InvalidOperationException(
                $"{context} is missing valueType. Use one of: bool, int, float, string, Vector2, Vector3.");
        }

        string valueType = request.valueType.Trim().ToLowerInvariant();
        switch (valueType)
        {
            case "bool":
                EnsureTargetType(targetType, typeof(bool), request.valueType, context);
                return request.boolValue;

            case "int":
                EnsureTargetType(targetType, typeof(int), request.valueType, context);
                return request.intValue;

            case "float":
                EnsureTargetType(targetType, typeof(float), request.valueType, context);
                return request.floatValue;

            case "string":
                EnsureTargetType(targetType, typeof(string), request.valueType, context);
                return request.stringValue;

            case "vector2":
                EnsureTargetType(targetType, typeof(Vector2), request.valueType, context);
                return request.vector2Value;

            case "vector3":
                EnsureTargetType(targetType, typeof(Vector3), request.valueType, context);
                return request.vector3Value;

            default:
                throw new InvalidOperationException(
                    $"{context} has unsupported valueType '{request.valueType}'. Use one of: bool, int, float, string, Vector2, Vector3.");
        }
    }

    private static void EnsureTargetType(Type targetType, Type expectedType, string valueType, string context)
    {
        if (targetType == expectedType)
            return;

        throw new InvalidOperationException(
            $"{context} expects {GetFriendlyTypeName(targetType)}, but request valueType '{valueType}' maps to {GetFriendlyTypeName(expectedType)}.");
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(bool))
            return "bool";

        if (type == typeof(int))
            return "int";

        if (type == typeof(float))
            return "float";

        if (type == typeof(string))
            return "string";

        if (type == typeof(Vector2))
            return "Vector2";

        if (type == typeof(Vector3))
            return "Vector3";

        return type.Name;
    }

    private static TEnum ParseEnumValue<TEnum>(string rawValue, string context) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            throw new InvalidOperationException($"{context} cannot be empty.");

        string value = rawValue.Trim();
        if (IsNumericEnumValue(value))
            throw new InvalidOperationException($"{context} must be a named enum value, not a number: {rawValue}");

        TEnum parsed;
        if (!Enum.TryParse(value, true, out parsed) || !Enum.IsDefined(typeof(TEnum), parsed))
            throw new InvalidOperationException($"{context} has invalid value '{rawValue}' for {typeof(TEnum).Name}.");

        return parsed;
    }

    private static TEnum ParseEnumFlags<TEnum>(string rawValue, string context) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            throw new InvalidOperationException($"{context} cannot be empty.");

        string value = rawValue.Trim().Replace("|", ",");
        if (IsNumericEnumValue(value))
            throw new InvalidOperationException($"{context} must use named flags, not a number: {rawValue}");

        TEnum parsed;
        if (!Enum.TryParse(value, true, out parsed))
            throw new InvalidOperationException($"{context} has invalid value '{rawValue}' for {typeof(TEnum).Name}.");

        return parsed;
    }

    private static bool IsNumericEnumValue(string value)
    {
        int parsed;
        return int.TryParse(value, out parsed);
    }

    private static bool SortingLayerExists(string sortingLayerName)
    {
        foreach (SortingLayer sortingLayer in SortingLayer.layers)
        {
            if (string.Equals(sortingLayer.name, sortingLayerName, StringComparison.Ordinal))
                return true;
        }

        return false;
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
        public BoxCollider2DSettings boxCollider2D;
        public Rigidbody2DSettings rigidbody2D;
        public SpriteRendererSettings spriteRenderer;
        public FieldValueRequest[] fields = Array.Empty<FieldValueRequest>();
    }

    [Serializable]
    public sealed class BoxCollider2DSettings
    {
        public bool setIsTrigger;
        public bool isTrigger;
        public bool setSize;
        public Vector2 size;
        public bool setOffset;
        public Vector2 offset;
    }

    [Serializable]
    public sealed class Rigidbody2DSettings
    {
        public bool setBodyType;
        public string bodyType;
        public bool setGravityScale;
        public float gravityScale;
        public bool setConstraints;
        public string constraints;
    }

    [Serializable]
    public sealed class SpriteRendererSettings
    {
        public bool setSortingLayerName;
        public string sortingLayerName;
        public bool setSortingOrder;
        public int sortingOrder;
    }

    [Serializable]
    public sealed class FieldValueRequest
    {
        public string name;
        public string valueType;
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;
        public Vector2 vector2Value;
        public Vector3 vector3Value;
    }
}
