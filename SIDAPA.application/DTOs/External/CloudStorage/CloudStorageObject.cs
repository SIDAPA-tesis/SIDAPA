namespace SIDAPA.application.DTOs.External.CloudStorage;

/// <summary>Referencia a un objeto almacenado (resultado de una subida).</summary>
public record CloudStorageObject(string ObjectPath, long SizeInBytes, string ContentType);
