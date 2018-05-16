using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Microsoft.Extensions.Primitives;
using Unearth.Dns;

namespace Unearth.Kafka
{
    public class KafkaService : GenericService
    {
        public KafkaService() { }

        public KafkaService(IEnumerable<ServiceEndpoint> endpoints) : base(endpoints)
        { }

        public KafkaService(IEnumerable<DnsEntry> dnsEntries) : base(dnsEntries)
        { }

        public string Brokers
        {
            get
            {
                // get endpoints
                var sb = new StringBuilder();
                foreach (ServiceEndpoint ep in Endpoints)
                {
                    if (sb.Length > 0) sb.Append(',');
                    sb.Append($"{ep.Host}:{ep.Port}");
                }

                return sb.ToString();
            }
        }

        public Producer GetProducer()
        {
            return new Producer(GetProducerConfig);
        }

        public Producer<Null, TValue> GetProducer<TValue>(ISerializer<TValue> valueSerializer)
        {
            return new Producer<Null, TValue>(GetProducerConfig, null, valueSerializer);
        }

        public Producer<TKey, TValue> GetProducer<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
        {
            return new Producer<TKey, TValue>(GetProducerConfig, keySerializer, valueSerializer);
        }

        public IDictionary<string, object> GetProducerConfig => BuildConfig(KafkaConfigType.Producer);

        public Consumer GetConsumer()
        {
            return new Consumer(GetConsumerConfig);
        }

        public Consumer<Null, TValue> GetConsumer<TValue>(IDeserializer<TValue> valueDeserializer)
        {
            return new Consumer<Null, TValue>(GetConsumerConfig, null, valueDeserializer);
        }

        public Consumer<TKey, TValue> GetConsumer<TKey, TValue>(IDeserializer<TKey> keyDeserializer, IDeserializer<TValue> valueDeserializer)
        {
            return new Consumer<TKey, TValue>(GetConsumerConfig, keyDeserializer, valueDeserializer);
        }

        public IDictionary<string, object> GetConsumerConfig => BuildConfig(KafkaConfigType.Consumer);

        private IDictionary<string, object> BuildConfig(KafkaConfigType configType)
        {
            const string producerPrefix = "producer/", consumerPrefix = "consumer/";
            const StringComparison strComp = StringComparison.InvariantCultureIgnoreCase;

            var d = new Dictionary<string, object> {{"bootstrap.servers", Brokers}};

            foreach (KeyValuePair<string, StringValues> pair in Parameters)
            {
                string k = pair.Key;
                switch (configType)
                {
                    case KafkaConfigType.Producer:
                        if (k.StartsWith(consumerPrefix, strComp))
                            continue;

                        if (k.StartsWith(producerPrefix, strComp))
                            k = k.Substring(producerPrefix.Length);

                        d.Add(k, pair.Value.ToString());
                        break;
                    case KafkaConfigType.Consumer:
                        if (k.StartsWith(producerPrefix, strComp))
                            continue;

                        if (k.StartsWith(consumerPrefix, strComp))
                            k = k.Substring(consumerPrefix.Length);

                        d.Add(k, pair.Value.ToString());
                        break;
                }
            }

            return d;
        }

        private enum KafkaConfigType
        {
            Producer,
            Consumer
        }
    }
}
