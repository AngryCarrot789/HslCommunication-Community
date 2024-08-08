using HslCommunication.Profinet.AllenBradley;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HslCommunication.Tests.Profinet.AllenBradley;

[TestClass]
public class AllenBradleyHelperTest {
    [TestMethod]
    public void PackRequsetReadTest() {
        byte[] corrent = new byte[] { 0x4c, 0x05, 0x91, 0x08, 0x53, 0x74, 0x61, 0x72, 0x74, 0x5f, 0x69, 0x6e, 0x01, 0x00 };

        byte[] buffer = AllenBradleyHelper.PackRequsetRead("Start_in", 1);
        if (!HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(buffer, corrent)) {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("指令失败：" + HslCommunication.BasicFramework.SoftBasic.ByteToHexString(buffer));
        }
    }

    [TestMethod]
    public void PackRequestWriteTest() {
        byte[] corrent = new byte[] { 0x4d, 0x02, 0x91, 0x02, 0x41, 0x31, 0xc4, 0x00, 0x01, 0x00, 0xd2, 0x04, 0x00, 0x00 };


        byte[] buffer = AllenBradleyHelper.PackRequestWrite("A1", 0xc4, BitConverter.GetBytes(1234));
        if (!HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(buffer, corrent)) {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("指令失败：" + HslCommunication.BasicFramework.SoftBasic.ByteToHexString(buffer));
        }
    }
}