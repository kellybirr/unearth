using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unearth.Kafka;

namespace ServiceResolver.UnitTests
{
    [TestClass]
    public class KafkaTests
    {
        [TestMethod]
        public async Task Kafka_Test_1()
        {
            var locator = new KafkaLocator();
            KafkaService service = await locator.Locate();

            const string topicName = "unit-test";
            var config = new Dictionary<string, object>
            {
                { "group.id", "unit-test-1" },
                { "auto.offset.reset", "earliest" }
            };

            bool done = false;
            using (var consumer = service.GetConsumer(config, new StringDeserializer(Encoding.UTF8)))
            {
                consumer.OnPartitionEOF += (_, end)
                    => Console.WriteLine($"Reached end of topic {end.Topic} partition {end.Partition}, next message will be at offset {end.Offset}");

                consumer.OnError += (_, error)
                    => Console.WriteLine($"Error: {error}");

                consumer.OnPartitionsAssigned += (_, partitions) =>
                {
                    Console.WriteLine($"Assigned partitions: [{string.Join(", ", partitions)}], member id: {consumer.MemberId}");
                    consumer.Assign(partitions);
                };

                consumer.OnPartitionsRevoked += (_, partitions) =>
                {
                    Console.WriteLine($"Revoked partitions: [{string.Join(", ", partitions)}]");
                    consumer.Unassign();
                };

                consumer.OnStatistics += (_, json)
                    => Console.WriteLine($"Statistics: {json}");

                consumer.Subscribe(topicName);

                Task consumerTask = Task.Run(() =>
                {
                    while (! done)
                    {
                        if (consumer.Consume(out Message<Null, string> msg, TimeSpan.FromSeconds(1)))
                        {
                            Console.WriteLine($"Topic: {msg.Topic} Partition: {msg.Partition} Offset: {msg.Offset} {msg.Value}");
                            consumer.CommitAsync(msg).Wait();
                        }
                    }
                });

                await Task.Delay(2000);

                using (var producer = service.GetProducer(new StringSerializer(Encoding.UTF8)))
                {
                    Console.WriteLine($"{producer.Name} producing on {topicName}.");
                    for (int i = 22; i > 0; i--)
                        await producer.ProduceAsync(topicName, null, Guid.NewGuid().ToString());

                    producer.Flush(TimeSpan.FromSeconds(10));
                }

                done = true;
                await consumerTask;
            }
        }
    }
}
