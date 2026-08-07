#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class SampleSceneSetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    static SampleSceneSetup()
    {
        EditorApplication.delayCall += SetupScene;
    }

    private static void SetupScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != ScenePath)
        {
            return;
        }

        if (GameObject.Find("Environment") != null && GameObject.Find("Player") != null)
        {
            bool changed = false;
            if (GameObject.Find("BouncyBall") == null)
            {
                CreateBouncyBall();
                changed = true;
            }

            if (GameObject.Find("HairDryer") == null)
            {
                CreateHairDryer();
                changed = true;
            }
            else if (PrepareHairDryerForGround(GameObject.Find("HairDryer")))
            {
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            return;
        }

        GameObject environment = GameObject.Find("Environment");
        if (environment == null)
        {
            environment = new GameObject("Environment");
            Undo.RegisterCreatedObjectUndo(environment, "Create Environment");
        }

        CreateGround(environment.transform);
        CreateWall(environment.transform, "Wall_North", new Vector3(0f, 2f, 20.25f), new Vector3(40.5f, 4f, 0.5f));
        CreateWall(environment.transform, "Wall_South", new Vector3(0f, 2f, -20.25f), new Vector3(40.5f, 4f, 0.5f));
        CreateWall(environment.transform, "Wall_East", new Vector3(20.25f, 2f, 0f), new Vector3(0.5f, 4f, 40.5f));
        CreateWall(environment.transform, "Wall_West", new Vector3(-20.25f, 2f, 0f), new Vector3(0.5f, 4f, 40.5f));

        GameObject existingCamera = GameObject.Find("Main Camera");
        if (existingCamera != null)
        {
            Object.DestroyImmediate(existingCamera);
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1f, 0f);
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
        }

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = player.AddComponent<CharacterController>();
        }

        characterController.height = 2f;
        characterController.radius = 0.35f;
        characterController.center = Vector3.zero;

        FirstPersonController controller = player.GetComponent<FirstPersonController>();
        if (controller == null)
        {
            controller = player.AddComponent<FirstPersonController>();
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 75f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 200f;
        cameraObject.AddComponent<AudioListener>();

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("cameraRoot").objectReferenceValue = cameraObject.transform;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        CreateBouncyBall();
        CreateHairDryer();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static void CreateGround(Transform parent)
    {
        if (GameObject.Find("Ground") != null)
        {
            return;
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent, false);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        if (GameObject.Find(name) != null)
        {
            return;
        }

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        Undo.RegisterCreatedObjectUndo(wall, "Create Wall");
    }

    private static void CreateBouncyBall()
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "BouncyBall";
        ball.transform.position = new Vector3(0f, 2.5f, 5f);
        ball.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(ball, "Create Bouncy Ball");

        SphereCollider sphereCollider = ball.GetComponent<SphereCollider>();
        sphereCollider.material = GetBouncyMaterial();

        Rigidbody rigidbody = ball.AddComponent<Rigidbody>();
        rigidbody.mass = 1f;
        rigidbody.drag = 0f;
        rigidbody.angularDrag = 0.05f;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private static void CreateHairDryer()
    {
        GameObject cameraObject = GameObject.Find("Main Camera");
        if (cameraObject == null)
        {
            return;
        }

        GameObject dryer = new GameObject("HairDryer");
        dryer.transform.position = new Vector3(1.5f, 0.55f, 2.5f);
        dryer.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Undo.RegisterCreatedObjectUndo(dryer, "Create Hair Dryer");

        HairDryer wind = dryer.AddComponent<HairDryer>();
        BoxCollider pickupCollider = dryer.AddComponent<BoxCollider>();
        pickupCollider.center = new Vector3(0f, -0.18f, 0.16f);
        pickupCollider.size = new Vector3(0.8f, 0.8f, 1.35f);

        GameObject body = CreateDryerPart("Body", PrimitiveType.Cube, dryer.transform,
            new Vector3(0f, 0f, 0.05f), Vector3.zero, new Vector3(0.38f, 0.28f, 0.62f));
        GameObject handle = CreateDryerPart("Handle", PrimitiveType.Cube, dryer.transform,
            new Vector3(0f, -0.28f, -0.02f), new Vector3(-15f, 0f, 0f), new Vector3(0.18f, 0.52f, 0.2f));
        GameObject nozzle = CreateDryerPart("Nozzle", PrimitiveType.Cylinder, dryer.transform,
            new Vector3(0f, 0f, 0.55f), new Vector3(90f, 0f, 0f), new Vector3(0.16f, 0.24f, 0.16f));

        Object.DestroyImmediate(body.GetComponent<Collider>());
        Object.DestroyImmediate(handle.GetComponent<Collider>());
        Object.DestroyImmediate(nozzle.GetComponent<Collider>());

        SerializedObject serializedWind = new SerializedObject(wind);
        serializedWind.FindProperty("nozzle").objectReferenceValue = nozzle.transform;
        serializedWind.FindProperty("isHeld").boolValue = false;

        HairDryerRangeVisual rangeVisual = CreateRangeVisual(nozzle.transform);
        if (rangeVisual != null)
        {
            serializedWind.FindProperty("rangeVisual").objectReferenceValue = rangeVisual;
        }

        serializedWind.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HairDryerRangeVisual CreateRangeVisual(Transform nozzleTransform)
    {
        const string prefabPath = "Assets/Prefabs/HairDryerRangeVisual.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return null;
        }

        GameObject rangeVisualObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, nozzleTransform);
        rangeVisualObject.name = "RangeVisual";
        rangeVisualObject.transform.localPosition = Vector3.zero;
        rangeVisualObject.transform.localRotation = Quaternion.identity;
        rangeVisualObject.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(rangeVisualObject, "Create Hair Dryer Range Visual");
        return rangeVisualObject.GetComponent<HairDryerRangeVisual>();
    }

    private static bool PrepareHairDryerForGround(GameObject dryer)
    {
        bool changed = false;
        if (dryer.transform.parent != null)
        {
            dryer.transform.SetParent(null, true);
            changed = true;
        }

        Vector3 groundPosition = new Vector3(1.5f, 0.55f, 2.5f);
        Quaternion groundRotation = Quaternion.Euler(0f, 180f, 0f);
        if (dryer.transform.position != groundPosition || dryer.transform.rotation != groundRotation)
        {
            dryer.transform.SetPositionAndRotation(groundPosition, groundRotation);
            changed = true;
        }

        BoxCollider pickupCollider = dryer.GetComponent<BoxCollider>();
        if (pickupCollider == null)
        {
            pickupCollider = dryer.AddComponent<BoxCollider>();
            pickupCollider.center = new Vector3(0f, -0.18f, 0.16f);
            pickupCollider.size = new Vector3(0.8f, 0.8f, 1.35f);
            changed = true;
        }

        HairDryer wind = dryer.GetComponent<HairDryer>();
        if (wind != null)
        {
            SerializedObject serializedWind = new SerializedObject(wind);
            SerializedProperty heldProperty = serializedWind.FindProperty("isHeld");
            if (heldProperty != null && heldProperty.boolValue)
            {
                heldProperty.boolValue = false;
                serializedWind.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
        }

        return changed;
    }

    private static GameObject CreateDryerPart(string name, PrimitiveType primitiveType, Transform parent,
        Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localEulerAngles = localEulerAngles;
        part.transform.localScale = localScale;
        Undo.RegisterCreatedObjectUndo(part, "Create Hair Dryer Part");
        return part;
    }

    private static PhysicMaterial GetBouncyMaterial()
    {
        const string folderPath = "Assets/Materials";
        const string assetPath = folderPath + "/BouncyBallPhysicMaterial.physicMaterial";

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        PhysicMaterial material = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(assetPath);
        if (material == null)
        {
            material = new PhysicMaterial("BouncyBallPhysicMaterial")
            {
                bounciness = 0.9f,
                dynamicFriction = 0.1f,
                staticFriction = 0.1f,
                bounceCombine = PhysicMaterialCombine.Maximum,
                frictionCombine = PhysicMaterialCombine.Minimum
            };

            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.SaveAssets();
        }

        return material;
    }
}
#endif
