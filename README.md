# api-sumula

Back-end do **Súmula**, site de estatísticas do futebol brasileiro. Começa pelo Brasileirão Série A 2026.

Os dados vêm do plano gratuito do [football-data.org](https://www.football-data.org). As estatísticas
(turnos, confronto direto, casa/fora, forma, evolução na tabela) são calculadas aqui, a partir dos resultados.

## Como funciona

```
GitHub Actions (a cada 3h)
   │
   ├─► Sumula.Coletor ──► football-data.org
   │        │
   │        ▼
   │   Postgres (Neon)
   │        │
   ├─► Sumula.Exportador: sobe a Sumula.Api em memória, chama cada endereço
   │   que o site usa e grava as respostas como arquivos .json
   │        │
   └─► wrangler deploy ──► Cloudflare Worker "sumula-dados" (só arquivos estáticos)
                                   ▲
                    site (repositório sumula) lê os .json
```

Como os dados só mudam quando o coletor roda, a API é "pré-calculada": em produção não há servidor .NET
ligado, só arquivos na Cloudflare. A `Sumula.Api` continua sendo a fonte da verdade (o exportador chama ela
mesma) e é usada ao vivo no desenvolvimento.

| Projeto | O que faz |
|---|---|
| `src/Sumula.Core` | Modelos e cálculos (classificação, turnos, confronto direto, evolução, simulação, palpites...). Não depende de banco nem de API. |
| `src/Sumula.Data` | EF Core + Postgres: entidades, migrations e consultas. |
| `src/Sumula.Coletor` | Programa de linha de comando que busca os dados no football-data.org e grava no banco. Aplica as migrations ao iniciar. |
| `src/Sumula.Api` | API HTTP (ASP.NET Minimal API). |
| `src/Sumula.Exportador` | Gera os arquivos estáticos da API (`RotasEstaticas` lista os endereços e dá nome a cada arquivo). |
| `publicacao/` | Configuração do Worker `sumula-dados` na Cloudflare. |
| `tests/Sumula.Core.Tests` | Testes (xUnit). |

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
| `GET /classificacao?recorte=geral\|primeiroTurno\|segundoTurno&mando=todos\|casa\|fora&tempo=jogoTodo\|primeiroTempo\|segundoTempo` | Tabela com pontos, jogos, V/E/D, gols, saldo, aproveitamento e últimos 5 resultados. `tempo` monta a tabela "se o jogo acabasse no intervalo" ou "se só valesse o 2º tempo". |
| `GET /tempos` | 1º x 2º tempo de cada time: gols por tempo, viradas, pontos ganhos/perdidos depois do intervalo e a matriz "intervalo → final". Com dados detalhados, também os gols por faixa de 15 minutos (veja abaixo). |
| `GET /times` | Times da competição. |
| `GET /times/{id}` | Resumo do time: geral, casa, fora, 1º e 2º turno, evolução por rodada, últimas e próximas partidas. |
| `GET /confronto?timeA={id}&timeB={id}` | Confronto direto na temporada. |
| `GET /partidas?rodada={n}&timeId={id}` | Partidas (filtros opcionais). |
| `GET /evolucao` | Posição e pontos de todos os times ao fim de cada rodada. |
| `GET /palpites?rodada={n}&timeId={id}` | Palpite de cada jogo que falta: chances de vitória do mandante, empate e vitória do visitante, gols esperados e placar mais provável (filtros opcionais). |
| `GET /matematica` | O que já está decidido: melhor e pior posição possível de cada time e quantos pontos garantem terminar entre os k primeiros (`pontosParaGarantir[k-1]`). |
| `GET /estatisticas` | Perfil de gols de cada time (jogos sem sofrer gol, sem marcar, mais de 2,5 gols, ambos marcam), sequências atuais e maiores da temporada, e o resumo da liga. |
| `GET /probabilidades` | Chance de cada time terminar em cada posição, pontos e posição esperados (veja abaixo). |
| `GET /artilharia` | Artilheiros (gols, assistências e pênaltis). |
| `GET /health` | Verificação de saúde (fora do prefixo `/api`). |

As respostas ficam 5 minutos em cache, já que os dados só mudam quando o coletor roda.

### Critérios de desempate

Pontos, vitórias, saldo de gols, gols pró e confronto direto (só quando o empate é entre dois clubes), como no
regulamento do Brasileirão. Cartões vermelhos e amarelos não estão disponíveis no plano gratuito, então o último
critério é o nome do time.

### Probabilidades

`SimuladorCampeonato` joga os jogos restantes 10.000 vezes (método de Monte Carlo) e conta em que posição cada
time termina. O placar de cada jogo é sorteado por uma distribuição de Poisson, com a média de gols calculada assim:

- **Força de ataque** = gols marcados por jogo ÷ média da liga; **força de defesa** = gols sofridos por jogo ÷ média
  da liga. As duas são puxadas para a média com um peso de 5 jogos, para o começo do campeonato não exagerar.
- **Gols esperados do mandante** = média de gols dos mandantes na liga × ataque do mandante × defesa do visitante
  (e o equivalente para o visitante). Assim a vantagem de jogar em casa entra pelos próprios dados.

O mesmo modelo de gols (`ModeloGols`) alimenta o palpite de cada jogo em `/palpites`: em vez de sortear, soma a
probabilidade de todos os placares de 0 × 0 a 10 × 10.

### Matemática

`/matematica` é conservador de propósito: considera que cada adversário pode vencer todos os jogos que faltam
(mesmo quando dois deles ainda se enfrentam) e que empate em pontos pode ser decidido contra o time. Assim, nunca
diz "garantido" antes da hora; pode dizer uma rodada depois de uma conta que considerasse os confrontos entre eles.

A resposta traz, para cada time, a chance de terminar em cada posição (`posicoes[0]` é o 1º lugar). O site soma
essas chances pelas faixas da tabela (título, Libertadores, rebaixamento...), então as faixas ficam definidas em um
lugar só, no front.

### 1º x 2º tempo e gols por faixa de minuto

O plano gratuito do football-data.org traz o placar do intervalo, e é dele que saem as estatísticas por tempo.
O minuto de cada gol só vem no plano **Free + Deep Data** (€29/mês). Para usá-lo, defina
`FootballData__DadosDetalhados=true` no coletor. No GitHub Actions basta criar a *variável* de repositório
`FOOTBALL_DATA_DETALHADO` com o valor `true` (em *Settings → Secrets and variables → Actions → Variables*). O coletor
passa a pedir os gols (cabeçalho `X-Unfold-Goals`) e grava cada um na tabela `gols`. A partir daí, `/tempos` responde
`temFaixas: true` e traz os gols marcados e sofridos em cada faixa: 1–15, 16–30, 31–45+, 46–60, 61–75 e 76–90+.

O time que marcou é deduzido do placar logo depois de cada gol, então gol contra conta para o time certo.

> A leitura dos gols foi testada com dados no formato da documentação do football-data.org, não com a API real
> (que exige o plano pago). Na primeira coleta com o plano, confira se os totais de `/tempos` batem com a tabela.

## Publicação

Tudo gratuito, a não ser o domínio. O passo a passo completo (Cloudflare, Neon, domínio) está no README do
repositório [sumula](https://github.com/mariaclara7/sumula#publicação). Resumo do que este repositório precisa:

- **Secrets** (*Settings → Secrets and variables → Actions*): `FOOTBALL_DATA_TOKEN`, `DATABASE_URL` (Neon),
  `CLOUDFLARE_API_TOKEN` e `CLOUDFLARE_ACCOUNT_ID`.
- O workflow **Coletor** roda a cada 3 horas (ou na mão, na aba *Actions*): coleta, gera os arquivos e publica o
  Worker `sumula-dados`.

### Nome dos arquivos

`api/{competicao}/{temporada}{caminho}` seguido de `__{chave}-{valor}` para cada parâmetro, em ordem alfabética da
chave, e `.json`. Ex.: `/classificacao?recorte=geral&mando=todos&tempo=jogoTodo` vira
`api/BSA/2026/classificacao__mando-todos__recorte-geral__tempo-jogoTodo.json`. A mesma regra está no site
(`src/api/rotas.ts`); os testes dos dois lados usam os mesmos exemplos.

### Alternativa: API ao vivo

O `Dockerfile` continua aqui para rodar a `Sumula.Api` como servidor (Render, Cloudflare Containers etc.), se um
dia for preciso algo que não dá para pré-calcular.

> O GitHub pausa workflows agendados em repositórios sem commits há 60 dias. Se o coletor parar, reative-o na
> aba *Actions*.
