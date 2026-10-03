# Ferramenta temporária: lista e baixa fotos candidatas (Wikimedia Commons) dos 3 primeiros artilheiros,
# priorizando as que mencionam o clube atual, para escolher uma com o uniforme certo.
import json, os, re, sys, time, urllib.parse, urllib.request

UA = "BraSumula/1.0 (https://brasumula.com.br; https://github.com/mariaclara7/api-sumula)"
WD = "https://www.wikidata.org/w/api.php"
CM = "https://commons.wikimedia.org/w/api.php"
EXTRAS = {"Paranaense": ["Athletico"], "Mineiro": ["Atlético Mineiro", "Atletico Mineiro", "Atlético-MG"]}

def get(url, params=None, headers=None, raw=False):
    if params: url += "?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers={"User-Agent": UA, **(headers or {})})
    for tentativa in range(3):
        try:
            with urllib.request.urlopen(req, timeout=60) as r:
                dados = r.read()
                return dados if raw else json.loads(dados)
        except Exception as e:
            print("erro", url[:120], e); time.sleep(3)
    raise SystemExit(1)

def api(base, **p):
    p.update(format="json", formatversion="2"); time.sleep(0.3)
    return get(base, p)

fd = get("https://api.football-data.org/v4/competitions/BSA/scorers", {"limit": 10}, {"X-Auth-Token": os.environ["FD_TOKEN"]})
fotos = json.load(open("dados/fotos-jogadores.json"))["jogadores"]
saida = []
for s in fd["scorers"]:
    jogador, time_ = s["player"], s["team"]
    registro = fotos.get(str(jogador["id"])) or {}
    if registro.get("manual"):
        print(f"== {jogador['name']}: já escolhido à mão"); continue
    kws = [time_.get("shortName") or "", time_.get("name") or ""] + EXTRAS.get(time_.get("shortName"), [])
    kws = [k for k in kws if k]
    qid = (fotos.get(str(jogador["id"])) or {}).get("wikidata")
    completo = f"{jogador.get('firstName') or ''} {jogador.get('lastName') or ''}".strip()
    print(f"== {jogador['name']} / {completo} ({time_.get('shortName')}) nasc={jogador.get('dateOfBirth')} {qid}")
    if not qid and jogador.get("dateOfBirth"):
        # Busca mais ampla no Wikidata (50 resultados por termo), confirmando pela data de nascimento.
        ids = set()
        for termo in {jogador["name"], completo, f"{jogador['name']} futebolista"}:
            if termo:
                ids.update(r["title"] for r in api(WD, action="query", list="search", srnamespace=0, srlimit=50,
                                                    srsearch=f"{termo} haswbstatement:P106=Q937857")["query"]["search"])
        ids = sorted(ids)
        for i in range(0, len(ids), 50):
            ents = api(WD, action="wbgetentities", ids="|".join(ids[i:i+50]), props="claims")["entities"]
            for eid, e in ents.items():
                nasc = e.get("claims", {}).get("P569", [{}])[0].get("mainsnak", {}).get("datavalue", {}).get("value", {}).get("time", "")
                if nasc[1:11] == jogador["dateOfBirth"]:
                    qid = eid; print("  achado no Wikidata:", qid)
    titulos = set()
    if qid:
        ent = api(WD, action="wbgetentities", ids=qid, props="claims")["entities"][qid]
        cat = ent["claims"].get("P373", [{}])[0].get("mainsnak", {}).get("datavalue", {}).get("value")
        if cat:
            membros = api(CM, action="query", list="categorymembers", cmtitle=f"Category:{cat}", cmtype="file|subcat", cmlimit=500)["query"]["categorymembers"]
            for m in membros:
                if m["ns"] == 6: titulos.add(m["title"])
                elif any(k.lower() in m["title"].lower() for k in kws) or re.search(r"20(2[4-6])", m["title"]):
                    for f in api(CM, action="query", list="categorymembers", cmtitle=m["title"], cmtype="file", cmlimit=500)["query"]["categorymembers"]:
                        titulos.add(f["title"])
    nomes = [n for n in {jogador["name"], completo} if n and len(n) > 5]  # "Pedro" sozinho traz qualquer coisa
    for n in nomes:
        for k in kws[:2] + [""]:
            for r in api(CM, action="query", list="search", srnamespace=6, srlimit=40, srsearch=f'"{n}" {k}'.strip())["query"]["search"]:
                titulos.add(r["title"])
    if qid:
        # Categoria do Commons pelo link do item (quando não há P373).
        links = api(WD, action="wbgetentities", ids=qid, props="sitelinks", sitefilter="commonswiki")["entities"][qid].get("sitelinks", {})
        cat = links.get("commonswiki", {}).get("title")
        print("  commonswiki:", cat)
        if cat and cat.startswith("Category:"):
            for m in api(CM, action="query", list="categorymembers", cmtitle=cat, cmtype="file|subcat", cmlimit=500)["query"]["categorymembers"]:
                if m["ns"] == 6: titulos.add(m["title"])
                else:
                    for f in api(CM, action="query", list="categorymembers", cmtitle=m["title"], cmtype="file", cmlimit=500)["query"]["categorymembers"]:
                        titulos.add(f["title"])
    if len(titulos) < 5:
        # Nomes curtos e comuns ("Pedro"): procura categorias de jogador e fotos com o nome no título junto do clube.
        for r in api(CM, action="query", list="search", srnamespace=14, srlimit=20, srsearch=f'{jogador["name"]} footballer born {(jogador.get("dateOfBirth") or "")[:4]}')["query"]["search"]:
            print("  categoria candidata:", r["title"])
            if (jogador.get("dateOfBirth") or "")[:4] in r["title"] and jogador["name"].lower() in r["title"].lower():
                for f in api(CM, action="query", list="categorymembers", cmtitle=r["title"], cmtype="file", cmlimit=500)["query"]["categorymembers"]:
                    titulos.add(f["title"])
        for k in kws[:1]:
            for r in api(CM, action="query", list="search", srnamespace=6, srlimit=50, srsearch=f'intitle:"{jogador["name"]}" {k}')["query"]["search"]:
                titulos.add(r["title"])
    titulos = sorted(titulos)
    infos = []
    for i in range(0, len(titulos), 40):
        r = api(CM, action="query", titles="|".join(titulos[i:i+40]), prop="imageinfo|categories", clshow="!hidden", cllimit=500,
                iiprop="url|extmetadata|size|timestamp", iiurlwidth=360)
        for p in r["query"]["pages"]:
            if "imageinfo" not in p: continue
            ii = p["imageinfo"][0]; meta = ii.get("extmetadata", {})
            cats = " ".join(c["title"] for c in p.get("categories", []))
            desc = re.sub("<[^>]+>", " ", meta.get("ImageDescription", {}).get("value", ""))
            data = meta.get("DateTimeOriginal", {}).get("value", "") or ii.get("timestamp", "")
            texto = f"{p['title']} {cats} {desc}".lower()
            nota = sum(3 for k in kws if k.lower() in cats.lower()) + sum(2 for k in kws if k.lower() in p["title"].lower()) + sum(1 for k in kws if k.lower() in desc.lower())
            ano = re.search(r"(20\d\d)", data)
            infos.append({"titulo": p["title"], "nota": nota, "ano": int(ano.group(1)) if ano else 0, "data": re.sub("<[^>]+>", "", data)[:40],
                          "thumb": ii.get("thumburl"), "pagina": ii.get("descriptionurl"),
                          "autor": re.sub(r"\s+", " ", re.sub("<[^>]+>", " ", meta.get("Artist", {}).get("value", ""))).strip()[:80],
                          "licenca": meta.get("LicenseShortName", {}).get("value", ""), "categorias": cats[:400], "descricao": desc.strip()[:300],
                          "largura": ii.get("width"), "altura": ii.get("height")})
    infos.sort(key=lambda x: (x["nota"], x["ano"]), reverse=True)
    escolhidos = [x for x in infos if x["thumb"]][:10]
    pasta = f"candidatos/{jogador['id']}"
    os.makedirs(pasta, exist_ok=True)
    atual = (registro.get("foto") or {}).get("url")
    if atual:
        open(f"{pasta}/atual.jpg", "wb").write(get(atual, raw=True))
    for n, x in enumerate(escolhidos):
        x["arquivo"] = f"{pasta}/{n:02d}.jpg"
        open(x["arquivo"], "wb").write(get(x["thumb"], raw=True)); time.sleep(0.3)
        print(f"  {n:02d} nota={x['nota']} ano={x['ano']} {x['titulo']}")
    saida.append({"id": jogador["id"], "nome": jogador["name"], "time": time_.get("shortName"), "gols": s.get("goals"), "wikidata": qid,
                  "total": len(infos), "atual": registro.get("foto"), "candidatos": escolhidos})
json.dump(saida, open("candidatos/indice.json", "w"), ensure_ascii=False, indent=2)
