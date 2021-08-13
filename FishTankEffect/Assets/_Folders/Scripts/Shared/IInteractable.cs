
public interface IInteractable
{
    public string interactTxt { get; }
    public void OnLookedAt(PlayerInteractioner plrInteractor);
    public void OnInteractedWith(PlayerInteractioner plrInteractor);
}
