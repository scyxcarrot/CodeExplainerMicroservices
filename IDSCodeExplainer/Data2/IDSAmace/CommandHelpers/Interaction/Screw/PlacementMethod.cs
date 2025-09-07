namespace IDS.Amace.Enumerators
{
    /** Methods for determining the screw direction while picking initial point */

    public enum PlacementMethod
    {
        HeadTip, // First indicate entry point/head, then exit point/tip
        Camera, // Indicate entry point, direction according to camera directon
        Translate, // Translate head, keep parallel to original screw
        MoveHead, // Move entry point, keep exit point/tip
        MoveTip, // Move tip point, keep entry point/head
        AdjustLength, // Change the length of an existing screw
    }
}