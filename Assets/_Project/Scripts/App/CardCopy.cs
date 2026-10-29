namespace FlockFive
{
    // One flavor line for both faces. The reverse groaner used to replace the
    // face sentence on the flip, so the two sides did not say the same thing.
    // BeeKind.Back stays in the roster for the fit suite. The card draws Shown.
    public static class CardCopy
    {
        public static string Shown(BeeKind kind)
        {
            if (!string.IsNullOrEmpty(kind.Front)) return kind.Front;
            return kind.Back ?? "";
        }

        // The flipped side. OwnBack defaults to true, so every roster card says its
        // Back line here; Shown is only the fallback when Back is empty.
        public static string BackOwn(BeeKind kind) =>
            kind.OwnBack && !string.IsNullOrEmpty(kind.Back) ? kind.Back : Shown(kind);
    }
}
