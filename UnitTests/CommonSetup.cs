using NUnit.Framework;

using System;
using System.IO;

namespace UnitTests
{
    [SetUpFixture]
    public class CommonSetup
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            var dir = Path.GetDirectoryName(typeof(CommonSetup).Assembly.Location);
            Environment.CurrentDirectory = dir;
        }
    }
}