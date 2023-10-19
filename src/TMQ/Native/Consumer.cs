﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TDengine.Driver;
using TDengine.Driver.Impl.NativeMethods;
using TDengineHelper;

namespace TDengine.TMQ.Native
{
    internal class Consumer : IConsumer
    {
        private IntPtr _consumer = IntPtr.Zero;

        private void NullReferenceHandler(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                throw new NullReferenceException();
            }
        }

        internal void ConfSet(IntPtr conf, IEnumerable<KeyValuePair<string, string>> values)
        {
            NullReferenceHandler(conf);
            foreach (var v in values)
            {
                int code = NativeMethods.TmqConfSet(conf, v.Key, v.Value);
                if ((TMQ_CONF_RES)code == TMQ_CONF_RES.TMQ_CONF_UNKNOWN)
                {
                    throw new Exception("set config failed,since TMQ_CONF_UNKNOWN ");
                }
                else if ((TMQ_CONF_RES)code == TMQ_CONF_RES.TMQ_CONF_INVALID)
                {
                    throw new Exception($"set config failed,since TMQ_CONF_INVALID key:{v.Key} value:{v.Value}");
                }
            }
        }

        public Consumer(ConsumerBuilder builder)
        {
            var confPtr = NativeMethods.TmqConfNew();
            NullReferenceHandler(confPtr);
            ConfSet(confPtr, builder.Config);

            int errStringLength = 256;
            IntPtr errStrPtr = Marshal.AllocHGlobal(errStringLength);
            _consumer = NativeMethods.TmqConsumerNew(confPtr, errStrPtr, errStringLength);
            try
            {
                // error happened while create new consumer
                if (_consumer == IntPtr.Zero)
                {
                    // read Error string 
                    string errStr = StringHelper.PtrToStringUTF8(errStrPtr, errStringLength);
                    throw  new TDengineError(-1,$"Create new Consumer failed, reason:{errStr}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(errStrPtr);
                if (confPtr != IntPtr.Zero)
                {
                    NativeMethods.TmqConfDestroy(confPtr);
                }
            }
        }

        private void ErrorHandler(string errMsg, int code)
        {
            string errStr = StringHelper.PtrToStringUTF8(NativeMethods.TmqErr2Str(code));
            throw new TDengineError(code,$"TDengine TMQ Error:{errMsg} failed,reason:{errStr} \t code: {code}");
        }

        public List<string> Subscription()
        {
            NullReferenceHandler(_consumer);
            IntPtr topicListPtr = NativeMethods.TmqListNew();
            IntPtr topicListPPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)));
            Marshal.WriteIntPtr(topicListPPtr, topicListPtr);
            List<string> topics;
            try
            {
                int code = NativeMethods.TmqSubscription(_consumer, topicListPPtr);

                if (code == 0)
                {
                    var cArraySize = NativeMethods.TmqListGetSize(topicListPtr);
                    var cArrayPtr = NativeMethods.TmqListToCArray(topicListPtr);

                    topics = new List<string>(cArraySize);
                    var offset = Marshal.SizeOf(typeof(IntPtr));

                    for (int i = 0; i < cArraySize; i++)
                    {
                        string tmpStr = StringHelper.PtrToStringUTF8(Marshal.ReadIntPtr(cArrayPtr + (i * offset)));
                        topics.Add(tmpStr);
                    }

                    return topics;
                }
                else
                {
                    this.ErrorHandler("tmq_subscription", code);
                    return null;
                }
            }
            finally
            {
                NativeMethods.TmqListDestroy(topicListPtr);
                Marshal.FreeHGlobal(topicListPPtr);
            }
        }

        public Offset Position(TopicPartition partition)
        {
            var offset =  (Offset)NativeMethods.TmqPosition(_consumer, partition.Topic, partition.Partition);
            if (offset < 0 && !offset.IsSpecial)
            {
                ErrorHandler("tmq_position", (int)offset);
            }

            return offset;
        }

        public void Close()
        {
            if (_consumer != IntPtr.Zero)
            {
                NativeMethods.TmqConsumerClose(_consumer);
                _consumer = IntPtr.Zero;
            }
        }

        public ConsumeResult Consume(int millisecondsTimeout)
        {
            NullReferenceHandler(_consumer);
            IntPtr message = NativeMethods.TmqConsumerPoll(_consumer, millisecondsTimeout);


            if (message == IntPtr.Zero)
            {
                return null;
            }

            IntPtr numOfRowsPrt = Marshal.AllocHGlobal(sizeof(Int32));
            IntPtr pDataPtr = Marshal.AllocHGlobal(IntPtr.Size);
            IntPtr pData;
            TMQ_RES type = (TMQ_RES)NativeMethods.TmqGetResType(message);
            string topic = StringHelper.PtrToStringUTF8(NativeMethods.TmqGetTopicName(message));
            int vGourpId = NativeMethods.TmqGetVgroupId(message);
            
            long offset = NativeMethods.TmqGetVgroupOffset(message);
            ConsumeResult consumeResult = new ConsumeResult(message, topic, vGourpId, offset, type);

            if (NeedGetData(type))
            {
                try
                {
                    while (true)
                    {
                        var dataMessage = new TmqMessage();
                        int code = NativeMethods.FetchRawBlock(message, numOfRowsPrt, pDataPtr);

                        if (code != 0)
                        {
                            ErrorHandler($"TMQ fetch_raw_block failed",code);
                        }
                        string table = StringHelper.PtrToStringUTF8(NativeMethods.TmqGetTableName(message));
                        if (!string.IsNullOrEmpty(table))
                        {
                            dataMessage.TableName = table;
                        }
                        
                        int numOfRows = Marshal.ReadInt32(numOfRowsPrt);
                        int numOfFields = NativeMethods.FieldCount(message);
                        var precision = NativeMethods.ResultPrecision(message);
                        pData = Marshal.ReadIntPtr(pDataPtr);

                        if (numOfRows == 0)
                        {
                            break;
                        }

                        if (numOfRows < 0)
                        {
                            ErrorHandler("TMQ fetch_raw_block failed", numOfRows);
                        }
                        List<TDengineMeta> metaList = NativeMethods.FetchFields(message);
                        var types = new byte[metaList.Count];
                        for (int i = 0; i < metaList.Count; i++)
                        {
                            types[i] = metaList[i].type;
                        }

                        var data = new List<List<object>>(numOfRows);
                        var br = new BlockReader(0, numOfFields, precision, types, TimeZoneInfo.Local);
                        br.SetBlockPtr(pData,numOfRows);
                        
                        for (int rowIndex = 0; rowIndex < numOfRows; rowIndex++)
                        {
                            List<Object> dataRow = new List<object>(numOfFields);
                            for (int colIndex = 0; colIndex < numOfFields; colIndex++)
                            {
                                dataRow.Add(br.Read(rowIndex, colIndex));
                            }
                            data.Add(dataRow);
                        }

                        dataMessage.Datas = data;
                        dataMessage.Metas = metaList;
                        consumeResult.Message.Add(dataMessage);
                    }
                    return consumeResult;
                }
                finally
                {
                    Marshal.FreeHGlobal(numOfRowsPrt);
                    Marshal.FreeHGlobal(pDataPtr);
                }
            }
            return null;
        }

        private bool NeedGetData(TMQ_RES type)
        {
            if (type == TMQ_RES.TMQ_RES_DATA || type == TMQ_RES.TMQ_RES_METADATA)
            {
                return true;
            }

            return false;
        }

        public List<TopicPartition> Assignment
        {
            get
            {
                var topicPartitions = new List<TopicPartition>();
                var topics = Subscription();
                foreach (var topic in topics)
                {
                    var assignments = NativeMethods.GetTopicAssignments(_consumer, topic);
                    foreach (var assignment in assignments)
                    {
                        topicPartitions.Add(new TopicPartition(topic, assignment.vgId));
                    }
                }

                return topicPartitions;
            }
        }

        public void Subscribe(IEnumerable<string> topics)
        {
            NullReferenceHandler(_consumer);
            IntPtr topicPtr = NativeMethods.TmqListNew();
            NullReferenceHandler(topicPtr);

            foreach (var topic in topics)
            {
                int code = NativeMethods.TmqListAppend(topicPtr, topic);
                if (code != 0)
                {
                    ErrorHandler("tmq_list_append", code);
                }
            }

            NativeMethods.TmqSubscribe(_consumer, topicPtr);
            NativeMethods.TmqListDestroy(topicPtr);
        }

        public void Subscribe(string topic)
        {
            NullReferenceHandler(_consumer);
            IntPtr topicPtr = NativeMethods.TmqListNew();
            NullReferenceHandler(topicPtr);

            int code = NativeMethods.TmqListAppend(topicPtr, topic);
            if (code != 0)
            {
                ErrorHandler("tmq_list_append", code);
            }

            code = NativeMethods.TmqSubscribe(_consumer, topicPtr);
            if (code != 0)
            {
                ErrorHandler("tmq_list_append", code);
            }
            NativeMethods.TmqListDestroy(topicPtr);
        }

        public void Unsubscribe()
        {
            NullReferenceHandler(_consumer);
            int code = NativeMethods.TmqUnsubscribe(_consumer);
            if (code != 0)
            {
                ErrorHandler("tmq_unsubscribe", code);
            }
        }

        public void Commit(ConsumeResult consumerResult)
        {
            NullReferenceHandler(_consumer);
            int code;
            if ((code = NativeMethods.TmqCommitSync(_consumer, consumerResult.ResultPtr)) != 0)
            {
                ErrorHandler("Sync Commit", code);
            }
        }

        public List<TopicPartitionOffset> Commit()
        {
            NullReferenceHandler(_consumer);
            int code;
            if ((code = NativeMethods.TmqCommitSync(_consumer, IntPtr.Zero)) != 0)
            {
                ErrorHandler("Sync Commit", code);
            }

            return Committed(TimeSpan.Zero);
        }

        public void Commit(IEnumerable<TopicPartitionOffset> offsets)
        {
            foreach (var topicPartitionOffset in offsets)
            {
                var code = NativeMethods.TmqCommitOffsetSync(_consumer, topicPartitionOffset.Topic,
                    topicPartitionOffset.Partition.Value,
                    topicPartitionOffset.Offset);
                if (code != 0)
                {
                    ErrorHandler("commit offset", code);
                }
            }
        }

        public void Seek(TopicPartitionOffset tpo)
        {
            NativeMethods.TmqOffsetSeek(_consumer, tpo.Topic, tpo.Partition.Value, tpo.Offset);
        }

        public List<TopicPartitionOffset> Committed(TimeSpan timeout)
        {
            return Committed(Assignment, timeout);
        }

        public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout)
        {
            var result = new List<TopicPartitionOffset>();
            foreach (var topicPartition in partitions)
            {
                var offset =
                    (Offset)NativeMethods.TmqCommitted(_consumer, topicPartition.Topic, topicPartition.Partition.Value);
                if (offset < 0 && !offset.IsSpecial)
                {
                    ErrorHandler("tmq_committed", (int)offset);
                }

                result.Add(new TopicPartitionOffset(topicPartition.Topic, topicPartition.Partition.Value, offset));
            }

            return result;
        }
    }
}