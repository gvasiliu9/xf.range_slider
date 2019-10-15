using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Component.Helpers;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using TouchTracking;
using Utmdev.Xf.Components.Models;
using Utmdev.Xf.RangeSlider.Models;
using Xamarin.Forms;

namespace Utmdev.Xf.RangeSlider.Content
{
    public partial class RangeSlider : ContentView
    {
        #region Fields

        private ActiveThumb _activeThumb;

        private ActiveThumb _lastActiveThumb;

        private float _lastPercentage;

        private string _defaultColor = "#000000";

        private string _leftThumbValue;

        private string _rightThumbValue;

        // Canvas
        private CanvasInfo _thumbsCanvasInfo;
        private CanvasInfo _segmentsCanvasInfo;
        private CanvasInfo _valuesCanvasInfo;

        // Tumbs
        private ThumbDraw _leftThumbDraw;
        private ThumbDraw _rightThumbDraw;

        // Touch
        private Touch _touch;

        // Segments
        private SKPaint _valueSegmentPaint;
        private SKPaint _remainingValueSegmentPaint;

        #endregion

        #region Bindable Properties

        // Format
        public static readonly BindableProperty FormatProperty = BindableProperty
            .Create(nameof(Format),
                typeof(string),
                typeof(RangeSlider),
                "{0}");

        public string Format
        {
            get
            {
                return (string)GetValue(FormatProperty);
            }
            set
            {
                SetValue(FormatProperty, value);
            }
        }

        // Value type
        public static readonly BindableProperty ValueTypeProperty = BindableProperty
            .Create(nameof(ValueType),
                typeof(RangeSliderValueType),
                typeof(RangeSlider),
                default(RangeSliderValueType));

        public RangeSliderValueType ValueType
        {
            get
            {
                return (RangeSliderValueType)GetValue(ValueTypeProperty);
            }
            set
            {
                SetValue(ValueTypeProperty, value);
            }
        }

        // Is moving
        public static readonly BindableProperty IsMovingProperty = BindableProperty
            .Create(nameof(IsMoving),
                typeof(bool?),
                typeof(RangeSlider),
                default(bool?));

        public bool? IsMoving
        {
            get
            {
                return (bool?)GetValue(IsMovingProperty);
            }
            set
            {
                SetValue(IsMovingProperty, value);
            }
        }

        // From
        public static readonly BindableProperty FromProperty = BindableProperty
            .Create(nameof(From),
                typeof(object),
                typeof(RangeSlider),
                default(object));

        public object From
        {
            get
            {
                return (object)GetValue(FromProperty);
            }
            set
            {
                SetValue(FromProperty, value);
            }
        }

        // To
        public static readonly BindableProperty ToProperty = BindableProperty
            .Create(nameof(To),
                typeof(object),
                typeof(RangeSlider),
                default(object));

        public object To
        {
            get
            {
                return (object)GetValue(ToProperty);
            }
            set
            {
                SetValue(ToProperty, value);
            }
        }

        // Thumbs
        public static readonly BindableProperty ThumbsProperty = BindableProperty
            .Create(nameof(Thumbs),
                typeof(RangeSliderThumbs),
                typeof(RangeSlider),
                default(RangeSliderThumbs));

        public RangeSliderThumbs Thumbs
        {
            get
            {
                return (RangeSliderThumbs)GetValue(ThumbsProperty);
            }
            set
            {
                SetValue(ThumbsProperty, value);
            }
        }

        // Segments
        public static readonly BindableProperty SegmentsProperty = BindableProperty
            .Create(nameof(Segments),
                typeof(RangeSliderSegments),
                typeof(RangeSlider),
                default(RangeSliderSegments));

        public RangeSliderSegments Segments
        {
            get
            {
                return (RangeSliderSegments)GetValue(SegmentsProperty);
            }
            set
            {
                SetValue(SegmentsProperty, value);
            }
        }

        // Values
        public static readonly BindableProperty ValuesProperty = BindableProperty
            .Create(nameof(Values),
                typeof(List<object>),
                typeof(RangeSlider),
                default(List<object>));

        public List<object> Values
        {
            get
            {
                return (List<object>)GetValue(ValuesProperty);
            }
            set
            {
                SetValue(ValuesProperty, value);
            }
        }

        #endregion

        #region Commands

        public static readonly BindableProperty ReleasedCommandProperty = BindableProperty
            .Create(nameof(ReleasedCommand), typeof(ICommand), typeof(RangeSlider));

        public ICommand ReleasedCommand
        {
            get => (ICommand)GetValue(ReleasedCommandProperty);
            set => SetValue(ReleasedCommandProperty, value);
        }

        #endregion

        public RangeSlider()
        {
            InitializeComponent();

            InitializePaints();
        }

        #region Methods

        private void InitializePaints()
        {
            // Left thumb
            _leftThumbDraw.Paint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true
            };

            _leftThumbDraw.ValueTextDraw.Paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
            };

            _leftThumbDraw.IconTextDraw.Paint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Typeface = SkiaSharpHelper.LoadTtfFont(Fonts.Icons)
            };

            // Right thumb
            _rightThumbDraw.Paint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true
            };

            _rightThumbDraw.ValueTextDraw.Paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            };

            _rightThumbDraw.IconTextDraw.Paint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Typeface = SkiaSharpHelper.LoadTtfFont(Fonts.Icons)
            };

            // Thumbs shadow
            _leftThumbDraw.Paint.ImageFilter = _rightThumbDraw.Paint.ImageFilter = SKImageFilter.CreateDropShadow(
                   0,
                   0,
                   10,
                   10,
                   SKColor.Parse("#d8d8d8"),
                   SKDropShadowImageFilterShadowMode.DrawShadowAndForeground
                );

            // Value segment
            _valueSegmentPaint = new SKPaint
            {
                Color = SKColors.Transparent,
                IsAntialias = true,
            };

            // Remaining value segment
            _remainingValueSegmentPaint = new SKPaint
            {
                Color = SKColors.Transparent,
                IsAntialias = true,
            };
        }

        private bool HasValues()
            => (Values != null && Values.Any());

        private float GetXPercentage(RangeSliderThumbOptions thumbOptions)
        {
            // Check values
            if (!HasValues())
                return -1;

            // Calculate value index
            int index = Values.IndexOf(thumbOptions.Value);

            _lastPercentage = (float)index / (Values.Count - 1);

            return _lastPercentage;
        }

        private void Calculate(SKPaintSurfaceEventArgs e)
        {
            // Segments
            _valueSegmentPaint.StrokeWidth =
                _remainingValueSegmentPaint.StrokeWidth =
                e.Info.Width * 0.005393743257821f;

            // Text
            _leftThumbDraw.ValueTextDraw.Margin.Bottom = e.Info.Width * 0.001078748651564f;
            _leftThumbDraw.ValueTextDraw.Margin.Top = e.Info.Width * 0.005393743257821f;

            _rightThumbDraw.ValueTextDraw.Margin.Bottom = e.Info.Width * 0.001078748651564f;
            _rightThumbDraw.ValueTextDraw.Margin.Top = e.Info.Width * 0.005393743257821f;
        }

        private object GetThumbValue(SKPoint point)
        {
            // Calculate percentage
            float percentage = (point.X / _segmentsCanvasInfo.ImageInfo.Width);

            // Calculate value index
            int index = Math.Abs((int)(percentage * (Values.Count() - 1)));

            // Check value index
            if (index >= Values.Count() || index < 0)
                return Values[0];

            // Return value
            return Values[index];
        }

        private void CheckThumbBounds(ref ThumbDraw thumbDraw)
        {
            if ((thumbDraw.Point.X - thumbDraw.Radius) < _thumbsCanvasInfo.ImageInfo.Rect.Left)
                thumbDraw.Point.X = _thumbsCanvasInfo.ImageInfo.Rect.Left + thumbDraw.Radius;
            else if ((thumbDraw.Point.X + thumbDraw.Radius) > _thumbsCanvasInfo.ImageInfo.Rect.Right)
                thumbDraw.Point.X = _thumbsCanvasInfo.ImageInfo.Rect.Right - thumbDraw.Radius;
        }

        private void DrawSegments()
        {
            // Left thumb value segment
            float y = _thumbsCanvasInfo.ImageInfo.Rect.MidY;

            var leftThumbSegmentStart = new SKPoint
            {
                Y = y,
                X = _segmentsCanvasInfo.ImageInfo.Rect.Left
            };

            var leftThumbSegmentEnd = new SKPoint
            {
                Y = y,
                X = _leftThumbDraw.Point.X
            };

            DrawSegment(leftThumbSegmentStart, leftThumbSegmentEnd, _valueSegmentPaint);

            // Right thumb value segment
            var rightThumbSegmentStart = new SKPoint
            {
                Y = y,
                X = _rightThumbDraw.Point.X
            };

            var rightThumbSegmentEnd = new SKPoint
            {
                Y = y,
                X = _segmentsCanvasInfo.ImageInfo.Rect.Right
            };

            DrawSegment(rightThumbSegmentStart, rightThumbSegmentEnd, _valueSegmentPaint);

            // Remaining value segment
            var remaningValueSegmentStart = new SKPoint
            {
                Y = y,
                X = _leftThumbDraw.Point.X
            };

            var remaningValueSegmentEnd = new SKPoint
            {
                Y = y,
                X = _rightThumbDraw.Point.X
            };

            DrawSegment(remaningValueSegmentStart, remaningValueSegmentEnd, _remainingValueSegmentPaint);
        }

        private void DrawSegment(SKPoint start, SKPoint end, SKPaint paint)
            => _segmentsCanvasInfo.Canvas.DrawLine(start, end, paint);

        private void DrawThumb(ref ThumbDraw thumbDraw, RangeSliderThumbOptions thumbOptions)
        {
            // Colors
            thumbDraw.Paint.Color = SKColor.Parse((thumbOptions.BackgroundColor ?? _defaultColor));

            thumbDraw.ValueTextDraw.Paint.Color = SKColor.Parse((thumbOptions.TextColor ?? _defaultColor));

            float percentage = GetXPercentage(thumbOptions);

            if (percentage == -1f)
                return;

            // Calculate thumb bounds
            thumbDraw.Radius = _thumbsCanvasInfo.ImageInfo.Rect.MidY
                           - (_thumbsCanvasInfo.ImageInfo.Rect.MidY * 0.5f);

            thumbDraw.Point = new SKPoint
            {
                X = percentage * _thumbsCanvasInfo.ImageInfo.Width,
                Y = _thumbsCanvasInfo.ImageInfo.Rect.MidY
            };

            CheckThumbBounds(ref thumbDraw);

            _thumbsCanvasInfo.Canvas.DrawCircle(thumbDraw.Point, thumbDraw.Radius, thumbDraw.Paint);
        }

        private void DrawThumbIcon(ref ThumbDraw thumbDraw, RangeSliderThumbOptions thumbOptions)
        {
            // Set size
            thumbDraw.IconTextDraw.Paint.TextSize = thumbDraw.Radius * thumbOptions.IconScale;

            // Measure
            thumbDraw.IconTextDraw.Paint.MeasureText(thumbOptions.Icon,
                ref thumbDraw.IconTextDraw.Bounds);

            // Update paint
            thumbDraw.IconTextDraw.Paint.Color = SKColor.Parse(thumbOptions.IconColor ?? _defaultColor);

            // Draw
            var point = new SKPoint
            {
                X = thumbDraw.Point.X - Math.Abs(thumbDraw.IconTextDraw.Bounds.MidX),
                Y = thumbDraw.Point.Y + Math.Abs(thumbDraw.IconTextDraw.Bounds.MidY)
            };

            _thumbsCanvasInfo.Canvas.DrawText(thumbOptions.Icon,
                point,
                thumbDraw.IconTextDraw.Paint);
        }

        private float GetThumbValueXPosition(ThumbDraw thumbDraw, TextDraw textDraw)
        {
            if (thumbDraw.Point.X - textDraw.Bounds.MidX < _valuesCanvasInfo.ImageInfo.Rect.Left)
                return _valuesCanvasInfo.ImageInfo.Rect.Left;
            else if (thumbDraw.Point.X + textDraw.Bounds.MidX > _valuesCanvasInfo.ImageInfo.Rect.Right)
                return _valuesCanvasInfo.ImageInfo.Rect.Right - textDraw.Bounds.Width;
            else
                return thumbDraw.Point.X - textDraw.Bounds.MidX;
        }

        private void DrawValues()
        {
            // Check values
            if (Thumbs == null || Thumbs.Left?.Value == null || Thumbs.Right?.Value == null)
                return;

            // Get left thumb value
            _leftThumbValue = ConvertValue(Thumbs.Left.Value);

            _leftThumbDraw.ValueTextDraw.Paint.TextSize = _leftThumbDraw.Radius * 0.6f;

            _leftThumbDraw.ValueTextDraw.Paint.MeasureText(_leftThumbValue, ref _leftThumbDraw.ValueTextDraw.Bounds);

            // Get right thumb value
            _rightThumbValue = ConvertValue(Thumbs.Right.Value);

            _rightThumbDraw.ValueTextDraw.Paint.TextSize = _rightThumbDraw.Radius * 0.6f;

            _rightThumbDraw.ValueTextDraw.Paint.MeasureText(_rightThumbValue, ref _rightThumbDraw.ValueTextDraw.Bounds);

            // Right thumb value
            _valuesCanvasInfo.Canvas.DrawText(_rightThumbValue,
                new SKPoint
                {
                    X = GetThumbValueXPosition(_rightThumbDraw, _rightThumbDraw.ValueTextDraw),
                    Y = _rightThumbDraw.Point.Y + _rightThumbDraw.Radius
                        + _rightThumbDraw.ValueTextDraw.Paint.TextSize
                        + _rightThumbDraw.ValueTextDraw.Margin.Top,
                },
                _rightThumbDraw.ValueTextDraw.Paint);

            // Left thumb value
            _valuesCanvasInfo.Canvas.DrawText(_leftThumbValue,
                new SKPoint
                {
                    X = GetThumbValueXPosition(_leftThumbDraw, _leftThumbDraw.ValueTextDraw),
                    Y = _leftThumbDraw.Point.Y + _leftThumbDraw.Radius
                        + _leftThumbDraw.ValueTextDraw.Paint.TextSize
                        + _leftThumbDraw.ValueTextDraw.Margin.Top,
                },
                _leftThumbDraw.ValueTextDraw.Paint);
        }

        private void ExecuteReleasedChangedCommand(RangeSliderValue value)
            => XamarinHelper.ExecuteCommand(ReleasedCommand, value);

        private string ConvertValue(object value)
        {
            switch (ValueType)
            {
                case RangeSliderValueType.DateTime:
                    return ((DateTime)value).ToString(Format);
                case RangeSliderValueType.Numeric:
                    return string.Format(Format, (double)value);
            }

            return "_";
        }

        #endregion

        #region Events

        private void OnTouch
            (object sender, TouchTracking.TouchActionEventArgs args)
        {
            // Convert touch point to pixel point
            _touch.Current = SkiaSharpHelper.ToPixel(args.Location.X, args.Location.Y, ref thumbsCanvas);

            // Check bounds
            if (_touch.Current.X < _thumbsCanvasInfo.ImageInfo.Rect.Left
                || _touch.Current.X > _thumbsCanvasInfo.ImageInfo.Rect.Right)
                return;

            // Check touch type
            switch (args.Type)
            {
                case TouchActionType.Pressed:

                    IsMoving = true;

                    _touch.Matrix = SKMatrix.MakeIdentity();

                    // Adjust thumbs bounds
                    _leftThumbDraw.Bounds = new SKRect
                        (_leftThumbDraw.Point.X - _leftThumbDraw.Radius, _leftThumbDraw.Point.Y - _leftThumbDraw.Radius
                        , _leftThumbDraw.Point.X + _leftThumbDraw.Radius, _leftThumbDraw.Point.Y + _leftThumbDraw.Radius);

                    _rightThumbDraw.Bounds = new SKRect
                        (_rightThumbDraw.Point.X - _rightThumbDraw.Radius, _rightThumbDraw.Point.Y - _rightThumbDraw.Radius
                        , _rightThumbDraw.Point.X + _rightThumbDraw.Radius, _rightThumbDraw.Point.Y + _rightThumbDraw.Radius);

                    // Check if the inner circle was touched
                    if (_leftThumbDraw.Bounds.Contains(_touch.Current)
                        && _rightThumbDraw.Bounds.Contains(_touch.Current))
                    {
                        _touch.Id = args.Id;
                        _touch.Previous = _touch.Current;

                        _activeThumb = _lastActiveThumb;
                    }
                    else if (_leftThumbDraw.Bounds.Contains(_touch.Current))
                    {
                        _touch.Id = args.Id;
                        _touch.Previous = _touch.Current;

                        _activeThumb = ActiveThumb.Left;
                    }
                    else if (_rightThumbDraw.Bounds.Contains(_touch.Current))
                    {
                        _touch.Id = args.Id;
                        _touch.Previous = _touch.Current;

                        _activeThumb = ActiveThumb.Right;
                    }

                    break;

                case TouchActionType.Moved:
                    if (_touch.Id == args.Id)
                    {
                        // Adjust the matrix for the new position
                        _touch.Matrix.TransX += _touch.Current.X - _touch.Previous.X;
                        _touch.Matrix.TransY += _touch.Current.Y - _touch.Previous.Y;
                        _touch.Previous = _touch.Current;

                        var value = GetThumbValue(_touch.Current);

                        // Check active thumb
                        if (_activeThumb == ActiveThumb.Left)
                        {
                            Thumbs.Left.Value = value;

                            From = value;
                        }
                        else if (_activeThumb == ActiveThumb.Right)
                        {
                            Thumbs.Right.Value = value;

                            To = value;
                        }

                        // Update value
                        thumbsCanvas.InvalidateSurface();
                    }

                    break;

                case TouchActionType.Released:
                case TouchActionType.Cancelled:
                    {
                        _touch.Id = -1;

                        _lastActiveThumb = _activeThumb;

                        IsMoving = false;

                        ExecuteReleasedChangedCommand(new RangeSliderValue
                        {
                            From = From,
                            To = To
                        });
                    }
                    break;
            }
        }

        private void SegmentsCanvas_OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            // Get info
            _segmentsCanvasInfo.Canvas = e.Surface.Canvas;
            _segmentsCanvasInfo.Surface = e.Surface;
            _segmentsCanvasInfo.ImageInfo = e.Info;

            // Clear canvas
            e.Surface.Canvas.Clear(SKColors.Transparent);

            DrawSegments();
        }

        private void ValuesCanvas_OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            // Get info
            _valuesCanvasInfo.Canvas = e.Surface.Canvas;
            _valuesCanvasInfo.Surface = e.Surface;
            _valuesCanvasInfo.ImageInfo = e.Info;

            // Clear canvas
            e.Surface.Canvas.Clear(SKColors.Transparent);

            DrawValues();
        }

        private void ThumbsCanvas_OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            // Get info
            _thumbsCanvasInfo.Canvas = e.Surface.Canvas;
            _thumbsCanvasInfo.Surface = e.Surface;
            _thumbsCanvasInfo.ImageInfo = e.Info;

            // Clear canvas
            e.Surface.Canvas.Clear(SKColors.Transparent);

            Calculate(e);

            if (Thumbs == null)
                return;

            // Draw thumbs
            if (_activeThumb == ActiveThumb.Left)
            {
                // Rights
                DrawThumb(ref _rightThumbDraw, Thumbs.Right);
                DrawThumbIcon(ref _rightThumbDraw, Thumbs.Right);

                // Left
                DrawThumb(ref _leftThumbDraw, Thumbs.Left);
                DrawThumbIcon(ref _leftThumbDraw, Thumbs.Left);
            }
            else if (_activeThumb == ActiveThumb.Right)
            {
                // Left
                DrawThumb(ref _leftThumbDraw, Thumbs.Left);
                DrawThumbIcon(ref _leftThumbDraw, Thumbs.Left);

                // Right
                DrawThumb(ref _rightThumbDraw, Thumbs.Right);
                DrawThumbIcon(ref _rightThumbDraw, Thumbs.Right);
            }

            // Draw segments
            segmentsCanvas.InvalidateSurface();

            // Draw 
            valuesCanvas.InvalidateSurface();
        }

        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            // Values
            if (propertyName == ValuesProperty.PropertyName)
            {
                if (Thumbs == null)
                    return;

                thumbsCanvas.InvalidateSurface();
            }

            // Thumbs
            if (propertyName == ThumbsProperty.PropertyName)
            {
                if (Thumbs == null)
                    return;

                From = Thumbs.Left.Value;
                To = Thumbs.Right.Value;

                thumbsCanvas.InvalidateSurface();
            }

            // Segments
            if (propertyName == SegmentsProperty.PropertyName)
            {
                _valueSegmentPaint.Color = SKColor.Parse(Segments.ValueSegmentOptions.BackgorundColor);

                _remainingValueSegmentPaint.Color = SKColor.Parse(Segments.RemainingValueSegmentOptions.BackgorundColor);

                segmentsCanvas.InvalidateSurface();
            }
        }

        #endregion

        #region Types

        enum ActiveThumb
        {
            Left,
            Right
        }

        struct ThumbDraw
        {
            public SKRect Bounds;

            public SKPaint Paint;

            public TextDraw ValueTextDraw;

            public TextDraw IconTextDraw;

            public SKPoint Point;

            public float Radius;
        }

        struct TextDraw
        {
            public SKPaint Paint;

            public Margin Margin;

            public SKRect Bounds;
        }

        struct Margin
        {
            public float Left;

            public float Top;

            public float Right;

            public float Bottom;
        }

        struct Touch
        {
            public long Id;

            public SKPoint Current;

            public SKPoint Previous;

            public SKMatrix Matrix;
        }

        #endregion
    }
}
