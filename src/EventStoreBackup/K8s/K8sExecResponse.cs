using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming

namespace EventStoreBackup.K8s;

/// <summary>
/// Represents the result of executing
/// a command in a Kubernetes Pod
/// </summary>
[JsonSerializable(typeof(K8sExecResponse))]
public sealed class K8sExecResponse
{
    /// <summary>
    /// Contents written to <c>StdOut</c>
    /// </summary>
    public string? StdOut { get; set; }
    
    /// <summary>
    /// Contents written to <c>StdErr</c>
    /// </summary>
    public string? StdErr { get; set; }

    /// <summary>
    /// Indicates failure or success of the command
    /// </summary>
    public K8sExecStatus Status { get; set; }
    
    /// <summary>
    /// When <see cref="Status"/> is <see cref="K8sExecStatus.Failure"/>, a
    /// describe reason for the error
    /// </summary>
    public string? Message { get; set; }
    
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}

/// <summary>Indicates success or failure of a Kubernetes command</summary>
public enum K8sExecStatus
{
    Success,
    Failure,
    Unknown
}