﻿using System.Net.Sockets;
using HslCommunication.BasicFramework;
using HslCommunication.Core.Net.StateOne;
using HslCommunication.Core.Types;
using HslCommunication.Enthernet.FileNet;

namespace HslCommunication.Core.Net.NetworkBase;

/// <summary>
/// 包含了主动异步接收的方法实现和文件类异步读写的实现
/// </summary>
public class NetworkXBase : NetworkBase {
    /// <summary>
    /// 默认的无参构造方法
    /// </summary>
    public NetworkXBase() {
    }

    /// <summary>
    /// 发送数据的方法
    /// </summary>
    /// <param name="session">通信用的核心对象</param>
    /// <param name="content">完整的字节信息</param>
    internal void SendBytesAsync(AppSession session, byte[] content) {
        if (content == null)
            return;
        try {
            // 进入发送数据的锁，然后开启异步的数据发送
            session.HybirdLockSend.Enter();

            // 启用另外一个网络封装对象进行发送数据
            AsyncStateSend state = new AsyncStateSend() {
                WorkSocket = session.WorkSocket,
                Content = content,
                AlreadySendLength = 0,
                HybirdLockSend = session.HybirdLockSend,
            };

            state.WorkSocket.BeginSend(
                state.Content,
                state.AlreadySendLength,
                state.Content.Length - state.AlreadySendLength,
                SocketFlags.None,
                new AsyncCallback(this.SendCallBack),
                state);
        }
        catch (ObjectDisposedException) {
            // 不操作
            session.HybirdLockSend.Leave();
        }
        catch (Exception ex) {
            session.HybirdLockSend.Leave();
            if (!ex.Message.Contains(StringResources.Language.SocketRemoteCloseException)) {
                this.LogNet?.WriteException(this.ToString(), StringResources.Language.SocketSendException, ex);
            }
        }
    }

    /// <summary>
    /// 发送回发方法
    /// </summary>
    /// <param name="ar">异步对象</param>
    internal void SendCallBack(IAsyncResult ar) {
        if (ar.AsyncState is AsyncStateSend stateone) {
            try {
                stateone.AlreadySendLength += stateone.WorkSocket.EndSend(ar);
                if (stateone.AlreadySendLength < stateone.Content.Length) {
                    // 继续发送
                    stateone.WorkSocket.BeginSend(stateone.Content,
                        stateone.AlreadySendLength,
                        stateone.Content.Length - stateone.AlreadySendLength,
                        SocketFlags.None,
                        new AsyncCallback(this.SendCallBack),
                        stateone);
                }
                else {
                    stateone.HybirdLockSend.Leave();
                    // 发送完成
                    stateone = null;
                }
            }
            catch (ObjectDisposedException) {
                stateone.HybirdLockSend.Leave();
                // 不处理
                stateone = null;
            }
            catch (Exception ex) {
                this.LogNet?.WriteException(this.ToString(), StringResources.Language.SocketEndSendException, ex);
                stateone.HybirdLockSend.Leave();
                stateone = null;
            }
        }
    }

    /// <summary>
    /// 重新开始接收下一次的数据传递
    /// </summary>
    /// <param name="session">网络状态</param>
    /// <param name="isProcess">是否触发数据处理</param>
    internal void ReBeginReceiveHead(AppSession session, bool isProcess) {
        try {
            byte[] head = session.BytesHead, Content = session.BytesContent;
            session.Clear();
            session.WorkSocket.BeginReceive(session.BytesHead, session.AlreadyReceivedHead, session.BytesHead.Length - session.AlreadyReceivedHead,
                SocketFlags.None, new AsyncCallback(this.HeadBytesReceiveCallback), session);
            // 检测是否需要数据处理
            if (isProcess) {
                // 校验令牌
                if (this.CheckRemoteToken(head)) {
                    Content = HslProtocol.CommandAnalysis(head, Content);
                    int protocol = BitConverter.ToInt32(head, 0);
                    int customer = BitConverter.ToInt32(head, 4);
                    // 转移到数据中心处理
                    this.DataProcessingCenter(session, protocol, customer, Content);
                }
                else {
                    // 应该关闭网络通信
                    this.LogNet?.WriteWarn(this.ToString(), StringResources.Language.TokenCheckFailed);
                    this.AppSessionRemoteClose(session);
                }
            }
        }
        catch (Exception ex) {
            this.SocketReceiveException(session, ex);
            this.LogNet?.WriteException(this.ToString(), ex);
        }
    }

    /// <summary>
    /// 指令头接收方法
    /// </summary>
    /// <param name="ar">异步状态信息</param>
    protected void HeadBytesReceiveCallback(IAsyncResult ar) {
        if (ar.AsyncState is AppSession session) {
            try {
                int receiveCount = session.WorkSocket.EndReceive(ar);
                if (receiveCount == 0) {
                    // 断开了连接，需要做个处理，一个是直接关闭，另一个是触发下线
                    this.AppSessionRemoteClose(session);
                    return;
                }
                else {
                    session.AlreadyReceivedHead += receiveCount;
                }
            }
            catch (ObjectDisposedException) {
                // 不需要处理，来自服务器主动关闭
                return;
            }
            catch (SocketException ex) {
                // 已经断开连接了
                this.SocketReceiveException(session, ex);
                this.LogNet?.WriteException(this.ToString(), ex);
                return;
            }
            catch (Exception ex) {
                // 其他乱七八糟的异常重新启用接收数据
                this.ReBeginReceiveHead(session, false);
                this.LogNet?.WriteException(this.ToString(), StringResources.Language.SocketEndReceiveException, ex);
                return;
            }


            if (session.AlreadyReceivedHead < session.BytesHead.Length) {
                try {
                    // 仍需要接收
                    session.WorkSocket.BeginReceive(session.BytesHead, session.AlreadyReceivedHead, session.BytesHead.Length - session.AlreadyReceivedHead,
                        SocketFlags.None, new AsyncCallback(this.HeadBytesReceiveCallback), session);
                }
                catch (Exception ex) {
                    this.SocketReceiveException(session, ex);
                    this.LogNet?.WriteException(this.ToString(), ex);
                }
            }
            else {
                // 接收完毕，校验令牌
                if (!this.CheckRemoteToken(session.BytesHead)) {
                    this.LogNet?.WriteWarn(this.ToString(), StringResources.Language.TokenCheckFailed);
                    this.AppSessionRemoteClose(session);
                    return;
                }

                int receive_length = BitConverter.ToInt32(session.BytesHead, session.BytesHead.Length - 4);


                session.BytesContent = new byte[receive_length];

                if (receive_length > 0) {
                    try {
                        int receiveSize = session.BytesContent.Length - session.AlreadyReceivedContent;
                        session.WorkSocket.BeginReceive(session.BytesContent, session.AlreadyReceivedContent, receiveSize,
                            SocketFlags.None, new AsyncCallback(this.ContentReceiveCallback), session);
                    }
                    catch (Exception ex) {
                        this.SocketReceiveException(session, ex);
                        this.LogNet?.WriteException(this.ToString(), ex);
                    }
                }
                else {
                    // 处理数据并重新启动接收
                    this.ReBeginReceiveHead(session, true);
                }
            }
        }
    }


    /// <summary>
    /// 数据内容接收方法
    /// </summary>
    /// <param name="ar"></param>
    private void ContentReceiveCallback(IAsyncResult ar) {
        if (ar.AsyncState is AppSession receive) {
            try {
                receive.AlreadyReceivedContent += receive.WorkSocket.EndReceive(ar);
            }
            catch (ObjectDisposedException) {
                //不需要处理
                return;
            }
            catch (SocketException ex) {
                //已经断开连接了
                this.SocketReceiveException(receive, ex);
                this.LogNet?.WriteException(this.ToString(), ex);
                return;
            }
            catch (Exception ex) {
                //其他乱七八糟的异常重新启用接收数据
                this.ReBeginReceiveHead(receive, false);
                this.LogNet?.WriteException(this.ToString(), StringResources.Language.SocketEndReceiveException, ex);
                return;
            }


            if (receive.AlreadyReceivedContent < receive.BytesContent.Length) {
                int receiveSize = receive.BytesContent.Length - receive.AlreadyReceivedContent;
                try {
                    //仍需要接收
                    receive.WorkSocket.BeginReceive(receive.BytesContent, receive.AlreadyReceivedContent, receiveSize, SocketFlags.None, new AsyncCallback(this.ContentReceiveCallback), receive);
                }
                catch (Exception ex) {
                    this.ReBeginReceiveHead(receive, false);
                    this.LogNet?.WriteException(this.ToString(), StringResources.Language.SocketEndReceiveException, ex);
                }
            }
            else {
                //处理数据并重新启动接收
                this.ReBeginReceiveHead(receive, true);
            }
        }
    }

    /// <summary>
    /// [自校验] 将文件数据发送至套接字，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="filename">完整的文件路径</param>
    /// <param name="filelength">文件的长度</param>
    /// <param name="report">进度报告器</param>
    /// <returns>是否发送成功</returns>
    protected OperateResult SendFileStreamToSocket(Socket socket, string filename, long filelength, Action<long, long> report = null) {
        try {
            OperateResult result = null;
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
                result = this.SendStream(socket, fs, filelength, report, true);
            }

            return result;
        }
        catch (Exception ex) {
            socket?.Close();
            this.LogNet?.WriteException(this.ToString(), ex);
            return new OperateResult(ex.Message);
        }
    }


    /// <summary>
    /// [自校验] 将文件数据发送至套接字，具体发送细节将在继承类中实现，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">套接字</param>
    /// <param name="filename">文件名称，文件必须存在</param>
    /// <param name="servername">远程端的文件名称</param>
    /// <param name="filetag">文件的额外标签</param>
    /// <param name="fileupload">文件的上传人</param>
    /// <param name="sendReport">发送进度报告</param>
    /// <returns>是否发送成功</returns>
    protected OperateResult SendFileAndCheckReceive(
        Socket socket,
        string filename,
        string servername,
        string filetag,
        string fileupload,
        Action<long, long> sendReport = null) {
        // 发送文件名，大小，标签
        FileInfo info = new FileInfo(filename);

        if (!File.Exists(filename)) {
            // 如果文件不存在
            OperateResult stringResult = this.SendStringAndCheckReceive(socket, 0, "");
            if (!stringResult.IsSuccess)
                return stringResult;

            socket?.Close();
            return new OperateResult(StringResources.Language.FileNotExist);
        }

        // 文件存在的情况
        Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject {
            { "FileName", new Newtonsoft.Json.Linq.JValue(servername) },
            { "FileSize", new Newtonsoft.Json.Linq.JValue(info.Length) },
            { "FileTag", new Newtonsoft.Json.Linq.JValue(filetag) },
            { "FileUpload", new Newtonsoft.Json.Linq.JValue(fileupload) }
        };

        // 先发送文件的信息到对方
        OperateResult sendResult = this.SendStringAndCheckReceive(socket, 1, json.ToString());
        if (!sendResult.IsSuccess)
            return sendResult;

        // 最后发送
        return this.SendFileStreamToSocket(socket, filename, info.Length, sendReport);
    }


    /// <summary>
    /// [自校验] 将流数据发送至套接字，具体发送细节将在继承类中实现，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">套接字</param>
    /// <param name="stream">文件名称，文件必须存在</param>
    /// <param name="servername">远程端的文件名称</param>
    /// <param name="filetag">文件的额外标签</param>
    /// <param name="fileupload">文件的上传人</param>
    /// <param name="sendReport">发送进度报告</param>
    /// <returns>是否成功的结果对象</returns>
    protected OperateResult SendFileAndCheckReceive(
        Socket socket,
        Stream stream,
        string servername,
        string filetag,
        string fileupload,
        Action<long, long> sendReport = null) {
        // 文件存在的情况
        Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject {
            { "FileName", new Newtonsoft.Json.Linq.JValue(servername) },
            { "FileSize", new Newtonsoft.Json.Linq.JValue(stream.Length) },
            { "FileTag", new Newtonsoft.Json.Linq.JValue(filetag) },
            { "FileUpload", new Newtonsoft.Json.Linq.JValue(fileupload) }
        };

        // 发送文件信息
        OperateResult fileResult = this.SendStringAndCheckReceive(socket, 1, json.ToString());
        if (!fileResult.IsSuccess)
            return fileResult;

        return this.SendStream(socket, stream, stream.Length, sendReport, true);
    }

    /// <summary>
    /// [自校验] 从套接字中接收文件头信息
    /// </summary>
    /// <param name="socket">套接字的网络</param>
    /// <returns>包含文件信息的结果对象</returns>
    protected OperateResult<FileBaseInfo> ReceiveFileHeadFromSocket(Socket socket) {
        // 先接收文件头信息
        OperateResult<int, string> receiveString = this.ReceiveStringContentFromSocket(socket);
        if (!receiveString.IsSuccess)
            return OperateResult.CreateFailedResult<FileBaseInfo>(receiveString);

        // 判断文件是否存在
        if (receiveString.Content1 == 0) {
            socket?.Close();
            this.LogNet?.WriteWarn(this.ToString(), StringResources.Language.FileRemoteNotExist);
            return new OperateResult<FileBaseInfo>(StringResources.Language.FileNotExist);
        }

        OperateResult<FileBaseInfo> result = new OperateResult<FileBaseInfo> {
            Content = new FileBaseInfo()
        };
        try {
            // 提取信息
            Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.Linq.JObject.Parse(receiveString.Content2);
            result.Content.Name = SoftBasic.GetValueFromJsonObject(json, "FileName", "");
            result.Content.Size = SoftBasic.GetValueFromJsonObject(json, "FileSize", 0L);
            result.Content.Tag = SoftBasic.GetValueFromJsonObject(json, "FileTag", "");
            result.Content.Upload = SoftBasic.GetValueFromJsonObject(json, "FileUpload", "");
            result.IsSuccess = true;
        }
        catch (Exception ex) {
            socket?.Close();
            result.Message = "Extra，" + ex.Message;
        }

        return result;
    }

    /// <summary>
    /// [自校验] 从网络中接收一个文件，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="savename">接收文件后保存的文件名</param>
    /// <param name="receiveReport">接收进度报告</param>
    /// <returns>包含文件信息的结果对象</returns>
    protected OperateResult<FileBaseInfo> ReceiveFileFromSocket(Socket socket, string savename, Action<long, long> receiveReport) {
        // 先接收文件头信息
        OperateResult<FileBaseInfo> fileResult = this.ReceiveFileHeadFromSocket(socket);
        if (!fileResult.IsSuccess)
            return fileResult;

        try {
            OperateResult write = null;
            using (FileStream fs = new FileStream(savename, FileMode.Create, FileAccess.Write)) {
                write = this.WriteStream(socket, fs, fileResult.Content.Size, receiveReport, true);
            }

            if (!write.IsSuccess) {
                if (File.Exists(savename))
                    File.Delete(savename);
                return OperateResult.CreateFailedResult<FileBaseInfo>(write);
            }

            return fileResult;
        }
        catch (Exception ex) {
            this.LogNet?.WriteException(this.ToString(), ex);
            socket?.Close();
            return new OperateResult<FileBaseInfo>() {
                Message = ex.Message
            };
        }
    }


    /// <summary>
    /// [自校验] 从网络中接收一个文件，写入数据流，如果结果异常，则结束通讯，参数顺序文件名，文件大小，文件标识，上传人
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="stream">等待写入的数据流</param>
    /// <param name="receiveReport">接收进度报告</param>
    /// <returns></returns>
    protected OperateResult<FileBaseInfo> ReceiveFileFromSocket(Socket socket, Stream stream, Action<long, long> receiveReport) {
        // 先接收文件头信息
        OperateResult<FileBaseInfo> fileResult = this.ReceiveFileHeadFromSocket(socket);
        if (!fileResult.IsSuccess)
            return fileResult;

        try {
            this.WriteStream(socket, stream, fileResult.Content.Size, receiveReport, true);
            return fileResult;
        }
        catch (Exception ex) {
            this.LogNet?.WriteException(this.ToString(), ex);
            socket?.Close();
            return new OperateResult<FileBaseInfo>() {
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// 删除文件的操作
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    protected bool DeleteFileByName(string filename) {
        try {
            if (!File.Exists(filename))
                return true;
            File.Delete(filename);
            return true;
        }
        catch (Exception ex) {
            this.LogNet?.WriteException(this.ToString(), "delete file failed:" + filename, ex);
            return false;
        }
    }

    /// <summary>
    /// 预处理文件夹的名称，除去文件夹名称最后一个'\'，如果有的话
    /// </summary>
    /// <param name="folder">文件夹名称</param>
    /// <returns></returns>
    protected string PreprocessFolderName(string folder) {
        if (folder.EndsWith(@"\")) {
            return folder.Substring(0, folder.Length - 1);
        }
        else {
            return folder;
        }
    }

    /// <summary>
    /// 数据处理中心，应该继承重写
    /// </summary>
    /// <param name="session">连接状态</param>
    /// <param name="protocol">协议头</param>
    /// <param name="customer">用户自定义</param>
    /// <param name="content">数据内容</param>
    internal virtual void DataProcessingCenter(AppSession session, int protocol, int customer, byte[] content) {
    }

    /// <summary>
    /// 接收出错的时候进行处理
    /// </summary>
    /// <param name="session">会话内容</param>
    /// <param name="ex">异常信息</param>
    internal virtual void SocketReceiveException(AppSession session, Exception ex) {
    }


    /// <summary>
    /// 当远端的客户端关闭连接时触发
    /// </summary>
    /// <param name="session">会话信息</param>
    internal virtual void AppSessionRemoteClose(AppSession session) {
    }

    /// <summary>
    /// 发送一个流的所有数据到网络套接字
    /// </summary>
    /// <param name="socket">套接字</param>
    /// <param name="stream">内存流</param>
    /// <param name="receive">发送的数据长度</param>
    /// <param name="report">进度报告的委托</param>
    /// <param name="reportByPercent">进度报告是否按照百分比报告</param>
    /// <returns>是否成功的结果对象</returns>
    protected OperateResult SendStream(Socket socket, Stream stream, long receive, Action<long, long> report, bool reportByPercent) {
        byte[] buffer = new byte[102400]; // 100K的数据缓存池
        long SendTotal = 0;
        long percent = 0;
        stream.Position = 0;
        while (SendTotal < receive) {
            // 先从流中接收数据
            OperateResult<int> read = this.ReadStream(stream, buffer);
            if (!read.IsSuccess)
                return new OperateResult() {
                    Message = read.Message,
                };
            else {
                SendTotal += read.Content;
            }

            // 然后再异步写到socket中
            byte[] newBuffer = new byte[read.Content];
            Array.Copy(buffer, 0, newBuffer, 0, newBuffer.Length);
            OperateResult write = this.SendBytesAndCheckReceive(socket, read.Content, newBuffer);
            if (!write.IsSuccess) {
                return new OperateResult() {
                    Message = write.Message,
                };
            }

            // 报告进度
            if (reportByPercent) {
                long percentCurrent = SendTotal * 100 / receive;
                if (percent != percentCurrent) {
                    percent = percentCurrent;
                    report?.Invoke(SendTotal, receive);
                }
            }
            else {
                // 报告进度
                report?.Invoke(SendTotal, receive);
            }
        }

        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 从套接字中接收所有的数据然后写入到流当中去
    /// </summary>
    /// <param name="socket">套接字</param>
    /// <param name="stream">数据流</param>
    /// <param name="totalLength">所有数据的长度</param>
    /// <param name="report">进度报告</param>
    /// <param name="reportByPercent">进度报告是否按照百分比</param>
    /// <returns>是否成功的结果对象</returns>
    protected OperateResult WriteStream(Socket socket, Stream stream, long totalLength, Action<long, long> report, bool reportByPercent) {
        long count_receive = 0;
        long percent = 0;
        while (count_receive < totalLength) {
            // 先从流中异步接收数据
            OperateResult<int, byte[]> read = this.ReceiveBytesContentFromSocket(socket);
            if (!read.IsSuccess) {
                return new OperateResult() {
                    Message = read.Message,
                };
            }

            count_receive += read.Content1;

            // 开始写入文件流
            OperateResult write = this.WriteStream(stream, read.Content2);
            if (!write.IsSuccess) {
                return new OperateResult() {
                    Message = write.Message,
                };
            }

            // 报告进度
            if (reportByPercent) {
                long percentCurrent = count_receive * 100 / totalLength;
                if (percent != percentCurrent) {
                    percent = percentCurrent;
                    report?.Invoke(count_receive, totalLength);
                }
            }
            else {
                report?.Invoke(count_receive, totalLength);
            }
        }

        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 获取本对象的字符串表示形式
    /// </summary>
    /// <returns>字符串信息</returns>
    public override string ToString() {
        return "NetworkXBase";
    }
}