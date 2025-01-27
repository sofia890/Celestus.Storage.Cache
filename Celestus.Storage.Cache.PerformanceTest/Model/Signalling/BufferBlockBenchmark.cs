using BenchmarkDotNet.Attributes;
using System.Threading.Tasks.Dataflow;

namespace Celestus.Storage.Cache.PerformanceTest.Model.Signalling
{
    public class BufferBlockBenchmark
    {
        readonly BufferBlock<Signal> _serverPort = new();
        readonly BufferBlock<Signal> _clientPort = new();
        Task? _server;

        [GlobalSetup]
        public void Setup()
        {
            _ = _serverPort.TryReceiveAll(out _);
            _ = _clientPort.TryReceiveAll(out _);

            _server = Task.Run(async () =>
            {
                while (_serverPort != null && !_serverPort.Completion.IsCompleted)
                {
                    var signal = await _serverPort.ReceiveAsync();

                    switch (signal.Type)
                    {
                        case SignalType.AdditionRequest when signal is SignalAdditionRequest payload:
                            var result = payload.ValueA + payload.ValueB;
                            _clientPort.Post(new SignalAdditionResponse(result));
                            break;

                        case SignalType.AdditionIndication when signal is SignalAdditionIndication payload:
                            _ = payload.ValueA + payload.ValueB;
                            break;

                        case SignalType.Stop:
                            return;
                    }
                }
            });
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _serverPort.Post(new Signal(SignalType.Stop));

            while (_server != null && !_server.IsCompleted)
            {
                Thread.Sleep(10);
            }

            _server?.Dispose();
        }

        [Benchmark]
        public void RoundTripTimeRequestResponse()
        {
            _serverPort.Post(new SignalAdditionRequest(4, 2));

            var signal = _clientPort.Receive();

            switch (signal.Type)
            {
                case SignalType.AdditionResponse when signal is SignalAdditionResponse payload:
                    var result = payload.Value;
                    break;
            }
        }

        [Benchmark]
        public void SendingSignal()
        {
            _serverPort.Post(new SignalAdditionIndication(4, 2));
        }
    }
}
