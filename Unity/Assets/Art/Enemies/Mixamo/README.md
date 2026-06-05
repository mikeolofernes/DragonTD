Mixamo enemy animation source folder.

Drop downloaded Mixamo FBX files here:

- one character/model FBX, for example `Orc_Model.fbx`
- one idle animation FBX with `Idle` in the name
- one walk animation FBX with `Walk` in the name

Then run Unity menu:

`DragonTD/Enemies/Install Mixamo Enemy Animator`

The installer creates `MixamoEnemy.controller`, attaches the model to enemy prefabs as
`MixamoModel`, drives Animator `Speed`, and rotates the model to match path direction.
