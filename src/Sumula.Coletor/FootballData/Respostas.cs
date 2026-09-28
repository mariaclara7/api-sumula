namespace Sumula.Coletor.FootballData;

// Formatos da API v4 do football-data.org: https://docs.football-data.org/general/v4/index.html

public sealed record RespostaTimes(List<TimeFd> Teams);

public sealed record RespostaPartidas(List<PartidaFd> Matches);

public sealed record RespostaArtilharia(List<ArtilheiroFd> Scorers);

public sealed record TimeFd(int? Id, string? Name, string? ShortName, string? Tla, string? Crest);

public sealed record PartidaFd(
    int Id,
    DateTimeOffset UtcDate,
    string Status,
    int? Matchday,
    TimeFd HomeTeam,
    TimeFd AwayTeam,
    PlacarFd Score);

public sealed record PlacarFd(GolsFd FullTime);

public sealed record GolsFd(int? Home, int? Away);

public sealed record ArtilheiroFd(
    JogadorFd Player,
    TimeFd Team,
    int? PlayedMatches,
    int? Goals,
    int? Assists,
    int? Penalties);

public sealed record JogadorFd(int Id, string Name);
