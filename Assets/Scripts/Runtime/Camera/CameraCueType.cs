public enum CameraCueType
{
    None,
    FocusCaster,
    FocusCasterThenTarget,
    FramePair,
    FrameCasterAndArea,
    FocusArea,
    FocusTarget,
    FocusCasterThenArea,
    // Appended last on purpose: the values above are stored as ints in ability and profile assets.
    FrameAll
}
