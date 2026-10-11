using UnityEngine;

namespace FlockFive
{
    // Placeholder neck and head until the painterly ostrich lands.
    // It sits on the feeder a completed set would have fed, behind the birds.
    public sealed class OstrichMark : MonoBehaviour
    {
        public int Feeders = 2;
        public SpriteRenderer Head;
        Transform _neck;

        public static OstrichMark Attach(Transform parent, int feeders)
        {
            var root = new GameObject("Ostrich").transform;
            root.SetParent(parent, false);
            var mark = root.gameObject.AddComponent<OstrichMark>();
            mark.Feeders = feeders < 1 ? 2 : feeders;
            var head = WorldBuilder.Sprite("Head", SpriteCatalog.Glow, Vector3.zero, 1f, 9, root);
            head.transform.localScale = new Vector3(1.15f, 1.35f, 1f);
            mark.Head = head.GetComponent<SpriteRenderer>();
            var neck = WorldBuilder.Sprite("Neck", SpriteCatalog.Glow, Vector3.zero, 1f, 8, root);
            neck.transform.localPosition = new Vector3(0f, -1.15f, 0f);
            neck.transform.localScale = new Vector3(0.35f, 2.4f, 1f);
            neck.GetComponent<SpriteRenderer>().color = new Color(0.48f, 0.34f, 0.24f, 0.9f);
            mark._neck = neck.transform;
            mark.Pose(0f, 0f, 0, true);
            return mark;
        }

        public void Sync(Board b)
        {
            if (b == null || b.Ostrich == null || !b.Ostrich.Alive)
            {
                gameObject.SetActive(false);
                return;
            }
            var o = b.Ostrich;
            int slot = o.BlockSlot;
            float x = slot >= 0 ? FeederRow.X(slot, Feeders) : 0f;
            Pose(x, WorldBuilder.FeederY - 0.15f, o.Hits, true);
        }

        public void Pose(float x, float y, int hits, bool show)
        {
            gameObject.SetActive(show);
            if (!show) return;
            transform.position = new Vector3(x, y, 0f);
            if (_neck != null) _neck.localPosition = new Vector3(0f, -1.15f, 0f);
            if (Head == null) return;
            float d = hits <= 0 ? 0f : hits / 3f;
            if (d > 1f) d = 1f;
            Head.color = Color.Lerp(
                new Color(0.62f, 0.46f, 0.32f, 0.95f),
                new Color(0.28f, 0.20f, 0.16f, 0.95f),
                d);
        }
    }
}
