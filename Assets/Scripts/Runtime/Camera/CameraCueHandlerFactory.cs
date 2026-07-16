using System.Collections.Generic;

public static class CameraCueHandlerFactory
{
    private static readonly Dictionary<CameraCueType, ICameraCueHandler> _handlers = new() {
        { CameraCueType.FocusCaster, new FocusCasterCueHandler() },
        { CameraCueType.FocusCasterThenTarget, new FocusCasterThenTargetCueHandler() },
        { CameraCueType.FramePair, new FramePairCueHandler() },
        { CameraCueType.FrameCasterAndArea, new FrameCasterAndAreaCueHandler() },
        { CameraCueType.FocusArea, new FocusAreaCueHandler() }
    };

    public static ICameraCueHandler GetHandler(CameraCueType type)
    {
        if (_handlers.TryGetValue(type, out var handler)) return handler;
        return null;
    }
}
