namespace HslCommunication.Devices.Melsec;

/// <summary>
/// 三菱PLC的数据类型，此处包含了几个常用的类型
/// </summary>
public readonly struct MelsecMcDataType : IEquatable<MelsecMcDataType> {
    /// <summary>
    /// Input
    /// </summary>
    public static readonly MelsecMcDataType X = new MelsecMcDataType(0x9C, 0x01, 'X', '*', 16);

    /// <summary>
    /// Output
    /// </summary>
    public static readonly MelsecMcDataType Y = new MelsecMcDataType(0x9D, 0x01, 'Y', '*', 16);

    /// <summary>
    /// Auxiliary Relay, aka, internal memory bit
    /// </summary>
    public static readonly MelsecMcDataType M = new MelsecMcDataType(0x90, 0x01, 'M', '*', 10);

    /// <summary>
    /// Data Register
    /// </summary>
    public static readonly MelsecMcDataType D = new MelsecMcDataType(0xA8, 0x00, 'D', '*', 10);

    /// <summary>
    /// Link Register
    /// </summary>
    public static readonly MelsecMcDataType W = new MelsecMcDataType(0xB4, 0x00, 'W', '*', 16);

    /// <summary>
    /// L latch relay
    /// </summary>
    public static readonly MelsecMcDataType L = new MelsecMcDataType(0x92, 0x01, 'L', '*', 10);

    /// <summary>
    /// F alarm
    /// </summary>
    public static readonly MelsecMcDataType F = new MelsecMcDataType(0x93, 0x01, 'F', '*', 10);

    /// <summary>
    /// V edge relay
    /// </summary>
    public static readonly MelsecMcDataType V = new MelsecMcDataType(0x94, 0x01, 'V', '*', 10);

    /// <summary>
    /// B link relay
    /// </summary>
    public static readonly MelsecMcDataType B = new MelsecMcDataType(0xA0, 0x01, 'B', '*', 16);

    /// <summary>
    /// R file register
    /// </summary>
    public static readonly MelsecMcDataType R = new MelsecMcDataType(0xAF, 0x00, 'R', '*', 10);

    /// <summary>
    /// S step relay
    /// </summary>
    public static readonly MelsecMcDataType S = new MelsecMcDataType(0x98, 0x01, 'S', '*', 10);

    /// <summary>
    /// address register
    /// </summary>
    public static readonly MelsecMcDataType Z = new MelsecMcDataType(0xCC, 0x00, 'Z', '*', 10);

    /// <summary>
    /// timer current value
    /// </summary>
    public static readonly MelsecMcDataType TN = new MelsecMcDataType(0xC2, 0x00, 'T', 'N', 10);

    /// <summary>
    /// timer contact
    /// </summary>
    public static readonly MelsecMcDataType TS = new MelsecMcDataType(0xC1, 0x01, 'T', 'S', 10);

    /// <summary>
    /// timer coil
    /// </summary>
    public static readonly MelsecMcDataType TC = new MelsecMcDataType(0xC0, 0x01, 'T', 'C', 10);

    /// <summary>
    /// accumulative timer contact
    /// </summary>
    public static readonly MelsecMcDataType SS = new MelsecMcDataType(0xC7, 0x01, 'S', 'S', 10);

    /// <summary>
    /// accumulative timer coil
    /// </summary>
    public static readonly MelsecMcDataType SC = new MelsecMcDataType(0xC6, 0x01, 'S', 'C', 10);

    /// <summary>
    /// Current value of the cumulative timer
    /// </summary>
    public static readonly MelsecMcDataType SN = new MelsecMcDataType(0xC8, 0x00, 'S', 'N', 100);

    /// <summary>
    /// Current value of the counter
    /// </summary>
    public static readonly MelsecMcDataType CN = new MelsecMcDataType(0xC5, 0x00, 'C', 'N', 10);

    /// <summary>
    /// Contacts of the counter
    /// </summary>
    public static readonly MelsecMcDataType CS = new MelsecMcDataType(0xC4, 0x01, 'C', 'S', 10);

    /// <summary>
    /// Coils of the counter
    /// </summary>
    public static readonly MelsecMcDataType CC = new MelsecMcDataType(0xC3, 0x01, 'C', 'C', 10);

    /// <summary>
    /// File register ZR area
    /// </summary>
    public static readonly MelsecMcDataType ZR = new MelsecMcDataType(0xB0, 0x00, 'Z', 'R', 16);


    /// <summary>
    /// X Input
    /// </summary>
    public static readonly MelsecMcDataType Keyence_X = new MelsecMcDataType(0x9C, 0x01, 'X', '*', 16);

    /// <summary>
    /// Y Output
    /// </summary>
    public static readonly MelsecMcDataType Keyence_Y = new MelsecMcDataType(0x9D, 0x01, 'Y', '*', 16);

    /// <summary>
    /// Link relay
    /// </summary>
    public static readonly MelsecMcDataType Keyence_B = new MelsecMcDataType(0xA0, 0x01, 'B', '*', 16);

    /// <summary>
    /// Internal auxiliary relay
    /// </summary>
    public static readonly MelsecMcDataType Keyence_M = new MelsecMcDataType(0x90, 0x01, 'M', '*', 10);

    /// <summary>
    /// Latch relay
    /// </summary>
    public static readonly MelsecMcDataType Keyence_L = new MelsecMcDataType(0x92, 0x01, 'L', '*', 10);

    /// <summary>
    /// Control relay
    /// </summary>
    public static readonly MelsecMcDataType Keyence_SM = new MelsecMcDataType(0x91, 0x01, 'S', 'M', 10);

    /// <summary>
    /// Control memory
    /// </summary>
    public static readonly MelsecMcDataType Keyence_SD = new MelsecMcDataType(0xA9, 0x00, 'S', 'D', 10);

    /// <summary>
    /// Data memory
    /// </summary>
    public static readonly MelsecMcDataType Keyence_D = new MelsecMcDataType(0xA8, 0x00, 'D', '*', 10);

    /// <summary>
    /// File register
    /// </summary>
    public static readonly MelsecMcDataType Keyence_R = new MelsecMcDataType(0xAF, 0x00, 'R', '*', 10);

    /// <summary>
    /// File register
    /// </summary>
    public static readonly MelsecMcDataType Keyence_ZR = new MelsecMcDataType(0xB0, 0x00, 'Z', 'R', 16);

    /// <summary>
    /// Link register
    /// </summary>
    public static readonly MelsecMcDataType Keyence_W = new MelsecMcDataType(0xB4, 0x00, 'W', '*', 16);

    /// <summary>
    /// Timer (current value)
    /// </summary>
    public static readonly MelsecMcDataType Keyence_TN = new MelsecMcDataType(0xC2, 0x00, 'T', 'N', 10);

    /// <summary>
    /// Timer (contact)
    /// </summary>
    public static readonly MelsecMcDataType Keyence_TS = new MelsecMcDataType(0xC1, 0x01, 'T', 'S', 10);

    /// <summary>
    /// Counter (current value)
    /// </summary>
    public static readonly MelsecMcDataType Keyence_CN = new MelsecMcDataType(0xC5, 0x00, 'C', 'N', 10);

    /// <summary>
    /// Counter
    /// </summary>
    public static readonly MelsecMcDataType Keyence_CS = new MelsecMcDataType(0xC4, 0x01, 'C', 'S', 10);


    /// <summary>
    /// Input
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_X = new MelsecMcDataType(0x9C, 0x01, 'X', '*', 10);

    /// <summary>
    /// Output
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_Y = new MelsecMcDataType(0x9D, 0x01, 'Y', '*', 10);

    /// <summary>
    /// Link relay
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_L = new MelsecMcDataType(0xA0, 0x01, 'L', '*', 10);

    /// <summary>
    /// Internal relay
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_R = new MelsecMcDataType(0x90, 0x01, 'R', '*', 10);

    /// <summary>
    /// Data memory
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_DT = new MelsecMcDataType(0xA8, 0x00, 'D', '*', 10);

    /// <summary>
    /// Link memory
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_LD = new MelsecMcDataType(0xB4, 0x00, 'W', '*', 10);

    /// <summary>
    /// Timer (current value)
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_TN = new MelsecMcDataType(0xC2, 0x00, 'T', 'N', 10);

    /// <summary>
    /// Timer (contact)
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_TS = new MelsecMcDataType(0xC1, 0x01, 'T', 'S', 10);

    /// <summary>
    /// Counter (current value)
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_CN = new MelsecMcDataType(0xC5, 0x00, 'C', 'N', 10);

    /// <summary>
    /// Counter
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_CS = new MelsecMcDataType(0xC4, 0x01, 'C', 'S', 10);

    /// <summary>
    /// Special Link Relay
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_SM = new MelsecMcDataType(0x91, 0x01, 'S', 'M', 10);

    /// <summary>
    /// Special link memory
    /// </summary>
    public static readonly MelsecMcDataType Panasonic_SD = new MelsecMcDataType(0xA9, 0x00, 'S', 'D', 10);

    private readonly ulong data;

    /// <summary>
    /// Type code value
    /// </summary>
    public byte DataCode => (byte) (this.data & 255);
    
    /// <summary>
    /// Data type, 0 represents word, 1 represents bit
    /// </summary>
    public byte DataType => (byte) (this.data >> 8 & 255);
    
    /// <summary>
    /// The first char of the type code
    /// </summary>
    public char Char1 => (char) (this.data >> 16 & 255);

    /// <summary>
    /// The second char of the type code
    /// </summary>
    public char Char2 => (char) (this.data >> 24 & 255);

    /// <summary>
    /// Indicates whether the address is decimal or hexadecimal
    /// </summary>
    public int FromBase => (int) (this.data >> 32 & 0xFFFFFFFF);

    /// <summary>
    /// Type description when communicating in ASCII format
    /// </summary>
    public string AsciiCode => new string(new char[] {(char) this.Char1, (char) this.Char2});

    public string AsciiCodeOrChar => this.Char2 == '*' ? this.Char1.ToString() : this.AsciiCode;

    /// <summary>
    /// Creates an instance of a Melsec data type
    /// </summary>
    /// <param name="code">Data type code</param>
    /// <param name="type">1 for bit, 0 for word</param>
    /// <param name="asciiCode">Type information in ASCII format</param>
    /// <param name="fromBase">Indicates the base of the address, 10 or 16</param>
    public MelsecMcDataType(byte code, byte type, char ch1, char ch2, int fromBase) {
        if (type != 0 && type != 1)
            throw new ArgumentOutOfRangeException(nameof(type), "Type is out of range. Must be 0 or 1");
        this.data = (ulong) (code | ((long) type << 8) | ((long) ch1 << 16) | ((long) ch2 << 24) | ((long) fromBase << 32));
    }

    public bool Equals(MelsecMcDataType other) => this.data == other.data;

    public override bool Equals(object? obj) => obj is MelsecMcDataType other && this.data == other.data;

    public override int GetHashCode() => this.data.GetHashCode();

    public override string ToString() {
        return $"['{this.AsciiCodeOrChar}' (Code {this.DataCode}, Type {this.DataType}, Base {this.FromBase})]";
    }

    public static bool operator ==(MelsecMcDataType left, MelsecMcDataType right) => left.data == right.data;

    public static bool operator !=(MelsecMcDataType left, MelsecMcDataType right) => left.data != right.data;
}