using BenchmarkDotNet.Attributes;
using System.Threading.Tasks.Dataflow;

namespace Celestus.Storage.Cache.PerformanceTest.Model.Signaling
{
    public class BufferBlockBenchmark
    {
        const int N_OPERATION_PER_INVOKE = 256;

        [Benchmark(Baseline = true)]
        public void CreatePorts()
        {

            //
            // Request handling
            //
            BufferBlock<Signal> serverPort = new();
            BufferBlock<Signal> clientPort = new();
        }

        [Benchmark(OperationsPerInvoke = N_OPERATION_PER_INVOKE)]
        public void RoundTripTimeRequestResponse()
        {
            BufferBlock<Signal> serverPort = new();
            BufferBlock<Signal> clientPort = new();

            for (int i = 0; i < N_OPERATION_PER_INVOKE; i++)
            {
                //
                // Request handling
                //
                serverPort.Post(new SignalAdditionRequest(4, 2));

                var signal = serverPort.Receive();

                switch (signal.Type)
                {
                    case SignalType.AdditionRequest when signal is SignalAdditionRequest payload:
                        var result = payload.ValueA + payload.ValueB;
                        clientPort.Post(new SignalAdditionResponse(result));
                        break;

                    case SignalType.AdditionIndication when signal is SignalAdditionIndication payload:
                        _ = payload.ValueA + payload.ValueB;
                        break;

                    case SignalType.Stop:
                        return;
                }

                //
                // Response handling
                //
                signal = clientPort.Receive();

                switch (signal.Type)
                {
                    case SignalType.AdditionResponse when signal is SignalAdditionResponse payload:
                        _ = payload.Value;
                        break;
                }
            }

            serverPort.Post(new Signal(SignalType.Stop));
        }

        [Benchmark(OperationsPerInvoke = N_OPERATION_PER_INVOKE)]
        public void SendingSignal()
        {
            BufferBlock<Signal> serverPort = new();

            for (int i = 0; i < N_OPERATION_PER_INVOKE; i++)
            {
                serverPort.Post(new SignalAdditionIndication(4, 2));
            }
        }
    }
}
