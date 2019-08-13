using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCoreTest.TestClasses
{
    public class TestService : ITestService
    {
        public TestService()
        {
            TID = Guid.NewGuid().ToString();
        }

        public string TID
        {get;
        }

        public string GetId()
        {
            return TID;
        }
    }
}
