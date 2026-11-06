public class LoadingBlock : UIClickBlock<LoadingBlock>
{
    protected override void InternalOnAwake()
    {
    }

    protected override void InternalOnDestroy()
    {
        base.InternalOnDestroy();
    }

    private object _defaultReason = new();
    private void OnShowMask(bool showMask)
    {
        // gameObject.SetActive(showMask);
        if (showMask) AddBlock(_defaultReason);
        else RemoveBlock(_defaultReason);
    }
}
