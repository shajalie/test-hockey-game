# GameHUD - Complete Hockey Game HUD System

## Overview
A professional, sports-broadcast style HUD system for your hockey game that automatically creates all UI elements at runtime. Fully integrated with GameManager, MatchManager, TeamManager, and player systems.

## Features

### 1. Score Display
- **Location:** Top center of screen
- **Shows:** Current score for both teams (Player - Opponent)
- **Dynamic Color:** Green when winning, red when losing, white when tied
- **Size:** Large, bold, easy to read

### 2. Game Clock/Timer
- **Location:** Below score
- **Shows:** Match time in MM:SS format
- **Color:** Yellow/gold for visibility
- **Updates:** Real-time countdown

### 3. Period Indicator
- **Location:** Below timer
- **Shows:** Current period (1st, 2nd, 3rd, etc.)
- **Format:** Ordinal suffix (1st, 2nd, 3rd)

### 4. Shot Power Charge Bar
- **Location:** Bottom center of screen
- **Shows:** Shot charging power (0-100%)
- **Color:** Yellow to red gradient as power increases
- **Visibility:** Only shows when charging a shot
- **Integration:** Call `SetShotPowerCharge(float power, bool isCharging)` from shooting system

### 5. Stamina Bar
- **Location:** Bottom left
- **Shows:** Current player's stamina percentage
- **Color:** Green (high), yellow (medium), red (low)
- **Updates:** Automatically from controlled HockeyPlayer

### 6. Puck Possession Indicator
- **Location:** Top right
- **Shows:**
  - "YOUR PUCK" (green) - player's team has possession
  - "OPPONENT PUCK" (red) - opponent has possession
  - "LOOSE PUCK" (yellow) - no possession
- **Updates:** Automatically via GameEvents

### 7. Current Controlled Player Name
- **Location:** Bottom left, above stamina
- **Shows:** Position name (CENTER, LEFT WING, etc.)
- **Color:** Team color background

### 8. Mini Player Switch Indicator
- **Location:** Next to controlled player display
- **Shows:** "SWITCH: Q/E" hint
- **Visibility:** Only shows when multiple players available
- **Effect:** Flashes when player switches

### 9. Goal Celebration Popup
- **Location:** Center screen
- **Shows:** "GOAL!" or "OPPONENT SCORES!"
- **Effect:** Pulsing scale animation
- **Duration:** 3 seconds
- **Color:** Green for player goal, red for opponent goal

### 10. Face-off Indicator
- **Location:** Center screen
- **Shows:** "FACE OFF" message
- **Duration:** 2 seconds (configurable)
- **Usage:** Automatically shown on match start, or call `ShowFaceoffMessage(string message, float duration)`

## Setup Instructions

### Quick Setup (Automatic)
1. Create an empty GameObject in your scene
2. Name it "GameHUD"
3. Add the `GameHUD` component
4. Press Play - the HUD will auto-create all UI elements!

### Manual Setup (Advanced)
If you want to customize the HUD:
1. Set `autoCreateUI = false` in the inspector
2. Create a Canvas manually and assign it to `hudCanvas`
3. Create your custom UI elements
4. The script will still handle all the logic and updates

### Integration with Existing Systems

#### GameManager
- Automatically finds GameManager.Instance
- Reads PlayerScore and OpponentScore
- Subscribes to OnGoalScored, OnMatchStart, OnMatchEnd events

#### MatchManager
- Automatically finds MatchManager via FindObjectOfType
- Can be manually assigned in inspector
- Used for timer and match state

#### TeamManager
- Automatically finds TeamManager via FindObjectOfType
- Subscribes to OnPlayerSwitched event
- Updates controlled player display

#### HockeyPlayer
- Automatically tracks controlled player from TeamManager
- Reads CurrentStamina, MaxStamina, Position
- Updates stamina bar in real-time

#### Puck System
- Subscribes to GameEvents.OnPuckPossessionChanged
- Updates possession indicator automatically

## Public API

### Shot Power Integration
```csharp
// From your shooting controller:
GameHUD hud = FindObjectOfType<GameHUD>();

// When player starts charging shot:
hud.SetShotPowerCharge(0f, true);

// While charging (in Update):
float chargePercent = chargeTime / maxChargeTime;
hud.SetShotPowerCharge(chargePercent, true);

// When shot is released:
hud.SetShotPowerCharge(0f, false);
```

### Period Management
```csharp
// Update period display:
hud.SetPeriod(2); // Shows "2nd PERIOD"
```

### Custom Face-off Messages
```csharp
// Show custom message:
hud.ShowFaceoffMessage("OVERTIME!", 3f);
hud.ShowFaceoffMessage("PENALTY SHOT!", 2.5f);
```

## Customization

### Inspector Settings
- **Auto Create UI:** Enable/disable automatic UI generation
- **Canvas Sort Order:** Control rendering order (default: 100)
- **Show Debug Info:** Display debug information overlay
- **Shot Power Display Time:** How long to show shot power bar
- **Goal Celebration Time:** Duration of goal popup
- **Faceoff Indicator Time:** Duration of faceoff message

### Visual Customization
All UI elements are created with default professional styling, but you can easily modify:
- Colors: Edit the Color values in the Create methods
- Sizes: Edit the `sizeDelta` values
- Positions: Edit the `anchoredPosition` values
- Fonts: Assign custom TextMeshPro fonts

### Performance Optimization
- All TextMeshPro elements have `raycastTarget = false` for better performance
- UI elements are only updated when necessary
- Coroutines are used for animations to avoid Update() overhead

## Architecture

### Event-Driven Design
The HUD uses Unity's event system for decoupled communication:
- Subscribes to `GameEvents` for game state changes
- Subscribes to `TeamManager.OnPlayerSwitched` for player control
- No tight coupling to game systems

### Auto-Creation System
The HUD can automatically create all UI elements if they don't exist:
- Creates Canvas with proper settings
- Generates all TextMeshPro elements
- Sets up Image components for bars
- Configures RectTransforms for proper positioning

### Clean Code Structure
- Organized into regions: Initialization, UI Update, Event Handlers, Display Effects
- Helper methods for creating UI elements
- Public API for external integration

## Troubleshooting

### HUD Not Showing
1. Check if Canvas exists in hierarchy
2. Verify `autoCreateUI` is enabled
3. Check Canvas render mode is ScreenSpaceOverlay
4. Look for errors in Console

### Score Not Updating
1. Verify GameManager.Instance exists
2. Check if GameEvents.OnGoalScored is being triggered
3. Enable `showDebugInfo` to see manager status

### Stamina Bar Not Working
1. Check if TeamManager is finding/assigning controlled player
2. Verify HockeyPlayer has stamina system
3. Check CurrentStamina and MaxStamina properties exist

### Shot Power Not Showing
1. Call `SetShotPowerCharge(power, true)` from shooting system
2. Verify shotPowerContainer is being created
3. Check isChargingShot flag is being set

## Best Practices

1. **Single Instance:** Only have one GameHUD in your scene
2. **Manager References:** Let the HUD find managers automatically
3. **Event Subscription:** Always unsubscribe in OnDisable()
4. **Performance:** Use the auto-creation feature for consistency
5. **Customization:** Modify the Create methods before first play, not in inspector

## Future Enhancements

Potential additions you could make:
- Penalty timer display
- Power play indicator
- Shot counter for each team
- Time on ice for current player
- Mini-map or radar
- Replay indicator
- Commentary text ticker
- Player stats overlay (press Tab)

## Example Scene Setup

```
Scene Hierarchy:
├── GameManager (singleton, persists)
├── MatchManager
├── TeamManager
├── GameHUD (this script)
├── Players (spawned by TeamManager)
├── Puck
└── Rink/Environment
```

## Credits
Created for Test Hockey Game
Integrates with: GameManager, MatchManager, TeamManager, HockeyPlayer, Puck, GameEvents
UI Framework: Unity UI + TextMeshPro
