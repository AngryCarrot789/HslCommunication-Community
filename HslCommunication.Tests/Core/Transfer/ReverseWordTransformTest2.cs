using HslCommunication.Core.Transfer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HslCommunication.Tests.Core.Transfer;

[TestClass]
public class ReverseWordTransformTest2 : ReverseWordTransformTest {
    public ReverseWordTransformTest2() {
        this.byteTransform.DataFormat = DataFormat.BADC;
    }
}