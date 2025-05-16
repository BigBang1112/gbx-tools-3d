using GBX.NET.Engines.Plug;
using GBX.NET;
using GbxTools3D.Data.Entities;
using GbxTools3D.Enums;
using GbxTools3D.Data;
using GBX.NET.Engines.Hms;
using Microsoft.EntityFrameworkCore;
using GbxTools3D.Client.Extensions;

namespace GbxTools3D.Services;

internal sealed class SoundService
{
    private readonly AppDbContext db;
    private readonly ILogger<SoundService> logger;

    public SoundService(AppDbContext db, ILogger<SoundService> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public async Task<Sound?> CreateOrUpdateSoundAsync(
        string gamePath,
        CHmsSoundSource soundSource,
        Dictionary<string, Sound> sounds,
        CancellationToken cancellationToken)
    {
        var soundPath = soundSource.PlugSoundFile is null
            ? null
            : Path.GetRelativePath(gamePath, GbxPath.ChangeExtension(soundSource.PlugSoundFile.GetFullPath(), null));
        
        if (soundSource.PlugSound is not CPlugSound plugSound)
        {
            logger.LogWarning("PlugSound is null, possibly unrecognized class: {Path}", soundPath);
            return null;
        }

        return await CreateOrUpdateSoundAsync(gamePath, plugSound, soundPath, sounds, cancellationToken);
    }

    public async Task<Sound?> CreateOrUpdateSoundAsync(string gamePath, CPlugSound plugSound, string? soundPath, Dictionary<string, Sound> sounds, CancellationToken cancellationToken)
    {
        const GameVersion gameVersion = GameVersion.TMF;

        if (soundPath is null)
        {
            return null;
        }

        var hash = $"GbxTools3D|Sound|{gameVersion}|{soundPath}|ItsChallengeNotAltered".Hash();

        var sound = await db.Sounds.FirstOrDefaultAsync(x =>
            x.GameVersion == gameVersion && x.Path == soundPath, cancellationToken) ?? sounds.GetValueOrDefault(soundPath);
        var rawSoundFilePath = plugSound.PlugFileFile?.GetFullPath();

        if (!File.Exists(rawSoundFilePath))
        {
            logger.LogWarning("Sound file not found: {Path}", rawSoundFilePath);
            return null;
        }

        SoundType type;
        if (rawSoundFilePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            type = SoundType.Wav;
        }
        else if (rawSoundFilePath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
        {
            type = SoundType.Ogg;
        }
        else
        {
            throw new Exception($"Unknown sound type: {rawSoundFilePath}");
        }

        var data = await File.ReadAllBytesAsync(rawSoundFilePath, cancellationToken);

        if (sound is null)
        {
            sound = new Sound
            {
                Hash = hash,
                Data = data,
                GameVersion = gameVersion,
                Path = soundPath
            };
            await db.Sounds.AddAsync(sound, cancellationToken);
            sounds.Add(soundPath, sound);
        }

        sound.Hash = hash;
        sound.Data = data;
        sound.AudioPath = Path.GetRelativePath(gamePath, rawSoundFilePath);
        sound.UpdatedAt = DateTime.UtcNow;
        sound.Type = type;

        return sound;
    }
}
