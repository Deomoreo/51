namespace Project51.Unity
{
    /// <summary>
    /// Interfaccia per tutti i modal/popup.
    /// Ogni modal deve implementare Open/Close.
    /// </summary>
    public interface IModal
    {
        void Open();
        void Close();
        bool IsOpen { get; }
    }
}
