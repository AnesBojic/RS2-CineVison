namespace CineVision.Services;

/// <summary>
/// Validates base64 image payloads by content (magic bytes), not by client-supplied MIME or extension.
/// </summary>
public static class ImageContentValidator
{
    public static readonly string[] AllowedContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    public static bool TryValidateBase64(
        string? base64,
        out string detectedContentType,
        out string error)
    {
        detectedContentType = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(base64))
        {
            error = "Image content is required.";
            return false;
        }

        var payload = base64.Trim();
        var comma = payload.IndexOf(',');
        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
        {
            payload = payload[(comma + 1)..];
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            error = "Image content must be valid base64.";
            return false;
        }

        if (bytes.Length < 12)
        {
            error = "Image content is too small to be a valid image.";
            return false;
        }

        if (bytes.Length > 5 * 1024 * 1024)
        {
            error = "Image must be 5 MB or smaller.";
            return false;
        }

        if (IsJpeg(bytes))
        {
            detectedContentType = "image/jpeg";
            return true;
        }

        if (IsPng(bytes))
        {
            detectedContentType = "image/png";
            return true;
        }

        if (IsGif(bytes))
        {
            detectedContentType = "image/gif";
            return true;
        }

        if (IsWebp(bytes))
        {
            detectedContentType = "image/webp";
            return true;
        }

        error = "Image must be JPEG, PNG, GIF, or WebP (validated by file content, not extension).";
        return false;
    }

    public static bool ContentTypeMatches(string? claimedContentType, string detectedContentType)
    {
        if (string.IsNullOrWhiteSpace(claimedContentType))
        {
            return false;
        }

        var claimed = claimedContentType.Trim().ToLowerInvariant();
        if (claimed is "image/jpg")
        {
            claimed = "image/jpeg";
        }

        return claimed == detectedContentType;
    }

    private static bool IsJpeg(byte[] b) =>
        b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;

    private static bool IsPng(byte[] b) =>
        b.Length >= 8
        && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
        && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A;

    private static bool IsGif(byte[] b) =>
        b.Length >= 6
        && b[0] == (byte)'G' && b[1] == (byte)'I' && b[2] == (byte)'F'
        && b[3] == (byte)'8' && (b[4] == (byte)'7' || b[4] == (byte)'9') && b[5] == (byte)'a';

    private static bool IsWebp(byte[] b) =>
        b.Length >= 12
        && b[0] == (byte)'R' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'F'
        && b[8] == (byte)'W' && b[9] == (byte)'E' && b[10] == (byte)'B' && b[11] == (byte)'P';
}
