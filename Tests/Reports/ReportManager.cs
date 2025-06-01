using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using System;
using System.IO;

namespace NUnitTests.Reports
{
    public static class ReportManager
    {
        private static ExtentReports _extent;
        private static ExtentTest _currentTest;
        private static ExtentSparkReporter _sparkReporter;

        static ReportManager()
        {
            string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "TestReport.html");
            _sparkReporter = new ExtentSparkReporter(reportPath);
            _sparkReporter.Config.DocumentTitle = "UI Test Report";
            _sparkReporter.Config.ReportName = "API Functional Test Report";

            _extent = new ExtentReports();
            _extent.AttachReporter(_sparkReporter);
        }

        public static void CreateTest(string testName)
        {
            _currentTest = _extent.CreateTest(testName);
        }

        public static void LogInfo(string message)
        {
            _currentTest?.Info(message);
        }

        public static void LogFail(string message)
        {
            _currentTest?.Fail(message);
        }

        public static void LogPass(string message)
        {
            _currentTest?.Pass(message);
        }

        public static void LogSkip(string message)
        {
            _currentTest?.Skip(message);
        }

        public static void FlushReport()
        {
            _extent.Flush();
        }
    }
}
