using System.Text;
using HslCommunication.Core.Types;
using HslCommunication.Devices.Panasonic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HslCommunication.Tests.Devices.Panasonic;

[TestClass]
public class PanasonicMewtocolTest {
    [TestMethod]
    public void BuildReadCommandTest() {
        OperateResult<byte[]> read = PanasonicMewtocol.BuildReadCommand(0xEE, "X1", 10);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(read.IsSuccess, "Build read command failed");

        string command = Encoding.ASCII.GetString(read.Content);
        string corrent = "%EE#RCCX00010010";

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(corrent == command.Substring(0, command.Length - 3), "data is not same");
    }
}