using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HockeyGame.Core;

/// <summary>
/// Complete editor utility to set up a full 5v5 hockey game scene.
/// Creates both teams with 6 players each (5 skaters + 1 goalie), TeamManager,
/// proper formations, AI difficulty, player switching, and GameHUD.
/// Access via menu: Hockey Game > Setup Full 5v5 Game
/// </summary>
public class HockeySceneSetup : EditorWindow
{
    private static PlayerStats playerStats;
    private static PlayerStats goalieStats;
    private static ArtifactDatabase artifactDatabase;
    private static TeamData homeTeamData;
    private static TeamData awayTeamData;

    [MenuItem("Hockey Game/Setup Full 5v5 Game", false, 1)]
    public static void SetupCompleteScene()
    {
        if (!EditorUtility.DisplayDialog("Setup 5v5 Hockey Scene",
            "This will set up a complete 5v5 hockey game with:\n\n" +
            "- 2 Teams (6 players each: 5 skaters + 1 goalie)\n" +
            "- TeamManager with player switching\n" +
            "- AI opponents with difficulty settings\n" +
            "- Hockey Rink with goals\n" +
            "- Puck physics\n" +
            "- Broadcast camera\n" +
            "- GameHUD canvas\n" +
            "- All managers wired up\n\n" +
            "Continue?", "Setup", "Cancel"))
        {
            return;
        }

        Debug.Log("[HockeySceneSetup] Starting full 5v5 scene setup...");

        // Create required assets first
        CreateRequiredAssets();

        // Setup scene objects
        SetupManagers();
        SetupGameSystems();
        SetupRink();
        SetupTeamManager();
        SetupPuck();
        SetupCamera();
        SetupGameHUD();
        SetupLighting();

        // Wire up all references
        WireUpReferences();

        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[HockeySceneSetup] 5v5 scene setup complete!");
        EditorUtility.DisplayDialog("Setup Complete",
            "5v5 Hockey scene is ready!\n\n" +
            "Press Play to test the game.\n\n" +
            "Controls:\n" +
            "WASD - Move\n" +
            "Space - Shoot (hold to charge)\n" +
            "E - Pass\n" +
            "Shift - Dash\n" +
            "Tab - Switch Players\n" +
            "Q/E - Previous/Next Player", "OK");
    }

    [MenuItem("Hockey Game/Setup 5v5 (Quick - No Dialog)", false, 2)]
    public static void SetupQuick()
    {
        CreateRequiredAssets();
        SetupManagers();
        SetupGameSystems();
        SetupRink();
        SetupTeamManager();
        SetupPuck();
        SetupCamera();
        SetupGameHUD();
        SetupLighting();
        WireUpReferences();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[HockeySceneSetup] Quick 5v5 setup complete!");
    }

    [MenuItem("Hockey Game/Create Required Assets", false, 20)]
    public static void CreateRequiredAssets()
    {
        // Create folders
        CreateFolderIfNeeded("Assets/_Scripts/Data", "Artifacts");
        CreateFolderIfNeeded("Assets", "GameData");
        CreateFolderIfNeeded("Assets/GameData", "Teams");

        // Create Skater PlayerStats
        playerStats = AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/GameData/DefaultPlayerStats.asset");
        if (playerStats == null)
        {
            playerStats = ScriptableObject.CreateInstance<PlayerStats>();
            playerStats.maxSpeed = 25f;
            playerStats.accelerationForce = 120f;
            playerStats.maxShotPower = 35f;
            playerStats.puckMagnetStrength = 8f;
            playerStats.dashMultiplier = 2.0f;
            playerStats.dashCooldown = 2f;
            AssetDatabase.CreateAsset(playerStats, "Assets/GameData/DefaultPlayerStats.asset");
            Debug.Log("[HockeySceneSetup] Created DefaultPlayerStats.asset");
        }

        // Create Goalie PlayerStats
        goalieStats = AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/GameData/GoaliePlayerStats.asset");
        if (goalieStats == null)
        {
            goalieStats = ScriptableObject.CreateInstance<PlayerStats>();
            goalieStats.maxSpeed = 5f;
            goalieStats.accelerationForce = 8f;
            goalieStats.maxShotPower = 10f;
            goalieStats.puckMagnetStrength = 0.9f;
            goalieStats.dashMultiplier = 1.2f;
            goalieStats.dashCooldown = 3f;
            AssetDatabase.CreateAsset(goalieStats, "Assets/GameData/GoaliePlayerStats.asset");
            Debug.Log("[HockeySceneSetup] Created GoaliePlayerStats.asset");
        }

        // Create ArtifactDatabase
        artifactDatabase = AssetDatabase.LoadAssetAtPath<ArtifactDatabase>("Assets/GameData/ArtifactDatabase.asset");
        if (artifactDatabase == null)
        {
            artifactDatabase = ScriptableObject.CreateInstance<ArtifactDatabase>();
            AssetDatabase.CreateAsset(artifactDatabase, "Assets/GameData/ArtifactDatabase.asset");
            Debug.Log("[HockeySceneSetup] Created ArtifactDatabase.asset");
        }

        // Create Home Team Data (Blue Team)
        homeTeamData = AssetDatabase.LoadAssetAtPath<TeamData>("Assets/GameData/Teams/HomeTeam.asset");
        if (homeTeamData == null)
        {
            homeTeamData = ScriptableObject.CreateInstance<TeamData>();
            homeTeamData.teamName = "Blue Jackets";
            homeTeamData.abbreviation = "BLU";
            homeTeamData.primaryColor = new Color(0.2f, 0.4f, 0.9f); // Blue
            homeTeamData.secondaryColor = Color.white;
            homeTeamData.goalieColor = new Color(0.1f, 0.3f, 0.8f); // Darker blue
            homeTeamData.skatersOnIce = 5;
            homeTeamData.goalies = 1;
            homeTeamData.defaultFormation = FormationType.Balanced;
            homeTeamData.aggressiveness = 0.5f;
            homeTeamData.aiDifficulty = 0.5f;
            AssetDatabase.CreateAsset(homeTeamData, "Assets/GameData/Teams/HomeTeam.asset");
            Debug.Log("[HockeySceneSetup] Created HomeTeam.asset");
        }

        // Create Away Team Data (Red Team)
        awayTeamData = AssetDatabase.LoadAssetAtPath<TeamData>("Assets/GameData/Teams/AwayTeam.asset");
        if (awayTeamData == null)
        {
            awayTeamData = ScriptableObject.CreateInstance<TeamData>();
            awayTeamData.teamName = "Red Wings";
            awayTeamData.abbreviation = "RED";
            awayTeamData.primaryColor = new Color(0.9f, 0.2f, 0.2f); // Red
            awayTeamData.secondaryColor = Color.white;
            awayTeamData.goalieColor = new Color(0.8f, 0.1f, 0.1f); // Darker red
            awayTeamData.skatersOnIce = 5;
            awayTeamData.goalies = 1;
            awayTeamData.defaultFormation = FormationType.Balanced;
            awayTeamData.aggressiveness = 0.6f;
            awayTeamData.aiDifficulty = 0.6f; // Slightly harder AI
            AssetDatabase.CreateAsset(awayTeamData, "Assets/GameData/Teams/AwayTeam.asset");
            Debug.Log("[HockeySceneSetup] Created AwayTeam.asset");
        }

        // Link stats to team data
        var homeTeamSO = new SerializedObject(homeTeamData);
        homeTeamSO.FindProperty("skaterStats").objectReferenceValue = playerStats;
        homeTeamSO.FindProperty("goalieStats").objectReferenceValue = goalieStats;
        homeTeamSO.ApplyModifiedProperties();

        var awayTeamSO = new SerializedObject(awayTeamData);
        awayTeamSO.FindProperty("skaterStats").objectReferenceValue = playerStats;
        awayTeamSO.FindProperty("goalieStats").objectReferenceValue = goalieStats;
        awayTeamSO.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Generate artifacts
        var db = AssetDatabase.LoadAssetAtPath<ArtifactDatabase>("Assets/GameData/ArtifactDatabase.asset");
        if (db != null)
        {
            var serializedObject = new SerializedObject(db);
            var artifactsProp = serializedObject.FindProperty("allArtifacts");
            if (artifactsProp != null && artifactsProp.arraySize == 0)
            {
                GenerateDefaultArtifacts(db);
            }
        }
    }

    private static void GenerateDefaultArtifacts(ArtifactDatabase database)
    {
        string folderPath = "Assets/GameData/Artifacts";
        CreateFolderIfNeeded("Assets/GameData", "Artifacts");

        var artifacts = new System.Collections.Generic.List<RunModifier>();

        // Common
        artifacts.Add(CreateArtifact("Speed Skates", "Slightly faster skating.", ArtifactRarity.Common, folderPath, speedMult: 0.1f));
        artifacts.Add(CreateArtifact("Grip Tape", "Better puck control.", ArtifactRarity.Common, folderPath, puckMult: 0.15f));
        artifacts.Add(CreateArtifact("Power Workout", "Stronger shots.", ArtifactRarity.Common, folderPath, shotMult: 0.1f));

        // Uncommon
        artifacts.Add(CreateArtifact("Heavy Hitter", "Devastating body checks!", ArtifactRarity.Uncommon, folderPath, checkMult: 0.2f, heavyHitter: true));
        artifacts.Add(CreateArtifact("Rocket Skates", "Much faster skating.", ArtifactRarity.Uncommon, folderPath, speedMult: 0.2f, accelMult: 0.1f));

        // Rare
        artifacts.Add(CreateArtifact("Slippery Ice", "Less friction. Chaos!", ArtifactRarity.Rare, folderPath, speedMult: 0.15f, slipperyIce: true));
        artifacts.Add(CreateArtifact("Cannon Arm", "Massive shot power.", ArtifactRarity.Rare, folderPath, shotMult: 0.4f));

        // Legendary
        artifacts.Add(CreateArtifact("Explosive Puck", "Shots EXPLODE!", ArtifactRarity.Legendary, folderPath, shotMult: 0.3f, explosivePucks: true));
        artifacts.Add(CreateArtifact("The Great One", "All stats boosted.", ArtifactRarity.Legendary, folderPath,
            speedMult: 0.2f, accelMult: 0.2f, shotMult: 0.2f, checkMult: 0.2f, puckMult: 0.2f));

        // Assign to database
        var so = new SerializedObject(database);
        var prop = so.FindProperty("allArtifacts");
        prop.ClearArray();

        for (int i = 0; i < artifacts.Count; i++)
        {
            prop.InsertArrayElementAtIndex(i);
            prop.GetArrayElementAtIndex(i).objectReferenceValue = artifacts[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"[HockeySceneSetup] Created {artifacts.Count} artifacts");
    }

    private static RunModifier CreateArtifact(string name, string desc, ArtifactRarity rarity, string folder,
        float speedMult = 0, float accelMult = 0, float shotMult = 0, float checkMult = 0, float puckMult = 0,
        bool explosivePucks = false, bool slipperyIce = false, bool heavyHitter = false)
    {
        var artifact = ScriptableObject.CreateInstance<RunModifier>();
        artifact.artifactName = name;
        artifact.description = desc;
        artifact.rarity = rarity;
        artifact.skatingSpeedMultiplier = speedMult;
        artifact.accelerationMultiplier = accelMult;
        artifact.shotPowerMultiplier = shotMult;
        artifact.checkForceMultiplier = checkMult;
        artifact.puckControlMultiplier = puckMult;
        artifact.explosivePucks = explosivePucks;
        artifact.slipperyIce = slipperyIce;
        artifact.heavyHitter = heavyHitter;

        string path = $"{folder}/{name.Replace(" ", "_")}.asset";
        AssetDatabase.CreateAsset(artifact, path);
        return artifact;
    }

    private static void SetupManagers()
    {
        // Game Manager
        var gmObj = GameObject.Find("GameManager");
        if (gmObj == null)
        {
            gmObj = new GameObject("GameManager");
        }

        var gm = gmObj.GetComponent<GameManager>();
        if (gm == null)
        {
            gm = gmObj.AddComponent<GameManager>();
        }

        // Set references via SerializedObject
        var gmSO = new SerializedObject(gm);
        gmSO.FindProperty("defaultPlayerStats").objectReferenceValue = playerStats;

        var dbAsset = AssetDatabase.LoadAssetAtPath<ArtifactDatabase>("Assets/GameData/ArtifactDatabase.asset");
        gmSO.FindProperty("allArtifacts").ClearArray();
        if (dbAsset != null)
        {
            var allArtifacts = dbAsset.AllArtifacts;
            for (int i = 0; i < allArtifacts.Count; i++)
            {
                gmSO.FindProperty("allArtifacts").InsertArrayElementAtIndex(i);
                gmSO.FindProperty("allArtifacts").GetArrayElementAtIndex(i).objectReferenceValue = allArtifacts[i];
            }
        }
        gmSO.ApplyModifiedProperties();

        // Input Manager
        var imObj = GameObject.Find("InputManager");
        if (imObj == null)
        {
            imObj = new GameObject("InputManager");
        }
        if (imObj.GetComponent<InputManager>() == null)
        {
            imObj.AddComponent<InputManager>();
        }

        // Match Manager
        var mmObj = GameObject.Find("MatchManager");
        if (mmObj == null)
        {
            mmObj = new GameObject("MatchManager");
        }
        if (mmObj.GetComponent<MatchManager>() == null)
        {
            mmObj.AddComponent<MatchManager>();
        }

        Debug.Log("[HockeySceneSetup] Managers created");
    }

    private static void SetupGameSystems()
    {
        // SoundManager
        var soundManagerObj = GameObject.Find("SoundManager");
        if (soundManagerObj == null)
        {
            soundManagerObj = new GameObject("SoundManager");
        }
        if (soundManagerObj.GetComponent<SoundManager>() == null)
        {
            soundManagerObj.AddComponent<SoundManager>();
        }

        // VisualEffectsManager
        var vfxManagerObj = GameObject.Find("VisualEffectsManager");
        if (vfxManagerObj == null)
        {
            vfxManagerObj = new GameObject("VisualEffectsManager");
        }
        if (vfxManagerObj.GetComponent<VisualEffectsManager>() == null)
        {
            vfxManagerObj.AddComponent<VisualEffectsManager>();
        }

        // PenaltySystem
        var penaltySystemObj = GameObject.Find("PenaltySystem");
        if (penaltySystemObj == null)
        {
            penaltySystemObj = new GameObject("PenaltySystem");
        }
        if (penaltySystemObj.GetComponent<PenaltySystem>() == null)
        {
            penaltySystemObj.AddComponent<PenaltySystem>();
        }

        // HockeyRules
        var rulesObj = GameObject.Find("HockeyRules");
        if (rulesObj == null)
        {
            rulesObj = new GameObject("HockeyRules");
        }
        if (rulesObj.GetComponent<HockeyRules>() == null)
        {
            rulesObj.AddComponent<HockeyRules>();
        }

        // FaceoffSystem
        var faceoffSystemObj = GameObject.Find("FaceoffSystem");
        if (faceoffSystemObj == null)
        {
            faceoffSystemObj = new GameObject("FaceoffSystem");
        }
        if (faceoffSystemObj.GetComponent<FaceoffSystem>() == null)
        {
            faceoffSystemObj.AddComponent<FaceoffSystem>();
        }

        Debug.Log("[HockeySceneSetup] Game systems created (SoundManager, VisualEffectsManager, PenaltySystem, HockeyRules, FaceoffSystem)");
    }

    private static void SetupRink()
    {
        var rinkObj = GameObject.Find("RinkBuilder");
        if (rinkObj == null)
        {
            rinkObj = new GameObject("RinkBuilder");
        }

        var rink = rinkObj.GetComponent<RinkBuilder>();
        if (rink == null)
        {
            rink = rinkObj.AddComponent<RinkBuilder>();
        }

        // Build the rink
        rink.BuildRink();

        Debug.Log("[HockeySceneSetup] Rink built");
    }

    private static void SetupTeamManager()
    {
        var rink = Object.FindObjectOfType<RinkBuilder>();
        if (rink == null)
        {
            Debug.LogError("[HockeySceneSetup] No RinkBuilder found! Cannot setup teams.");
            return;
        }

        // Create TeamManager object
        var teamManagerObj = GameObject.Find("TeamManager");
        if (teamManagerObj == null)
        {
            teamManagerObj = new GameObject("TeamManager");
        }

        var teamManager = teamManagerObj.GetComponent<TeamManager>();
        if (teamManager == null)
        {
            teamManager = teamManagerObj.AddComponent<TeamManager>();
        }

        // Create spawn root transforms
        Transform homeSpawnRoot = CreateSpawnRoot("HomeTeamSpawn", new Vector3(-15, 0, 0));
        Transform awaySpawnRoot = CreateSpawnRoot("AwayTeamSpawn", new Vector3(15, 0, 0));

        // Configure TeamManager via SerializedObject
        var tmSO = new SerializedObject(teamManager);
        tmSO.FindProperty("homeTeamData").objectReferenceValue = homeTeamData;
        tmSO.FindProperty("awayTeamData").objectReferenceValue = awayTeamData;
        tmSO.FindProperty("homeSpawnRoot").objectReferenceValue = homeSpawnRoot;
        tmSO.FindProperty("awaySpawnRoot").objectReferenceValue = awaySpawnRoot;
        tmSO.FindProperty("homeGoalTransform").objectReferenceValue = rink.HomeGoal?.transform;
        tmSO.FindProperty("awayGoalTransform").objectReferenceValue = rink.AwayGoal?.transform;
        tmSO.FindProperty("allowPlayerSwitching").boolValue = true;
        tmSO.FindProperty("switchCooldown").floatValue = 0.5f;
        tmSO.FindProperty("autoSwitchDistance").floatValue = 5f;
        tmSO.FindProperty("enableLineChanges").boolValue = false; // Disabled for now
        tmSO.FindProperty("showDebugGizmos").boolValue = true;
        tmSO.FindProperty("showTeamStats").boolValue = true;
        tmSO.ApplyModifiedProperties();

        // Create player and goalie prefabs
        CreatePlayerPrefabs(rink);

        // Link prefabs to team data
        GameObject skaterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HockeySkater.prefab");
        GameObject goaliePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HockeyGoalie.prefab");

        if (skaterPrefab != null && goaliePrefab != null)
        {
            var homeTeamSO = new SerializedObject(homeTeamData);
            homeTeamSO.FindProperty("skaterPrefab").objectReferenceValue = skaterPrefab;
            homeTeamSO.FindProperty("goaliePrefab").objectReferenceValue = goaliePrefab;
            homeTeamSO.ApplyModifiedProperties();

            var awayTeamSO = new SerializedObject(awayTeamData);
            awayTeamSO.FindProperty("skaterPrefab").objectReferenceValue = skaterPrefab;
            awayTeamSO.FindProperty("goaliePrefab").objectReferenceValue = goaliePrefab;
            awayTeamSO.ApplyModifiedProperties();

            EditorUtility.SetDirty(homeTeamData);
            EditorUtility.SetDirty(awayTeamData);
            AssetDatabase.SaveAssets();
        }

        Debug.Log("[HockeySceneSetup] TeamManager configured with 5v5 teams");
    }

    private static Transform CreateSpawnRoot(string name, Vector3 position)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            return existing.transform;
        }

        var obj = new GameObject(name);
        obj.transform.position = position;
        return obj.transform;
    }

    private static void CreatePlayerPrefabs(RinkBuilder rink)
    {
        // Create Prefabs folder if needed
        CreateFolderIfNeeded("Assets", "Prefabs");

        // Create Skater Prefab
        GameObject skaterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HockeySkater.prefab");
        if (skaterPrefab == null)
        {
            GameObject skaterObj = new GameObject("HockeySkater");

            // Rigidbody
            var rb = skaterObj.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.mass = 80f;
            rb.linearDamping = 0f; // Let HockeyPlayer script control damping dynamically

            // Collider
            var col = skaterObj.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.center = Vector3.up;
            col.radius = 0.4f;

            // HockeyPlayer
            var player = skaterObj.AddComponent<HockeyPlayer>();
            var playerSO = new SerializedObject(player);
            playerSO.FindProperty("baseStats").objectReferenceValue = playerStats;
            playerSO.ApplyModifiedProperties();

            // ShootingController
            skaterObj.AddComponent<ShootingController>();

            // AIController
            skaterObj.AddComponent<AIController>();

            // HockeyStick component
            skaterObj.AddComponent<HockeyStick>();

            // Create visual hockey stick hierarchy
            var stickRoot = CreateVisualStickHierarchy(skaterObj.transform);

            // Set stick tip reference (BladeContactPoint from hierarchy)
            var bladeContactPoint = stickRoot.Find("StickShaft/StickBlade/BladeContactPoint");
            playerSO = new SerializedObject(player);
            playerSO.FindProperty("stickTip").objectReferenceValue = bladeContactPoint;
            playerSO.ApplyModifiedProperties();

            // Visual
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(skaterObj.transform);
            visual.transform.localPosition = Vector3.up;
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(skaterObj, "Assets/Prefabs/HockeySkater.prefab");
            Object.DestroyImmediate(skaterObj);
            Debug.Log("[HockeySceneSetup] Created HockeySkater prefab");
        }

        // Create Goalie Prefab
        GameObject goaliePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HockeyGoalie.prefab");
        if (goaliePrefab == null)
        {
            GameObject goalieObj = new GameObject("HockeyGoalie");

            // Rigidbody
            var rb = goalieObj.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.mass = 90f;
            rb.linearDamping = 0.8f;

            // Collider
            var col = goalieObj.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.center = Vector3.up;
            col.radius = 0.5f;

            // HockeyPlayer
            var player = goalieObj.AddComponent<HockeyPlayer>();
            var playerSO = new SerializedObject(player);
            playerSO.FindProperty("baseStats").objectReferenceValue = goalieStats;
            playerSO.ApplyModifiedProperties();

            // GoalieController
            var goalieController = goalieObj.AddComponent<GoalieController>();

            // HockeyStick component
            goalieObj.AddComponent<HockeyStick>();

            // Create visual hockey stick hierarchy
            var stickRoot = CreateVisualStickHierarchy(goalieObj.transform);

            // Set stick tip reference (BladeContactPoint from hierarchy)
            var bladeContactPoint = stickRoot.Find("StickShaft/StickBlade/BladeContactPoint");
            playerSO = new SerializedObject(player);
            playerSO.FindProperty("stickTip").objectReferenceValue = bladeContactPoint;
            playerSO.ApplyModifiedProperties();

            // Visual
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(goalieObj.transform);
            visual.transform.localPosition = Vector3.up;
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(goalieObj, "Assets/Prefabs/HockeyGoalie.prefab");
            Object.DestroyImmediate(goalieObj);
            Debug.Log("[HockeySceneSetup] Created HockeyGoalie prefab");
        }
    }

    private static void SetupPuck()
    {
        var rink = Object.FindObjectOfType<RinkBuilder>();

        var puckObj = GameObject.Find("Puck");
        if (puckObj == null)
        {
            puckObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puckObj.name = "Puck";
        }

        Transform puckSpawn = rink?.GetSpawnPoint("PuckSpawn");
        puckObj.transform.position = puckSpawn != null ? puckSpawn.position : new Vector3(0, 0.1f, 0);
        puckObj.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);

        // Replace cylinder collider with sphere for better rolling
        var cylCol = puckObj.GetComponent<CapsuleCollider>();
        if (cylCol != null) Object.DestroyImmediate(cylCol);

        if (puckObj.GetComponent<SphereCollider>() == null)
        {
            var sphere = puckObj.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
        }

        if (puckObj.GetComponent<Rigidbody>() == null)
        {
            var rb = puckObj.AddComponent<Rigidbody>();
            rb.mass = 0.17f;
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.5f;
        }

        if (puckObj.GetComponent<Puck>() == null)
        {
            puckObj.AddComponent<Puck>();
        }

        // Create puck material properly to avoid edit-mode material leak
        Material puckMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        puckMat.color = Color.black;
        puckObj.GetComponent<MeshRenderer>().sharedMaterial = puckMat;
        puckObj.tag = "Puck";

        Debug.Log("[HockeySceneSetup] Puck created");
    }

    private static void SetupCamera()
    {
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            var camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
        }

        // Position camera
        mainCam.transform.position = new Vector3(0, 20, -25);
        mainCam.transform.rotation = Quaternion.Euler(40, 0, 0);

        // Add broadcast camera
        var broadcastCam = mainCam.GetComponent<BroadcastCamera>();
        if (broadcastCam == null)
        {
            broadcastCam = mainCam.gameObject.AddComponent<BroadcastCamera>();
        }

        // Set targets (will be wired up later)
        Debug.Log("[HockeySceneSetup] Camera configured");
    }

    private static void SetupGameHUD()
    {
        // Check for existing canvas
        var canvas = Object.FindObjectOfType<Canvas>();
        GameObject canvasObj;

        if (canvas == null)
        {
            canvasObj = new GameObject("GameHUD");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
            canvasObj.name = "GameHUD";
        }

        // Add EventSystem if needed
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Create HUD elements
        CreateScoreDisplay(canvasObj);
        CreateTimerDisplay(canvasObj);
        CreateShotMeter(canvasObj);
        CreatePlayerIndicator(canvasObj);
        CreateTeamStatsPanel(canvasObj);

        Debug.Log("[HockeySceneSetup] GameHUD created");
    }

    private static void CreateScoreDisplay(GameObject canvas)
    {
        // Score Panel (top center)
        var scorePanelObj = new GameObject("ScorePanel");
        scorePanelObj.transform.SetParent(canvas.transform, false);

        var scoreRect = scorePanelObj.AddComponent<UnityEngine.UI.Image>();
        scoreRect.color = new Color(0, 0, 0, 0.7f);

        var scoreRectTransform = scorePanelObj.GetComponent<RectTransform>();
        scoreRectTransform.anchorMin = new Vector2(0.5f, 1f);
        scoreRectTransform.anchorMax = new Vector2(0.5f, 1f);
        scoreRectTransform.pivot = new Vector2(0.5f, 1f);
        scoreRectTransform.anchoredPosition = new Vector2(0, -10);
        scoreRectTransform.sizeDelta = new Vector2(300, 60);

        // Score Text
        var scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(scorePanelObj.transform, false);

        var scoreText = scoreTextObj.AddComponent<UnityEngine.UI.Text>();
        scoreText.text = "HOME 0 - 0 AWAY";
        scoreText.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 24;
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.color = Color.white;

        var scoreTextRect = scoreTextObj.GetComponent<RectTransform>();
        scoreTextRect.anchorMin = Vector2.zero;
        scoreTextRect.anchorMax = Vector2.one;
        scoreTextRect.sizeDelta = Vector2.zero;
    }

    private static void CreateTimerDisplay(GameObject canvas)
    {
        // Timer Text (top left)
        var timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(canvas.transform, false);

        var timerText = timerObj.AddComponent<UnityEngine.UI.Text>();
        timerText.text = "20:00";
        timerText.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.fontSize = 28;
        timerText.alignment = TextAnchor.UpperLeft;
        timerText.color = Color.white;

        var timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0, 1);
        timerRect.anchorMax = new Vector2(0, 1);
        timerRect.pivot = new Vector2(0, 1);
        timerRect.anchoredPosition = new Vector2(20, -80);
        timerRect.sizeDelta = new Vector2(150, 40);
    }

    private static void CreateShotMeter(GameObject canvas)
    {
        // Shot Meter Panel (bottom center)
        var shotMeterObj = new GameObject("ShotMeter");
        shotMeterObj.transform.SetParent(canvas.transform, false);

        var shotMeterRect = shotMeterObj.GetComponent<RectTransform>();
        if (shotMeterRect == null) shotMeterRect = shotMeterObj.AddComponent<RectTransform>();

        shotMeterRect.anchorMin = new Vector2(0.5f, 0f);
        shotMeterRect.anchorMax = new Vector2(0.5f, 0f);
        shotMeterRect.pivot = new Vector2(0.5f, 0f);
        shotMeterRect.anchoredPosition = new Vector2(0, 20);
        shotMeterRect.sizeDelta = new Vector2(200, 30);

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(shotMeterObj.transform, false);
        var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Fill
        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(shotMeterObj.transform, false);
        var fillImage = fillObj.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = new Color(1f, 0.5f, 0f, 0.9f);
        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0, 0);
    }

    private static void CreatePlayerIndicator(GameObject canvas)
    {
        // Player Indicator (bottom left)
        var playerIndicatorObj = new GameObject("PlayerIndicator");
        playerIndicatorObj.transform.SetParent(canvas.transform, false);

        var playerText = playerIndicatorObj.AddComponent<UnityEngine.UI.Text>();
        playerText.text = "Controlling: Center";
        playerText.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playerText.fontSize = 18;
        playerText.alignment = TextAnchor.LowerLeft;
        playerText.color = Color.yellow;

        var playerRect = playerIndicatorObj.GetComponent<RectTransform>();
        playerRect.anchorMin = new Vector2(0, 0);
        playerRect.anchorMax = new Vector2(0, 0);
        playerRect.pivot = new Vector2(0, 0);
        playerRect.anchoredPosition = new Vector2(20, 70);
        playerRect.sizeDelta = new Vector2(300, 30);
    }

    private static void CreateTeamStatsPanel(GameObject canvas)
    {
        // Team Stats Panel (bottom right)
        var statsObj = new GameObject("TeamStatsPanel");
        statsObj.transform.SetParent(canvas.transform, false);

        var statsRect = statsObj.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(1, 0);
        statsRect.anchorMax = new Vector2(1, 0);
        statsRect.pivot = new Vector2(1, 0);
        statsRect.anchoredPosition = new Vector2(-20, 20);
        statsRect.sizeDelta = new Vector2(250, 120);

        var statsImage = statsObj.AddComponent<UnityEngine.UI.Image>();
        statsImage.color = new Color(0, 0, 0, 0.6f);

        // Stats Text
        var statsTextObj = new GameObject("StatsText");
        statsTextObj.transform.SetParent(statsObj.transform, false);

        var statsText = statsTextObj.AddComponent<UnityEngine.UI.Text>();
        statsText.text = "SHOTS: 0\nHITS: 0\nPOSS: 00:00";
        statsText.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statsText.fontSize = 16;
        statsText.alignment = TextAnchor.UpperLeft;
        statsText.color = Color.white;

        var statsTextRect = statsTextObj.GetComponent<RectTransform>();
        statsTextRect.anchorMin = Vector2.zero;
        statsTextRect.anchorMax = Vector2.one;
        statsTextRect.offsetMin = new Vector2(10, 10);
        statsTextRect.offsetMax = new Vector2(-10, -10);
    }

    private static void SetupLighting()
    {
        // Check for directional light
        var lights = Object.FindObjectsOfType<Light>();
        bool hasDirectional = false;

        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                hasDirectional = true;
                break;
            }
        }

        if (!hasDirectional)
        {
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        Debug.Log("[HockeySceneSetup] Lighting configured");
    }

    private static void WireUpReferences()
    {
        var inputManager = Object.FindObjectOfType<InputManager>();
        var teamManager = Object.FindObjectOfType<TeamManager>();
        var matchManager = Object.FindObjectOfType<MatchManager>();
        var rink = Object.FindObjectOfType<RinkBuilder>();
        var broadcastCam = Object.FindObjectOfType<BroadcastCamera>();
        var puck = Object.FindObjectOfType<Puck>();

        // Wire InputManager to TeamManager
        if (inputManager != null && teamManager != null)
        {
            var imSO = new SerializedObject(inputManager);
            // InputManager will get controlled player from TeamManager.OnPlayerSwitched event
            imSO.ApplyModifiedProperties();
        }

        // Wire MatchManager
        if (matchManager != null && rink != null)
        {
            var mmSO = new SerializedObject(matchManager);
            mmSO.FindProperty("puckSpawnPoint").objectReferenceValue = rink.GetSpawnPoint("PuckSpawn");
            mmSO.FindProperty("playerGoal").objectReferenceValue = rink.HomeGoal;
            mmSO.FindProperty("opponentGoal").objectReferenceValue = rink.AwayGoal;
            mmSO.ApplyModifiedProperties();
        }

        // Wire BroadcastCamera
        if (broadcastCam != null && puck != null)
        {
            var bcSO = new SerializedObject(broadcastCam);
            bcSO.FindProperty("puck").objectReferenceValue = puck.transform;
            // activePlayer will be set by TeamManager at runtime
            bcSO.ApplyModifiedProperties();
        }

        // Wire AIController references for all players
        if (puck != null && rink != null && teamManager != null)
        {
            var allAIControllers = Object.FindObjectsOfType<AIController>();
            Debug.Log($"[HockeySceneSetup] Found {allAIControllers.Length} AIController components to wire up");

            foreach (var aiController in allAIControllers)
            {
                var player = aiController.GetComponent<HockeyPlayer>();
                if (player == null)
                {
                    Debug.LogWarning($"[HockeySceneSetup] AIController on {aiController.name} has no HockeyPlayer component!");
                    continue;
                }

                var aiSO = new SerializedObject(aiController);

                // Set puck reference
                aiSO.FindProperty("puck").objectReferenceValue = puck;

                // Set rinkBuilder reference
                aiSO.FindProperty("rinkBuilder").objectReferenceValue = rink;

                // Set teamManager reference
                aiSO.FindProperty("teamManagerRef").objectReferenceValue = teamManager;

                // Set goal references based on team
                // Team 0 = Home team (attacks away goal, defends home goal)
                // Team 1 = Away team (attacks home goal, defends away goal)
                if (player.TeamId == 0)
                {
                    aiSO.FindProperty("opponentGoal").objectReferenceValue = rink.AwayGoal?.transform;
                    aiSO.FindProperty("ownGoal").objectReferenceValue = rink.HomeGoal?.transform;
                }
                else
                {
                    aiSO.FindProperty("opponentGoal").objectReferenceValue = rink.HomeGoal?.transform;
                    aiSO.FindProperty("ownGoal").objectReferenceValue = rink.AwayGoal?.transform;
                }

                aiSO.ApplyModifiedProperties();
                Debug.Log($"[HockeySceneSetup] Wired up AIController for {player.name} (Team {player.TeamId})");
            }
        }

        Debug.Log("[HockeySceneSetup] All references wired up");
    }

    private static Transform CreateVisualStickHierarchy(Transform parent)
    {
        // StickRoot - positioned at player's side
        var stickRoot = new GameObject("StickRoot");
        stickRoot.transform.SetParent(parent);
        stickRoot.transform.localPosition = new Vector3(0.3f, 0.8f, 0.5f);
        stickRoot.transform.localRotation = Quaternion.Euler(0, 0, -30);

        // StickShaft - cylinder mesh
        var stickShaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stickShaft.name = "StickShaft";
        stickShaft.transform.SetParent(stickRoot.transform);
        stickShaft.transform.localPosition = new Vector3(0, -0.5f, 0);
        stickShaft.transform.localRotation = Quaternion.identity;
        stickShaft.transform.localScale = new Vector3(0.05f, 0.6f, 0.05f);
        Object.DestroyImmediate(stickShaft.GetComponent<Collider>());

        // Create shaft material
        var shaftMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        shaftMat.color = new Color(0.4f, 0.3f, 0.2f); // Brown wood color
        stickShaft.GetComponent<MeshRenderer>().sharedMaterial = shaftMat;

        // StickBlade - cube mesh
        var stickBlade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stickBlade.name = "StickBlade";
        stickBlade.transform.SetParent(stickShaft.transform);
        stickBlade.transform.localPosition = new Vector3(0, -0.7f, 0.15f);
        stickBlade.transform.localRotation = Quaternion.Euler(10, 0, 0);
        stickBlade.transform.localScale = new Vector3(0.6f, 0.15f, 0.4f);
        Object.DestroyImmediate(stickBlade.GetComponent<Collider>());

        // Create blade material
        var bladeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bladeMat.color = Color.black;
        stickBlade.GetComponent<MeshRenderer>().sharedMaterial = bladeMat;

        // BladeContactPoint - empty transform at blade tip
        var bladeContactPoint = new GameObject("BladeContactPoint");
        bladeContactPoint.transform.SetParent(stickBlade.transform);
        bladeContactPoint.transform.localPosition = new Vector3(0, -0.1f, 0.25f);
        bladeContactPoint.transform.localRotation = Quaternion.identity;

        return stickRoot.transform;
    }

    private static void CreateFolderIfNeeded(string parent, string folderName)
    {
        string fullPath = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
