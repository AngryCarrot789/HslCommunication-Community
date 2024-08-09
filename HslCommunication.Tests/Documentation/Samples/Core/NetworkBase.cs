using System.Net.Sockets;
using HslCommunication.Core.Net;
using HslCommunication.Core.Net.NetworkBase;
using HslCommunication.Core.Net.StateOne;
using HslCommunication.Core.Types;
using HslCommunication.Devices.Melsec;
using HslCommunication.Enthernet.SimplifyNet;
using HslCommunication.LogNet.Logs;

namespace HslCommunication.Tests.Documentation.Samples.Core;

public class NetworkBaseExample {
    public void TokenClientExample() {
        NetSimplifyClient simplifyClient = new NetSimplifyClient("127.0.0.1", 12345);

        // 这个toeken需要和服务器的设置的token相匹配才可以
        simplifyClient.Token = new Guid("787f9607-dd7a-4ba7-9f98-769d24de05df");
    }

    private NetSimplifyServer simplifyServer = null;

    public void TokenServerExample() {
        this.simplifyServer = new NetSimplifyServer();
        this.simplifyServer.Token = new Guid("787f9607-dd7a-4ba7-9f98-769d24de05df");

        this.simplifyServer.ReceiveStringEvent += this.SimplifyServer_ReceiveStringEvent;
        this.simplifyServer.ServerStart(12345);
    }

    private void SimplifyServer_ReceiveStringEvent(AppSession session, NetHandle handle, string data) {
        // 示例情况，接收到数据后返回消息
        this.simplifyServer.SendMessage(session, handle, "Back:" + data);
    }

    public void LogNetExample() {
        // 设备连接对象的日志
        MelsecMcNet melsec = new MelsecMcNet("192.168.0.100", 6000);

        // 举例实现日志文件为单日志文件
        melsec.LogNet = new LogNetSingle("D://123.txt");

        // 一般服务器对象的
        NetSimplifyServer simplifyServer = new NetSimplifyServer();
        simplifyServer.LogNet = new LogNetSingle("D://log.txt");
        simplifyServer.ReceiveStringEvent += (AppSession session, NetHandle handle, string data) => {
            simplifyServer.SendMessage(session, handle, "Back:" + data);
        };
        simplifyServer.ServerStart(45678);
    }

    public class NetworkMy : NetworkBase {
        public void CreateSocketAndConnectExample1() {
            // 连接远程的端口
            OperateResult<Socket> socketResult = this.CreateSocketAndConnect("192.168.0.100", 12345);
            if (socketResult.IsSuccess) {
                // connect success
            }
            else {
                // failed
            }
        }

        public void CreateSocketAndConnectExample2() {
            // 连接远程的端口，允许设置超时时间，比如1秒
            OperateResult<Socket> socketResult = this.CreateSocketAndConnect("192.168.0.100", 12345, 1000);
            if (socketResult.IsSuccess) {
                // connect success
            }
            else {
                // failed
            }
        }

        public void CreateSocketAndConnectExample3() {
            // 连接远程的端口，允许设置超时时间，比如1秒
            OperateResult<Socket> socketResult = this.CreateSocketAndConnect(new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.0.100"), 12345), 1000);
            if (socketResult.IsSuccess) {
                // connect success
            }
            else {
                // failed
            }
        }
    }
}