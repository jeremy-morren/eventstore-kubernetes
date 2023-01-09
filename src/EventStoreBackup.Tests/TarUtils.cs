using System.Text;
using System;

namespace EventStoreBackup.Tests;

public static class TarUtils
{
    //Shamelessly stolen from https://stackoverflow.com/a/51975178/6614154
    
    public static async IAsyncEnumerable<string> ListTarFiles(Stream stream)
    {
        if (!stream.CanSeek)
            throw new InvalidOperationException("Stream does not support seeking");
        var buffer = new byte[100];
        while (true)
        {
            var length = await stream.ReadAsync(buffer.AsMemory());
            
            var name = Encoding.ASCII.GetString(buffer, 0, length).Trim('\0');
            if (string.IsNullOrWhiteSpace(name))
                break;
            yield return name;
            
            stream.Seek(24, SeekOrigin.Current);
            length = await stream.ReadAsync(buffer.AsMemory(0, 12));
            
            //All sizes are suffixed with '\0'
            var size = Convert.ToInt64(Encoding.ASCII.GetString(buffer, 0, length - 2).Trim(), 8);
            
            stream.Seek(376L + size, SeekOrigin.Current);

            var offset = 512 - (stream.Position  % 512);
            if (offset == 512)
                offset = 0;

            stream.Seek(offset, SeekOrigin.Current);
        }
    }
}