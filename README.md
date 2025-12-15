# V2 Virtual Volley

A VR volleyball training application built with Unity, featuring ball launchers, serve receive, spike receive, and free ball training modes.

## Requirements

- **Unity Version**: 2022.3.62f3 (LTS)
- **VR Headset**: Compatible with OpenXR (Quest, SteamVR, etc.)
- **Platform**: Windows, Android (Quest), or other OpenXR-compatible platforms

## Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd V2_VirtualVolley
```

### 2. Open in Unity

1. Launch Unity Hub
2. Install Unity 2022.3.62f3 (LTS) if not already installed
3. Click "Open" and select the `V2_VirtualVolley` folder
4. Unity will automatically import packages and resolve dependencies

### 3. Initial Setup

Once Unity has finished importing, you need to set up the game systems:

1. Open the main scene (typically `Assets/Scenes/SampleScene.unity` or your main scene)
2. In Unity's menu bar, go to: **`VirtualVolley > Setup > Setup Complete System`**
3. This will automatically create:
   - Game Manager
   - Ball Launcher Manager
   - All ball launchers (service line, net, opponent court)
   - Scene Selection UI
   - Settings UI (for POV arms adjustments)

### 4. Configure Input Actions

The setup script should automatically configure the X button input, but if you encounter issues:

1. Ensure you have the XR Interaction Toolkit package installed (should be automatic)
2. Check that `Assets/Settings/BallLauncherLeftActionButton.asset` exists
3. If missing, the setup script will create it automatically

## Building the Project

### For Windows (PC VR)

1. Go to **File > Build Settings**
2. Select **PC, Mac & Linux Standalone**
3. Choose your target platform (Windows)
4. Click **Build** and select an output folder
5. Run the built executable

### For Android (Quest)

1. Go to **File > Build Settings**
2. Select **Android**
3. Configure Android settings:
   - **Player Settings > XR Plug-in Management**: Enable OpenXR
   - **Player Settings > Other Settings**: Set minimum API level (Android 7.0 or higher)
4. Connect your Quest via USB (with Developer Mode enabled)
5. Click **Build And Run**

## Testing

### In-Editor Testing

1. Press **Play** in the Unity Editor
2. Use the Scene Selection menu (on the left side) to switch between modes:
   - **Free Play**: Spawn balls manually with X button
   - **Free Balls**: Automatic free ball tosses
   - **Serve Receive**: Service line serves
   - **Spike Receive**: Net-height spikes
3. Press **X button** on the left controller to trigger launchers (or spawn balls in Free Play)

### VR Testing

1. Build and run the project on your target platform
2. Put on your VR headset
3. Use the Scene Selection menu to switch modes
4. Press the X button on your left controller to trigger launchers
5. Adjust POV arms settings in the Settings menu if needed

## Project Structure

```
Assets/
├── Scripts/
│   ├── Runtime/          # Core game scripts
│   │   ├── BallLauncher.cs
│   │   ├── BallLauncherManager.cs
│   │   ├── GameManager.cs
│   │   ├── UIEventManager.cs
│   │   └── POVArmsPrimitives.cs
│   └── Editor/          # Editor setup scripts
│       ├── SetupCompleteSystem.cs
│       ├── UpdateLauncherSpeeds.cs
│       └── FixSceneSelectionUI.cs
├── Scenes/              # Unity scenes
├── Settings/            # Input action references
└── Data/                # Models, materials, audio
```

## Key Features

- **Ball Launchers**: Automated ball launchers for different training scenarios
- **Scene Selection**: Switch between Free Play, Free Balls, Serve Receive, and Spike Receive
- **POV Arms**: Adjustable first-person view arms with offset controls
- **VR Interaction**: Full VR support with hand tracking and controller input

## Troubleshooting

### Input Button Not Working

If the X button doesn't work after switching scenes:

1. Run: **`VirtualVolley > UI Setup > Fix Scene Selection UI`**
2. Check that `BallLauncherManager` has the input action reference assigned
3. Ensure the XR Interaction Toolkit is properly configured

### Launchers Not Firing

1. Verify that `Ball Launcher Manager` exists in the scene
2. Check that launchers are assigned to the manager's arrays
3. Ensure the target transform (camera/head) is assigned

### Build Errors

- Ensure all packages are imported (check Package Manager)
- Verify Unity version matches exactly (2022.3.62f3)
- Check that all required assets are included in the build

## Editor Scripts

The project includes several editor scripts for setup and configuration:

- **Setup Complete System**: Initial project setup (run this first)
- **Update Launcher Speeds**: Adjust launcher speeds (2.5x multiplier for service/free balls)
- **Fix Scene Selection UI**: Re-wire scene selection buttons if they stop working

Access these via: **`VirtualVolley`** menu in Unity's menu bar.

