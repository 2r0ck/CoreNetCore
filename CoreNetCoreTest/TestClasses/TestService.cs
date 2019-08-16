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



    public class TestService_One : ITestService
    {
        public TestService_One()
        {
            TID = "TestService_One";
        }

        public string TID
        {
            get;
        }

        public string GetId()
        {
            return TID;
        }
    }

    public class TestService_Second : ITestService
    {
        public TestService_Second()
        {
            TID = "TestService_Second";
        }

        public string TID
        {
            get;
        }

        public string GetId()
        {
            return TID;
        }
    }
}
