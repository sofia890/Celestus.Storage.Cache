namespace Celestus.Storage.Cache.PerformanceTest.Model.Signalling
{
    enum SignalType
    {
        AdditionRequest,
        AdditionResponse,
        AdditionIndication,
        Stop
    }

    record Signal(SignalType Type);

    record SignalAdditionRequest(int ValueA, int ValueB) : Signal(SignalType.AdditionRequest);

    record SignalAdditionIndication(int ValueA, int ValueB) : Signal(SignalType.AdditionIndication);

    record SignalAdditionResponse(int Value) : Signal(SignalType.AdditionResponse);
}
