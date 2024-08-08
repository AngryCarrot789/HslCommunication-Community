using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HslCommunication.Tests.Core.Transfer;

[TestClass]
public class ReverseWordTransformTest4 : ReverseWordTransformTest {
    public ReverseWordTransformTest4() {
        this.byteTransform.DataFormat = HslCommunication.Core.DataFormat.DCBA;
    }
}