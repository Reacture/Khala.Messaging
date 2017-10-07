namespace Khala.Messaging.Azure
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventHubMessageBus_specs
    {
        public const string EventHubConnectionStringPropertyName = "eventhubmessagebus-eventhub-connectionstring";
        public const string ConsumerGroupPropertyName = "eventhubmessagebus-eventhub-consumergroup";

        private static string ConnectionParametersRequired => $@"
Event Hub connection information is not set. To run tests on the EventHubMessageBus class, you must set the connection information in the *.runsettings file as follows:

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{EventHubConnectionStringPropertyName}"" value=""your event hub connection string for testing"" />
    <Parameter name=""{ConsumerGroupPropertyName}"" value=""[OPTIONAL] your event hub consumer group name for testing"" />
  </TestRunParameters>  
</RunSettings>

References
- https://msdn.microsoft.com/en-us/library/jj635153.aspx
".Trim();

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
        }

        [TestInitialize]
        public void TestInitialize()
        {
        }
    }
}
