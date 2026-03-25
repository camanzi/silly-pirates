public struct SelectedAbilityPayload
{
    public SelectedAbilityPayload(AbilityBase ability, GridElement caster)
    {
        this.ability = ability;
        this.caster = caster;
    }

    public AbilityBase ability;
    public GridElement caster;
}
