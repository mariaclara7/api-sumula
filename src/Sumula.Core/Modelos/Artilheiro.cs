namespace Sumula.Core.Modelos;

public sealed record Artilheiro(
    int JogadorId,
    string Nome,
    int TimeId,
    int? Jogos,
    int Gols,
    int? Assistencias,
    int? Penaltis);
