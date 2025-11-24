using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FaithburnEngine.Core.Inventory;
using FaithburnEngine.Content;
using System.Linq;

namespace FaithburnEngine.Rendering
{
    public sealed class HotbarRenderer
    {
        private readonly SpriteBatch _sb;
        private readonly ContentLoader _content;
        private readonly Texture2D _whitePixel;

        public HotbarRenderer(SpriteBatch sb, ContentLoader content, GraphicsDevice gd)
        {
            _sb = sb;
            _content = content;
            _whitePixel = new Texture2D(gd, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
        }

        public void Draw(Inventory inv, int selectedIndex, int slotSize = 48)
        {
            var screenW = _sb.GraphicsDevice.Viewport.Width;
            var y = _sb.GraphicsDevice.Viewport.Height - slotSize - 10;
            var total = inv.Slots.Length;
            var width = total * (slotSize + 6);
            var startX = (screenW - width) / 2;

            _sb.Begin(samplerState: SamplerState.PointClamp);

            for (int i = 0; i < total; i++)
            {
                var x = startX + i * (slotSize + 6);
                var rect = new Rectangle(x, y, slotSize, slotSize);
                var bg = (i == selectedIndex) ? Color.Yellow * 0.6f : Color.Black * 0.6f;
                _sb.Draw(_whitePixel, rect, bg);

                var slot = inv.Slots[i];
                if (!slot.IsEmpty)
                {
                    // For PoC: draw a placeholder rectangle or load sprite texture by spriteRef
                    var itemDef = _content.Items.FirstOrDefault(it => it.Id == slot.ItemId);
                    if (itemDef != null && itemDef.SpriteRef != null)
                    {
                        // Load texture from file path (cache in a real AtlasManager)
                        var tex = TextureCache.GetOrLoad(_sb.GraphicsDevice, itemDef.SpriteRef);
                        var dest = new Rectangle(x + 4, y + 4, slotSize - 8, slotSize - 8);
                        _sb.Draw(tex, dest, Color.White);
                    }

                    // Draw count
                    var countText = slot.Count.ToString();
                    // You need a SpriteFont loaded; for PoC use a debug draw or skip text
                }
            }

            _sb.End();
        }
    }
}