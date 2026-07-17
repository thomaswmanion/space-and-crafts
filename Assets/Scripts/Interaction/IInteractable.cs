namespace SpaceAndCrafts.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact();
    }
}
