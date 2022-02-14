using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
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

        public IProducer<Null, TValue> GetProducer<TValue>(ISerializer<TValue> valueSerializer)
        {
            return GetProducer<Null, TValue>(GetProducerConfig(), null, valueSerializer);
        }

        public IProducer<Null, TValue> GetProducer<TValue>(IEnumerable<KeyValuePair<string, string>> configuration, ISerializer<TValue> valueSerializer)
        {
            return GetProducer<Null, TValue>(GetProducerConfig(configuration), null, valueSerializer);
        }

        public IProducer<TKey, TValue> GetProducer<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
        {
            return GetProducer<TKey, TValue>(GetProducerConfig(), keySerializer, valueSerializer);
        }

        public IProducer<TKey, TValue> GetProducer<TKey, TValue>(IEnumerable<KeyValuePair<string, string>> configuration, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
        {
            var builder = new ProducerBuilder<TKey, TValue>(GetProducerConfig(configuration));
            if (keySerializer != null) builder.SetKeySerializer(keySerializer);
            if (valueSerializer != null) builder.SetValueSerializer(valueSerializer);

            return builder.Build();

        }

        public IDictionary<string, string> GetProducerConfig() => BuildConfig(KafkaConfigType.Producer);

        public IDictionary<string, string> GetProducerConfig(IEnumerable<KeyValuePair<string, string>> extendedConfig) => BuildConfig(KafkaConfigType.Producer, extendedConfig);

        public IConsumer<Null, TValue> GetConsumer<TValue>(IDeserializer<TValue> valueDeserializer)
        {
            return GetConsumer<Null, TValue>(GetConsumerConfig(), null, valueDeserializer);
        }

        public IConsumer<Null, TValue> GetConsumer<TValue>(IEnumerable<KeyValuePair<string, string>> configuration, IDeserializer<TValue> valueDeserializer)
        {
            return GetConsumer<Null, TValue>(GetConsumerConfig(configuration), null, valueDeserializer);
        }

        public IConsumer<TKey, TValue> GetConsumer<TKey, TValue>(IDeserializer<TKey> keyDeserializer, IDeserializer<TValue> valueDeserializer)
        {
            return GetConsumer(GetConsumerConfig(), keyDeserializer, valueDeserializer);
        }

        public IConsumer<TKey, TValue> GetConsumer<TKey, TValue>(IEnumerable<KeyValuePair<string, string>> configuration, IDeserializer<TKey> keyDeserializer, IDeserializer<TValue> valueDeserializer)
        {
            var builder = new ConsumerBuilder<TKey, TValue>(GetConsumerConfig(configuration));
            if (keyDeserializer != null) builder.SetKeyDeserializer(keyDeserializer);
            if (valueDeserializer != null) builder.SetValueDeserializer(valueDeserializer);

            return builder.Build();
        }

        public IDictionary<string, string> GetConsumerConfig() => BuildConfig(KafkaConfigType.Consumer);

        public IDictionary<string, string> GetConsumerConfig(IEnumerable<KeyValuePair<string, string>> extendedConfig) => BuildConfig(KafkaConfigType.Consumer, extendedConfig);

        private IDictionary<string, string> BuildConfig(KafkaConfigType configType)
        {
            const string producerPrefix = "producer/", consumerPrefix = "consumer/";
            const StringComparison strComp = StringComparison.InvariantCultureIgnoreCase;

            var d = new Dictionary<string, string> {{"bootstrap.servers", Brokers}};

            foreach (KeyValuePair<string, StringValues> pair in Parameters)
            {
                if (pair.Key.StartsWith("#")) continue;

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

        private IDictionary<string, string> BuildConfig(KafkaConfigType configType,
            IEnumerable<KeyValuePair<string, string>> extendedConfig)
        {
            IDictionary<string, string> cfg = BuildConfig(configType);
            foreach (KeyValuePair<string, string> pair in extendedConfig)
                cfg[pair.Key] = pair.Value;

            return cfg;
        }

        private enum KafkaConfigType
        {
            Producer,
            Consumer
        }
    }
}
