using Xunit;

namespace Driver.Test.Client.TMQ
{
    public partial class Consumer
    {
        [Fact]
        public void NativeConsumerTest()
        {
            var db = "tmq_consumer_test";
            var topic = "tmq_consumer_test_topic";
            this.NewConsumerTest(this._nativeConnectString, db, topic, this._nativeTMQCfg);
        }

        [Fact]
        public void NativeConsumerSeekTest()
        {
            var db = "tmq_seek_test";
            var topic = "tmq_seek_test_topic";
            this.ConsumerSeekTest(this._nativeConnectString, db, topic, this._nativeTMQCfg);
        }

        [Fact]
        public void NativeConsumerCommitTest()
        {
            var db = "tmq_commit_test";
            var topic = "tmq_commit_test_topic";
            this.ConsumerCommitTest(this._nativeConnectString, db, topic, this._nativeTMQCfg);
        }

        [Fact]
        public void NativeAutoCommitTest()
        {
            var db = "tmq_auto_commit_test";
            var topic = "tmq_auto_commit_test_topic";
            this.ConsumerAutoCommitTest(this._nativeConnectString, db, topic, this._nativeTMQCfgAutoCommit);
        }
    }
}