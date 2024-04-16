using System;
using System.Collections.Generic;
using TDengine.Driver;
using TDengine.Driver.Impl.WebSocketMethods;
using TDengine.Driver.Impl.WebSocketMethods.Protocol;
using TDengineHelper;

namespace TDengine.TMQ.WebSocket
{
    public class Consumer<TValue> : IConsumer<TValue>
    {
        private readonly TMQOptions _options;
        private readonly TMQConnection _connection;

        private IDeserializer<TValue> valueDeserializer;

        private Dictionary<Type, object> defaultDeserializers = new Dictionary<Type, object>
        {
            { typeof(Dictionary<string, object>), DictionaryDeserializer.Dictionary },
        };

        public Consumer(ConsumerBuilder<TValue> builder)
        {
            _options = new TMQOptions(builder.Config);
            _connection = new TMQConnection(_options);
            if (builder.ValueDeserializer == null)
            {
                if (!defaultDeserializers.TryGetValue(typeof(TValue), out object deserializer))
                {
                    throw new InvalidOperationException(
                        $"Value deserializer was not specified and there is no default deserializer defined for type {typeof(TValue).Name}.");
                }

                this.valueDeserializer = (IDeserializer<TValue>)deserializer;
            }
            else
            {
                this.valueDeserializer = builder.ValueDeserializer;
            }
        }

        public ConsumeResult<TValue> Consume(int millisecondsTimeout)
        {
            var resp = _connection.Poll(millisecondsTimeout);
            if (!resp.HaveMessage)
            {
                return null;
            }

            var consumeResult = new ConsumeResult<TValue>(resp.MessageId, resp.Topic, resp.VgroupId, resp.Offset,
                (TMQ_RES)resp.MessageType);
            if (!NeedGetData((TMQ_RES)resp.MessageType)) return null;
            var result = new TMQWSRows(resp, _connection, TimeZoneInfo.Local);
            while (result.Read())
            {
                var value = this.valueDeserializer.Deserialize(result, false, null);
                consumeResult.Message.Add(new TmqMessage<TValue> { Value = value, TableName = result.TableName });
            }

            return consumeResult;
        }

        public List<TopicPartition> Assignment
        {
            get
            {
                var result = new List<TopicPartition>();
                var topics = Subscription();
                foreach (var topic in topics)
                {
                    var resp = _connection.Assignment(topic);
                    foreach (var assignment in resp.Assignment)
                    {
                        result.Add(new TopicPartition(topic, assignment.VGroupId));
                    }
                }

                return result;
            }
        }

        public List<string> Subscription()
        {
            var resp = _connection.Subscription();
            return resp.Topics;
        }

        public void Subscribe(IEnumerable<string> topic)
        {
            _connection.Subscribe((List<string>)topic, _options);
        }

        public void Subscribe(string topic)
        {
            _connection.Subscribe(new List<string> { topic }, _options);
        }

        public void Unsubscribe()
        {
            _connection.Unsubscribe();
        }

        public void Commit(ConsumeResult<TValue> consumerResult)
        {
            _connection.CommitOffset(consumerResult.Topic, consumerResult.Partition, consumerResult.Offset);
        }

        public List<TopicPartitionOffset> Commit()
        {
            _connection.Commit();
            return Committed(TimeSpan.Zero);
        }

        public void Commit(IEnumerable<TopicPartitionOffset> tpos)
        {
            foreach (var tpo in tpos)
            {
                _connection.CommitOffset(tpo.Topic, tpo.Partition, tpo.Offset);
            }
        }

        public void Seek(TopicPartitionOffset tpo)
        {
            _connection.Seek(tpo.Topic, tpo.Partition, tpo.Offset);
        }

        public List<TopicPartitionOffset> Committed(TimeSpan timeout)
        {
            var assignment = Assignment;
            var args = new List<WSTopicVgroupId>(assignment.Count);
            var result = new List<TopicPartitionOffset>(assignment.Count);
            foreach (var topicPartition in assignment)
            {
                args.Add(new WSTopicVgroupId
                {
                    Topic = topicPartition.Topic,
                    VGroupId = topicPartition.Partition,
                });
            }

            var resp = _connection.Committed(args);
            for (int i = 0; i < args.Count; i++)
            {
                result.Add(new TopicPartitionOffset(args[i].Topic, args[i].VGroupId, resp.Committed[i]));
            }

            return result;
        }

        public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout)
        {
            var args = new List<WSTopicVgroupId>();
            var result = new List<TopicPartitionOffset>();
            foreach (var topicPartition in partitions)
            {
                args.Add(new WSTopicVgroupId
                {
                    Topic = topicPartition.Topic,
                    VGroupId = topicPartition.Partition,
                });
            }

            var resp = _connection.Committed(args);
            for (int i = 0; i < args.Count; i++)
            {
                result.Add(new TopicPartitionOffset(args[i].Topic, args[i].VGroupId, resp.Committed[i]));
            }

            return result;
        }

        public Offset Position(TopicPartition partition)
        {
            var vgid = new List<WSTopicVgroupId>(1)
            {
                new WSTopicVgroupId
                {
                    Topic = partition.Topic,
                    VGroupId = partition.Partition
                }
            };
            var resp = _connection.Position(vgid);
            return resp.Position[0];
        }

        public void Close()
        {
            _connection.Close();
        }

        private bool NeedGetData(TMQ_RES type)
        {
            return type == TMQ_RES.TMQ_RES_DATA || type == TMQ_RES.TMQ_RES_METADATA;
        }
    }
}