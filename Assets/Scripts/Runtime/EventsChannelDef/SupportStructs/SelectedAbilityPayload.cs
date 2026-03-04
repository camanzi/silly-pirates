public struct SelectedAbilityPayload
{
    public SelectedAbilityPayload(AbilityBase selectedAbility, GridElement caster)
    {
        this.selectedAbility = selectedAbility;
        this.caster = caster;
    }

    public AbilityBase selectedAbility;
    public GridElement caster;
}
