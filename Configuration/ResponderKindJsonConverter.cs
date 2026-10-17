using System.Text.Json;
using System.Text.Json.Serialization;
using CrowdCheckpointSim.Core;

namespace CrowdCheckpointSim.Configuration;

/// <summary>Writes wheeled robot identities and migrates older humanoid robot posts.</summary>
public sealed class ResponderKindJsonConverter : JsonConverter<ResponderKind>
{
    public override ResponderKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int value) && value is 0 or 1)
        {
            return (ResponderKind)value;
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            string? name = reader.GetString();
            if (string.Equals(name, "PurpleOperator", StringComparison.OrdinalIgnoreCase) || name == "0")
                return ResponderKind.PurpleOperator;
            if (string.Equals(name, "WheeledRobot", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "HumanoidRobot", StringComparison.OrdinalIgnoreCase) || name == "1")
                return ResponderKind.WheeledRobot;
        }
        throw new JsonException("Unknown responder kind. Expected PurpleOperator or WheeledRobot.");
    }

    public override void Write(Utf8JsonWriter writer, ResponderKind value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            ResponderKind.PurpleOperator => "PurpleOperator",
            ResponderKind.WheeledRobot => "WheeledRobot",
            _ => throw new JsonException("Cannot save an unknown responder kind."),
        });
    }
}
