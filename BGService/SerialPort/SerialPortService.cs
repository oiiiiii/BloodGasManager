using System.IO.Ports;
using System.Text;
using BGShared.Utils;

namespace BGService.SerialPort;

public class SerialPortSettings
{
    public string PortName { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits StopBits { get; set; } = StopBits.One;
    public bool AutoReconnect { get; set; } = true;
    public int ReconnectIntervalMs { get; set; } = 5000;
    public bool EnableAck { get; set; } = true;
}

public class SerialPortService : IDisposable
{
    private System.IO.Ports.SerialPort? _port;
    private CancellationTokenSource? _cts;
    private Thread? _reconnectThread;
    private bool _disposed;

    // ASTM协议状态 - 使用字节流缓冲
    private readonly MemoryStream _messageBuffer = new(1024);
    private readonly MemoryStream _frameBuffer = new(1024);
    private bool _waitForStx = true;
    private static readonly byte[] _cr = new byte[] { 0x0D };

    public SerialPortSettings Settings { get; }
    public bool IsOpen => _port?.IsOpen ?? false;

    public event Action<string>? OnLog;
    public event Action<string, byte[]>? OnMessageReceived;
    public event Action<bool>? OnConnectionChanged;

    public SerialPortService(SerialPortSettings settings)
    {
        Settings = settings;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        OpenPort();

        if (Settings.AutoReconnect)
        {
            _reconnectThread = new Thread(ReconnectLoop) { IsBackground = true, Name = "SerialReconnect" };
            _reconnectThread.Start();
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        ClosePort();
    }

    private void OpenPort()
    {
        try
        {
            ClosePort();

            _port = new System.IO.Ports.SerialPort(
                Settings.PortName,
                Settings.BaudRate,
                Settings.Parity,
                Settings.DataBits,
                Settings.StopBits);

            _port.ReadBufferSize = 1024;
            _port.WriteBufferSize = 256;
            _port.Encoding = EncodingHelper.GBK;
            _port.DataReceived += Port_DataReceived;
            _port.ErrorReceived += Port_ErrorReceived;

            _port.Open();
            ResetASTMState();

            Log($"串口 {Settings.PortName} 已打开 (波特率={Settings.BaudRate}, 数据位={Settings.DataBits}, 校验={Settings.Parity}, 停止位={Settings.StopBits})");
            OnConnectionChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            Log($"打开串口失败: {ex.Message}");
            OnConnectionChanged?.Invoke(false);
        }
    }

    private void ClosePort()
    {
        if (_port != null)
        {
            try
            {
                if (_port.IsOpen)
                {
                    _port.Close();
                    Log($"串口 {Settings.PortName} 已关闭");
                }
            }
            catch (Exception ex)
            {
                Log($"关闭串口失败: {ex.Message}");
            }

            try { _port.Dispose(); }
            catch (Exception ex)
            {
                Log($"释放串口资源失败: {ex.Message}");
            }
            _port = null;
            OnConnectionChanged?.Invoke(false);
        }
    }

    private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_port == null || !_port.IsOpen) return;

        try
        {
            int bytesToRead = _port.BytesToRead;
            if (bytesToRead <= 0) return;

            byte[] buffer = new byte[bytesToRead];
            int read = _port.Read(buffer, 0, bytesToRead);

            if (read > 0)
            {
                ProcessASTM(buffer, read);
            }
        }
        catch (Exception ex)
        {
            Log($"读取数据错误: {ex.Message}");
        }
    }

    private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        Log($"串口错误: {e.EventType}");
    }

    /// <summary>
    /// 处理ASTM协议 - 逐字节处理，使用MemoryStream缓冲
    /// </summary>
    private void ProcessASTM(byte[] buffer, int length)
    {
        // 记录原始数据
        var hex = BitConverter.ToString(buffer, 0, length).Replace("-", " ");
        Log($"接收({length}字节): {hex}");

        for (int i = 0; i < length; i++)
        {
            byte b = buffer[i];

            // ENQ (0x05) - 建立连接
            if (b == ASTMConstants.ENQ)
            {
                Log("收到ENQ, 回复ACK");
                ResetASTMState();
                SendAck();
                continue;
            }

            // EOT (0x04) - 传输结束
            if (b == ASTMConstants.EOT)
            {
                Log("收到EOT, 传输结束");
                _waitForStx = true;

                if (_messageBuffer.Length > 0)
                {
                    var msgBytes = new byte[(int)_messageBuffer.Length];
                    _messageBuffer.Position = 0;
                    _messageBuffer.Read(msgBytes, 0, msgBytes.Length);
                    var messageText = EncodingHelper.GBK.GetString(msgBytes);

                    Log($"消息完成, 长度{msgBytes.Length}字节");
                    Log($"消息预览: {messageText.Substring(0, Math.Min(100, messageText.Length))}");

                    OnMessageReceived?.Invoke(messageText, msgBytes);
                    _messageBuffer.SetLength(0);
                }
                continue;
            }

            // STX (0x02) - 帧开始
            if (b == ASTMConstants.STX)
            {
                Log("检测到STX, 开始帧");
                _frameBuffer.SetLength(0);
                _waitForStx = false;
                continue;
            }

            // ETX (0x03) 或 ETB (0x17) - 帧结束
            if (b == ASTMConstants.ETX || b == ASTMConstants.ETB)
            {
                var frameLen = (int)_frameBuffer.Length;
                Log($"检测到{(b == ASTMConstants.ETX ? "ETX" : "ETB")}, 帧长度{frameLen}字节");

                // 将帧内容加入消息缓冲区
                if (frameLen > 0)
                {
                    var frameBytes = new byte[frameLen];
                    _frameBuffer.Position = 0;
                    _frameBuffer.Read(frameBytes, 0, frameLen);

                    // 帧内容写入消息缓冲，跳过帧号和校验和
                    int start = 0;
                    if (frameBytes.Length > 0 && char.IsDigit((char)frameBytes[0]))
                    {
                        start = 1; // 跳过帧号
                    }

                    // 找到校验和位置（最后2字节 + CR）
                    int end = frameBytes.Length;
                    for (int j = frameBytes.Length - 1; j >= start; j--)
                    {
                        if (frameBytes[j] == ASTMConstants.CR)
                        {
                            end = j - 2; // 跳过校验和
                            break;
                        }
                    }
                    if (end < start) end = frameBytes.Length;

                    // 写入消息缓冲
                    for (int j = start; j < end; j++)
                    {
                        _messageBuffer.WriteByte(frameBytes[j]);
                    }
                    _messageBuffer.Write(_cr, 0, 1);
                }

                _waitForStx = true;
                SendAck();
                Log("帧处理完成, 已回复ACK");
                continue;
            }

            // NAK (0x15)
            if (b == ASTMConstants.NAK)
            {
                Log("收到NAK");
                continue;
            }

            // ACK (0x06)
            if (b == ASTMConstants.ACK)
            {
                Log("收到ACK");
                continue;
            }

            // 数据内容 - 写入帧缓冲区
            if (!_waitForStx)
            {
                _frameBuffer.WriteByte(b);
            }
        }
    }

    private void ResetASTMState()
    {
        _messageBuffer.SetLength(0);
        _frameBuffer.SetLength(0);
        _waitForStx = true;
    }

    private void SendAck()
    {
        if (Settings.EnableAck && _port != null && _port.IsOpen)
        {
            try
            {
                _port.Write(new byte[] { ASTMConstants.ACK }, 0, 1);
            }
            catch (Exception ex)
            {
                Log($"回复ACK失败: {ex.Message}");
            }
        }
    }

    private void ReconnectLoop()
    {
        while (_cts != null && !_cts.IsCancellationRequested)
        {
            try
            {
                Thread.Sleep(Settings.ReconnectIntervalMs);
            }
            catch (Exception ex)
            {
                Log($"重连等待中断: {ex.Message}");
                break;
            }

            if (_cts.IsCancellationRequested) break;

            if (!IsOpen)
            {
                Log($"尝试重新连接串口 {Settings.PortName}...");
                OpenPort();
            }
        }
    }

    private void Log(string message)
    {
        OnLog?.Invoke($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _port?.Dispose();
        _cts?.Dispose();
        _messageBuffer?.Dispose();
        _frameBuffer?.Dispose();
    }
}