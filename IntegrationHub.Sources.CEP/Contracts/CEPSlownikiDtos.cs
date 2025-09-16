namespace IntegrationHub.Sources.CEP.Contracts;

// nagłówek słownika
public sealed class SlownikNaglowekDto
{
    public string? Id { get; init; }
    public string? NazwaSlownika { get; init; }
    public string? Opis { get; init; }
    public DateTime? DataAktualizacji { get; init; }
    public DateTime? DataOd { get; init; }
    public DateTime? DataDo { get; init; }
    public WartoscSlownikowaDto? RodzajSlownika { get; init; }
}

// element zagnieżdżony (rodzaj słownika)
public sealed class WartoscSlownikowaDto
{
    public string? Kod { get; init; }
    public string? WartoscOpisowa { get; init; }
    public string? IdentyfikatorSlownika { get; init; }
}
