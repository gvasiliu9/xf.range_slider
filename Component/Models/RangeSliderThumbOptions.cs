using System;
namespace Utmdev.Xf.RangeSlider.Models
{
    public class RangeSliderThumbOptions
    {
        public string BackgroundColor { get; set; }

        public string IconColor { get; set; }

        public string TextColor { get; set; }

        public string Icon { get; set; }

        public float IconScale { get; set; } = 0.75f;

        public object Value { get; set; }
    }
}
