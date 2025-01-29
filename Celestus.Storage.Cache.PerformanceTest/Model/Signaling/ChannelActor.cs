using System.Threading.Channels;

namespace Celestus.Storage.Cache.PerformanceTest.Model.Signaling
{
    public class ChannelActor
    {
        public const int N_OPERATION_PER_INVOKE = 256;

        public static async Task RoundTripTimeRequestResponse()
        {
            var serverPort = Channel.CreateUnbounded<Signal>();
            var clientPort = Channel.CreateUnbounded<Signal>();

            for (int i = 0; i < N_OPERATION_PER_INVOKE; i++)
            {
                //
                // Request handling
                //
                _ = serverPort.Writer.TryWrite(new SignalAdditionRequest(4, 2));
                var signal = await serverPort.Reader.ReadAsync();

                var requestPayload = (SignalAdditionRequest)signal;
                var result = requestPayload.ValueA + requestPayload.ValueB;
                _ = clientPort.Writer.TryWrite(new SignalAdditionResponse(result));

                //
                // Response handling
                //
                signal = await clientPort.Reader.ReadAsync();

                var responsePayload = (SignalAdditionResponse)signal;
                _ = responsePayload.Value;
            }
        }

        public static async Task SendingSignal()
        {
            var serverPort = Channel.CreateUnbounded<Signal>();

            for (int i = 0; i < N_OPERATION_PER_INVOKE; i++)
            {
                _ = serverPort.Writer.TryWrite(new SignalAdditionIndication(4, 2));

                var signal = await serverPort.Reader.ReadAsync();
                var indicationPayload = (SignalAdditionIndication)signal;
                _ = indicationPayload.ValueA + indicationPayload.ValueB;
            }
        }
    }
}
