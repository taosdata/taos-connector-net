using Xunit;

namespace Driver.Test.Client.TMQ
{
    public partial class Consumer
    {
        [Fact]
        public void WSConsumerTest()
        {
            var db = "ws_tmq_consumer_test";
            var topic = "ws_tmq_consumer_test_topic";
            this.NewConsumerTest(this._wsConnectString, db, topic, this._wsTMQCfg);
        }

        [Fact]
        public void WSConsumerSeekTest()
        {
            var db = "ws_tmq_seek_test";
            var topic = "ws_tmq_seek_test_topic";
            this.ConsumerSeekTest(this._wsConnectString, db, topic, this._wsTMQCfg);
        }

        [Fact]
        public void WSConsumerCommitTest()
        {
            var db = "ws_tmq_commit_test";
            var topic = "ws_tmq_commit_test_topic";
            this.ConsumerCommitTest(this._wsConnectString, db, topic, this._wsTMQCfg);
        }

        [Fact]
        public void WSAutoCommitTest()
        {
            var db = "ws_tmq_auto_commit_test";
            var topic = "ws_tmq_auto_commit_test_topic";
            this.ConsumerAutoCommitTest(this._wsConnectString, db, topic, this._wsTMQCfgAutoCommit);
        }
    }
}