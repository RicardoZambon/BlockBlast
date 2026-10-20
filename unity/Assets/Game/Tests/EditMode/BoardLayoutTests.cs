#nullable enable
using NUnit.Framework;
using UnityEngine;

namespace BlockPuzzle.Game.Tests
{
    public class BoardLayoutTests
    {
        const float Tolerance = 1e-4f;

        static Rect Area(float width, float height) => new Rect(-width / 2, -height / 2, width, height);

        static void AssertInside(Rect area, Vector2 point)
        {
            bool inside = point.x >= area.xMin - Tolerance && point.x <= area.xMax + Tolerance &&
                          point.y >= area.yMin - Tolerance && point.y <= area.yMax + Tolerance;
            Assert.IsTrue(inside, $"{point} is outside {area}");
        }

        [TestCase(9f, 16f)]
        [TestCase(9f, 20f)]
        [TestCase(12f, 16f)]
        public void The_board_and_tray_fit_inside_the_safe_area(float width, float height)
        {
            var area = Area(width, height);
            var layout = new BoardLayout(area);
            float cell = layout.CellSize;

            Vector2 topLeft = layout.BoardTopLeft;
            AssertInside(area, topLeft);
            AssertInside(area, topLeft + new Vector2(8 * cell, 0));
            AssertInside(area, topLeft + new Vector2(0, -8 * cell));
            AssertInside(area, topLeft + new Vector2(8 * cell, -8 * cell));

            var half = new Vector2(8f / 3f * cell / 2, 1.5f * cell);   // a slot is 8/3 cells wide and 3 cells tall
            for (int slot = 0; slot < 3; slot++)
            {
                Vector2 centre = layout.TraySlotCenter(slot);
                AssertInside(area, centre - half);
                AssertInside(area, centre + half);
                AssertInside(area, centre + new Vector2(half.x, -half.y));
                AssertInside(area, centre + new Vector2(-half.x, half.y));
                Assert.LessOrEqual(centre.y + half.y, topLeft.y - 8 * cell + Tolerance, "the tray must be below the board");
            }
        }

        [TestCase(9f, 16f)]
        [TestCase(9f, 20f)]
        [TestCase(12f, 16f)]
        public void Cell_size_is_the_smaller_of_width_and_height_limits(float width, float height)
        {
            Assert.AreEqual(Mathf.Min(width * 0.92f / 8, height / 13.25f), new BoardLayout(Area(width, height)).CellSize, 1e-5f);
        }

        [TestCase(9f, 16f)]
        [TestCase(9f, 20f)]
        [TestCase(12f, 16f)]
        public void Cell_centres_map_back_to_their_cells(float width, float height)
        {
            var layout = new BoardLayout(Area(width, height));

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    layout.ToGrid(layout.CellCenter(r, c), out float row, out float col);
                    Assert.AreEqual(r + 0.5f, row, Tolerance);
                    Assert.AreEqual(c + 0.5f, col, Tolerance);
                }
            }
        }

        [TestCase(9f, 16f)]
        [TestCase(9f, 20f)]
        [TestCase(12f, 16f)]
        public void Each_tray_slot_is_found_at_its_centre_and_the_board_is_not_a_slot(float width, float height)
        {
            var layout = new BoardLayout(Area(width, height));

            for (int slot = 0; slot < 3; slot++) Assert.AreEqual(slot, layout.TraySlotAt(layout.TraySlotCenter(slot)));
            Assert.AreEqual(-1, layout.TraySlotAt(layout.CellCenter(4, 4)));
        }

        [TestCase(9f, 16f)]
        [TestCase(9f, 20f)]
        [TestCase(12f, 16f)]
        public void A_dragged_piece_floats_one_and_a_half_cells_above_the_pointer(float width, float height)
        {
            var layout = new BoardLayout(Area(width, height));
            float cell = layout.CellSize;
            var pointer = new Vector2(0.3f, -2f);

            Vector2 topLeft = layout.DraggedTopLeft(pointer, 3, 2);

            // Centred horizontally on the pointer, bottom edge 1.5 cells above it, so the top is 1.5 + 2 cells above.
            Assert.AreEqual(pointer.x - 1.5f * cell, topLeft.x, Tolerance);
            Assert.AreEqual(pointer.y + 3.5f * cell, topLeft.y, Tolerance);
        }

        [TestCase(9f, 16f)]
        [TestCase(9f, 20f)]
        [TestCase(12f, 16f)]
        public void A_tray_piece_is_centred_in_its_slot_at_tray_scale(float width, float height)
        {
            var layout = new BoardLayout(Area(width, height));
            float scaledCell = layout.CellSize * BoardLayout.TrayScale;

            Vector2 topLeft = layout.TrayPieceTopLeft(1, 3, 2);

            Vector2 centre = layout.TraySlotCenter(1);
            Assert.AreEqual(centre.x - 1.5f * scaledCell, topLeft.x, Tolerance);
            Assert.AreEqual(centre.y + 1f * scaledCell, topLeft.y, Tolerance);
        }
    }
}
