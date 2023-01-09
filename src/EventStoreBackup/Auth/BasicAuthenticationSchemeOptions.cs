using System.Text;
using Microsoft.AspNetCore.Authentication;
using Serilog;
// ReSharper disable StringLiteralTypo

namespace EventStoreBackup.Auth;

public class BasicAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public Encoding Encoding { get; set; } = Encoding.Latin1;

    public override void Validate()
    {
        base.Validate();
        if (Encoding == null)
            throw new InvalidOperationException("Encoding is required");
    }
}