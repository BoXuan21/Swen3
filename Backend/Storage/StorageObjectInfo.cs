namespace Swen3.API.Storage;

public sealed record StorageObjectInfo(string ObjectKey, string OriginalFileName, string ContentType, long Size);

