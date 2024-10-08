﻿using System.Net;
using System.Net.Sockets;
using HslCommunication.Core.Thread;
using HslCommunication.Core.Types;

namespace HslCommunication.Core.Net.NetworkBase;

/// <summary>
/// 带登录认证的服务器类
/// </summary>
public class NetworkAuthenticationServerBase : NetworkServerBase, IDisposable {
    /// <summary>
    /// 当客户端的socket登录的时候额外检查的信息
    /// </summary>
    /// <param name="socket">套接字</param>
    /// <param name="endPoint">终结点</param>
    /// <returns>验证的结果</returns>
    protected override OperateResult SocketAcceptExtraCheck(Socket socket, IPEndPoint endPoint) {
        if (this.IsUseAccountCertificate) {
            OperateResult<byte[], byte[]> receive = this.ReceiveAndCheckBytes(socket, 2000);
            if (!receive.IsSuccess)
                return new OperateResult(string.Format("Client login failed[{0}]", endPoint));

            if (BitConverter.ToInt32(receive.Content1, 0) != HslProtocol.ProtocolAccountLogin) {
                this.LogNet?.WriteError(this.ToString(), StringResources.Language.NetClientAccountTimeout);
                socket?.Close();
                return new OperateResult(string.Format("Client login failed[{0}]", endPoint));
            }

            string[] infos = HslProtocol.UnPackStringArrayFromByte(receive.Content2);
            string ret = this.CheckAccountLegal(infos);
            this.SendStringAndCheckReceive(socket, ret == "success" ? 1 : 0, new string[] { ret });

            if (ret != "success") {
                return new OperateResult(string.Format("Client login failed[{0}]:{1}", endPoint, ret));
            }

            this.LogNet?.WriteDebug(this.ToString(), string.Format("Account Login:{0} Endpoint:[{1}]", infos[0], endPoint));
        }

        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 获取或设置是否对客户端启动账号认证
    /// </summary>
    public bool IsUseAccountCertificate { get; set; }

    private Dictionary<string, string> accounts = new Dictionary<string, string>();
    private SimpleHybirdLock lockLoginAccount = new SimpleHybirdLock();

    /// <summary>
    /// 新增账户，如果想要启动账户登录，比如将<see cref="IsUseAccountCertificate"/>设置为<c>True</c>。
    /// </summary>
    /// <param name="userName">账户名称</param>
    /// <param name="password">账户名称</param>
    public void AddAccount(string userName, string password) {
        if (!string.IsNullOrEmpty(userName)) {
            this.lockLoginAccount.Enter();
            if (this.accounts.ContainsKey(userName)) {
                this.accounts[userName] = password;
            }
            else {
                this.accounts.Add(userName, password);
            }

            this.lockLoginAccount.Leave();
        }
    }

    /// <summary>
    /// 删除一个账户的信息
    /// </summary>
    /// <param name="userName">账户名称</param>
    public void DeleteAccount(string userName) {
        this.lockLoginAccount.Enter();
        if (this.accounts.ContainsKey(userName)) {
            this.accounts.Remove(userName);
        }

        this.lockLoginAccount.Leave();
    }

    private string CheckAccountLegal(string[] infos) {
        if (infos?.Length < 2)
            return "User Name input wrong";
        string ret = "";
        this.lockLoginAccount.Enter();
        if (!this.accounts.ContainsKey(infos[0])) {
            ret = "User Name input wrong";
        }
        else {
            if (this.accounts[infos[0]] != infos[1]) {
                ret = "Password is not corrent";
            }
            else {
                ret = "success";
            }
        }

        this.lockLoginAccount.Leave();
        return ret;
    }

    private bool disposedValue = false; // 要检测冗余调用

    /// <summary>
    /// 释放当前的对象
    /// </summary>
    /// <param name="disposing">是否托管对象</param>
    protected virtual void Dispose(bool disposing) {
        if (!this.disposedValue) {
            if (disposing) {
                this.ServerClose();
                this.lockLoginAccount?.Dispose();
                // TODO: 释放托管状态(托管对象)。
            }

            // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
            // TODO: 将大型字段设置为 null。

            this.disposedValue = true;
        }
    }

    // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
    // ~NetworkDataServerBase()
    // {
    //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
    //   Dispose(false);
    // }

    // 添加此代码以正确实现可处置模式。

    /// <summary>
    /// 释放当前的对象
    /// </summary>
    public void Dispose() {
        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        this.Dispose(true);
        // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
        // GC.SuppressFinalize(this);
    }
}