using CommandLine;

namespace ClientCertificatesCli.Verbs;

public abstract class VerbBase
{
    protected VerbBase(string? @namespace, string? secret)
    {
        Namespace = @namespace;
        Secret = secret ?? Environment.GetEnvironmentVariable("PROXY_CLIENTCERTS")!;
    }
    
    [Option("namespace", Required = false, 
        HelpText = "Kubernetes namespace containing certificate secret")]
    public string? Namespace { get; }
    
    [Option('s', "secret", Required = false,
        HelpText = "Kubernetes secret name containing certificate secret.  Will be created if not found. Default is PROXY_CLIENTCERTS environment variable")]
    public string? Secret { get; }
}