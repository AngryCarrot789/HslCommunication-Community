using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HslCommunication.Tests.Core.Transfer;

[TestClass]
public class ReverseWordTransformTest3 : ReverseWordTransformTest {
    public ReverseWordTransformTest3() {
        this.byteTransform.DataFormat = HslCommunication.Core.DataFormat.CDAB;
    }
}