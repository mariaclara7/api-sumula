using Sumula.Core.Modelos;

namespace Sumula.Data.Entidades;

// Os IDs vêm do football-data.org. Se um dia a fonte mudar, os IDs precisarão ser mapeados.

public class TimeEntidade
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public required string NomeCurto { get; set; }
    public required string Sigla { get; set; }
    public string? Escudo { get; set; }

    public Time ParaModelo() => new(Id, Nome, NomeCurto, Sigla, Escudo);
}

/// <summary>Quais times disputam uma competição em uma temporada.</summary>
public class ParticipacaoEntidade
{
    public required string Competicao { get; set; }
    public int Temporada { get; set; }
    public int TimeId { get; set; }
    public TimeEntidade Time { get; set; } = null!;
}

public class PartidaEntidade
{
    public int Id { get; set; }
    public required string Competicao { get; set; }
    public int Temporada { get; set; }
    public int Rodada { get; set; }
    public DateTimeOffset Data { get; set; }
    public StatusPartida Status { get; set; }
    public int MandanteId { get; set; }
    public int VisitanteId { get; set; }
    public int? GolsMandante { get; set; }
    public int? GolsVisitante { get; set; }
    public int? GolsMandanteIntervalo { get; set; }
    public int? GolsVisitanteIntervalo { get; set; }

    public Partida ParaModelo() =>
        new(Id, Rodada, Data, Status, MandanteId, VisitanteId, GolsMandante, GolsVisitante,
            GolsMandanteIntervalo, GolsVisitanteIntervalo);
}

public class GolEntidade
{
    public int Id { get; set; }
    public int PartidaId { get; set; }
    public int Minuto { get; set; }
    public int? Acrescimo { get; set; }
    public int TimeId { get; set; }
    public TipoGol Tipo { get; set; }
    public string? Autor { get; set; }

    public Gol ParaModelo() => new(PartidaId, Minuto, Acrescimo, TimeId, Tipo);
}

public class ArtilheiroEntidade
{
    public required string Competicao { get; set; }
    public int Temporada { get; set; }
    public int JogadorId { get; set; }
    public required string Nome { get; set; }
    public int TimeId { get; set; }
    public int? Jogos { get; set; }
    public int Gols { get; set; }
    public int? Assistencias { get; set; }
    public int? Penaltis { get; set; }

    public Artilheiro ParaModelo() => new(JogadorId, Nome, TimeId, Jogos, Gols, Assistencias, Penaltis);
}

public class ColetaEntidade
{
    public int Id { get; set; }
    public required string Competicao { get; set; }
    public int Temporada { get; set; }
    public DateTimeOffset ExecutadaEm { get; set; }
    public int PartidasAtualizadas { get; set; }
}
