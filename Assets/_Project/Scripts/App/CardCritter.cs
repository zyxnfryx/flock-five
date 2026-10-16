using UnityEngine;

namespace FlockFive
{
    // The picture on a card. Moth Guest is its own fuzzy moth. Every other kind
    // is the painted bee, multiplied by BeeInk so a dark kind stays legible.
    public static class CardCritter
    {
        public static void Draw(Rect r, string id, Color tint, float alpha)
        {
            if (r.width < 4f || r.height < 4f || alpha < 0.04f) return;
            if (id == "moth")
            {
                var moth = MothArt.Texture();
                if (moth == null) return;
                GUI.color = new Color(1f, 0.98f, 0.94f, alpha);
                GUI.DrawTexture(r, moth, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                return;
            }
            var bee = SpriteCatalog.Bee;
            if (bee == null || bee.texture == null) return;
            var ink = BeeInk.Mul(tint);
            ink.a = alpha;
            float grow = Mathf.Max(1.5f, r.width * 0.05f);
            var halo = new Rect(r.x - grow, r.y - grow * 0.25f, r.width + grow * 2f, r.height + grow * 0.5f);
            GUI.color = new Color(0.12f, 0.07f, 0.03f, alpha * 0.72f);
            GUI.DrawTexture(halo, bee.texture, ScaleMode.ScaleToFit, true);
            GUI.color = ink;
            GUI.DrawTexture(r, bee.texture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            BeeAccessoryArt.DrawFor(r, id, alpha);
        }
    }
}
