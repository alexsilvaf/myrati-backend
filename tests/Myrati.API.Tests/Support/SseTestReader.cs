using System.Text;

namespace Myrati.API.Tests.Support;

public static class SseTestReader
{
    public sealed record Message(string? Event, string Data);

    public static async Task<Message> ReadNextMessageAsync(StreamReader reader, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        string? eventName = null;
        var dataBuilder = new StringBuilder();

        while (DateTime.UtcNow < deadline)
        {
            var remaining = deadline - DateTime.UtcNow;
            var line = await reader.ReadLineAsync().WaitAsync(remaining);

            if (line is null)
            {
                throw new EndOfStreamException("Fluxo SSE encerrado antes do esperado.");
            }

            if (line.Length == 0)
            {
                if (eventName is not null || dataBuilder.Length > 0)
                {
                    return new Message(eventName, dataBuilder.ToString());
                }

                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventName = line["event:".Length..].Trim();
                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (dataBuilder.Length > 0)
                {
                    dataBuilder.Append('\n');
                }

                dataBuilder.Append(line["data:".Length..].Trim());
            }
        }

        throw new TimeoutException("Nenhuma mensagem SSE foi recebida no tempo esperado.");
    }

    public static async Task<Message> ReadUntilEventAsync(
        StreamReader reader,
        string eventName,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var message = await ReadNextMessageAsync(reader, deadline - DateTime.UtcNow);
            if (message.Event == eventName)
            {
                return message;
            }
        }

        throw new TimeoutException($"Evento SSE '{eventName}' não recebido.");
    }
}
