namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Tag color value object
/// </summary>
public class TagColor
{
    public byte R { get; private set; }
    public byte G { get; private set; }
    public byte B { get; private set; }
    public string Hex { get; private set; }
    public string Name { get; private set; }

    public TagColor(byte r, byte g, byte b, string name = "")
    {
        R = r;
        G = g;
        B = b;
        Hex = $"#{r:X2}{g:X2}{b:X2}";
        Name = string.IsNullOrWhiteSpace(name) ? $"RGB({r},{g},{b})" : name;
    }

    public TagColor(string hex, string name)
    {
        if (string.IsNullOrWhiteSpace(hex))
            throw new ArgumentException("Hex color cannot be null or empty", nameof(hex));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Color name cannot be null or empty", nameof(name));

        // Validate hex color format
        if (!IsValidHexColor(hex))
            throw new ArgumentException("Invalid hex color format", nameof(hex));

        Hex = hex.ToUpperInvariant();
        Name = name;
        
        // Convert hex to RGB
        var rgb = HexToRgb(hex);
        R = rgb.R;
        G = rgb.G;
        B = rgb.B;
    }

    public static TagColor Default => new("#3B82F6", "Blue");
    public static TagColor Red => new("#EF4444", "Red");
    public static TagColor Green => new("#10B981", "Green");
    public static TagColor Yellow => new("#F59E0B", "Yellow");
    public static TagColor Purple => new("#8B5CF6", "Purple");
    public static TagColor Pink => new("#EC4899", "Pink");
    public static TagColor Orange => new("#F97316", "Orange");
    public static TagColor Gray => new("#6B7280", "Gray");

    public static TagColor[] AllColors => new[]
    {
        Default, Red, Green, Yellow, Purple, Pink, Orange, Gray
    };

    private static bool IsValidHexColor(string hex)
    {
        if (hex.Length != 7 || !hex.StartsWith("#"))
            return false;

        return hex.Substring(1).All(c => "0123456789ABCDEFabcdef".Contains(c));
    }

    private static (byte R, byte G, byte B) HexToRgb(string hex)
    {
        var cleanHex = hex.TrimStart('#');
        return (
            R: Convert.ToByte(cleanHex.Substring(0, 2), 16),
            G: Convert.ToByte(cleanHex.Substring(2, 2), 16),
            B: Convert.ToByte(cleanHex.Substring(4, 2), 16)
        );
    }

    public override bool Equals(object? obj)
    {
        if (obj is TagColor other)
        {
            return Hex == other.Hex && Name == other.Name;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Hex, Name);
    }

    public override string ToString()
    {
        return $"{Name} ({Hex})";
    }
}
