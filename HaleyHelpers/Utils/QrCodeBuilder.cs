using System;
using QRCoder;

namespace Haley.Utils
{
    /// <summary>
    /// Creates cross-platform QR code images without exposing renderer-specific types.
    /// </summary>
    public static class QrCodeBuilder
    {
        private const int DefaultSvgPixelsPerModule = 8;
        private const int DefaultPngPixelsPerModule = 12;
        private const int MaximumPixelsPerModule = 100;

        /// <summary>
        /// Creates a scalable SVG QR code for the supplied text or URL.
        /// </summary>
        public static string CreateSvg(string content, int pixelsPerModule = DefaultSvgPixelsPerModule)
        {
            Validate(content, pixelsPerModule);

            using (var data = QRCodeGenerator.GenerateQrCode(content, QRCodeGenerator.ECCLevel.Q))
            using (var renderer = new SvgQRCode(data))
            {
                return renderer.GetGraphic(pixelsPerModule);
            }
        }

        /// <summary>
        /// Creates a PNG QR code for the supplied text or URL.
        /// </summary>
        public static byte[] CreatePng(string content, int pixelsPerModule = DefaultPngPixelsPerModule)
        {
            Validate(content, pixelsPerModule);
            return PngByteQRCodeHelper.GetQRCode(
                content,
                QRCodeGenerator.ECCLevel.Q,
                pixelsPerModule,
                drawQuietZones: true);
        }

        private static void Validate(string content, int pixelsPerModule)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("QR code content cannot be empty.", nameof(content));
            if (pixelsPerModule < 1 || pixelsPerModule > MaximumPixelsPerModule)
                throw new ArgumentOutOfRangeException(
                    nameof(pixelsPerModule),
                    pixelsPerModule,
                    $"Pixels per module must be between 1 and {MaximumPixelsPerModule}.");
        }
    }
}
