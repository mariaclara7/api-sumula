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
    PlacarFd Score,
    // Só vem com o cabeçalho X-Unfold-Goals, disponível a partir do plano "Free + Deep Data".
    List<GolFd>? Goals = null);

public sealed record PlacarFd(GolsFd FullTime, GolsFd? HalfTime = null);

public sealed record GolsFd(int? Home, int? Away);

/// <param name="Score">Placar logo após o gol; é por ele que sabemos que lado marcou.</param>
public sealed record GolFd(int? Minute, int? InjuryTime, string? Type, JogadorFd? Scorer, GolsFd? Score);

public sealed record ArtilheiroFd(
    JogadorFd Player,
    TimeFd Team,
    int? PlayedMatches,
    int? Goals,
    int? Assists,
    int? Penalties);

public sealed record JogadorFd(int Id, string Name);
