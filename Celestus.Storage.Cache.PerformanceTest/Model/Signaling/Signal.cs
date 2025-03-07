﻿namespace Celestus.Storage.Cache.PerformanceTest.Model.Signaling
{
    enum SignalType
    {
        AdditionRequest,
        AdditionResponse,
        AdditionIndication
    }

    record Signal(SignalType Type);

    record SignalAdditionRequest(int ValueA, int ValueB) : Signal(SignalType.AdditionRequest);

    record SignalAdditionIndication(int ValueA, int ValueB) : Signal(SignalType.AdditionIndication);

    record SignalAdditionResponse(int Value) : Signal(SignalType.AdditionResponse);
}
