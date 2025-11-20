using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace ZLauncher.Services;

public class ServerStatusService
{
    public async Task<(string Status, int Count)> PingServerAsync(string host, int port)
    {
        if (host == "0.0.0.0") return ("Оффлайн", 0); //поменять на реальный айпи адрес а не локалку

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);

            if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask)
            {
                return ("Оффлайн", 0);
            }

            using var stream = client.GetStream();
            
            using var ms = new MemoryStream();
            WriteVarInt(ms, 0);
            WriteVarInt(ms, -1);
            WriteString(ms, host);
            ms.Write(BitConverter.GetBytes((ushort)port).Reverse().ToArray()); 
            WriteVarInt(ms, 1);

            WriteVarInt(stream, (int)ms.Length);
            ms.Position = 0;
            await ms.CopyToAsync(stream);

            ms.SetLength(0);
            WriteVarInt(ms, 0);
            WriteVarInt(stream, (int)ms.Length);
            ms.Position = 0;
            await ms.CopyToAsync(stream);

            var packetLength = ReadVarInt(stream);
            var packetId = ReadVarInt(stream);

            if (packetId == -1) return ("Оффлайн", 0);

            var jsonLength = ReadVarInt(stream);
            var buffer = new byte[jsonLength];
            int bytesRead = 0;
            while (bytesRead < jsonLength)
            {
                int read = await stream.ReadAsync(buffer, bytesRead, jsonLength - bytesRead);
                if (read == 0) break;
                bytesRead += read;
            }

            var json = Encoding.UTF8.GetString(buffer);
            var doc = JsonDocument.Parse(json);
            
            int online = 0;
            if (doc.RootElement.TryGetProperty("players", out var players))
            {
                if (players.TryGetProperty("online", out var onlineProp))
                {
                    online = onlineProp.GetInt32();
                }
            }

            return ("Онлайн", online);
        }
        catch
        {
            return ("Оффлайн", 0);
        }
    }

    private void WriteVarInt(Stream stream, int value)
    {
        while ((value & 128) != 0)
        {
            stream.WriteByte((byte)(value & 127 | 128));
            value = (int)((uint)value >> 7);
        }
        stream.WriteByte((byte)value);
    }

    private void WriteString(Stream stream, string data)
    {
        var buffer = Encoding.UTF8.GetBytes(data);
        WriteVarInt(stream, buffer.Length);
        stream.Write(buffer);
    }

    private int ReadVarInt(Stream stream)
    {
        int numRead = 0;
        int result = 0;
        byte read;
        do
        {
            int b = stream.ReadByte();
            if (b == -1) return -1;
            read = (byte)b;
            int value = (read & 0b01111111);
            result |= (value << (7 * numRead));
            numRead++;
            if (numRead > 5) return -1;
        } while ((read & 0b10000000) != 0);

        return result;
    }
}