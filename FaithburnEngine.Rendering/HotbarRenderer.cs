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
        private readonly Texture2D _slotBg;
        private readonly SpriteFont _font;
        private readonly Texture2D _whitePixel;
        private readonly ContentLoader _content;

        public HotbarRenderer(SpriteBatch sb, ContentLoader content, Texture2D slotBg, SpriteFont font, GraphicsDevice gd)
        {
            _sb = sb;
            _content = content;
            _slotBg = slotBg;
            _font = font;
            _whitePixel = new Texture2D(gd, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
        }

        // displayCount: if > 0, draw only the first displayCount slots (useful for hotbar)
        public void Draw(Inventory inv, int selectedIndex, int slotSize = 96, int displayCount = -1, int padding = 12)
        {
            int total = (displayCount > 0) ? Math.Min(displayCount, inv.Slots.Length) : inv.Slots.Length;

            var screenW = _sb.GraphicsDevice.Viewport.Width;
            var y = _sb.GraphicsDevice.Viewport.Height - slotSize - 10;
            var width = total * slotSize + (total - 1) * padding;
            var startX = (screenW - width) / 2;

            _sb.Begin(samplerState: SamplerState.PointClamp);

            for (int i = 0; i < total; i++)
            {
                var x = startX + i * (slotSize + padding);
                var rect = new Rectangle(x, y, slotSize, slotSize);

                // background
                if (_slotBg != null)
                    _sb.Draw(_slotBg, rect, Color.White);
                else
                    _sb.Draw(_whitePixel, rect, Color.Black * 0.6f);

                // selection highlight (overlay)
                if (i == selectedIndex)
                {
                    _sb.Draw(_whitePixel, new Rectangle(x - 6, y - 6, slotSize + 12, slotSize + 12), Color.Yellow * 0.25f);
                }

                var slot = inv.Slots[i];
                if (!slot.IsEmpty)
                {
                    var itemDef = _content.Items.FirstOrDefault(it => it.Id == slot.ItemId);
                    if (itemDef != null && itemDef.SpriteRef != null)
                    {
                        var tex = TextureCache.GetOrLoad(_sb.GraphicsDevice, itemDef.SpriteRef);
                        if (tex != null)
                        {
                            var dest = new Rectangle(x + slotSize / 12, y + slotSize / 12, slotSize - slotSize / 6, slotSize - slotSize / 6);
                            _sb.Draw(tex, dest, Color.White);
                        }
                        else
                        {
                            _sb.Draw(_whitePixel, new Rectangle(x + slotSize / 8, y + slotSize / 8, slotSize - slotSize / 4, slotSize - slotSize / 4), Color.Gray);
                        }
                    }
                }

                // Draw slot number in upper-left with small padding
                if (_font != null)
                {
                    string label = (i < 9) ? (i + 1).ToString() : "0";
                    var paddingVec = new Vector2(slotSize * 0.08f, slotSize * 0.04f);
                    _sb.DrawString(_font, label, new Vector2(x, y) + paddingVec, Color.White);
                }
            }

            _sb.End();
        }
    }
}