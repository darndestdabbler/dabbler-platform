namespace Dabbler.Components.MasterDetail;

/// <summary>
/// Position of the <see cref="DirtyAwareActionRow.CustomActions"/> slot
/// relative to the standard Save / Cancel / Delete buttons.
/// </summary>
public enum ActionRowSlot
{
    /// <summary>Custom actions render after the standard buttons (default).</summary>
    End,

    /// <summary>Custom actions render before the standard buttons.</summary>
    Start,
}
