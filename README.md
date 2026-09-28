# api-sumula

Back-end do **Súmula**, site de estatísticas do futebol brasileiro. Começa pelo Brasileirão Série A 2026.

Os dados vêm do plano gratuito do [football-data.org](https://www.football-data.org). As estatísticas
(turnos, confronto direto, casa/fora, forma, evolução na tabela) são calculadas aqui, a partir dos resultados.

## Como funciona

```
GitHub Actions (a cada 3h) ─► Sumula.Coletor ─► football-data.org
                                    │
                                    ▼
                              Postgres (Neon)
                                    ▲
                    Sumula.Api ─────┘  ◄── site (repositório sumula)
```

| Projeto | O que faz |
|---|---|
| `src/Sumula.Core` | Modelos e cálculos (classificação, turnos, confronto direto, evolução, resumo do time). Não depende de banco nem de API. |
| `src/Sumula.Data` | EF Core + Postgres: entidades, migrations e consultas. |
| `src/Sumula.Coletor` | Programa de linha de comando que busca os dados no football-data.org e grava no banco. Aplica as migrations ao iniciar. |
| `src/Sumula.Api` | API HTTP (ASP.NET Minimal API) consumida pelo site. |
| `tests/Sumula.Core.Tests` | Testes dos cálculos (xUnit). |

## Rodando na sua máquina

Pré-requisitos: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) e Docker (só para o banco).

```bash
# 1. Sobe o Postgres local
docker compose up -d

# 2. Guarda o token do football-data.org (fica fora do repositório)
dotnet user-secrets set FootballData:Token "SEU_TOKEN" --project src/Sumula.Coletor
dotnet user-secrets set ConnectionStrings:Sumula "Host=localhost;Database=sumula;Username=sumula;Password=sumula" --project src/Sumula.Coletor

# 3. Busca os dados (cria as tabelas na primeira vez)
dotnet run --project src/Sumula.Coletor

# 4. Sobe a API em http://localhost:5080
dotnet run --project src/Sumula.Api

# Testes
dotnet test
```

O arquivo `src/Sumula.Api/Sumula.Api.http` tem exemplos de chamadas prontos para o VS Code (extensão REST Client),
Visual Studio ou Rider.

## Endpoints

Todos começam com `/api/{competicao}/{temporada}`, por exemplo `/api/BSA/2026`. `BSA` é o código do Brasileirão
Série A no football-data.org.

| Rota | Descrição |
|---|---|
| `GET /classificacao?recorte=geral\|primeiroTurno\|segundoTurno&mando=todos\|casa\|fora` | Tabela com pontos, jogos, V/E/D, gols, saldo, aproveitamento e últimos 5 resultados. |
| `GET /times` | Times da competição. |
| `GET /times/{id}` | Resumo do time: geral, casa, fora, 1º e 2º turno, evolução por rodada, últimas e próximas partidas. |
| `GET /confronto?timeA={id}&timeB={id}` | Confronto direto na temporada. |
| `GET /partidas?rodada={n}&timeId={id}` | Partidas (filtros opcionais). |
| `GET /evolucao` | Posição e pontos de todos os times ao fim de cada rodada. |
| `GET /artilharia` | Artilheiros (gols, assistências e pênaltis). |
| `GET /health` | Verificação de saúde (fora do prefixo `/api`). |

As respostas ficam 5 minutos em cache, já que os dados só mudam quando o coletor roda.

### Critérios de desempate

Pontos, vitórias, saldo de gols, gols pró e confronto direto (só quando o empate é entre dois clubes), como no
regulamento do Brasileirão. Cartões vermelhos e amarelos não estão disponíveis no plano gratuito, então o último
critério é o nome do time.

## Publicação (tudo gratuito)

1. **Banco:** crie um projeto no [Neon](https://neon.tech) e copie a connection string
   (`postgresql://...`). O formato URL é aceito direto.
2. **Coletor:** em *Settings → Secrets and variables → Actions* deste repositório, crie os secrets
   `FOOTBALL_DATA_TOKEN` e `DATABASE_URL`. O workflow `Coletor` roda a cada 3 horas e pode ser disparado
   manualmente na aba *Actions*.
3. **API:** no [Render](https://render.com), crie um *Web Service* a partir deste repositório usando o
   `Dockerfile`, com as variáveis:
   - `DATABASE_URL`: a mesma connection string do Neon
   - `Cors__Origens__0`: o endereço do site (ex.: `https://sumula.pages.dev`)

   No plano gratuito o Render desliga a API após 15 minutos sem acesso. A primeira requisição depois disso
   pode levar ~30s.

> O GitHub pausa workflows agendados em repositórios sem commits há 60 dias. Se o coletor parar, reative-o na
> aba *Actions*.
