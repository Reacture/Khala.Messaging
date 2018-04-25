namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventDataSender_specs
    {
        public const string ConnectionStringParam = "EventDataSender/ConnectionString";
        public const string ConsumerGroupNameParam = "EventDataSender/ConsumerGroupName";

        private static readonly string ConnectionParametersRequired = $@"Event Hub connection information is not set. To run tests on the EventDataSender class, you must set the connection information in the *.runsettings file as follows:

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
            if (context.Properties.TryGetValue(ConnectionStringParam, out object connectionString))
            {
                _connectionString = (string)connectionString;
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                Assert.Inconclusive(ConnectionParametersRequired);
            }

            _consumerGroupName = context.Properties.TryGetValue(ConsumerGroupNameParam, out object consumerGroupName)
                ? (string)consumerGroupName
                : PartitionReceiver.DefaultConsumerGroupName;
        }

        private static async Task<IReadOnlyList<PartitionReceiver>> GetReceivers(
            EventHubClient eventHubClient, string consumerGroupName)
        {
            EventHubRuntimeInformation runtimeInformation =
                await eventHubClient.GetRuntimeInformationAsync();

            var receivers = new List<PartitionReceiver>();

            foreach (string partitionId in runtimeInformation.PartitionIds)
            {
                EventHubPartitionRuntimeInformation partitionRuntimeInformation =
                    await eventHubClient.GetPartitionRuntimeInformationAsync(partitionId);

                PartitionReceiver receiver = eventHubClient.CreateReceiver(
                    consumerGroupName,
                    partitionId,
                    partitionRuntimeInformation.LastEnqueuedOffset,
                    offsetInclusive: false);

                receivers.Add(receiver);
            }

            return receivers;
        }

        private static async Task<IReadOnlyList<EventData>> ReceiveAll(
            IEnumerable<PartitionReceiver> receivers)
        {
            IReadOnlyList<EventData>[] unflattened =
                await Task.WhenAll(receivers.Select(ReceiveAll));
            return unflattened.SelectMany(received => received).ToList();
        }

        private static async Task<IReadOnlyList<EventData>> ReceiveAll(
            PartitionReceiver receiver)
        {
            var messages = new List<EventData>();
            var waitTime = TimeSpan.FromMilliseconds(1000);

            while (true)
            {
                IEnumerable<EventData> received =
                    await receiver.ReceiveAsync(10, waitTime);
                if (received == null)
                {
                    break;
                }

                messages.AddRange(received);
            }

            return messages;
        }

        [TestMethod]
        public void sut_implements_IEventDataSender()
        {
            typeof(EventDataSender).Should().Implement<IEventDataSender>();
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            IFixture builder = new Fixture().Customize(new AutoMoqCustomization());
            builder.Inject(EventHubClient.CreateFromConnectionString(_connectionString));
            new GuardClauseAssertion(builder).Verify(typeof(EventDataSender));
        }

        [TestMethod]
        public async Task Send_sends_messages_correctly()
        {
            // Arrange
            var eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString);
            var serializer = new EventDataSerializer();
            var sut = new EventDataSender(eventHubClient);

            var envelopes = new Fixture()
                .CreateMany<Message>()
                .Select(message => new Envelope(
                    messageId: Guid.NewGuid(),
                    message,
                    operationId: Guid.NewGuid(),
                    correlationId: Guid.NewGuid(),
                    contributor: $"{Guid.NewGuid()}"))
                .ToList();

            var events = envelopes
                .Select(envelope => serializer.Serialize(envelope))
                .ToList();

            IEnumerable<PartitionReceiver> receivers =
                await GetReceivers(eventHubClient, _consumerGroupName);

            // Act
            await sut.Send(events);

            // Assert
            IEnumerable<EventData> received = await ReceiveAll(receivers);
            await eventHubClient.CloseAsync();

            IEnumerable<Envelope> actual = from eventData in received
                                           select serializer.Deserialize(eventData);

            actual.Should().BeEquivalentTo(
                envelopes,
                opts =>
                opts.WithStrictOrdering().RespectingRuntimeTypes());
        }

        [TestMethod]
        public void Send_has_guard_clause_against_null_element()
        {
            var builder = new Fixture();
            EventData[] envelopes = new[]
            {
                builder.Create<EventData>(),
                builder.Create<EventData>(),
                default,
            };
            var sut = new EventDataSender(EventHubClient.CreateFromConnectionString(_connectionString));
            var random = new Random();

            Func<Task> action = () =>
            sut.Send(
                from e in envelopes
                orderby random.Next()
                select e);

            action.Should().Throw<ArgumentException>().Where(x => x.ParamName == "events");
        }

        [TestMethod]
        public void Send_does_not_fail_even_if_events_is_empty()
        {
            var eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString);
            var sut = new EventDataSender(eventHubClient);

            Func<Task> action = () => sut.Send(new EventData[] { });

            action.Should().NotThrow();
        }

        [TestMethod]
        public async Task Send_sends_messages_with_partition_key_correctly()
        {
            // Arrange
            var eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString);
            var serializer = new EventDataSerializer();
            var sut = new EventDataSender(eventHubClient);

            var envelopes = new Fixture()
                .Build<Message>()
                .CreateMany()
                .Select(message => new Envelope(
                    messageId: Guid.NewGuid(),
                    message,
                    operationId: Guid.NewGuid(),
                    correlationId: Guid.NewGuid(),
                    contributor: $"{Guid.NewGuid()}"))
                .ToList();

            var events = envelopes
                .Select(envelope => serializer.Serialize(envelope))
                .ToList();

            string partitionKey = Guid.NewGuid().ToString();

            IEnumerable<PartitionReceiver> receivers =
                await GetReceivers(eventHubClient, _consumerGroupName);

            // Act
            await sut.Send(events, partitionKey);

            // Assert
            var received = new List<EventData>(await ReceiveAll(receivers));
            await eventHubClient.CloseAsync();

            IEnumerable<Envelope> actual = from eventData in received
                                           select serializer.Deserialize(eventData);

            actual.Should().BeEquivalentTo(
                envelopes,
                opts =>
                opts.WithStrictOrdering().RespectingRuntimeTypes());

            received.Should().OnlyContain(
                eventData =>
                eventData.SystemProperties.PartitionKey == partitionKey);
        }

        [TestMethod]
        public void Send_with_partition_key_has_guard_clause_against_null_element()
        {
            var builder = new Fixture();
            EventData[] envelopes = new[]
            {
                builder.Create<EventData>(),
                builder.Create<EventData>(),
                default,
            };
            var sut = new EventDataSender(EventHubClient.CreateFromConnectionString(_connectionString));
            var random = new Random();

            Func<Task> action = () =>
            sut.Send(
                from e in envelopes
                orderby random.Next()
                select e,
                "partition key");

            action.Should().Throw<ArgumentException>().Where(x => x.ParamName == "events");
        }

        [TestMethod]
        public void Send_with_partition_key_does_not_fail_even_if_events_is_empty()
        {
            var eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString);
            var sut = new EventDataSender(eventHubClient);

            Func<Task> action = () => sut.Send(new EventData[] { }, "partition key");

            action.Should().NotThrow();
        }

        public class Message
        {
            public string Content { get; set; }
        }
    }
}
