using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// WebSocket Client class.
/// Responsible for handling communication between server and ClientObject.
/// </summary>
public class WsClient : IDisposable
{
    // WebSocket
    private readonly ClientWebSocket ws = new();
    private CancellationTokenSource cts;
    private readonly UTF8Encoding encoder = new(); // For websocket text message encoding.
    private const UInt64 MAXREADSIZE = 1 * 1024 * 1024;

    // Server address
    private readonly Uri ServerUri;

    //JWT
    private readonly string JWT;

    // Queues
    public ConcurrentQueue<string> ReceiveQueue { get; }
    public BlockingCollection<ArraySegment<byte>> SendQueue { get; }

    // Threads
    private Thread ReceiveThread { get; set; }
    private Thread SendThread { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:WsClient"/> class.
    /// </summary>
    /// <param name="serverURL">Server URL.</param>
    public WsClient(string serverURL, string JWT)
    {
        this.JWT = JWT;
        ws.Options.AddSubProtocol(JWT);

        ServerUri = new Uri(serverURL);

        ReceiveQueue = new ConcurrentQueue<string>();
        ReceiveThread = new Thread(RunReceiveAsync);
        ReceiveThread.Start();

        SendQueue = new BlockingCollection<ArraySegment<byte>>();
        SendThread = new Thread(RunSendAsync);
        SendThread.Start();
    }

    public void Dispose()
    {
        ws.Dispose();
        ReceiveThread.Abort();
        SendThread.Abort();
    }

    /// <summary>
    /// Method which connects ClientObject to the server.
    /// </summary>
    public async Task ConnectAsync()
    {
        Debug.Log("Connecting to: " + ServerUri);
        cts = new();
        await ws.ConnectAsync(ServerUri, cts.Token);
        while (IsConnecting())
        {
            Debug.Log("Waiting to connect...");
            Task.Delay(50).Wait();
        }
        Debug.Log("Connect status: " + ws.State);
    }

    public async Task CloseAsync()
    {
        Debug.Log("WsClient CloseAsync.");
        if (ws != null)
        {
            try
            {
                // Cancel all async operations immediately
                cts?.Cancel();

                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                {
                    Debug.Log("Closing connection to: " + ServerUri);
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application Closed Normally", CancellationToken.None);
                    while (!IsClosed())
                    {
                        Debug.Log("Waiting to close...");
                        Task.Delay(50).Wait();
                    }
                    Debug.Log("Connect status: " + ws.State);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error while closing WebSocket: {ex.Message}");
            }
            finally
            {
                Dispose();
                cts?.Dispose();
            }
        }
    }

    #region [Status]

    /// <summary>
    /// Return if is connecting to the server.
    /// </summary>
    /// <returns><c>true</c>, if is connecting to the server, <c>false</c> otherwise.</returns>
    public bool IsConnecting()
    {
        return ws.State == WebSocketState.Connecting;
    }

    /// <summary>
    /// Return if connection to the server is closed.
    /// </summary>
    /// <returns><c>true</c>, if connection to the server is closed, <c>false</c> otherwise.</returns>
    public bool IsClosed()
    {
        return ws.State == WebSocketState.Closed;
    }

    /// <summary>
    /// Return if connection with server is open.
    /// </summary>
    /// <returns><c>true</c>, if connection with server is open, <c>false</c> otherwise.</returns>
    public bool IsConnectionOpen()
    {
        return ws.State == WebSocketState.Open;
    }

    #endregion

    #region [Send]

    /// <summary>
    /// Method used to send a message to the server.
    /// </summary>
    /// <param name="message">Message.</param>
    public void Send(string message)
    {
        byte[] buffer = encoder.GetBytes(message);
        //Debug.Log("Message to queue for send: " + buffer.Length + ", message: " + message);
        var sendBuf = new ArraySegment<byte>(buffer);

        SendQueue.Add(sendBuf);
    }

    /// <summary>
    /// Method for other thread, which sends messages to the server.
    /// </summary>
    private async void RunSendAsync()
    {
        Debug.Log("WebSocket Message Sender looping.");
        ArraySegment<byte> msg;
        while (true)
        {
            while (!SendQueue.IsCompleted)
            {
                msg = SendQueue.Take();
                //Debug.Log("Dequeued this message to send: " + msg);
                try
                {
                    await ws.SendAsync(msg, WebSocketMessageType.Text, true /* is last part of message */, cts.Token);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
            }
        }
    }

    #endregion

    #region [Receive]

    /// <summary>
    /// Reads the message from the server.
    /// </summary>
    /// <returns>The message.</returns>
    /// <param name="maxSize">Max size.</param>
    private async Task<string> ReceiveAsync(UInt64 maxSize = MAXREADSIZE)
    {
        // A read buffer, and a memory stream to stuff unknown number of chunks into:
        byte[] buf = new byte[4 * 1024];
        var ms = new MemoryStream();
        ArraySegment<byte> arrayBuf = new ArraySegment<byte>(buf);
        WebSocketReceiveResult chunkResult = null;

        if (IsConnectionOpen())
        {
            do
            {
                chunkResult = await ws.ReceiveAsync(arrayBuf, cts.Token);
                ms.Write(arrayBuf.Array, arrayBuf.Offset, chunkResult.Count);
                //Debug.Log("Size of Chunk message: " + chunkResult.Count);
                if ((UInt64)(chunkResult.Count) > MAXREADSIZE)
                {
                    Console.Error.WriteLine("Warning: Message is bigger than expected!");
                }
            } while (!chunkResult.EndOfMessage);
            ms.Seek(0, SeekOrigin.Begin);

            // Looking for UTF-8 JSON type messages.
            if (chunkResult.MessageType == WebSocketMessageType.Text)
            {
                return CommunicationUtils.StreamToString(ms, Encoding.UTF8);
            }

        }

        return "";
    }

    /// <summary>
    /// Method for other thread, which receives messages from the server.
    /// </summary>
    private async void RunReceiveAsync()
    {
        Debug.Log("WebSocket Message Receiver looping.");
        string result;
        while (true)
        {
            //Debug.Log("Awaiting ReceiveAsync...");
            result = await ReceiveAsync();
            if (result != null && result.Length > 0)
            {
                ReceiveQueue.Enqueue(result);
            }
            else
            {
                Task.Delay(50).Wait();
            }
        }
    }

    #endregion
}
