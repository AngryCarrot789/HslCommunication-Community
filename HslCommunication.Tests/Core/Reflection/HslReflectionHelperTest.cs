using System.Reflection;
using HslCommunication.Core.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HslCommunication.Tests.Core.Reflection;

[TestClass]
public class HslReflectionTest {
    [TestMethod]
    public void HslReflectionHelperTest() {
        Myclass myclass = new Myclass();
        Type type = typeof(Myclass);
        PropertyInfo BoolProperty = type.GetProperty("BoolValue");
        PropertyInfo IntProperty = type.GetProperty("IntValue");
        PropertyInfo FloatProperty = type.GetProperty("FloatValue");

        HslReflectionHelper.SetPropertyExp(BoolProperty, myclass, true);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(myclass.BoolValue);

        HslReflectionHelper.SetPropertyExp(IntProperty, myclass, 1234);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(myclass.IntValue == 1234);

        HslReflectionHelper.SetPropertyExp(FloatProperty, myclass, 123.4f);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(myclass.FloatValue == 123.4f);

        //var sw = new Stopwatch( );
        //sw.Start( );
        //for (int i = 0; i < 100000; i++)
        //{
        //    IntProperty.SetValue( myclass, 123, null );
        //}
        //sw.Stop( );
        //Console.WriteLine( "正常的情况：" + sw.ElapsedMilliseconds );
        //sw.Restart( );
        //for (int i = 0; i < 100000; i++)
        //{
        //    HslReflectionHelper.SetPropertyExp( IntProperty, myclass, 1234 );
        //}
        //sw.Stop( );
        //Console.WriteLine( "表达式树的情况：" + sw.ElapsedMilliseconds );
    }
}

public class Myclass {
    public bool BoolValue { get; set; }

    public int IntValue { get; set; }

    public float FloatValue { get; set; }
}