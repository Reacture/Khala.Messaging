namespace Owin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Khala.TransientFaultHandling;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Owin.Builder;
    using Microsoft.Owin.BuilderProperties;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class OwinMessagingExtensions_specs
    {
        public const string EventHubConnectionStringParam = "OwinMessagingExtensions/EventHubConnectionString";
        public const string ConsumerGroupNameParam = "OwinMessagingExtensions/ConsumerGroupName";
        public const string StorageConnectionStringParam = "OwinMessagingExtensions/StorageConnectionString";
        public const string LeaseContainerNameParam = "OwinMessagingExtensions/LeaseContainerName";

        private static readonly string ConnectionParametersRequired = $@"Event Hub connection information is not set. To run tests on the OwinMessagingExtensions class, you must set the connection information in the *.runsettings file as follows:

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{EventHubConnectionStringParam}"" value=""your connection string to the Event Hub"" />
    <Parameter name=""{ConsumerGroupNameParam}"" value=""[OPTIONAL] The name of the consumer group within the Event Hub"" />
    <Parameter name=""{StorageConnectionStringParam}"" value=""your connection string to Storage account for leases and checkpointing"" />
    <Parameter name=""{LeaseContainerNameParam}"" value=""Azure Storage container name for leases and checkpointing"" />
  </TestRunParameters>  
</RunSettings>

References
- https://msdn.microsoft.com/en-us/library/jj635153.aspx";

        private static string _eventHubConnectionString;
        private static string _consumerGroupName;
        private static string _storageConnectionString;
        private static string _leaseContainerName;

        public TestContext TestContext { get; set; }

        private static EventProcessorHost GetEventProcessorHost()
        {
            return new EventProcessorHost(
                eventHubPath: null,
                consumerGroupName: _consumerGroupName,
                eventHubConnectionString: _eventHubConnectionString,
                storageConnectionString: _storageConnectionString,
                leaseContainerName: _leaseContainerName);
        }

        private static async Task<EventHubRuntimeInformation> GetRuntimeInformation()
        {
            var eventHubClient = EventHubClient.CreateFromConnectionString(_eventHubConnectionString);
            EventHubRuntimeInformation runtimeInformation = await eventHubClient.GetRuntimeInformationAsync();
            await eventHubClient.CloseAsync();
            return runtimeInformation;
        }

        private static async Task CheckpointLatest(EventHubRuntimeInformation runtimeInformation)
        {
            var leasedPartitionIds = new HashSet<string>();
            var stopwatch = new Stopwatch();

            EventProcessorHost processorHost = GetEventProcessorHost();

            var factory = new CheckpointerFactory(() => new Checkpointer
            {
                OnOpen = partitionContext => leasedPartitionIds.Add(partitionContext.PartitionId),
                OnCheckpoint = eventData => stopwatch.Restart()
            });
            await processorHost.RegisterEventProcessorFactoryAsync(factory);

            do
            {
                await Task.Delay(10);
            }
            while (leasedPartitionIds.Count < runtimeInformation.PartitionCount);

            stopwatch.Start();
            do
            {
                await Task.Delay(10);
            }
            while (stopwatch.Elapsed.TotalSeconds < 1.0);
            stopwatch.Stop();

            await processorHost.UnregisterEventProcessorAsync();
        }

        private class Checkpointer : IEventProcessor
        {
            public Action<PartitionContext> OnOpen { get; set; }

            public Action<EventData> OnCheckpoint { get; set; }

            public Task CloseAsync(PartitionContext context, CloseReason reason) => Task.CompletedTask;

            public Task OpenAsync(PartitionContext context)
            {
                OnOpen(context);
                return Task.CompletedTask;
            }

            public Task ProcessErrorAsync(PartitionContext context, Exception error) => Task.CompletedTask;

            public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
            {
                foreach (EventData eventData in messages)
                {
                    await context.CheckpointAsync(eventData);
                    OnCheckpoint?.Invoke(eventData);
                }
            }
        }

        private class CheckpointerFactory : IEventProcessorFactory
        {
            private readonly Func<Checkpointer> _function;

            public CheckpointerFactory(Func<Checkpointer> function) => _function = function;

            public IEventProcessor CreateEventProcessor(PartitionContext context) => _function.Invoke();
        }

        public static Task<ICloudBlob> GetPartitionLease(string partitionId)
        {
            var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(_leaseContainerName);
            return blobContainer.GetBlobReferenceFromServerAsync($"{_consumerGroupName}/{partitionId}");
        }

        private static async Task<IReadOnlyCollection<ICloudBlob>> GetPartitionLeases(EventHubRuntimeInformation runtimeInformation)
        {
            return new List<ICloudBlob>(await Task.WhenAll(runtimeInformation.PartitionIds.Select(GetPartitionLease)));
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _eventHubConnectionString = (string)context.Properties[EventHubConnectionStringParam];
            _storageConnectionString = (string)context.Properties[StorageConnectionStringParam];
            _leaseContainerName = (string)context.Properties[LeaseContainerNameParam];

            if (string.IsNullOrWhiteSpace(_eventHubConnectionString) ||
                string.IsNullOrWhiteSpace(_storageConnectionString) ||
                string.IsNullOrWhiteSpace(_leaseContainerName))
            {
                Assert.Inconclusive(ConnectionParametersRequired);
            }

            _consumerGroupName = (string)context.Properties[ConsumerGroupNameParam] ?? PartitionReceiver.DefaultConsumerGroupName;
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            builder.Customize(new AutoMoqCustomization());
            builder.Register(GetEventProcessorHost);
            new GuardClauseAssertion(builder).Verify(typeof(OwinMessagingExtensions));
        }

        [TestMethod]
        public async Task UseEventProcessor_leases_partitions()
        {
            // Arrange
            EventHubRuntimeInformation runtimeInformation = await GetRuntimeInformation();
            await CheckpointLatest(runtimeInformation);

            var app = new AppBuilder();

            var appProperties = new AppProperties(app.Properties);
            var cancellation = new CancellationTokenSource();
            appProperties.OnAppDisposing = cancellation.Token;

            EventProcessorHost processorHost = GetEventProcessorHost();
            var messageHandler = Mock.Of<IMessageHandler>();
            var exceptionHandler = Mock.Of<IEventProcessingExceptionHandler>();

            // Act
            app.UseEventProcessor(processorHost, messageHandler, exceptionHandler);
            await RetryPolicy<bool>
                .LinearTransientDefault(5, TimeSpan.FromMilliseconds(500))
                .Run(async () =>
                {
                    IReadOnlyCollection<ICloudBlob> partitionLeases = await GetPartitionLeases(runtimeInformation);
                    return partitionLeases.All(lease => lease.Properties.LeaseState == LeaseState.Leased);
                });

            // Assert
            IReadOnlyCollection<ICloudBlob> actual = await GetPartitionLeases(runtimeInformation);
            actual.Should().OnlyContain(lease => lease.Properties.LeaseState == LeaseState.Leased);
            cancellation.Cancel();
        }

        [TestMethod]
        public async Task UseEventProcessor_releases_leases_when_app_disposing()
        {
            // Arrange
            EventHubRuntimeInformation runtimeInformation = await GetRuntimeInformation();
            await CheckpointLatest(runtimeInformation);

            var app = new AppBuilder();

            var appProperties = new AppProperties(app.Properties);
            var cancellation = new CancellationTokenSource();
            appProperties.OnAppDisposing = cancellation.Token;

            EventProcessorHost processorHost = GetEventProcessorHost();
            var messageHandler = Mock.Of<IMessageHandler>();
            var exceptionHandler = Mock.Of<IEventProcessingExceptionHandler>();

            // Act
            app.UseEventProcessor(processorHost, messageHandler, exceptionHandler);
            await RetryPolicy<bool>
                .LinearTransientDefault(5, TimeSpan.FromMilliseconds(500))
                .Run(async () =>
                {
                    IReadOnlyCollection<ICloudBlob> partitionLeases = await GetPartitionLeases(runtimeInformation);
                    return partitionLeases.All(lease => lease.Properties.LeaseState == LeaseState.Leased);
                });
            cancellation.Cancel();

            // Assert
            IReadOnlyCollection<ICloudBlob> actual = await GetPartitionLeases(runtimeInformation);
            actual.Should().NotContain(lease => lease.Properties.LeaseState == LeaseState.Leased);
        }
    }
}
