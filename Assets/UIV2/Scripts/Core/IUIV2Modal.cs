namespace Project51.UIV2.Core
{
    public interface IUIV2Modal
    {
        void Open();
        void Close();
        void CloseImmediate();
        bool IsOpen { get; }
    }
}
