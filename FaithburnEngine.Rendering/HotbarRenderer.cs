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
        private readonly SpriteFont? _font;
        private readonly Texture2D _whitePixel;
        private readonly ContentLoader _content;
        private readonly AssetRegistry _assets;

        // Padding constants for UI elements
        // WHY these values: Provides visual breathing room so numbers don't touch slot edges
        private const int SlotNumberPaddingLeft = 8;
        private const int SlotNumberPaddingTop = 8;
        private const int StackCountPaddingBottom = 8;
        
        // Digit rendering size (1.5x larger than before: was 6x10, now 9x15)
        private const int DigitWidth = 9;
        private const int DigitHeight = 15;
        private const int DigitSpacing = 2;
        private const int SegmentThickness = 3; // Thicker segments for larger digits

        public HotbarRenderer(SpriteBatch sb, ContentLoader content, Texture2D slotBg, SpriteFont? font, GraphicsDevice gd, AssetRegistry assets)
        {
            _sb = sb;
            _content = content;
            _slotBg = slotBg;
            _font = font;
            _assets = assets;
            _whitePixel = new Texture2D(gd, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
        }

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

                // selection highlight (overlay)
                if (i == selectedIndex)
                {
                    int selOffset = Math.Max(2, padding / 2);
                    _sb.Draw(_whitePixel, new Rectangle(x - selOffset, y - selOffset, slotSize + selOffset * 2, slotSize + selOffset * 2), Color.Yellow * 0.25f);
                }

                // Draw slot number in upper-left corner (1-9, 0 for slot 10)
                // WHY: Helps player quickly identify which key activates which slot
                string slotNumber = (i < 9) ? (i + 1).ToString() : "0";
                DrawSlotNumber(x, y, slotNumber);

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

                        // Draw stack count at bottom-center for items with count > 1
                        if (slot.Count > 1)
                        {
                            DrawStackCount(x, y, slotSize, slot.Count.ToString());
                        }
                    }
                }
            }

            _sb.End();
        }

        /// <summary>
        /// Draw slot number in upper-left corner of slot.
        /// WHY upper-left: Matches Terraria convention, doesn't overlap with stack count at bottom.
        /// </summary>
        private void DrawSlotNumber(int slotX, int slotY, string number)
        {
            int textX = slotX + SlotNumberPaddingLeft;
            int textY = slotY + SlotNumberPaddingTop;

            // Draw background for readability
            _sb.Draw(_whitePixel, new Rectangle(textX - 2, textY - 2, DigitWidth + 4, DigitHeight + 4), Color.Black * 0.5f);

            // Draw the digit
            if (number.Length > 0)
            {
                DrawDigit(number[0], textX, textY, DigitWidth, DigitHeight, Color.White * 0.9f);
            }
        }

        /// <summary>
        /// Draw stack count at bottom-center of slot.
        /// WHY bottom-center: Standard convention, easy to read without obscuring icon.
        /// </summary>
        private void DrawStackCount(int slotX, int slotY, int slotSize, string countText)
        {
            int totalWidth = countText.Length * DigitWidth + (countText.Length - 1) * DigitSpacing;
            int textX = slotX + (slotSize - totalWidth) / 2;
            int textY = slotY + slotSize - DigitHeight - StackCountPaddingBottom;

            // Draw background for readability
            _sb.Draw(_whitePixel, new Rectangle(textX - 3, textY - 2, totalWidth + 6, DigitHeight + 4), Color.Black * 0.7f);

            // Draw each digit
            for (int d = 0; d < countText.Length; d++)
            {
                int dx = textX + d * (DigitWidth + DigitSpacing);
                DrawDigit(countText[d], dx, textY, DigitWidth, DigitHeight, Color.White);
            }
        }

        /// <summary>
        /// Draw a simple 7-segment style digit using rectangles.
        /// WHY: Fallback when no SpriteFont is loaded. Ensures numbers are always visible.
        /// </summary>
        private void DrawDigit(char digit, int x, int y, int w, int h, Color color)
        {
            int segW = w;
            int segH = SegmentThickness;
            int halfH = h / 2;

            bool[] segments = digit switch
            {
                '0' => new[] { true, true, true, false, true, true, true },
                '1' => new[] { false, false, true, false, false, true, false },
                '2' => new[] { true, false, true, true, true, false, true },
                '3' => new[] { true, false, true, true, false, true, true },
                '4' => new[] { false, true, true, true, false, true, false },
                '5' => new[] { true, true, false, true, false, true, true },
                '6' => new[] { true, true, false, true, true, true, true },
                '7' => new[] { true, false, true, false, false, true, false },
                '8' => new[] { true, true, true, true, true, true, true },
                '9' => new[] { true, true, true, true, false, true, true },
                _ => new[] { false, false, false, false, false, false, false }
            };

            // Top
            if (segments[0]) _sb.Draw(_whitePixel, new Rectangle(x, y, segW, segH), color);
            // Top-left
            if (segments[1]) _sb.Draw(_whitePixel, new Rectangle(x, y, segH, halfH), color);
            // Top-right
            if (segments[2]) _sb.Draw(_whitePixel, new Rectangle(x + segW - segH, y, segH, halfH), color);
            // Middle
            if (segments[3]) _sb.Draw(_whitePixel, new Rectangle(x, y + halfH - segH / 2, segW, segH), color);
            // Bottom-left
            if (segments[4]) _sb.Draw(_whitePixel, new Rectangle(x, y + halfH, segH, halfH), color);
            // Bottom-right
            if (segments[5]) _sb.Draw(_whitePixel, new Rectangle(x + segW - segH, y + halfH, segH, halfH), color);
            // Bottom
            if (segments[6]) _sb.Draw(_whitePixel, new Rectangle(x, y + h - segH, segW, segH), color);
        }
    }
}