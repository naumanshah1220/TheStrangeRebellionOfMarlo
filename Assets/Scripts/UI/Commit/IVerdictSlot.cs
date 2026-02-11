namespace Detective.UI.Commit
{
    public interface IVerdictSlot
    {
        string SlotId { get; }
        void Populate(VerdictSlotDefinition definition, System.Collections.Generic.List<VerdictOption> options);
        bool TryGetSelection(out CaseVerdict.SlotSelection selection, out string error);
        bool TryGetOptions(out System.Collections.Generic.List<VerdictOption> options);
        string CurrentLabelOrBlank();
        event System.Action OnSlotChanged;
    }
}
