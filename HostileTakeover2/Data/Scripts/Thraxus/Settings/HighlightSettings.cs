using HostileTakeover2.Thraxus.Common.Interfaces;
using VRageMath;

namespace HostileTakeover2.Thraxus.Settings
{
    /// <summary>
    /// Plain-data value bag that carries the parameters needed for a single
    /// <c>MyVisualScriptLogicProvider.SetHighlight</c> call.  Instances are pooled via
    /// the <see cref="Mediator"/> and reused across
    /// highlight cycles; <see cref="Reset"/> zeroes every field so stale data from a
    /// previous highlight does not bleed into the next use.
    /// </summary>
    internal class HighlightSettings : IReset
    {
        /// <summary>Block name passed to the game highlight API as the target identifier.</summary>
        public string Name;
        /// <summary>True while the highlight should be visible; false to remove it.</summary>
        public bool Enabled;
        /// <summary>Thickness of the highlight outline in pixels.  -1 disables the outline.</summary>
        public int LineThickness;
        /// <summary>Duration of one pulse cycle in ticks.  0 = no pulsing (solid highlight).</summary>
        public int PulseDuration;
        /// <summary>Colour of the highlight.  Each block type uses a distinct colour defined in <c>DefaultSettings</c>.</summary>
        public Color Color;
        /// <summary>Identity ID of the player who will see the highlight.</summary>
        public long PlayerId;

        /// <summary>
        /// Resets all fields to their zero/default values so the pooled object is safe
        /// for reuse without carrying over any state from its previous use.
        /// </summary>
        public void Reset()
        {
            Name = null;
            Enabled = false;
            LineThickness = 0;
            PulseDuration = 0;
            Color = default(Color);
            PlayerId = 0;
        }
    }
}
