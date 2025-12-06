using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FaithburnEngine.Core.Inventory;
using FaithburnEngine.Content;
using System.Linq;
using FaithburnEngine.Core;
using System;

namespace FaithburnEngine.Rendering
{
    public sealed class HotbarRenderer
    {
        private readonly SpriteBatch _sb;
        private readonly Texture2D _slotBg;
        private readonly SpriteFont _font;
        private readonly Texture2D _whitePixel;
        private readonly ContentLoader _content;
        private readonly AssetRegistry _assets;

        public HotbarRenderer(SpriteBatch sb, ContentLoader content, Texture2D slotBg, SpriteFont font, GraphicsDevice gd, AssetRegistry assets)
        {
            _sb = sb;
            _content = content;
            _slotBg = slotBg;
            _font = font;
            _assets = assets;
            _whitePixel = new Texture2D(gd, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
        }

        // displayCount: if > 0, draw only the first displayCount slots (useful for hotbar)
        // if upperLeft is true, draw hotbar at upper-left using HotbarConstants.LeftPadding/TopPadding
        public void Draw(Inventory inv, int selectedIndex, int slotSize = HotbarConstants.SlotSize, int displayCount = HotbarConstants.DisplayCount, int padding = HotbarConstants.Padding, bool upperLeft = false)
        {
            int total = (displayCount > 0) ? Math.Min(displayCount, inv.Slots.Length) : inv.Slots.Length;

            var screenW = _sb.GraphicsDevice.Viewport.Width;
            var screenH = _sb.GraphicsDevice.Viewport.Height;

            int startX;
            int y;

            if (upperLeft)
            {
                startX = HotbarConstants.LeftPadding;
                y = HotbarConstants.TopPadding;
            }
            else
            {
                var width = total * slotSize + (total - 1) * padding;
                startX = (screenW - width) / 2;
                y = screenH - slotSize - HotbarConstants.BottomOffset;
            }

            _sb.Begin(samplerState: SamplerState.PointClamp);

            for (int i = 0; i < total; i++)
            {
                var x = startX + i * (slotSize + padding);
                var rect = new Rectangle(x, y, slotSize, slotSize);
                var bg = (i == selectedIndex) ? Color.Yellow * 0.6f : Color.Black * 0.6f;

                // background
                if (_slotBg != null)
                    _sb.Draw(_slotBg, rect, Color.White);
                else
                    _sb.Draw(_whitePixel, rect, bg);

                // selection highlight (overlay) — size based on padding
                if (i == selectedIndex)
                {
                    int selOffset = Math.Max(2, padding / 2);
                    _sb.Draw(_whitePixel, new Rectangle(x - selOffset, y - selOffset, slotSize + selOffset * 2, slotSize + selOffset * 2), Color.Yellow * 0.25f);
                }

                var slot = inv.Slots[i];
                if (!slot.IsEmpty)
                {
                    var itemDef = _content.Items.FirstOrDefault(it => it.Id == slot.ItemId);
                    if (itemDef != null)
                    {
                        Texture2D? tex = null;
                        if (!string.IsNullOrEmpty(itemDef.SpriteKey))
                        {
                            _assets.TryGetTexture(itemDef.SpriteKey, out tex);
                        }

                        if (tex != null)
                        {
                            int iconPad = Math.Max(4, slotSize / 12);
                            int protoMax = 84;
                            int maxW = slotSize - iconPad * 2;
                            int maxH = slotSize - iconPad * 2;
                            if (itemDef.Id == "proto_pickaxe")
                            {
                                maxW = Math.Min(maxW, protoMax);
                                maxH = Math.Min(maxH, protoMax);
                            }

                            float scale = Math.Min((float)maxW / tex.Width, (float)maxH / tex.Height);
                            int drawW = Math.Max(1, (int)Math.Round(tex.Width * scale));
                            int drawH = Math.Max(1, (int)Math.Round(tex.Height * scale));

                            int drawX = x + (slotSize - drawW) / 2;
                            int drawY = y + (slotSize - drawH) / 2;

                            var dest = new Rectangle(drawX, drawY, drawW, drawH);
                            _sb.Draw(tex, dest, Color.White);
                        }
                        else
                        {
                            int iconPad = Math.Max(6, slotSize / 8);
                            _sb.Draw(_whitePixel, new Rectangle(x + iconPad, y + iconPad, slotSize - iconPad * 2, slotSize - iconPad * 2), Color.Gray);
                        }
                    }
                }

                // Draw count (skipped here if no SpriteFont available)
                if (_font != null)
                {
                    string label = (i < 9) ? (i + 1).ToString() : "0";

                    // Measure and center label horizontally inside the slot
                    var size = _font.MeasureString(label) * HotbarConstants.FontScale;
                    float labelX = x + (slotSize - size.X) * 0.5f;
                    float labelY = y + HotbarConstants.FontPaddingY;

                    _sb.DrawString(_font, label, new Vector2(labelX, labelY), Color.White, 0f, Vector2.Zero, HotbarConstants.FontScale, SpriteEffects.None, 0f);
                }
            }

            _sb.End();
        }
    }
}