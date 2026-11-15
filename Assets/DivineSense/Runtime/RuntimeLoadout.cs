namespace DivineSense {
[System.Serializable] public sealed class RuntimeLoadout {
 public int BundleId, OriginalPlayerId;
 public ActiveKind ActiveKind; public MovementKind MovementKind;
 public float ActiveReadyAt;
 public RuntimeLoadout(int id, ActiveKind active, MovementKind movement) { BundleId=id; OriginalPlayerId=id; ActiveKind=active; MovementKind=movement; }
} }