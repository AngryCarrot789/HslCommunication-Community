﻿using HslCommunication.Core.Net;

namespace HslCommunication.Tests.Documentation.Samples.Core;

public class NetHandleExample {
    public void Example() {
        NetHandle netHandle1 = new NetHandle(1, 1, 1);

        NetHandle netHandle2 = 16842753;

        if (netHandle1 == netHandle2) {
            Console.WriteLine("true"); // 会执行这一步
        }

        // 因为 1*256*65536+1*65536+1 = 16842753

        netHandle2++;

        if (netHandle2 == 16842754) {
            Console.WriteLine("true"); // 会执行这一步
        }
    }
}