using System.Text.Json.Serialization;

namespace GbxTools3D.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TmxSite
{
    TMNF,
    TMUF,
    Nations,
    Sunrise,
    Original
}
