# Enemy Skeletal Export Workflow

Use this folder for 2D skeletal tool output before applying it to Unity enemy prefabs.

Expected folder:

```text
Assets/Art/Enemies/Skeletal/OrcRunner/
  rig.json
  body.png
  head.png
  front_upper_leg.png
  front_lower_leg.png
  back_upper_leg.png
  back_lower_leg.png
  front_arm.png
  back_arm.png
  tail.png
```

The folder name must match the enemy prefab name in `Assets/Prefabs/Enemies`.

In Unity:

1. Select the enemy folder, for example `OrcRunner`.
2. Run `DragonTD > Enemies > Apply Skeletal Rig From Selected Folder`.
3. Open the enemy prefab and tune part positions if needed.
4. Press Play. `EnemyBase` will use `EnemySkeletalAnimator` instead of the flat sprite.

Part names may also use aliases like `torso`, `face`, `weapon_arm`, `right_upper_leg`, `left_lower_leg`, or `scarf`.
