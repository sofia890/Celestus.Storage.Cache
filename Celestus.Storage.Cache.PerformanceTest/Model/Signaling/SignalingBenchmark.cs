using BenchmarkDotNet.Attributes;

namespace Celestus.Storage.Cache.PerformanceTest.Model.Signaling
{
    public class SignalingBenchmark
    {
        [Benchmark(OperationsPerInvoke = BufferBlockActor.N_OPERATION_PER_INVOKE)]
        public void BufferBlockRoundTripTimeRequestResponse()
        {
            BufferBlockActor.RoundTripTimeRequestResponse();
        }

        [Benchmark(OperationsPerInvoke = BufferBlockActor.N_OPERATION_PER_INVOKE)]
        public void BufferBlockSendingSignal()
        {
            BufferBlockActor.SendingSignal();
        }

        [Benchmark(OperationsPerInvoke = ChannelActor.N_OPERATION_PER_INVOKE)]
        public void ChannelRoundTripTimeRequestResponse()
        {
            ChannelActor.RoundTripTimeRequestResponse().Wait();
        }

        [Benchmark(OperationsPerInvoke = ChannelActor.N_OPERATION_PER_INVOKE)]
        public void ChannelSendingSignal()
        {
            ChannelActor.SendingSignal().Wait();
        }
    }
}
