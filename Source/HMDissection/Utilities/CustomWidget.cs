using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HMDissection
{
    [StaticConstructorOnStartup]
    public static class CustomWidget
    {

        public static float HorizontalSlider(Rect rect, float value, float min, float max, bool drawLabel = true, string label = null, string leftAlignedLabel = null, string rightAlignedLabel = null, float roundTo = -1f)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            float newValue = value;
            TextAnchor originalAnchor = Text.Anchor;
            try
            {
                switch (Event.current.GetTypeForControl(controlID))
                {
                    case EventType.MouseDown:
                        if (rect.Contains(Event.current.mousePosition) && Event.current.button == 0)
                        {
                            GUIUtility.hotControl = controlID;
                            Event.current.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID)
                        {
                            float previousValue = newValue;
                            newValue = Mathf.Clamp((Event.current.mousePosition.x - rect.x) / rect.width * (max - min) + min, min, max);
                            if (roundTo > 0f)
                            {
                                newValue = Mathf.Round(newValue / roundTo) * roundTo;
                            }
                            if (newValue != previousValue)
                            {
                                CustomWidget.CheckPlayDragSliderSound();
                            }

                            Event.current.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlID && Event.current.button == 0)
                        {
                            GUIUtility.hotControl = 0;
                            Event.current.Use();
                        }
                        break;

                    case EventType.Repaint:
                        // Adjust the sliderRect to move it slightly to the left
                        float leftShift = 10f; // Adjust this value to shift the slider to the left
                        Rect sliderRect = new Rect(rect.x - leftShift, rect.y + (drawLabel ? 20f : 0f), rect.width + leftShift, 8f); // Adjust position based on label presence

                        Widgets.DrawAtlas(sliderRect, CustomWidget.SliderRailAtlas);

                        float handleX = Mathf.Clamp(rect.x + (newValue - min) / (max - min) * rect.width - 6f, rect.x - leftShift, rect.x + rect.width - 12f);
                        GUI.DrawTexture(new Rect(handleX, sliderRect.y - 2.5f, 12f, 12f), CustomWidget.SliderHandle);

                        if (drawLabel && !string.IsNullOrEmpty(label))
                        {
                            Text.Anchor = TextAnchor.MiddleCenter;
                            Vector2 labelSize = Text.CalcSize(label);
                            Rect labelRect = new Rect(
                                rect.x + (rect.width / 2f) - (labelSize.x / 2f),
                                rect.y,
                                labelSize.x,
                                labelSize.y
                            );
                            Widgets.Label(labelRect, label);
                        }
                        if (!string.IsNullOrEmpty(leftAlignedLabel))
                        {
                            Text.Anchor = TextAnchor.MiddleLeft;
                            Widgets.Label(new Rect(rect.x, rect.y, rect.width / 2, 20f), leftAlignedLabel);
                        }

                        if (!string.IsNullOrEmpty(rightAlignedLabel))
                        {
                            Text.Anchor = TextAnchor.MiddleRight;
                            Widgets.Label(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, 20f), rightAlignedLabel);
                        }
                        break;
                }
                return newValue;
            }
            finally
            {
                Text.Anchor = originalAnchor;
            }
        }
        private static void CheckPlayDragSliderSound()
        {
            if (Time.realtimeSinceStartup > CustomWidget.lastDragSliderSoundTime + 0.075f)
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
                CustomWidget.lastDragSliderSoundTime = Time.realtimeSinceStartup;
            }
        }
        public static void CheckboxLabeled(Rect rect, string label, ref bool checkOn, bool disabled = false, Texture2D texChecked = null, Texture2D texUnchecked = null, bool placeCheckboxNearText = false, bool paintable = false)
        {
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (placeCheckboxNearText)
            {
                rect.width = Mathf.Min(rect.width, Text.CalcSize(label).x + 24f + 10f);
            }
            Rect rect2 = rect;
            rect2.xMax -= 24f;
            Widgets.Label(rect2, label);
            if (!disabled)
            {
                Widgets.ToggleInvisibleDraggable(rect, ref checkOn, true, paintable);
            }
            Widgets.CheckboxDraw(rect.x + rect.width - 48f, rect.y + (rect.height - 24f) / 2f, checkOn, disabled, 24f, texChecked, texUnchecked);
            Text.Anchor = anchor;
        }
        private static float lastDragSliderSoundTime = -1f;
        private static readonly Texture2D SliderHandle = ContentFinder<Texture2D>.Get("UI/Buttons/SliderHandle", true);
        private static readonly Texture2D SliderRailAtlas = ContentFinder<Texture2D>.Get("UI/Buttons/SliderRail", true);
        private static readonly Color RangeControlTextColor = new Color(0.6f, 0.6f, 0.6f);

    }
}
