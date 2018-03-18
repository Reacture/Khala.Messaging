namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventHubMessageBus_specs
    {
        public const string ConnectionStringParam = "EventHubMessageBus/ConnectionString";
        public const string ConsumerGroupNameParam = "EventHubMessageBus/ConsumerGroupName";

        private static readonly string ConnectionParametersRequired = $@"Event Hub connection information is not set. To run tests on the EventHubMessageBus class, you must set the connection information in the *.runsettings file as follows:

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

        private static async Task<IReadOnlyList<PartitionReceiver>> GetReceivers(EventHubClient eventHubClient, string consumerGroupName)
        {
            EventHubRuntimeInformation runtimeInformation = await eventHubClient.GetRuntimeInformationAsync();

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

        private static async Task<IReadOnlyList<EventData>> ReceiveAll(IEnumerable<PartitionReceiver> receivers)
        {
            IReadOnlyList<EventData>[] unflattened = await Task.WhenAll(receivers.Select(ReceiveAll));
            return unflattened.SelectMany(received => received).ToList();
        }

        private static async Task<IReadOnlyList<EventData>> ReceiveAll(PartitionReceiver receiver)
        {
            var messages = new List<EventData>();
            var waitTime = TimeSpan.FromMilliseconds(1000);

            while (true)
            {
                IEnumerable<EventData> received = await receiver.ReceiveAsync(10, waitTime);
                if (received == null)
                {
                    break;
                }

                messages.AddRange(received);
            }

            return messages;
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            builder.Customize(new AutoMoqCustomization());
            builder.Inject(EventHubClient.CreateFromConnectionString(_connectionString));
            new GuardClauseAssertion(builder).Verify(typeof(EventHubMessageBus));
        }

        [TestMethod]
        public async Task Send_sends_message_correctly()
        {
            var eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString);
            var serializer = new EventDataSerializer();
            var sut = new EventHubMessageBus(eventHubClient, serializer);
            var envelope = new Envelope(new Fixture().Create<Message>());
            IEnumerable<PartitionReceiver> receivers = await GetReceivers(eventHubClient, _consumerGroupName);

            await sut.Send(envelope, CancellationToken.None);

            IEnumerable<EventData> received = await ReceiveAll(receivers);
            await eventHubClient.CloseAsync();
            received.Should().HaveCount(1);
            Envelope actual = serializer.Deserialize(received.Single());
            actual.Should().BeEquivalentTo(envelope);
        }

        [TestMethod]
        public async Task Send_sets_partition_key_correctly()
        {
            var eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString);
            var sut = new EventHubMessageBus(eventHubClient);
            PartitionedMessage message = new Fixture().Create<PartitionedMessage>();
            IEnumerable<PartitionReceiver> receivers = await GetReceivers(eventHubClient, _consumerGroupName);

            await sut.Send(new Envelope(message), CancellationToken.None);

            IEnumerable<EventData> received = await ReceiveAll(receivers);
            await eventHubClient.CloseAsync();
            EventData eventData = received.Single();
            eventData.SystemProperties.PartitionKey.Should().Be(message.PartitionKey);
        }

        [TestMethod]
        public async Task Send_with_envelopes_sends_multiple_messages_correctly()
        {
            // Arrange
            var eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString);
            var serializer = new EventDataSerializer();
            var sut = new EventHubMessageBus(eventHubClient, serializer);

            var envelopes = new Fixture()
                .CreateMany<Message>()
                .Select(message => new Envelope(message))
                .ToList();

            IEnumerable<PartitionReceiver> receivers = await GetReceivers(eventHubClient, _consumerGroupName);

            // Act
            await sut.Send(envelopes, CancellationToken.None);

            // Assert
            IEnumerable<EventData> received = await ReceiveAll(receivers);
            await eventHubClient.CloseAsync();

            IEnumerable<Envelope> actual = from eventData in received
                                           select serializer.Deserialize(eventData);

            actual.Should().BeEquivalentTo(
                envelopes,
                opts =>
                opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        public async Task Send_with_envelopes_sends_partitioned_messages_correctly()
        {
            // Arrange
            var eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString);
            var serializer = new EventDataSerializer();
            var sut = new EventHubMessageBus(eventHubClient, serializer);

            string partitionKey = Guid.NewGuid().ToString();
            var envelopes = new Fixture()
                .Build<PartitionedMessage>()
                .With(message => message.PartitionKey, partitionKey)
                .CreateMany()
                .Select(message => new Envelope(message))
                .ToList();

            IEnumerable<PartitionReceiver> receivers = await GetReceivers(eventHubClient, _consumerGroupName);

            // Act
            await sut.Send(envelopes, CancellationToken.None);

            // Assert
            IEnumerable<EventData> received = await ReceiveAll(receivers);
            await eventHubClient.CloseAsync();

            IEnumerable<Envelope> actual = from eventData in received
                                           select serializer.Deserialize(eventData);

            actual.Should().BeEquivalentTo(
                envelopes,
                opts =>
                opts.WithStrictOrdering()
                    .RespectingRuntimeTypes());
        }

        [TestMethod]
        public async Task Send_with_envelopes_sets_partition_key_correctly()
        {
            // Arrange
            var eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString);
            var serializer = new EventDataSerializer();
            var sut = new EventHubMessageBus(eventHubClient, serializer);

            string partitionKey = Guid.NewGuid().ToString();
            var envelopes = new Fixture()
                .Build<PartitionedMessage>()
                .With(message => message.PartitionKey, partitionKey)
                .CreateMany()
                .Select(message => new Envelope(message))
                .ToList();

            IEnumerable<PartitionReceiver> receivers = await GetReceivers(eventHubClient, _consumerGroupName);

            // Act
            await sut.Send(envelopes, CancellationToken.None);

            // Assert
            IEnumerable<EventData> received = await ReceiveAll(receivers);
            await eventHubClient.CloseAsync();

            IEnumerable<string> partitionKeys =
                from eventData in received
                select eventData.SystemProperties.PartitionKey;

            partitionKeys.Should().OnlyContain(x => x == partitionKey);
        }

        [TestMethod]
        public void Send_with_envelopes_has_guard_clause_against_null_envelope()
        {
            Envelope[] envelopes = new[]
            {
                new Envelope(new object()),
                new Envelope(new object()),
                default,
            };
            var sut = new EventHubMessageBus(EventHubClient.CreateFromConnectionString(_connectionString));
            var random = new Random();

            Func<Task> action = () =>
            sut.Send(
                from e in envelopes
                orderby random.Next()
                select e,
                CancellationToken.None);

            action.Should().Throw<ArgumentException>().Where(x => x.ParamName == "envelopes");
        }

        [TestMethod]
        public void given_empty_envelopes_Send_returns_CompletedTask()
        {
            var sut = new EventHubMessageBus(EventHubClient.CreateFromConnectionString(_connectionString));
            Task actual = sut.Send(Enumerable.Empty<Envelope>(), CancellationToken.None);
            actual.Should().BeSameAs(Task.CompletedTask);
        }

        [TestMethod]
        public void Send_has_guard_clause_against_partition_key_conflict()
        {
            var sut = new EventHubMessageBus(EventHubClient.CreateFromConnectionString(_connectionString));
            var envelopes = new List<Envelope>(
                from message in new Fixture().CreateMany<PartitionedMessage>()
                select new Envelope(message));

            Func<Task> action = () => sut.Send(envelopes, CancellationToken.None);

            action.Should().Throw<ArgumentException>().Where(x => x.ParamName == "envelopes");
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
    }
}
