using System;

namespace Project51.Core
{
    /// <summary>Only visible, unobscured reading time counts; promotion grants a fresh countdown.</summary>
    public sealed class RoundAdvanceCountdown
    {
        public const float Duration = 10f; // "Si riparte da sola tra 10 secondi" (FineSmazzata, anche offline: scelta utente 01/10)
        public bool IsRunning { get; private set; }
        public float Remaining { get; private set; }
        private bool hadAuthority;
        public void Start() { IsRunning = true; Remaining = Duration; hadAuthority = false; }
        public void Cancel() { IsRunning = false; Remaining = 0f; hadAuthority = false; }
        public bool Advance(float delta, bool authority, bool blocked)
        {
            if (!IsRunning) return false;
            if (!authority) { hadAuthority = false; return false; }
            if (!hadAuthority) { Remaining = Duration; hadAuthority = true; }
            if (blocked) return false;
            Remaining = Math.Max(0f, Remaining - Math.Max(0f, delta));
            if (Remaining > 0f) return false;
            Cancel();
            return true;
        }
    }
}
