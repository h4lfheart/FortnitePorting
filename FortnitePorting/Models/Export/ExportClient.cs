using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;
using FortnitePorting.Extensions;
using FortnitePorting.Services;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.Models.Export;

public enum EExportCommandType : byte
{
    Message = 0,
    Data = 1
}

public class ExportClient(EExportServerType serverType) : IDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private TcpClient? _client;
    private Thread? _listenerThread;
    private volatile bool _isListening;

    public async Task<bool> IsRunning()
    {
        await _connectionLock.WaitAsync();
        try
        {
            await EnsureConnectedAsync();
            return IsConnectedInternal();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task SendDataAsync<T>(T data, EExportCommandType commandType)
    {
        var jsonBytes = SerializeData(data);

        await _connectionLock.WaitAsync();
        try
        {
            await EnsureConnectedAsync();
            
            if (!IsConnectedInternal())
                return;

            var success = await WriteDataAsync(commandType, jsonBytes);
            
            if (!success)
            {
                await EnsureConnectedAsync();
                
                if (IsConnectedInternal())
                    await WriteDataAsync(commandType, jsonBytes);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private static byte[] SerializeData<T>(T data)
    {
        var json = JsonConvert.SerializeObject(data);
        return Encoding.UTF8.GetBytes(json);
    }

    private async Task<bool> WriteDataAsync(EExportCommandType commandType, byte[] jsonBytes)
    {
        if (_client?.GetStream() is not { } stream)
            return false;

        try
        {
            var archive = new FArchiveWriter();
            archive.Write((byte)commandType);
            archive.Write(jsonBytes.Length);
            archive.Write(jsonBytes);

            var buffer = archive.GetBuffer();
            await stream.WriteAsync(buffer);
            await stream.FlushAsync();
            return true;
        }
        catch
        {
            CloseConnection();
            return false;
        }
    }

    private bool IsConnectedInternal()
    {
        if (_client is null)
            return false;

        try
        {
            var socket = _client.Client;
            
            if (!socket.Connected)
                return false;
            
            var canWrite = socket.Poll(1000, SelectMode.SelectWrite);
            var hasError = socket.Poll(1000, SelectMode.SelectError);
            
            if (hasError)
                return false;
            
            var remoteClosed = socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0;
            
            return canWrite && !remoteClosed;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnsureConnectedAsync()
    {
        if (_client is null || !_client.Connected)
        {
            CloseConnection();
            await ConnectAsync();
            return;
        }
        
        if (!IsConnectedInternal())
        {
            CloseConnection();
            await ConnectAsync();
        }
    }

    private async Task ConnectAsync()
    {
        _client = new TcpClient();

        try
        {
            await _client.ConnectAsync("127.0.0.1", (int) serverType);
            StartListener();
        }
        catch
        {
            CloseConnection();
        }
    }

    private void StartListener()
    {
        StopListener();
        
        _isListening = true;
        _listenerThread = new Thread(ListenerLoop)
        {
            IsBackground = true,
            Name = "ExportClientListener"
        };
        _listenerThread.Start();
    }

    private void StopListener()
    {
        _isListening = false;

        if (_listenerThread != null)
        {
            if (_listenerThread.IsAlive)
            {
                _listenerThread.Join(TimeSpan.FromSeconds(2));
            }
            _listenerThread = null;
        }
    }

    private void ListenerLoop()
    {
        while (_isListening)
        {
            try
            {
                if (_client?.GetStream() is not { } stream)
                    continue;
                
                var commandType = (EExportCommandType) Read<byte>(stream);

                var length = Read<int>(stream);

                var dataBytes = ReadExact(stream, length);
                var jsonData = Encoding.UTF8.GetString(dataBytes);
                
                HandleReceivedMessage(commandType, jsonData);
            }
            catch (Exception ex)
            {
                // ignored
            }
        }
    }
    
    
    private static T Read<T>(NetworkStream stream) where T : struct
    {
        var size = Unsafe.SizeOf<T>();
        var buffer = ReadExact(stream, size);
        return Unsafe.ReadUnaligned<T>(ref buffer[0]);
    }

    private static byte[] ReadExact(NetworkStream stream, int length)
    {
        var buffer = new byte[length];
        var totalRead = 0;
        while (totalRead < length)
        {
            var bytesRead = stream.Read(buffer, totalRead, length - totalRead);
            if (bytesRead == 0)
                throw new EndOfStreamException();
            totalRead += bytesRead;
        }
        return buffer;
    }


    private void HandleReceivedMessage(EExportCommandType commandType, string jsonData)
    {
        var message = JsonConvert.DeserializeObject<string>(jsonData) ?? string.Empty;
        Info.Message($"{serverType.Description} Server", message);
    }

    private void CloseConnection()
    {
        StopListener();
        
        try
        {
            _client?.Close();
        }
        catch
        {
            // ignored
        }
        finally
        {
            _client?.Dispose();
            _client = null;
        }
    }

    public void Dispose()
    {
        _connectionLock.Wait();
        try
        {
            CloseConnection();
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
    }
}