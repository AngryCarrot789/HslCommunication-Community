using System.Collections;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net.NetworkBase;
using HslCommunication.Core.Transfer;
using HslCommunication.Core.Types;

namespace HslCommunication.Devices.LSIS;

/// <summary>
/// XGB Fast Enet I/F module supports open Ethernet. It provides network configuration that is to connect LSIS and other company PLC, PC on network
/// </summary>
/// <remarks>
/// Address example likes the follow
/// [welcome to finish]
/// </remarks>
public class XGBFastEnet : NetworkDeviceBase<LsisFastEnetMessage, RegularByteTransform> {
    /// <summary>
    /// Instantiate a Default object
    /// </summary>
    public XGBFastEnet() {
        this.WordLength = 2;
        this.IpAddress = string.Empty;
        this.Port = 2004;
    }

    /// <summary>
    /// Instantiate a object by ipaddress and port
    /// </summary>
    /// <param name="ipAddress">the ip address of the plc</param>
    /// <param name="port">the port of the plc, default is 2004</param>
    public XGBFastEnet(string ipAddress, int port) {
        this.WordLength = 2;
        this.IpAddress = ipAddress;
        this.Port = port;
    }

    /// <summary>
    /// CPU TYPE
    /// </summary>
    public string CpuType { get; private set; }

    /// <summary>
    /// Cpu is error
    /// </summary>
    public bool CpuError { get; private set; }

    /// <summary>
    /// RUN, STOP, ERROR, DEBUG
    /// </summary>
    public LSCpuStatus LSCpuStatus { get; private set; }

    /// <summary>
    /// FEnet I/F module’s Base No.
    /// </summary>
    public byte BaseNo {
        get => this.baseNo;
        set => this.baseNo = value;
    }

    /// <summary>
    /// FEnet I/F module’s Slot No.
    /// </summary>
    public byte SlotNo {
        get => this.slotNo;
        set => this.slotNo = value;
    }

    /// <summary>
    /// Read Bytes from plc, you should specify address
    /// </summary>
    /// <param name="address">Start Address, for example: M100</param>
    /// <param name="length">Array of data Lengths</param>
    /// <returns>Whether to read the successful result object</returns>
    /// <exception cref="NullReferenceException"></exception>
    /// <remarks>
    /// </remarks>
    /// <example>
    /// </example>
    public override OperateResult<byte[]> Read(string address, ushort length) {
        // build read command
        OperateResult<byte[]> coreResult = BuildReadByteCommand(address, length);
        if (!coreResult.IsSuccess)
            return coreResult;

        // communication
        OperateResult<byte[]> read = this.ReadFromCoreServer(this.PackCommand(coreResult.Content));
        if (!read.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(read);

        // analysis read result
        return this.ExtractActualData(read.Content);
    }

    /// <summary>
    /// Write bytes to plc, you should specify bytes, can't be null
    /// </summary>
    /// <param name="address">Start Address, for example: M100</param>
    /// <param name="value">source dara</param>
    /// <returns>Whether to write the successful result object</returns>
    /// <exception cref="NullReferenceException"></exception>
    public override OperateResult Write(string address, byte[] value) {
        // build write command
        OperateResult<byte[]> coreResult = BuildWriteByteCommand(address, value);
        if (!coreResult.IsSuccess)
            return coreResult;

        // communication
        OperateResult<byte[]> read = this.ReadFromCoreServer(this.PackCommand(coreResult.Content));
        if (!read.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(read);

        // analysis read result
        return this.ExtractActualData(read.Content);
    }

    /// <summary>
    /// Read single byte value from plc
    /// </summary>
    /// <param name="address">Start address</param>
    /// <returns>result</returns>
    public OperateResult<byte> ReadByte(string address) {
        OperateResult<byte[]> read = this.Read(address, 2);
        if (!read.IsSuccess)
            return OperateResult.CreateFailedResult<byte>(read);

        return OperateResult.CreateSuccessResult(read.Content[0]);
    }

    /// <summary>
    /// Write single byte value to plc
    /// </summary>
    /// <param name="address">Start address</param>
    /// <param name="value">value</param>
    /// <returns>Whether to write the successful</returns>
    public OperateResult Write(string address, byte value) {
        return this.Write(address, new byte[] { value });
    }

    /// <summary>
    /// WriteCoil
    /// </summary>
    /// <param name="address"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public OperateResult WriteCoil(string address, bool value) {
        return this.Write(address, new byte[] { (byte) (value == true ? 0x01 : 0x00), 0x00 });
    }

    private byte[] PackCommand(byte[] coreCommand) {
        byte[] command = new byte[coreCommand.Length + 20];
        Encoding.ASCII.GetBytes(CompanyID1).CopyTo(command, 0);
        switch (this.cpuInfo) {
            case LSCpuInfo.XGK:
                command[12] = 0xA0;
                break;
            case LSCpuInfo.XGI:
                command[12] = 0xA4;
                break;
            case LSCpuInfo.XGR:
                command[12] = 0xA8;
                break;
            case LSCpuInfo.XGB_MK:
                command[12] = 0xB0;
                break;
            case LSCpuInfo.XGB_IEC:
                command[12] = 0xB4;
                break;
            default: break;
        }

        command[13] = 0x33;
        BitConverter.GetBytes((short) coreCommand.Length).CopyTo(command, 16);
        command[18] = (byte) (this.baseNo * 16 + this.slotNo);

        int count = 0;
        for (int i = 0; i < 19; i++) {
            count += command[i];
        }

        command[19] = (byte) count;

        coreCommand.CopyTo(command, 20);

        string hex = SoftBasic.ByteToHexString(command, ' ');
        return command;
    }

    private const string CompanyID1 = "LSIS-XGT";
    private const string CompanyID2 = "LGIS-GLOGA";
    private LSCpuInfo cpuInfo = LSCpuInfo.XGK;
    private byte baseNo = 0;
    private byte slotNo = 3;

    /// <summary>
    /// AnalysisAddress
    /// </summary>
    /// <param name="address"></param>
    /// <param name="isRead"></param>
    /// <returns></returns>
    public static OperateResult<string> AnalysisAddress(string address, bool isRead) {
        // P,M,L,K,F,T
        // P,M,L,K,F,T,C,D,S
        StringBuilder sb = new StringBuilder();
        try {
            sb.Append("%");
            char[] types = new char[] { 'P', 'M', 'L', 'K', 'F', 'T', 'C', 'D', 'S', 'Q', 'I', 'N', 'U', 'Z', 'R' };
            bool exsist = false;
            if (isRead) {
                for (int i = 0; i < types.Length; i++) {
                    if (types[i] == address[0]) {
                        sb.Append(types[i]);
                        sb.Append("B");
                        if (address[1] == 'B') {
                            sb.Append(int.Parse(address.AsSpan(2)) * 2);
                        }
                        else if (address[1] == 'W') {
                            sb.Append(int.Parse(address.AsSpan(2)) * 2);
                        }
                        else if (address[1] == 'D') {
                            sb.Append(int.Parse(address.AsSpan(2)) * 4);
                        }
                        else if (address[1] == 'L') {
                            sb.Append(int.Parse(address.AsSpan(2)) * 8);
                        }
                        else {
                            sb.Append(int.Parse(address.AsSpan(1)));
                        }

                        exsist = true;
                        break;
                    }
                }
            }
            else {
                sb.Append(address);
                exsist = true;
            }


            if (!exsist)
                throw new Exception(StringResources.Language.NotSupportedDataType);
        }
        catch (Exception ex) {
            return new OperateResult<string>(ex.Message);
        }

        return OperateResult.CreateSuccessResult(sb.ToString());
    }

    /// <summary>
    /// Get DataType to Address
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public static OperateResult<string> GetDataTypeToAddress(string address) {
        string lSDataType = string.Empty;
        try {
            char[] types = new char[] { 'P', 'M', 'L', 'K', 'F', 'T', 'C', 'D', 'S', 'Q', 'I', 'R' };
            bool exsist = false;

            for (int i = 0; i < types.Length; i++) {
                if (types[i] == address[1]) {
                    switch (address[2]) {
                        case 'W': lSDataType = "Word"; break;
                        case 'D': lSDataType = "DWord"; break;
                        case 'L': lSDataType = "LWord"; break;
                        case 'B': lSDataType = "Continuous"; break;
                        case 'X': lSDataType = "Bit"; break;
                    }

                    exsist = true;
                    break;
                }
            }

            if (!exsist)
                throw new Exception(StringResources.Language.NotSupportedDataType);
        }
        catch (Exception ex) {
            return new OperateResult<string>(ex.Message);
        }

        return OperateResult.CreateSuccessResult(lSDataType);
    }

    private static OperateResult<byte[]> BuildReadByteCommand(string address, ushort length) {
        OperateResult<string> analysisResult = AnalysisAddress(address, true);
        if (!analysisResult.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(analysisResult);
        byte[] command = new byte[12 + analysisResult.Content.Length];

        command[0] = 0x54; // read
        command[1] = 0x00;
        command[2] = 0x14; // continuous reading
        command[3] = 0x00;
        command[4] = 0x00; // Reserved
        command[5] = 0x00;
        command[6] = 0x01; // Block No         ?? i don't know what is the meaning
        command[7] = 0x00;
        command[8] = (byte) analysisResult.Content.Length; //  Variable Length
        command[9] = 0x00;

        Encoding.ASCII.GetBytes(analysisResult.Content).CopyTo(command, 10);
        BitConverter.GetBytes(length).CopyTo(command, command.Length - 2);

        return OperateResult.CreateSuccessResult(command);
    }

    private static OperateResult<byte[]> BuildWriteByteCommand(string address, byte[] data) {
        OperateResult<string> analysisResult = AnalysisAddress(address, false);
        if (!analysisResult.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(analysisResult);
        OperateResult<string> DataTypeResult = GetDataTypeToAddress(analysisResult.Content);
        if (!DataTypeResult.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(DataTypeResult);

        byte[] command = new byte[12 + analysisResult.Content.Length + data.Length];

        switch (DataTypeResult.Content) {
            case "Bit":
                command[2] = 0x00;
                break;
            case "Byte":
                command[2] = 0x01;
                break;
            case "Word":
                command[2] = 0x02;
                break;
            case "DWord":
                command[2] = 0x03;
                break;
            case "LWord":
                command[2] = 0x04;
                break;
            case "Continuous":
                command[2] = 0x14;
                break;
            default: break;
        }

        command[0] = 0x58; // write
        command[1] = 0x00;
        command[3] = 0x00;
        command[4] = 0x00; // Reserved
        command[5] = 0x00;
        command[6] = 0x01; // Block No         ?? i don't know what is the meaning
        command[7] = 0x00;
        command[8] = (byte) analysisResult.Content.Length; //  Variable Length
        command[9] = 0x00;

        Encoding.ASCII.GetBytes(analysisResult.Content).CopyTo(command, 10);
        BitConverter.GetBytes(data.Length).CopyTo(command, command.Length - 2 - data.Length);
        data.CopyTo(command, command.Length - data.Length);

        return OperateResult.CreateSuccessResult(command);
    }

    /// <summary>
    /// Returns true data content, supports read and write returns
    /// </summary>
    /// <param name="response">response data</param>
    /// <returns>real data</returns>
    public OperateResult<byte[]> ExtractActualData(byte[] response) {
        if (response.Length < 20)
            return new OperateResult<byte[]>("Length is less than 20:" + SoftBasic.ByteToHexString(response));

        ushort plcInfo = BitConverter.ToUInt16(response, 10);
        BitArray array_plcInfo = new BitArray(BitConverter.GetBytes(plcInfo));

        switch (plcInfo % 32) {
            case 1:
                this.CpuType = "XGK/R-CPUH";
                break;
            case 2:
                this.CpuType = "XGK-CPUS";
                break;
            case 4:
                this.CpuType = "XGK-CPUE";
                break;
            case 5:
                this.CpuType = "XGK/R-CPUH";
                break;
        }

        this.CpuError = array_plcInfo[7];
        if (array_plcInfo[8])
            this.LSCpuStatus = LSCpuStatus.RUN;
        if (array_plcInfo[9])
            this.LSCpuStatus = LSCpuStatus.STOP;
        if (array_plcInfo[10])
            this.LSCpuStatus = LSCpuStatus.ERROR;
        if (array_plcInfo[11])
            this.LSCpuStatus = LSCpuStatus.DEBUG;

        if (response.Length < 28)
            return new OperateResult<byte[]>("Length is less than 28:" + SoftBasic.ByteToHexString(response));
        ushort error = BitConverter.ToUInt16(response, 26);
        if (error > 0)
            return new OperateResult<byte[]>(response[28], "Error:" + GetErrorDesciption(response[28]));

        if (response[20] == 0x59)
            return OperateResult.CreateSuccessResult(Array.Empty<byte>()); // write

        if (response[20] == 0x55) // read
        {
            try {
                ushort length = BitConverter.ToUInt16(response, 30);
                byte[] content = new byte[length];
                Array.Copy(response, 32, content, 0, length);
                return OperateResult.CreateSuccessResult(content);
            }
            catch (Exception ex) {
                return new OperateResult<byte[]>(ex.Message);
            }
        }

        return new OperateResult<byte[]>(StringResources.Language.NotSupportedFunction);
    }

    /// <summary>
    /// get the description of the error code meanning
    /// </summary>
    /// <param name="code">code value</param>
    /// <returns>string information</returns>
    public static string GetErrorDesciption(byte code) {
        switch (code) {
            case 0: return "Normal";
            case 1: return "Physical layer error (TX, RX unavailable)";
            case 3: return "There is no identifier of Function Block to receive in communication channel";
            case 4: return "Mismatch of data type";
            case 5: return "Reset is received from partner station";
            case 6: return "Communication instruction of partner station is not ready status";
            case 7: return "Device status of remote station is not desirable status";
            case 8: return "Access to some target is not available";
            case 9: return "Can’ t deal with communication instruction of partner station by too many reception";
            case 10: return "Time Out error";
            case 11: return "Structure error";
            case 12: return "Abort";
            case 13: return "Reject(local/remote)";
            case 14: return "Communication channel establishment error (Connect/Disconnect)";
            case 15: return "High speed communication and connection service error";
            case 33: return "Can’t find variable identifier";
            case 34: return "Address error";
            case 50: return "Response error";
            case 113: return "Object Access Unsupported";
            case 187: return "Unknown error code (communication code of other company) is received";
            default: return "Unknown error";
        }
    }

    /// <summary>
    /// Returns a string representing the current object
    /// </summary>
    /// <returns>字符串</returns>
    public override string ToString() {
        return $"XGBFastEnet[{this.IpAddress}:{this.Port}]";
    }
}