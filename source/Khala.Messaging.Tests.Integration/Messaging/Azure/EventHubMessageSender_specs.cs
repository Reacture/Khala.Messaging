namespace Khala.Messaging.Azure
{
    using System.Collections.Generic;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventHubMessageSender_specs
    {
        public const string ConnectionStringParam = "EventHubMessageSender/ConnectionString";
        public const string ConsumerGroupNameParam = "EventHubMessageSender/ConsumerGroupName";

        private static readonly string ConnectionParametersRequired = $@"Event Hub connection information is not set. To run tests on the EventHubMessageSender class, you must set the connection information in the *.runsettings file as follows:

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{ConnectionStringParam}"" value=""your connection string to the Event Hub"" />
    <Parameter name=""{ConsumerGroupNameParam}"" value=""[OPTIONAL] The name of the consumer group within the Event Hub"" />
  </TestRunParameters>  
</RunSettings>

References
- https://msdn.microsoft.com/en-us/library/jj635153.aspx";

        private static string _connectionString;
        private static string _consumerGroupName;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            IDictionary<string, object> properties = context.Properties;

            if (properties.TryGetValue(
                ConnectionStringParam, out object connectionString))
            {
                _connectionString = (string)connectionString;
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                Assert.Inconclusive(ConnectionParametersRequired);
            }

            _consumerGroupName =
                properties.TryGetValue(
                    ConsumerGroupNameParam, out object consumerGroupName)
                ? (string)consumerGroupName
                : PartitionReceiver.DefaultConsumerGroupName;
        }

        public class Message
        {
            public string Content { get; set; }
        }

        public class PartitionedMessage : IPartitioned
        {
            public string PartitionKey { get; set; }

            public string Content { get; set; }
        }

        [TestMethod]
        public void Send_sends_messages_correctly()
        {
            // Arrange
            var eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString);
            IMessageSerializer serializer = new JsonMessageSerializer();
            var sut = new EventHubMessageSender(serializer, eventHubClient);

            // Act

            // Assert
        }
    }
}
