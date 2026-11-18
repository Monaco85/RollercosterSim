using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Achterbahn
{
    /// <summary>
    /// Verbindung zur Achterbahn-Bruecke (Achterbahn.exe): ws://127.0.0.1:8765/telemetrie.
    /// Gesendet wird genau der 31-Werte-Frame (Float32, little endian, 124 Bytes) der HTML-Fassung.
    /// Die Bruecke baut daraus das SimHub-Paket und blendet die Bewegung aus, wenn 300 ms nichts ankommt.
    /// </summary>
    public class BridgeClient
    {
        public const int FrameLen = 31;
        const string Url = "ws://127.0.0.1:8765/telemetrie";

        public volatile bool Connected, Simhub, Sig;

        readonly object lk = new object();
        byte[] latest;
        readonly SemaphoreSlim sem = new SemaphoreSlim(0, 1);
        volatile bool stop;
        ClientWebSocket ws;

        public void Start() { Task.Run(Loop); }

        public void Stop()
        {
            stop = true;
            try { sem.Release(); } catch { }
            try { ws?.Abort(); } catch { }
        }

        /// <summary>Neuester Frame. Aeltere, noch nicht gesendete Frames werden verworfen.</summary>
        public void Send(float[] frame)
        {
            if (frame == null || frame.Length != FrameLen) return;
            var b = new byte[FrameLen * 4];
            Buffer.BlockCopy(frame, 0, b, 0, b.Length);
            lock (lk) latest = b;
            try { sem.Release(); } catch (SemaphoreFullException) { }
        }

        async Task Loop()
        {
            while (!stop)
            {
                try
                {
                    ws = new ClientWebSocket();
                    using (var cts = new CancellationTokenSource(3000))
                        await ws.ConnectAsync(new Uri(Url), cts.Token);
                    Connected = true;
                    var rx = Receive(ws);
                    while (ws.State == WebSocketState.Open && !stop)
                    {
                        await sem.WaitAsync(400);
                        byte[] b; lock (lk) { b = latest; latest = null; }
                        if (b != null) await ws.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                }
                catch { }
                finally
                {
                    Connected = false; Simhub = false; Sig = false;
                    try { ws?.Dispose(); } catch { }
                }
                if (!stop) await Task.Delay(1500);
            }
        }

        async Task Receive(ClientWebSocket c)
        {
            var buf = new byte[512];
            try
            {
                while (c.State == WebSocketState.Open)
                {
                    var r = await c.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
                    if (r.MessageType == WebSocketMessageType.Close) break;
                    string txt = Encoding.UTF8.GetString(buf, 0, r.Count);
                    Simhub = txt.Contains("\"simhub\":true");
                    Sig = txt.Contains("\"sig\":true");
                }
            }
            catch { }
        }
    }
}
