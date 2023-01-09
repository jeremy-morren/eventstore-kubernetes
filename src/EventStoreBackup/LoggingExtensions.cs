using EventStoreBackup.K8s;
using k8s.Models;
using Serilog;

namespace EventStoreBackup;

public static class LoggingExtensions
{
    public static LoggerConfiguration DestructureObjects(this LoggerConfiguration conf) => conf
        .Destructure.ByTransforming<V1Pod>(p => new {Name = p.Name(), Namespace = p.Namespace()})
        .Destructure.ByTransforming<K8sExecResponse>(p => new
        {
            p.Status,
            StdOut = K8sExecException.FormatMessage(p.StdOut),
            StdErr = K8sExecException.FormatMessage(p.StdErr),
            Message = K8sExecException.FormatMessage(p.Message)
        });
}