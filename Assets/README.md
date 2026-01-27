# VR Unity Project

## Game Object
### Snowman
- Layer: Player

#### PlayerController

##### Movement
- MoveSpeed
- MaxSpped
- Acceleration

##### Jump
- JumpImpulse
- GroundCheckDistance: 3
- GroundMask: Ground andd Stairs

##### Referances
- GroundCheckOrigin: GroundCheck
- Camera Transform: Main Camera

##### Facing
- FaceMoveDirection: Enable
- TurnSpeedDeg

### Ground
- Layer: Ground

## Prefabs

### Snow

#### Capsule Collider
- IsTrigger: Enable

### Star

#### Sphere Collider
- IsTrigger: Enable

#### Collect Star

- StarNum

### Stairs
- Layer: Stairs