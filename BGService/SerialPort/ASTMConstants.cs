namespace BGService.SerialPort;

public static class ASTMConstants
{
    public const byte ENQ = 0x05;
    public const byte ACK = 0x06;
    public const byte NAK = 0x15;
    public const byte STX = 0x02;
    public const byte ETX = 0x03;
    public const byte ETB = 0x17;
    public const byte EOT = 0x04;
    public const byte CR = 0x0D;
    public const byte LF = 0x0A;

    public const char FieldSeparator = '|';
    public const char RepeatSeparator = '\\';
    public const char ComponentSeparator = '^';
    public const char EscapeCharacter = '&';
}

public enum ProtocolState
{
    Idle,
    WaitForEnq,
    WaitForFrame,
    Transferring,
    Complete
}
