public interface IStackablePassive
{
    // incoming è l'istanza appena creata dal chiamante, scartata subito dopo:
    // serve alle passive che sovrascrivono il proprio valore invece di stackare.
    void OnReapplied(PassiveAbilityController controller, PassiveAbilitySO incoming);
}
