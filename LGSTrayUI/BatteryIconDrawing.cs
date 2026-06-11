using LGSTrayCore;
using LGSTrayUI.Properties;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using System.Runtime.InteropServices;
using LGSTrayPrimitives;
using Microsoft.Win32;
using System.Drawing.Text;

namespace LGSTrayUI
{
    public static partial class BatteryIconDrawing
    {
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DestroyIcon(IntPtr handle);

        private static Bitmap Mouse => CheckTheme.LightTheme ? Resources.Mouse : Resources.Mouse_dark;
        private static Bitmap Keyboard => CheckTheme.LightTheme ? Resources.Keyboard : Resources.Keyboard_dark;
        private static Bitmap Headset => CheckTheme.LightTheme ? Resources.Headset : Resources.Headset_dark;
        private static Bitmap Battery => CheckTheme.LightTheme ? Resources.Battery : Resources.Battery_dark;
        private static Bitmap Missing => CheckTheme.LightTheme ? Resources.Missing : Resources.Missing_dark;
        private static Bitmap Charging => CheckTheme.LightTheme ? Resources.Charging : Resources.Charging_dark;

        private static int ImageSize;

        static BatteryIconDrawing()
        {
            int dpi;

            try
            {
                var reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ThemeManager", false);
                var _dpi = (string?)reg?.GetValue("LastLoadedDPI");
                if (!int.TryParse(_dpi, out dpi))
                {
                    dpi = 96;
                }
            }
            catch { dpi = 96; }
            var scale = dpi / 96f;

            ImageSize = (int)(32 * scale);
        }

        private static Bitmap GetDeviceIcon(LogiDevice device) => device.DeviceType switch
        {
            DeviceType.Keyboard => Keyboard,
            DeviceType.Headset => Headset,
            _ => Mouse,
        };

        private static Color GetDeviceColor(LogiDevice device) => device.DeviceType switch
        {
            _ => CheckTheme.LightTheme ? Color.FromArgb(0x11, 0x11, 0x11) : Color.FromArgb(0xEE, 0xEE, 0xEE)

            //return device.DeviceType switch
            //{
            //    DeviceType.Keyboard => Color.FromArgb(0xA1, 0xE4, 0x4D),
            //    DeviceType.Headset => Color.FromArgb(0xFA, 0x79, 0x21),
            //    _ => Color.FromArgb(0xBB, 0x86, 0xFC),
            //};
        };

        private static Bitmap GetBatteryValue(LogiDevice device) => device.BatteryPercentage switch
        {
            { } when device.PowerSupplyStatus == PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING => Charging,
            < 0 => Missing,
            < 10 => Resources.Indicator_10,
            < 50 => Resources.Indicator_30,
            < 85 => Resources.Indicator_50,
            _ => Resources.Indicator_100
        };

        public static void DrawUnknown(TaskbarIcon taskbarIcon)
        {
            DrawIcon(taskbarIcon, new()
            {
                BatteryPercentage = -1,
            });
        }

        public static void DrawIcon(TaskbarIcon taskbarIcon, LogiDevice device)
        {
            var destRect = new Rectangle(0, 0, ImageSize, ImageSize);
            using var b = new Bitmap(ImageSize, ImageSize);
            using var g = Graphics.FromImage(b);
            g.CompositingMode = CompositingMode.SourceOver;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using var wrapMode = new ImageAttributes();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);

            Bitmap[] layers = [
                GetBatteryValue(device),
                Battery,
                GetDeviceIcon(device),
            ];

            foreach (var image in layers)
            {
                g.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                image.Dispose();
            }

            g.Save();

            IntPtr iconHandle = b.GetHicon();
            Icon tempManagedRes = Icon.FromHandle(iconHandle);
            taskbarIcon.Icon = (Icon)tempManagedRes.Clone();
            tempManagedRes.Dispose();
            DestroyIcon(iconHandle);
        }

        private static Color? TryParseColor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            try { return ColorTranslator.FromHtml(value); }
            catch { return null; }
        }

        public static void DrawNumeric(TaskbarIcon taskbarIcon, LogiDevice device, NumericDisplaySettings? settings = null)
        {
            using Bitmap b = new(ImageSize, ImageSize);
            using Graphics g = Graphics.FromImage(b);

            g.CompositingMode = CompositingMode.SourceOver;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            float multiplier = settings?.FontSizeMultiplier ?? 0.8f;
            Color textColor = TryParseColor(settings?.TextColor) ?? GetDeviceColor(device);
            Color? bgColor = TryParseColor(settings?.BackgroundColor);

            if (bgColor.HasValue)
                g.Clear(bgColor.Value);

            string displayString = (device.BatteryPercentage < 0) ? "?" : $"{device.BatteryPercentage:f0}";
            g.DrawString(
                displayString,
                new Font("Segoe UI", (int)(multiplier * ImageSize), GraphicsUnit.Pixel),
                new SolidBrush(textColor),
                ImageSize / 2, ImageSize / 2,
                new(StringFormatFlags.FitBlackBox, 0)
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center,
                }
            );

            IntPtr iconHandle = b.GetHicon();
            Icon tempManagedRes = Icon.FromHandle(iconHandle);
            taskbarIcon.Icon = (Icon)tempManagedRes.Clone();
            tempManagedRes.Dispose();
            DestroyIcon(iconHandle);
        }
    }
}
