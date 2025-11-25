using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace FaithburnEngine.UI
{
    public class HotbarUI
    {
        readonly IInventoryAdapter inventory;
        readonly Texture2D slotBg;
        readonly SpriteFont font;
        readonly int slotSize;
        readonly int padding;
        readonly int slotCount;
        int selectedIndex = 0;
        int lastScrollValue;

        public event Action<int> OnUseRequested;

        public HotbarUI(IInventoryAdapter inventory, Texture2D slotBg, SpriteFont font, int slotSize = 48, int padding = 6)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.slotBg = slotBg ?? throw new ArgumentNullException(nameof(slotBg));
            this.font = font ?? throw new ArgumentNullException(nameof(font));
            this.slotSize = slotSize;
            this.padding = padding;
            this.slotCount = Math.Max(1, inventory.SlotCount);
            lastScrollValue = Mouse.GetState().ScrollWheelValue;
            inventory.OnSlotChanged += idx => { /* optional: mark dirty or animate */ };
        }

        public void Update(GameTime gameTime)
        {
            HandleKeyboard();
            HandleMouseWheel();
            HandleMouseClicks();
        }

        void HandleKeyboard()
        {
            var kb = Keyboard.GetState();

            // Map 1..9,0 to 0..9
            for (int i = 0; i < slotCount && i < 10; i++)
            {
                Keys key = i == 9 ? Keys.D0 : (Keys)((int)Keys.D1 + i);
                if (kb.IsKeyDown(key))
                {
                    selectedIndex = i;
                }
            }

            // Optional: arrow keys (single-frame edge detection recommended if you want one-step per press)
            if (kb.IsKeyDown(Keys.Left))
                selectedIndex = (selectedIndex - 1 + slotCount) % slotCount;
            if (kb.IsKeyDown(Keys.Right))
                selectedIndex = (selectedIndex + 1) % slotCount;
        }

        void HandleMouseWheel()
        {
            var ms = Mouse.GetState();
            int scroll = ms.ScrollWheelValue;
            if (scroll != lastScrollValue)
            {
                int delta = Math.Sign(scroll - lastScrollValue);
                selectedIndex = (selectedIndex - delta + slotCount) % slotCount;
                lastScrollValue = scroll;
            }
        }

        void HandleMouseClicks()
        {
            var ms = Mouse.GetState();
            if (ms.LeftButton == ButtonState.Pressed)
            {
                OnUseRequested?.Invoke(selectedIndex);
            }
        }

        public void Draw(SpriteBatch sb, GraphicsDevice gd)
        {
            int totalWidth = slotCount * slotSize + (slotCount - 1) * padding;
            int x0 = (gd.Viewport.Width - totalWidth) / 2;
            int y = gd.Viewport.Height - slotSize - 10;

            for (int i = 0; i < slotCount; i++)
            {
                int x = x0 + i * (slotSize + padding);
                var slot = inventory.GetSlot(i);

                // selection highlight (behind)
                if (i == selectedIndex)
                {
                    var highlightRect = new Rectangle(x - 3, y - 3, slotSize + 6, slotSize + 6);
                    sb.Draw(slotBg, highlightRect, Color.Yellow * 0.25f);
                }

                // background
                sb.Draw(slotBg, new Rectangle(x, y, slotSize, slotSize), Color.White);

                // icon
                if (slot.Icon != null)
                {
                    var iconRect = new Rectangle(x + 4, y + 4, slotSize - 8, slotSize - 8);
                    sb.Draw(slot.Icon, iconRect, Color.White);
                }

                // count
                if (!string.IsNullOrEmpty(slot.ItemId) && slot.Count > 1)
                {
                    var text = slot.Count.ToString();
                    var measure = font.MeasureString(text);
                    var pos = new Vector2(x + slotSize - measure.X - 4, y + slotSize - measure.Y - 2);
                    sb.DrawString(font, text, pos, Color.White);
                }
            }
        }

        public int SelectedIndex => selectedIndex;
    }
}