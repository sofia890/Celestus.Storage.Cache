using System.Threading.Tasks.Dataflow;

namespace Celestus.Storage.Cache.PerformanceTest.Model.Signaling
{
    public class BufferBlockActor
    {
        public const int N_OPERATION_PER_INVOKE = 256;

        public static void RoundTripTimeRequestResponse()
        {
            BufferBlock<Signal> serverPort = new();
            BufferBlock<Signal> clientPort = new();

            for (int i = 0; i < N_OPERATION_PER_INVOKE; i++)
            {
                //
                // Request handling
                //
                serverPort.Post(new SignalAdditionRequest(4, 2));

                var requestPayload = (SignalAdditionRequest)serverPort.Receive();
                var result = requestPayload.ValueA + requestPayload.ValueB;
                clientPort.Post(new SignalAdditionResponse(result));

                //
                // Response handling
                //
                var responsePayload = (SignalAdditionResponse)clientPort.Receive();
                _ = responsePayload.Value;
            }
        }

        public static void SendingSignal()
        {
            BufferBlock<Signal> serverPort = new();

            for (int i = 0; i < N_OPERATION_PER_INVOKE; i++)
            {
                serverPort.Post(new SignalAdditionIndication(4, 2));

                var payload = (SignalAdditionIndication)serverPort.Receive();
                _ = payload.ValueA + payload.ValueB;
            }
        }
    }
}
