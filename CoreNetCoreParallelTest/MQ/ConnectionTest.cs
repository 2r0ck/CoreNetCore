using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
namespace CoreNetCoreParallelTest.MQ
{  
    [TestClass]
    public class ConnectionTest
    {
        [TestMethod]
        public void Run1()
        {
            Thread.Sleep(3000);
        }

        [TestMethod]
        public void Run2()
        {
            Thread.Sleep(3000);
        }
    }
}
