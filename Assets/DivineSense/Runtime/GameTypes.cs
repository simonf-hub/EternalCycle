using UnityEngine;

namespace DivineSense {
public enum MatchState { Setup, Playing, Finished }
public enum LifeState { Alive, RespawnPlacement }
public enum SceneMode { Heaven, Hell }
public enum GodKind
{
    Zeus = 0,
    [InspectorName("Legacy/Hermes")] Hermes = 1,
    [InspectorName("Legacy/Demeter")] Demeter = 2,
    Athena = 3,
    [InspectorName("Legacy/Dionysus")] Dionysus = 4,
    [InspectorName("Legacy/Poseidon")] Poseidon = 5,
    Ares = 6,
    Hera = 7
}
public enum ActiveKind { Sight, Sound }
public enum MovementKind { Touch, Taste }
}
