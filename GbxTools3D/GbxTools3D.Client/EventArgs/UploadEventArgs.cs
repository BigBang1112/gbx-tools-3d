namespace GbxTools3D.Client.EventArgs;

public sealed record UploadEventArgs(string FileName, byte[] Data, DateTimeOffset LastModified);
