# POV Arms Setup Explanation

## Understanding Your Arms Model

There are two main types of arm models:

### 1. **Rigged Model (with bones/skeleton)**
- Has an **Animator** component
- Has **SkinnedMeshRenderer** (not regular MeshRenderer)
- Contains a bone hierarchy (shoulder → upper arm → forearm → hand)
- **Best for**: Natural arm movement with IK (Inverse Kinematics)
- **Requires**: Animation Rigging package or custom IK script

### 2. **Non-Rigged Model (static mesh)**
- Has regular **MeshRenderer** components
- No bone structure
- Left and right arms might be separate GameObjects or a single mesh
- **Best for**: Simple positioning (what we're doing now)
- **Works with**: Direct transform positioning

## Current Approach

We're using **Input Action References** to directly position the hands/wrists. This works for:
- ✅ Non-rigged models (just move the hand transforms)
- ✅ Rigged models (if you only move the hand bone, the arm will follow naturally)

## What You Need to Do

### Step 1: Run Diagnostics
1. Go to `VirtualVolley > Diagnostics > Diagnose POV Arms Setup`
2. Check the Console output to see:
   - If hands are found
   - If Input Action References are assigned
   - If the model is rigged or not

### Step 2: Fix Controllers
1. Go to `VirtualVolley > Setup > Fix Controllers Visibility`
2. This ensures controllers are visible

### Step 3: Assign Input Action References
1. Select `POVArms` in Hierarchy
2. In Inspector, find `POVArms Input Action Driver` component
3. Manually assign these (drag from Project window):
   - **Left Position Action**: Find `XRI Left/Position` Input Action Reference
   - **Left Rotation Action**: Find `XRI Left/Rotation` Input Action Reference
   - **Left Tracking State Action**: Find `XRI Left/Tracking State` Input Action Reference
   - **Right Position Action**: Find `XRI Right/Position` Input Action Reference
   - **Right Rotation Action**: Find `XRI Right/Rotation` Input Action Reference
   - **Right Tracking State Action**: Find `XRI Right/Tracking State` Input Action Reference

### Step 4: Assign Hand Transforms
1. In the same component, assign:
   - **Left Hand**: Find the left hand/wrist transform in your model
   - **Right Hand**: Find the right hand/wrist transform in your model

## If Your Model is Rigged

If the diagnostic shows you have an **Animator** or **SkinnedMeshRenderer**, you have two options:

### Option A: Simple (Current Approach)
- Just assign the hand/wrist bones to the Left Hand and Right Hand fields
- The arms will follow naturally because they're parented to the hands

### Option B: IK (More Natural)
- Use Unity's Animation Rigging package
- Set up IK constraints to make arms bend naturally
- More complex but looks better

## Troubleshooting

**Controllers not appearing:**
- Run `Fix Controllers Visibility` menu item
- Check that controllers are enabled in XR Origin

**Arms not moving:**
- Check that Input Action References are assigned (not null)
- Check that Hand transforms are assigned
- Run diagnostics to see what's missing
- Check Console for error messages

**Hands not found:**
- Manually expand the POVArms GameObject in Hierarchy
- Find the left and right hand/wrist transforms
- Drag them to the Left Hand and Right Hand fields in Inspector

