using UnityEngine;

/// <summary>
/// Procedurally generates a hockey rink with proper physics setup.
/// </summary>
public class RinkBuilder : MonoBehaviour
{
    [Header("Rink Dimensions")]
    [SerializeField] private float length = 60f; // NHL standard ~60m
    [SerializeField] private float width = 30f;  // NHL standard ~30m
    [SerializeField] private float wallHeight = 1.2f;
    [SerializeField] private float wallThickness = 0.3f;
    [SerializeField] private float cornerRadius = 8f;
    [SerializeField] private int cornerSegments = 8;

    [Header("Goal Dimensions")]
    [SerializeField] private float goalWidth = 1.8f;
    [SerializeField] private float goalHeight = 1.2f;
    [SerializeField] private float goalDepth = 1f;

    [Header("Physics Materials")]
    [SerializeField] private PhysicsMaterial iceMaterial;
    [SerializeField] private PhysicsMaterial boardsMaterial;

    [Header("Visual Materials")]
    [SerializeField] private Material iceVisualMaterial;
    [SerializeField] private Material boardsVisualMaterial;
    [SerializeField] private Material goalVisualMaterial;

    [Header("References (Auto-Generated)")]
    [SerializeField] private GameObject rinkRoot;
    [SerializeField] private Goal homeGoal;
    [SerializeField] private Goal awayGoal;

    [Header("Auto Build")]
    [SerializeField] private bool buildOnStart = true;

    // Properties
    public float Length => length;
    public float Width => width;
    public Goal HomeGoal => homeGoal;
    public Goal AwayGoal => awayGoal;
    public Vector3 CenterIce => Vector3.zero;
    public Vector3 HomeGoalPosition => new Vector3(-length / 2f + goalDepth, 0, 0);
    public Vector3 AwayGoalPosition => new Vector3(length / 2f - goalDepth, 0, 0);

    private void Start()
    {
        if (buildOnStart && rinkRoot == null)
        {
            BuildRink();
        }
    }

    [ContextMenu("Build Rink")]
    public void BuildRink()
    {
        // Clear existing
        if (rinkRoot != null)
        {
            DestroyImmediate(rinkRoot);
        }

        rinkRoot = new GameObject("Rink");
        rinkRoot.transform.SetParent(transform);
        rinkRoot.transform.localPosition = Vector3.zero;

        // Create physics materials if not assigned
        CreateDefaultPhysicsMaterials();

        // Build components
        CreateIceSurface();
        CreateBoards();
        CreateGoals();
        CreateSpawnPoints();
        CreateInvisibleBoundaries();

        Debug.Log("[RinkBuilder] Rink built successfully!");
    }

    private void CreateDefaultPhysicsMaterials()
    {
        // Create ice material (low friction)
        if (iceMaterial == null)
        {
            iceMaterial = new PhysicsMaterial("Ice");
            iceMaterial.dynamicFriction = 0.02f;
            iceMaterial.staticFriction = 0.02f;
            iceMaterial.bounciness = 0.1f;
            iceMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            iceMaterial.bounceCombine = PhysicsMaterialCombine.Average;
        }

        // Create boards material (bouncy)
        if (boardsMaterial == null)
        {
            boardsMaterial = new PhysicsMaterial("Boards");
            boardsMaterial.dynamicFriction = 0.4f;
            boardsMaterial.staticFriction = 0.4f;
            boardsMaterial.bounciness = 0.6f;
            boardsMaterial.frictionCombine = PhysicsMaterialCombine.Average;
            boardsMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
        }
    }

    private void CreateIceSurface()
    {
        GameObject ice = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ice.name = "IceSurface";
        ice.transform.SetParent(rinkRoot.transform);
        ice.transform.localPosition = new Vector3(0, -0.05f, 0);
        ice.transform.localScale = new Vector3(length, 0.1f, width);

        // Apply physics material
        var collider = ice.GetComponent<BoxCollider>();
        collider.material = iceMaterial;

        // Apply visual material
        var renderer = ice.GetComponent<MeshRenderer>();
        if (iceVisualMaterial != null)
        {
            renderer.sharedMaterial = iceVisualMaterial;
        }
        else
        {
            // Default ice color - create a new material to avoid modifying shared default
            Material iceMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            iceMat.color = new Color(0.9f, 0.95f, 1f);
            renderer.sharedMaterial = iceMat;
        }

        ice.layer = LayerMask.NameToLayer("Ground");
    }

    private void CreateBoards()
    {
        GameObject boards = new GameObject("Boards");
        boards.transform.SetParent(rinkRoot.transform);

        float halfLength = length / 2f - cornerRadius;
        float halfWidth = width / 2f - cornerRadius;

        // Long sides (along X axis)
        CreateWall(boards.transform, new Vector3(0, wallHeight / 2f, width / 2f),
                   new Vector3(length - cornerRadius * 2f, wallHeight, wallThickness), "NorthWall");
        CreateWall(boards.transform, new Vector3(0, wallHeight / 2f, -width / 2f),
                   new Vector3(length - cornerRadius * 2f, wallHeight, wallThickness), "SouthWall");

        // Short sides (along Z axis) - with gap for goals
        float sideWallLength = (width - cornerRadius * 2f - goalWidth) / 2f;

        // Home end (left, -X)
        CreateWall(boards.transform, new Vector3(-length / 2f, wallHeight / 2f, width / 4f + goalWidth / 4f),
                   new Vector3(wallThickness, wallHeight, sideWallLength), "HomeWallTop");
        CreateWall(boards.transform, new Vector3(-length / 2f, wallHeight / 2f, -width / 4f - goalWidth / 4f),
                   new Vector3(wallThickness, wallHeight, sideWallLength), "HomeWallBottom");

        // Away end (right, +X)
        CreateWall(boards.transform, new Vector3(length / 2f, wallHeight / 2f, width / 4f + goalWidth / 4f),
                   new Vector3(wallThickness, wallHeight, sideWallLength), "AwayWallTop");
        CreateWall(boards.transform, new Vector3(length / 2f, wallHeight / 2f, -width / 4f - goalWidth / 4f),
                   new Vector3(wallThickness, wallHeight, sideWallLength), "AwayWallBottom");

        // Corners
        CreateCorner(boards.transform, new Vector3(halfLength, 0, halfWidth), 0, "CornerNE");
        CreateCorner(boards.transform, new Vector3(-halfLength, 0, halfWidth), 90, "CornerNW");
        CreateCorner(boards.transform, new Vector3(-halfLength, 0, -halfWidth), 180, "CornerSW");
        CreateCorner(boards.transform, new Vector3(halfLength, 0, -halfWidth), 270, "CornerSE");
    }

    private void CreateWall(Transform parent, Vector3 position, Vector3 size, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = position;
        wall.transform.localScale = size;

        var collider = wall.GetComponent<BoxCollider>();
        collider.material = boardsMaterial;

        var renderer = wall.GetComponent<MeshRenderer>();
        if (boardsVisualMaterial != null)
        {
            renderer.sharedMaterial = boardsVisualMaterial;
        }
        else
        {
            // Create shared wall material once
            if (_wallMaterial == null)
            {
                _wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                _wallMaterial.color = new Color(0.2f, 0.2f, 0.3f);
            }
            renderer.sharedMaterial = _wallMaterial;
        }

        wall.layer = LayerMask.NameToLayer("Wall");
        wall.tag = "Wall";
    }

    // Cached materials for edit mode
    private static Material _wallMaterial;
    private static Material _goalMaterial;

    private void CreateCorner(Transform parent, Vector3 centerPosition, float startAngle, string name)
    {
        GameObject corner = new GameObject(name);
        corner.transform.SetParent(parent);
        corner.transform.localPosition = centerPosition;

        for (int i = 0; i < cornerSegments; i++)
        {
            float angle1 = (startAngle + (90f / cornerSegments) * i) * Mathf.Deg2Rad;
            float angle2 = (startAngle + (90f / cornerSegments) * (i + 1)) * Mathf.Deg2Rad;

            Vector3 p1 = new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * cornerRadius;
            Vector3 p2 = new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * cornerRadius;
            Vector3 mid = (p1 + p2) / 2f;

            float segmentLength = Vector3.Distance(p1, p2);
            float segmentAngle = Mathf.Atan2(p2.z - p1.z, p2.x - p1.x) * Mathf.Rad2Deg;

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"Segment_{i}";
            segment.transform.SetParent(corner.transform);
            segment.transform.localPosition = mid + Vector3.up * wallHeight / 2f;
            segment.transform.localRotation = Quaternion.Euler(0, -segmentAngle + 90, 0);
            segment.transform.localScale = new Vector3(wallThickness, wallHeight, segmentLength + 0.1f);

            var collider = segment.GetComponent<BoxCollider>();
            collider.material = boardsMaterial;

            var renderer = segment.GetComponent<MeshRenderer>();
            if (boardsVisualMaterial != null)
            {
                renderer.sharedMaterial = boardsVisualMaterial;
            }
            else
            {
                if (_wallMaterial == null)
                {
                    _wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    _wallMaterial.color = new Color(0.2f, 0.2f, 0.3f);
                }
                renderer.sharedMaterial = _wallMaterial;
            }

            segment.layer = LayerMask.NameToLayer("Wall");
            segment.tag = "Wall";
        }
    }

    private void CreateGoals()
    {
        // Home goal (Team 0 defends, Team 1 scores here)
        GameObject homeGoalObj = CreateGoalStructure("HomeGoal", HomeGoalPosition, Quaternion.Euler(0, 90, 0));
        homeGoal = homeGoalObj.GetComponentInChildren<Goal>();
        if (homeGoal != null)
        {
            // Home goal = Team 0's goal, so when puck enters, Team 1 (index 1) would score
            // But based on our Goal.cs logic, teamIndex is who OWNS the goal
            // So Team 0 owns home goal, Team 1 scores when puck enters
        }

        // Away goal (Team 1 defends, Team 0 scores here)
        GameObject awayGoalObj = CreateGoalStructure("AwayGoal", AwayGoalPosition, Quaternion.Euler(0, -90, 0));
        awayGoal = awayGoalObj.GetComponentInChildren<Goal>();
    }

    private GameObject CreateGoalStructure(string name, Vector3 position, Quaternion rotation)
    {
        GameObject goal = new GameObject(name);
        goal.transform.SetParent(rinkRoot.transform);
        goal.transform.localPosition = position;
        goal.transform.localRotation = rotation;

        // Goal frame (visual)
        GameObject frame = new GameObject("Frame");
        frame.transform.SetParent(goal.transform);

        // Crossbar
        GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crossbar.name = "Crossbar";
        crossbar.transform.SetParent(frame.transform);
        crossbar.transform.localPosition = new Vector3(0, goalHeight, 0);
        crossbar.transform.localRotation = Quaternion.Euler(0, 0, 90);
        crossbar.transform.localScale = new Vector3(0.05f, goalWidth / 2f, 0.05f);
        ApplyGoalMaterial(crossbar);
        ApplyGoalPostPhysics(crossbar);

        // Posts
        GameObject leftPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftPost.name = "LeftPost";
        leftPost.transform.SetParent(frame.transform);
        leftPost.transform.localPosition = new Vector3(0, goalHeight / 2f, goalWidth / 2f);
        leftPost.transform.localScale = new Vector3(0.05f, goalHeight / 2f, 0.05f);
        ApplyGoalMaterial(leftPost);
        ApplyGoalPostPhysics(leftPost);

        GameObject rightPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightPost.name = "RightPost";
        rightPost.transform.SetParent(frame.transform);
        rightPost.transform.localPosition = new Vector3(0, goalHeight / 2f, -goalWidth / 2f);
        rightPost.transform.localScale = new Vector3(0.05f, goalHeight / 2f, 0.05f);
        ApplyGoalMaterial(rightPost);
        ApplyGoalPostPhysics(rightPost);

        // Back of net (visual netting + collision)
        CreateGoalNetting(goal.transform);

        // Goal trigger zone
        GameObject trigger = new GameObject("GoalTrigger");
        trigger.transform.SetParent(goal.transform);
        trigger.transform.localPosition = new Vector3(goalDepth / 2f, goalHeight / 2f, 0);

        BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(goalDepth, goalHeight, goalWidth);

        Goal goalComponent = trigger.AddComponent<Goal>();
        trigger.tag = "Goal";

        return goal;
    }

    private void ApplyGoalMaterial(GameObject obj)
    {
        var renderer = obj.GetComponent<MeshRenderer>();
        if (goalVisualMaterial != null)
        {
            renderer.sharedMaterial = goalVisualMaterial;
        }
        else
        {
            if (_goalMaterial == null)
            {
                _goalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                _goalMaterial.color = Color.red;
            }
            renderer.sharedMaterial = _goalMaterial;
        }
    }

    private void ApplyGoalPostPhysics(GameObject obj)
    {
        // Create bouncy physics material for posts (PING!)
        PhysicsMaterial postMaterial = new PhysicsMaterial("GoalPost");
        postMaterial.bounciness = 0.8f;
        postMaterial.dynamicFriction = 0.3f;
        postMaterial.staticFriction = 0.3f;
        postMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;

        var collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            collider.material = postMaterial;
        }

        obj.layer = LayerMask.NameToLayer("Wall");
        obj.tag = "GoalPost";
    }

    private void CreateGoalNetting(Transform goalTransform)
    {
        GameObject netting = new GameObject("Netting");
        netting.transform.SetParent(goalTransform);
        netting.transform.localPosition = Vector3.zero;

        // Back panel of net
        GameObject backNet = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backNet.name = "BackNet";
        backNet.transform.SetParent(netting.transform);
        backNet.transform.localPosition = new Vector3(goalDepth, goalHeight / 2f, 0);
        backNet.transform.localScale = new Vector3(0.1f, goalHeight, goalWidth);

        var backCollider = backNet.GetComponent<BoxCollider>();
        backCollider.material = boardsMaterial;
        backNet.layer = LayerMask.NameToLayer("Wall");

        // Make netting semi-transparent white
        var renderer = backNet.GetComponent<MeshRenderer>();
        Material netMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        netMat.color = new Color(1f, 1f, 1f, 0.5f);
        renderer.sharedMaterial = netMat;

        // Side panels
        GameObject leftNet = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftNet.name = "LeftNet";
        leftNet.transform.SetParent(netting.transform);
        leftNet.transform.localPosition = new Vector3(goalDepth / 2f, goalHeight / 2f, goalWidth / 2f);
        leftNet.transform.localScale = new Vector3(goalDepth, goalHeight, 0.1f);
        leftNet.GetComponent<BoxCollider>().material = boardsMaterial;
        leftNet.layer = LayerMask.NameToLayer("Wall");
        leftNet.GetComponent<MeshRenderer>().sharedMaterial = netMat;

        GameObject rightNet = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightNet.name = "RightNet";
        rightNet.transform.SetParent(netting.transform);
        rightNet.transform.localPosition = new Vector3(goalDepth / 2f, goalHeight / 2f, -goalWidth / 2f);
        rightNet.transform.localScale = new Vector3(goalDepth, goalHeight, 0.1f);
        rightNet.GetComponent<BoxCollider>().material = boardsMaterial;
        rightNet.layer = LayerMask.NameToLayer("Wall");
        rightNet.GetComponent<MeshRenderer>().sharedMaterial = netMat;
    }

    private void CreateSpawnPoints()
    {
        GameObject spawns = new GameObject("SpawnPoints");
        spawns.transform.SetParent(rinkRoot.transform);

        // Center ice faceoff
        CreateSpawnPoint(spawns.transform, "CenterFaceoff", Vector3.zero);

        // Player spawn (home side)
        CreateSpawnPoint(spawns.transform, "PlayerSpawn", new Vector3(-length / 4f, 0, 0));

        // AI spawn (away side)
        CreateSpawnPoint(spawns.transform, "AISpawn", new Vector3(length / 4f, 0, 0));

        // Puck spawn
        CreateSpawnPoint(spawns.transform, "PuckSpawn", new Vector3(0, 0.1f, 0));
    }

    private void CreateSpawnPoint(Transform parent, string name, Vector3 position)
    {
        GameObject spawn = new GameObject(name);
        spawn.transform.SetParent(parent);
        spawn.transform.localPosition = position;

        // Face toward center
        if (position.x < 0)
        {
            spawn.transform.localRotation = Quaternion.Euler(0, 90, 0);
        }
        else if (position.x > 0)
        {
            spawn.transform.localRotation = Quaternion.Euler(0, -90, 0);
        }
    }

    /// <summary>
    /// Creates invisible boundary walls to prevent players/puck from escaping.
    /// </summary>
    private void CreateInvisibleBoundaries()
    {
        GameObject boundaries = new GameObject("InvisibleBoundaries");
        boundaries.transform.SetParent(rinkRoot.transform);

        float boundaryHeight = 10f; // Tall invisible walls
        float boundaryThickness = 2f;
        float extraMargin = 3f; // Extra space beyond boards

        // Invisible walls around entire rink (taller than visible boards)
        // North wall
        CreateInvisibleWall(boundaries.transform, "BoundaryNorth",
            new Vector3(0, boundaryHeight / 2f, width / 2f + extraMargin),
            new Vector3(length + extraMargin * 2, boundaryHeight, boundaryThickness));

        // South wall
        CreateInvisibleWall(boundaries.transform, "BoundarySouth",
            new Vector3(0, boundaryHeight / 2f, -width / 2f - extraMargin),
            new Vector3(length + extraMargin * 2, boundaryHeight, boundaryThickness));

        // East wall (behind away goal)
        CreateInvisibleWall(boundaries.transform, "BoundaryEast",
            new Vector3(length / 2f + extraMargin, boundaryHeight / 2f, 0),
            new Vector3(boundaryThickness, boundaryHeight, width + extraMargin * 2));

        // West wall (behind home goal)
        CreateInvisibleWall(boundaries.transform, "BoundaryWest",
            new Vector3(-length / 2f - extraMargin, boundaryHeight / 2f, 0),
            new Vector3(boundaryThickness, boundaryHeight, width + extraMargin * 2));

        // Floor boundary (in case something falls through)
        CreateInvisibleWall(boundaries.transform, "BoundaryFloor",
            new Vector3(0, -2f, 0),
            new Vector3(length + extraMargin * 4, 1f, width + extraMargin * 4));

        Debug.Log("[RinkBuilder] Invisible boundaries created");
    }

    private void CreateInvisibleWall(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.localPosition = position;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
        collider.material = boardsMaterial;

        wall.layer = LayerMask.NameToLayer("Wall");
        wall.tag = "Wall";
    }

    // === UTILITY ===

    /// <summary>
    /// Get spawn point by name.
    /// </summary>
    public Transform GetSpawnPoint(string name)
    {
        if (rinkRoot == null) return null;

        Transform spawns = rinkRoot.transform.Find("SpawnPoints");
        if (spawns == null) return null;

        return spawns.Find(name);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw rink outline
        Gizmos.color = Color.cyan;

        Vector3 halfSize = new Vector3(length / 2f, 0, width / 2f);

        // Draw rectangle outline
        Gizmos.DrawLine(new Vector3(-halfSize.x, 0, halfSize.z), new Vector3(halfSize.x, 0, halfSize.z));
        Gizmos.DrawLine(new Vector3(-halfSize.x, 0, -halfSize.z), new Vector3(halfSize.x, 0, -halfSize.z));
        Gizmos.DrawLine(new Vector3(-halfSize.x, 0, halfSize.z), new Vector3(-halfSize.x, 0, -halfSize.z));
        Gizmos.DrawLine(new Vector3(halfSize.x, 0, halfSize.z), new Vector3(halfSize.x, 0, -halfSize.z));

        // Draw goal positions
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(HomeGoalPosition, new Vector3(goalDepth, goalHeight, goalWidth));
        Gizmos.DrawWireCube(AwayGoalPosition, new Vector3(goalDepth, goalHeight, goalWidth));
    }
}
