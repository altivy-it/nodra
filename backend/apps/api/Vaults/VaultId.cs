namespace Nodra.Notes.Api.Vaults;

public readonly record struct VaultId
{
    private VaultId(string value) => Value = value;

    public string Value { get; }

    public static bool TryParse(string? value, out VaultId vaultId)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
        {
            vaultId = default;
            return false;
        }

        foreach (var character in value)
        {
            if ((character is < 'a' or > 'z') &&
                (character is < '0' or > '9') &&
                character != '-')
            {
                vaultId = default;
                return false;
            }
        }

        vaultId = new VaultId(value);
        return true;
    }

    public override string ToString() => Value;
}
