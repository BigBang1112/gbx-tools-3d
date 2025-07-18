using System.Text.Json.Serialization;

namespace GbxTools3D.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MxSite
{
    TM2,
    SM,
    TM2020,
    //QM
}
