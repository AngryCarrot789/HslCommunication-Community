using HslCommunication.Core.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HslCommunication.Tests.Core.Net;

/// <summary>
/// NetHandle的测试类对象
/// </summary>
[TestClass]
public class NetHandleTest {
    /// <summary>
    /// 实例化的方法测试
    /// </summary>
    [TestMethod]
    public void IsCreateSuccess() {
        NetHandle netHandle = new NetHandle(1, 1, 1);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<byte>(1, netHandle.CodeMajor);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<byte>(1, netHandle.CodeMinor);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ushort>(1, netHandle.CodeIdentifier);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(16842753, netHandle.CodeValue);


        netHandle = new NetHandle(16842753);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<byte>(1, netHandle.CodeMajor);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<byte>(1, netHandle.CodeMinor);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ushort>(1, netHandle.CodeIdentifier);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(16842753, netHandle.CodeValue);
    }

    /// <summary>
    /// 增加的一个方法测试
    /// </summary>
    [TestMethod]
    public void AddTest() {
        NetHandle netHandle = new NetHandle(1, 1, 1);


        netHandle++;
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<byte>(netHandle.CodeMajor, 1);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<byte>(netHandle.CodeMinor, 1);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ushort>(netHandle.CodeIdentifier, 2);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(16842754, netHandle.CodeValue);
    }

    /// <summary>
    /// 减小的一个方法测试
    /// </summary>
    [TestMethod]
    public void SubTest() {
        NetHandle netHandle = new NetHandle(1, 1, 1);


        netHandle--;
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<byte>(netHandle.CodeMajor, 1);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<byte>(netHandle.CodeMinor, 1);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ushort>(netHandle.CodeIdentifier, 0);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(16842752, netHandle.CodeValue);
    }
}