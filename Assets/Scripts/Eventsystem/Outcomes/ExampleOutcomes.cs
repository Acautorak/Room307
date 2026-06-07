using System.Linq;
using UnityEngine;

namespace StoryEvents.Outcomes
{
    // ── Stat Change Outcome ──────────────────────────────────────────────────

    /// <summary>
    /// Adds (or subtracts) a value from a named stat on GameContextSO.
    /// Create via: right-click → CK2Events/Outcomes/Stat Change
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Outcomes/Stat Change")]
    public class StatChangeOutcome : EventOutcomeSO
    {
        public enum Stat { Gold, Prestige, Piety, Troops }

        [Tooltip("Which stat to modify.")]
        public Stat stat;

        [Tooltip("Amount to add. Use negative values to subtract.")]
        public int delta;

        public override void Apply(GameContextSO context)
        {
            switch (stat)
            {
                case Stat.Gold:     context.gold     += delta; break;
                case Stat.Prestige: context.prestige += delta; break;
                case Stat.Piety:    context.sanity    += delta; break;
                case Stat.Troops:   context.xp   += delta; break;
            }

            Debug.Log($"[Events] StatChange: {stat} {(delta >= 0 ? "+" : "")}{delta} → now {GetValue(context)}");
        }

        public override string GetPreviewText()
        {
            string sign = delta >= 0 ? "+" : "";
            return $"{sign}{delta} {stat}";
        }

        private int GetValue(GameContextSO ctx) => stat switch
        {
            Stat.Gold     => ctx.gold,
            Stat.Prestige => ctx.prestige,
            Stat.Piety    => ctx.sanity,
            Stat.Troops   => ctx.xp,
            _             => 0
        };
    }

    // ── Set Flag Outcome ─────────────────────────────────────────────────────

    /// <summary>
    /// Sets or clears a boolean flag on GameContextSO.
    /// Use flags to gate future event chains via requiredCompletedChainIds,
    /// or check them in custom outcome logic.
    /// Create via: right-click → CK2Events/Outcomes/Set Flag
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Outcomes/Set Flag")]
    public class SetFlagOutcome : EventOutcomeSO
    {
        [Tooltip("Index into GameContextSO.flags[]")]
        [Range(0, 63)]
        public int flagIndex;

        [Tooltip("True to set the flag, false to clear it.")]
        public bool value = true;

        public override void Apply(GameContextSO context)
        {
            context.flags[flagIndex] = value;
            Debug.Log($"[Events] SetFlag: flags[{flagIndex}] = {value}");
        }

        public override string GetPreviewText() =>
            $"Flag {flagIndex} → {(value ? "set" : "cleared")}";
    }

    // ── Compound Outcome ─────────────────────────────────────────────────────

    /// <summary>
    /// Applies multiple outcomes at once. Drag any combination of outcome assets here.
    /// Create via: right-click → CK2Events/Outcomes/Compound
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Outcomes/Compound")]
    public class CompoundOutcome : EventOutcomeSO
    {
        [Tooltip("All outcomes applied in order when this chain ends.")]
        public EventOutcomeSO[] outcomes;

        public override void Apply(GameContextSO context)
        {
            foreach (var outcome in outcomes)
                outcome?.Apply(context);
        }

        public override string GetPreviewText()
        {
            var previews = System.Array.FindAll(outcomes, o => o != null)
                .Select(o => o.GetPreviewText())
                .Where(s => s != null)
                .ToArray();

            return previews.Length > 0 ? string.Join(", ", previews) : null;
        }
    }
}
