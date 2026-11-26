// Rewritten from git HEAD when the editor loads and before a player build.
// A commit cannot contain its own hash, so this fallback is the HEAD at the
// last time the writer ran.
namespace FlockFive
{
    public static class DevShaStamp
    {
        public const string Sha = "144ec3ee";
    }
}
