using System.Numerics;

namespace CrowdCheckpointSim.Core;

/// <summary>Responder identity; both types share the existing operator state machine.</summary>
public enum ResponderKind
{
    /// <summary>Human Purple operator, including its visual-confirmation delay.</summary>
    PurpleOperator = 0,

    /// <summary>Wheeled robot with immediate localisation and fixed on-site clearance.</summary>
    WheeledRobot = 1,

    /// <summary>Source compatibility for earlier configuration/client code; robots are now wheeled.</summary>
    HumanoidRobot = WheeledRobot,
}

/// <summary>A typed setup post, retained across configuration saves and simulation restarts.</summary>
public readonly record struct ResponderPost(Vector2 Position, ResponderKind Kind);
