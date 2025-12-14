# Hockey Stick System - Setup Guide

## Overview

The Hockey Stick system provides a complete visual and functional stick implementation for hockey players in Unity. It includes:

- Visual stick attachment to player
- Automatic rotation following player movement
- Blade contact point for puck interactions
- Animation states (skating, shooting, passing, poke check)
- Integration with existing HockeyPlayer.cs framework

## Architecture

### Core Components

1. **HockeyStick.cs** - Main stick component
   - Manages stick positioning and rotation
   - Handles animation state transitions
   - Provides blade contact point tracking
   - Optional puck glow visual effect

2. **HockeyPlayer.cs** (Updated) - Player controller
   - Auto-detects HockeyStick component
   - Smart StickTip property (uses HockeyStick if available)
   - Backward compatible with legacy stick setup

3. **HockeyStickIntegration.cs** (Optional) - Example integration
   - Shows usage patterns for shooting, passing, poke checks
   - Example AI and input integration
   - Reference implementation for custom actions

### Animation States

```
Skating     - Default neutral position while moving
Shooting    - Stick drawn back during shot charge/release
Passing     - Stick angled for pass execution
PokeCheck   - Stick extended forward with reach extension
```

## Quick Start Setup

### Step 1: Create Stick Hierarchy

Create the following GameObject hierarchy under your player model:

```
PlayerObject
├── PlayerModel
│   └── StickRoot (empty GameObject)
│       ├── StickShaft (visual mesh - cylinder or custom model)
│       ├── StickBlade (visual mesh - blade model)
│       └── BladeContactPoint (empty GameObject at blade tip)
├── HockeyPlayer (component)
└── HockeyStick (component)
```

#### Recommended Transform Values:

**StickRoot** (relative to player):
- Position: (0.3, 0.5, 0.4)
- Rotation: (-45, 0, 0)
- Scale: (1, 1, 1)

**StickShaft** (relative to StickRoot):
- Position: (0, 0, 0)
- Rotation: (0, 0, 0)
- Scale: (0.05, 0.8, 0.05)
- Mesh: Cylinder (or custom stick model)

**StickBlade** (relative to StickRoot):
- Position: (0, -0.8, 0)
- Rotation: (90, 0, 0)
- Scale: (0.15, 0.05, 0.25)
- Mesh: Cube (or custom blade model)

**BladeContactPoint** (relative to StickRoot):
- Position: (0, -0.9, 0.15) ← This is where puck attaches
- Rotation: (0, 0, 0)
- Scale: (1, 1, 1)

### Step 2: Add HockeyStick Component

1. Select your player GameObject
2. Add Component → HockeyStick
3. Assign references in inspector:
   - **Stick Root**: The StickRoot GameObject
   - **Stick Shaft**: The StickShaft GameObject (visual)
   - **Stick Blade**: The StickBlade GameObject (visual)
   - **Blade Contact Point**: The BladeContactPoint GameObject

### Step 3: Configure Settings

**Stick Positioning:**
- Adjust `stickPositionOffset` to position stick relative to player
- Adjust `stickRotationOffset` to angle stick correctly
- Adjust `followSmoothness` for responsive vs smooth following (0.1-0.3 recommended)

**Animation Settings:**
- Configure angle offsets for each animation state
- Adjust `pokeCheckExtension` for reach distance
- Set `animationTransitionSpeed` (5-10 recommended)

**Visual Effects (Optional):**
- Enable `enablePuckGlow` for blade glow when carrying puck
- Assign a glowing material to `glowMaterial`
- Configure `puckGlowColor` (yellow/orange recommended)

### Step 4: Integration

The HockeyStick automatically integrates with HockeyPlayer. No code changes required!

**Accessing the stick in code:**

```csharp
HockeyPlayer player = GetComponent<HockeyPlayer>();

// Get stick reference
HockeyStick stick = player.Stick;

// Get blade position (automatically uses HockeyStick if available)
Transform bladePos = player.StickTip;

// Trigger animations
stick.TriggerShootAnimation();
stick.TriggerPassAnimation();
stick.TriggerPokeCheck();
```

## Advanced Usage

### Manual Animation Control

```csharp
// Set specific animation state
stick.SetAnimationState(StickAnimationState.Shooting);

// States auto-transition back to Skating after action completes
// Or manually return:
stick.SetAnimationState(StickAnimationState.Skating);
```

### Blade Position Queries

```csharp
// Get blade world position
Vector3 bladePos = stick.BladePosition;

// Get blade forward direction
Vector3 bladeForward = stick.BladeForward;

// Check if point is in range
bool inRange = stick.IsPointInRange(puckPosition, 1.5f);

// Get blade velocity (matches player velocity)
Vector3 velocity = stick.GetBladeVelocity();
```

### Integration with Shooting

```csharp
// In ShootingController.cs
public class ShootingController : MonoBehaviour
{
    private HockeyStick stick;

    void Awake()
    {
        stick = GetComponent<HockeyStick>();
    }

    void ShootPuck()
    {
        // ... shooting logic ...

        // Trigger stick animation
        stick.TriggerShootAnimation();
    }
}
```

### Integration with Poke Check

```csharp
// In InputManager.cs or AIController.cs
if (Input.GetButtonDown("PokeCheck"))
{
    HockeyStick stick = player.Stick;
    stick.TriggerPokeCheck();

    // Optionally check for hits
    if (stick.IsPointInRange(puckPosition, 2f))
    {
        // Knock puck loose
    }
}
```

## Visual Setup Options

### Option 1: Simple Primitives (Quick Setup)

Use Unity primitives for rapid prototyping:
- **Shaft**: White cylinder (0.05 x 0.8 x 0.05)
- **Blade**: Black cube (0.15 x 0.05 x 0.25)

Materials:
```
Shaft: Standard shader, white color
Blade: Standard shader, black color
```

### Option 2: Custom Models (Production Quality)

Import custom 3D stick model:
1. Create stick model in Blender/Maya (separate shaft and blade)
2. Import to Unity (FBX format)
3. Assign to StickShaft and StickBlade references
4. Adjust scale and position to match player

### Option 3: Team-Colored Sticks

Dynamically change stick colors per team:

```csharp
// In TeamManager or player initialization
Material stickMaterial = player.Stick.GetComponent<Renderer>().material;
stickMaterial.color = teamColor;
```

## Puck Glow Effect Setup

To enable the blade glow effect when player has puck:

1. Create glowing material:
   - Shader: Standard (or URP/Lit)
   - Enable Emission
   - Set Emission color to black (will be set at runtime)

2. Assign to HockeyStick:
   - Enable `enablePuckGlow`
   - Assign material to `glowMaterial`
   - Set `puckGlowColor` (e.g., orange: `#FF9900`)

3. The blade will automatically glow when player has puck!

## Troubleshooting

### Stick not visible
- Check that StickShaft and StickBlade have Mesh Renderer components
- Verify materials are assigned
- Check layer isn't hidden by camera culling mask

### Stick in wrong position
- Adjust `stickPositionOffset` in inspector
- Verify StickRoot hierarchy is correct
- Check player model scale (should be uniform)

### Stick rotating incorrectly
- Adjust `stickRotationOffset` in inspector
- Ensure player Rigidbody constraints are set (freeze X and Z rotation)
- Check `followSmoothness` value (lower = more responsive)

### Puck not attaching to blade
- Verify BladeContactPoint is positioned at blade tip
- Check PuckController is using `player.StickTip` for attachment
- Enable debug gizmos to visualize blade position

### Animations not triggering
- Ensure animation methods are being called (add Debug.Log)
- Check `animationTransitionSpeed` isn't too slow
- Verify stick isn't disabled or destroyed

## Performance Considerations

The HockeyStick system is lightweight and optimized:

- **Update calls**: 1 per frame (Update + LateUpdate)
- **Allocations**: Zero during gameplay (all variables pre-allocated)
- **Physics**: No physics computations (purely visual/transform based)
- **Recommended**: Up to 20 players with sticks = no performance impact

## Legacy Compatibility

The system maintains full backward compatibility:

```csharp
// Old code still works
Transform stickTip = player.StickTip;

// Behind the scenes:
// - If HockeyStick exists → returns BladeContactPoint
// - If not → returns legacy stickTip Transform
```

To migrate from legacy setup:
1. Add HockeyStick component
2. Create stick hierarchy
3. Keep old `stickTip` reference as fallback
4. System auto-detects and uses new stick

## Example Scene Setup Checklist

- [ ] Player GameObject with HockeyPlayer component
- [ ] StickRoot hierarchy created under player model
- [ ] StickShaft and StickBlade meshes assigned
- [ ] BladeContactPoint positioned at blade tip
- [ ] HockeyStick component added and references assigned
- [ ] Position/rotation offsets configured
- [ ] Animation angles tuned to your preference
- [ ] Debug gizmos enabled to verify positioning
- [ ] Test in Play mode - stick follows player rotation
- [ ] Test animations (shoot, pass, poke check)
- [ ] Verify puck attachment to BladeContactPoint

## Next Steps

1. **Puck Integration**: Update PuckController to use `player.StickTip` for attachment
2. **Shooting Integration**: Call `stick.TriggerShootAnimation()` in ShootingController
3. **Passing System**: Implement pass logic with `stick.TriggerPassAnimation()`
4. **Poke Check**: Implement poke check detection using `stick.IsPointInRange()`
5. **Visual Polish**: Add team colors, glow effects, particle trails
6. **Animation Polish**: Fine-tune angles and timings for best feel

## Support Files

- **HockeyStick.cs**: Main component (C:\Users\sammy\Documents\UnityProjects\Test Hockey Game\Assets\_Scripts\Units\HockeyStick.cs)
- **HockeyPlayer.cs**: Updated player controller with integration
- **HockeyStickIntegration.cs**: Example usage patterns
- **This Guide**: Setup and usage documentation

## Summary

The Hockey Stick system provides a complete, production-ready solution for hockey stick visuals and gameplay mechanics. It integrates seamlessly with your existing HockeyPlayer framework while maintaining backward compatibility.

Key benefits:
- Zero-code integration (just add component)
- Smooth, responsive stick following
- Professional animation states
- Flexible blade contact tracking
- Optional visual effects
- Performance optimized
- Full debug visualization

Happy hockey development!
